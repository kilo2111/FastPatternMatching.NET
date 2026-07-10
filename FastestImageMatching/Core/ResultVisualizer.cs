using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using FastestImageMatching.Models;

namespace FastestImageMatching.Core
{
    /// <summary>
    /// Visualization utilities for pattern matching results
    /// </summary>
    public static class ResultVisualizer
    {
        private static readonly Scalar[] Colors = new Scalar[]
        {
            new Scalar(0, 255, 0),      // Green
            new Scalar(255, 0, 0),      // Blue
            new Scalar(0, 255, 255),    // Yellow
            new Scalar(255, 0, 255),    // Magenta
            new Scalar(0, 165, 255),    // Orange
            new Scalar(255, 255, 0),    // Cyan
            new Scalar(255, 128, 0),    // Azure
            new Scalar(255, 0, 127),    // Spring Green
        };

        /// <summary>
        /// Draw match results on image
        /// </summary>
        public static Mat DrawMatches(Mat sourceImage, List<MatchResult>? results, Mat? template)
        {
            if (sourceImage == null || sourceImage.Empty())
                return sourceImage.Clone();

            Mat output = new Mat();
            Cv2.CvtColor(sourceImage, output, ColorConversionCodes.GRAY2BGR);

            if (results == null || results.Count == 0)
                return output;

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                Scalar color = Colors[i % Colors.Length];

                try
                {
                    // Draw bounding box
                    Cv2.Rectangle(output, result.BoundingBox, color, 2);

                    // Draw circle at center
                    Cv2.Circle(output, new Point((int)result.Location.X, (int)result.Location.Y), 5, color, -1);

                    // Prepare text with score and angle
                    string text = $"#{i} Score:{result.Score:F3}";
                    if (Math.Abs(result.Angle) > 0.1)
                    {
                        text += $" Ang:{result.Angle:F1}°";
                    }

                    // Get text size for background
                    Cv2.GetTextSize(text, HersheyFonts.HersheyPlain, 1.0, 1, out Size textSize);

                    // Draw text background
                    int x = result.BoundingBox.X;
                    int y = result.BoundingBox.Y - 5;
                    Cv2.Rectangle(output, 
                        new Point(x - 2, y - textSize.Height - 4),
                        new Point(x + textSize.Width + 2, y + 2),
                        color, -1);

                    // Draw text
                    Cv2.PutText(output, text,
                        new Point(x, y - 2),
                        HersheyFonts.HersheyPlain, 1.0,
                        new Scalar(255, 255, 255), 1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing match {i}: {ex.Message}");
                }
            }

            return output;
        }

        /// <summary>
        /// Draw template outline on image
        /// </summary>
        public static Mat DrawTemplate(Mat image, Rect templateBounds)
        {
            Mat output = image.Clone();
            Cv2.Rectangle(output, templateBounds, new Scalar(255, 255, 0), 2);
            return output;
        }
    }
}
