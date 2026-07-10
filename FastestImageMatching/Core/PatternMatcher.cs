using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using FastestImageMatching.Models;

namespace FastestImageMatching.Core
{
    /// <summary>
    /// Main pattern matcher using NCC-based algorithm
    /// </summary>
    public class PatternMatcher : IDisposable
    {
        private TemplateData templateData = new();
        private Mat? templateImage;

        /// <summary>
        /// Learn/preprocess the template image
        /// </summary>
        public void LearnTemplate(Mat template)
        {
            if (template == null || template.Empty())
                throw new ArgumentException("Template cannot be null or empty");

            try
            {
                templateData.Clear();
                
                // Convert to grayscale and float
                Mat grayTemplate = new Mat();
                if (template.Channels() == 3)
                {
                    Cv2.CvtColor(template, grayTemplate, ColorConversionCodes.BGR2GRAY);
                }
                else if (template.Channels() == 1)
                {
                    grayTemplate = template.Clone();
                }
                else
                {
                    throw new ArgumentException("Template must be grayscale or BGR color");
                }

                Mat floatTemplate = new Mat();
                grayTemplate.ConvertTo(floatTemplate, MatType.CV_32F);
                grayTemplate.Dispose();

                templateImage = floatTemplate.Clone();

                // Build pyramid
                templateData.PyramidLevels = ImagePyramid.BuildPyramid(floatTemplate);
                floatTemplate.Dispose();

                templateData.MeanValues = new List<Scalar>();
                templateData.NormValues = new List<double>();
                templateData.InverseAreaValues = new List<double>();

                // Precompute statistics for each pyramid level
                foreach (var level in templateData.PyramidLevels)
                {
                    if (level == null || level.Empty())
                    {
                        templateData.MeanValues.Add(new Scalar(0));
                        templateData.NormValues.Add(0);
                        templateData.InverseAreaValues.Add(0);
                        continue;
                    }

                    try
                    {
                        Cv2.MeanStdDev(level, out Scalar mean, out Scalar stdDev);
                        templateData.MeanValues.Add(mean);
                        templateData.NormValues.Add(stdDev.Val0);
                        templateData.InverseAreaValues.Add(1.0 / Math.Max(1, level.Width * level.Height));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error computing statistics for pyramid level: {ex.Message}");
                        templateData.MeanValues.Add(new Scalar(0));
                        templateData.NormValues.Add(0);
                        templateData.InverseAreaValues.Add(0);
                    }
                }

                templateData.IsLearned = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to learn template: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Find pattern matches in source image
        /// </summary>
        public List<MatchResult> Match(Mat sourceImage, MatchConfig? config = null)
        {
            if (!templateData.IsLearned)
                throw new InvalidOperationException("Template must be learned first using LearnTemplate()");

            if (sourceImage == null || sourceImage.Empty())
                throw new ArgumentException("Source image cannot be null or empty");

            if (templateImage == null)
                throw new InvalidOperationException("Template image is null");

            config ??= new MatchConfig();

            var results = new List<MatchResult>();

            try
            {
                // Convert source image to same format as template
                Mat graySource = new Mat();
                if (sourceImage.Channels() == 3)
                {
                    Cv2.CvtColor(sourceImage, graySource, ColorConversionCodes.BGR2GRAY);
                }
                else if (sourceImage.Channels() == 1)
                {
                    graySource = sourceImage.Clone();
                }
                else
                {
                    throw new ArgumentException("Source image must be grayscale or BGR color");
                }

                Mat floatSource = new Mat();
                graySource.ConvertTo(floatSource, MatType.CV_32F);
                graySource.Dispose();

                Mat result = new Mat();
                Cv2.MatchTemplate(floatSource, templateImage, result, TemplateMatchModes.CCoeffNormed);
                floatSource.Dispose();

                if (result.Empty())
                {
                    return results;
                }

                // Find top matches
                for (int i = 0; i < config.TargetNumber; i++)
                {
                    double minVal, maxVal;
                    Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                    if (maxVal < config.ScoreThreshold)
                        break;

                    var matchResult = new MatchResult(
                        new Point2d(maxLoc.X + templateImage.Width / 2.0, maxLoc.Y + templateImage.Height / 2.0),
                        maxVal,
                        0.0 // Angle would be computed if rotation search is enabled
                    )
                    {
                        BoundingBox = new Rect(maxLoc.X, maxLoc.Y, templateImage.Width, templateImage.Height)
                    };

                    results.Add(matchResult);

                    // Suppress area around found match
                    Cv2.Circle(result, maxLoc, (int)(Math.Max(templateImage.Width, templateImage.Height) * (1 - config.MaxOverlapRatio)), new Scalar(0), -1);
                }

                result?.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to match pattern: {ex.Message}", ex);
            }

            return results.OrderByDescending(r => r.Score).ToList();
        }

        /// <summary>
        /// Find pattern with rotation invariance
        /// </summary>
        public List<MatchResult> MatchWithRotation(Mat sourceImage, MatchConfig? config = null)
        {
            if (!templateData.IsLearned)
                throw new InvalidOperationException("Template must be learned first");

            if (templateImage == null)
                throw new InvalidOperationException("Template image is null");

            config ??= new MatchConfig();
            var allResults = new List<MatchResult>();

            try
            {
                // Search through rotation range
                double angleStep = 5.0; // 5 degree step for faster processing
                double startAngle = -config.ToleranceAngle / 2.0;
                double endAngle = config.ToleranceAngle / 2.0;

                for (double angle = startAngle; angle <= endAngle; angle += angleStep)
                {
                    // Rotate template
                    Mat rotatedTemplate = RotateImage(templateImage, angle);
                    
                    if (rotatedTemplate == null || rotatedTemplate.Empty())
                        continue;

                    // Create temporary matcher for rotated template
                    var tempMatcher = new PatternMatcher();
                    
                    try
                    {
                        tempMatcher.LearnTemplate(rotatedTemplate);
                        var results = tempMatcher.Match(sourceImage, config);
                        
                        // Update angle for each result
                        foreach (var result in results)
                        {
                            result.Angle = angle;
                            allResults.Add(result);
                        }
                    }
                    finally
                    {
                        rotatedTemplate?.Dispose();
                        tempMatcher?.Dispose();
                    }
                }

                // Filter and deduplicate results
                return FilterAndDedupResults(allResults, config);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to match pattern with rotation: {ex.Message}", ex);
            }
        }

        private Mat RotateImage(Mat image, double angle)
        {
            if (image == null || image.Empty())
                throw new ArgumentException("Image cannot be null or empty");

            try
            {
                Point2f center = new Point2f(image.Cols / 2.0f, image.Rows / 2.0f);
                Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
                Mat rotated = new Mat();
                Cv2.WarpAffine(image, rotated, rotationMatrix, new OpenCvSharp.Size(image.Width, image.Height));
                rotationMatrix?.Dispose();
                return rotated;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to rotate image: {ex.Message}", ex);
            }
        }

        private List<MatchResult> FilterAndDedupResults(List<MatchResult> results, MatchConfig config)
        {
            if (templateImage == null)
                return new List<MatchResult>();

            // Sort by score
            var sorted = results.OrderByDescending(r => r.Score).ToList();
            var filtered = new List<MatchResult>();

            foreach (var result in sorted)
            {
                // Check if too similar to already found result
                bool isDuplicate = filtered.Any(f => 
                    Math.Sqrt(Math.Pow(f.Location.X - result.Location.X, 2) + 
                             Math.Pow(f.Location.Y - result.Location.Y, 2)) < 
                    Math.Max(templateImage.Width, templateImage.Height) * 0.5);

                if (!isDuplicate && result.Score >= config.ScoreThreshold)
                {
                    filtered.Add(result);
                    if (filtered.Count >= config.TargetNumber)
                        break;
                }
            }

            return filtered;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                templateData?.Dispose();
                templateImage?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during dispose: {ex.Message}");
            }
            GC.SuppressFinalize(this);
        }
    }
}
