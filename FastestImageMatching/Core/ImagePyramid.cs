using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace FastestImageMatching.Core
{
    /// <summary>
    /// Builds and manages image pyramids for multi-scale processing
    /// </summary>
    public static class ImagePyramid
    {
        /// <summary>
        /// Build a downsampled image pyramid
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="minReducedArea">Minimum area at coarsest level</param>
        /// <param name="scaleRatio">Scale ratio between levels (e.g., 1.25)</param>
        /// <returns>List of pyramid levels from fine to coarse</returns>
        public static List<Mat> BuildPyramid(Mat image, int minReducedArea = 256, double scaleRatio = 1.25)
        {
            var pyramid = new List<Mat>();
            
            if (image == null || image.Empty())
                throw new ArgumentException("Input image cannot be null or empty");

            // Convert to grayscale if needed for consistency
            Mat grayImage = new Mat();
            if (image.Channels() == 3)
            {
                Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);
            }
            else if (image.Channels() == 1)
            {
                grayImage = image.Clone();
            }
            else
            {
                throw new ArgumentException("Image must be grayscale or BGR color");
            }

            // Convert to float for better precision
            Mat floatImage = new Mat();
            grayImage.ConvertTo(floatImage, MatType.CV_32F);
            grayImage.Dispose();

            // Start with original image
            pyramid.Add(floatImage.Clone());

            Mat current = floatImage.Clone();
            int level = 0;
            int currentRows = current.Rows;
            int currentCols = current.Cols;

            // Build coarser levels until minimum area reached
            while (currentRows * currentCols > minReducedArea && level < 20)
            {
                int newWidth = (int)(currentCols / scaleRatio);
                int newHeight = (int)(currentRows / scaleRatio);

                // Ensure at least 1x1
                newWidth = Math.Max(1, newWidth);
                newHeight = Math.Max(1, newHeight);

                if (newWidth >= currentCols && newHeight >= currentRows)
                    break; // No more scaling needed

                Mat downsampled = new Mat();
                try
                {
                    Cv2.Resize(current, downsampled, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Area);
                    pyramid.Add(downsampled.Clone());
                    downsampled.Dispose();
                }
                catch (Exception ex)
                {
                    downsampled?.Dispose();
                    throw new Exception($"Failed to create pyramid level {level}: {ex.Message}", ex);
                }

                current?.Dispose();
                current = new Mat();
                if (pyramid.Count > 0)
                {
                    current = pyramid[pyramid.Count - 1].Clone();
                    currentRows = current.Rows;
                    currentCols = current.Cols;
                }
                level++;
            }

            current?.Dispose();
            floatImage?.Dispose();
            return pyramid;
        }

        /// <summary>
        /// Release all pyramid levels
        /// </summary>
        public static void ReleasePyramid(List<Mat> pyramid)
        {
            if (pyramid != null)
            {
                foreach (var mat in pyramid)
                {
                    mat?.Dispose();
                }
                pyramid.Clear();
            }
        }

        /// <summary>
        /// Get pyramid level statistics
        /// </summary>
        public static void PrintPyramidInfo(List<Mat> pyramid)
        {
            Console.WriteLine($"Pyramid Levels: {pyramid.Count}");
            for (int i = 0; i < pyramid.Count; i++)
            {
                if (pyramid[i] != null && !pyramid[i].Empty())
                {
                    Console.WriteLine($"  Level {i}: {pyramid[i].Width}x{pyramid[i].Height}");
                }
            }
        }
    }
}
