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
        private Mat templateImage;

        /// <summary>
        /// Learn/preprocess the template image
        /// </summary>
        public void LearnTemplate(Mat template)
        {
            if (template == null || template.Empty())
                throw new ArgumentException("Template cannot be null or empty");

            templateData.Clear();
            templateImage = template.Clone();

            // Build pyramid
            templateData.PyramidLevels = ImagePyramid.BuildPyramid(template);
            templateData.MeanValues = new List<Scalar>();
            templateData.NormValues = new List<double>();
            templateData.InverseAreaValues = new List<double>();

            // Precompute statistics for each pyramid level
            foreach (var level in templateData.PyramidLevels)
            {
                Cv2.MeanStdDev(level, out Scalar mean, out Scalar stdDev);
                templateData.MeanValues.Add(mean);
                
                // Compute norm (standard deviation as proxy)
                templateData.NormValues.Add(stdDev.Val0);
                
                // Inverse area for normalization
                templateData.InverseAreaValues.Add(1.0 / (level.Width * level.Height));
            }

            templateData.IsLearned = true;
        }

        /// <summary>
        /// Find pattern matches in source image
        /// </summary>
        public List<MatchResult> Match(Mat sourceImage, MatchConfig config = null)
        {
            if (!templateData.IsLearned)
                throw new InvalidOperationException("Template must be learned first using LearnTemplate()");

            if (sourceImage == null || sourceImage.Empty())
                throw new ArgumentException("Source image cannot be null or empty");

            config ??= new MatchConfig();

            var results = new List<MatchResult>();

            // Start matching from coarsest pyramid level
            // For now, use simple template matching at original scale
            // This is a simplified implementation - full pyramid strategy would be more complex

            Mat result = new Mat();
            Cv2.MatchTemplate(sourceImage, templateImage, result, TemplateMatchModes.CCoeffNormed);

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
            return results.OrderByDescending(r => r.Score).ToList();
        }

        /// <summary>
        /// Find pattern with rotation invariance
        /// </summary>
        public List<MatchResult> MatchWithRotation(Mat sourceImage, MatchConfig config = null)
        {
            if (!templateData.IsLearned)
                throw new InvalidOperationException("Template must be learned first");

            config ??= new MatchConfig();
            var allResults = new List<MatchResult>();

            // Search through rotation range
            double angleStep = 1.0; // 1 degree step
            double startAngle = -config.ToleranceAngle / 2.0;
            double endAngle = config.ToleranceAngle / 2.0;

            for (double angle = startAngle; angle <= endAngle; angle += angleStep)
            {
                // Rotate template
                Mat rotatedTemplate = RotateImage(templateImage, angle);
                
                // Create temporary matcher for rotated template
                var tempMatcher = new PatternMatcher();
                tempMatcher.LearnTemplate(rotatedTemplate);
                
                var results = tempMatcher.Match(sourceImage, config);
                
                // Update angle for each result
                foreach (var result in results)
                {
                    result.Angle = angle;
                    allResults.Add(result);
                }
                
                rotatedTemplate?.Dispose();
            }

            // Filter and deduplicate results
            return FilterAndDedupResults(allResults, config);
        }

        private Mat RotateImage(Mat image, double angle)
        {
            Point2f center = new Point2f(image.Cols / 2.0f, image.Rows / 2.0f);
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            Mat rotated = new Mat();
            Cv2.WarpAffine(image, rotated, rotationMatrix, new Size(image.Width, image.Height));
            rotationMatrix?.Dispose();
            return rotated;
        }

        private List<MatchResult> FilterAndDedupResults(List<MatchResult> results, MatchConfig config)
        {
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

        public void Dispose()
        {
            templateData?.Dispose();
            templateImage?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
