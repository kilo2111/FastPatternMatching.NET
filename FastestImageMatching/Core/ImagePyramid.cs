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

            // Start with original image
            pyramid.Add(image.Clone());

            Mat current = image.Clone();
            int level = 0;

            // Build coarser levels until minimum area reached
            while (current.Rows * current.Cols > minReducedArea && level < 20)
            {
                int newWidth = (int)(current.Cols / scaleRatio);
                int newHeight = (int)(current.Rows / scaleRatio);

                // Ensure at least 1x1
                newWidth = Math.Max(1, newWidth);
                newHeight = Math.Max(1, newHeight);

                if (newWidth >= current.Cols && newHeight >= current.Rows)
                    break; // No more scaling needed

                Mat downsampled = new Mat();
                Cv2.Resize(current, downsampled, new Size(newWidth, newHeight), 0, 0, InterpolationFlags.Area);

                pyramid.Add(downsampled);
                current = downsampled;
                level++;
            }

            current?.Dispose();
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
                Console.WriteLine($"  Level {i}: {pyramid[i].Width}x{pyramid[i].Height}");
            }
        }
    }
}
