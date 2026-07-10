using OpenCvSharp;
using System;

namespace FastestImageMatching.Core
{
    /// <summary>
    /// Normalized Cross Correlation computation utilities
    /// </summary>
    public static class NormalizedCrossCorrelation
    {
        /// <summary>
        /// Compute NCC between template and source image at given position
        /// </summary>
        public static double ComputeNCC(Mat source, Mat template, int x, int y)
        {
            if (x + template.Width > source.Width || y + template.Height > source.Height)
                return -1.0;

            // Extract ROI
            Mat roi = new Mat(source, new Rect(x, y, template.Width, template.Height));

            // Compute means
            Cv2.MeanStdDev(roi, out Scalar sourceMean, out Scalar _);
            Cv2.MeanStdDev(template, out Scalar templateMean, out Scalar _);

            // Normalize
            Mat sourceNorm = new Mat();
            Mat templateNorm = new Mat();
            Cv2.Subtract(roi, sourceMean, sourceNorm);
            Cv2.Subtract(template, templateMean, templateNorm);

            // Compute correlation
            double numerator = sourceNorm.Dot(templateNorm);
            double denominator = Cv2.Norm(sourceNorm) * Cv2.Norm(templateNorm);

            roi?.Dispose();
            sourceNorm?.Dispose();
            templateNorm?.Dispose();

            if (denominator < 1e-6)
                return 0.0;

            double ncc = numerator / denominator;
            return Math.Clamp(ncc, -1.0, 1.0);
        }

        /// <summary>
        /// Compute full NCC correlation map
        /// </summary>
        public static Mat ComputeNCCMap(Mat source, Mat template)
        {
            int sourceRows = source.Rows;
            int sourceCols = source.Cols;
            int templateRows = template.Rows;
            int templateCols = template.Cols;

            Mat result = new Mat(sourceRows - templateRows + 1, sourceCols - templateCols + 1, MatType.CV_64F);

            for (int y = 0; y < result.Rows; y++)
            {
                for (int x = 0; x < result.Cols; x++)
                {
                    double ncc = ComputeNCC(source, template, x, y);
                    result.Set<double>(y, x, ncc);
                }
            }

            return result;
        }
    }
}
