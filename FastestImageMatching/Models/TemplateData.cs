using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace FastestImageMatching.Models
{
    /// <summary>
    /// Stores preprocessed template data for efficient matching
    /// </summary>
    public class TemplateData : IDisposable
    {
        /// <summary>
        /// Image pyramid of template
        /// </summary>
        public List<Mat> PyramidLevels { get; set; } = new();

        /// <summary>
        /// Mean values at each pyramid level
        /// </summary>
        public List<Scalar> MeanValues { get; set; } = new();

        /// <summary>
        /// Norm values at each pyramid level
        /// </summary>
        public List<double> NormValues { get; set; } = new();

        /// <summary>
        /// Inverse area values for optimization
        /// </summary>
        public List<double> InverseAreaValues { get; set; } = new();

        /// <summary>
        /// Whether template has been learned/preprocessed
        /// </summary>
        public bool IsLearned { get; set; }

        /// <summary>
        /// Border color for template
        /// </summary>
        public int BorderColor { get; set; }

        /// <summary>
        /// Clear all template data
        /// </summary>
        public void Clear()
        {
            foreach (var mat in PyramidLevels)
            {
                mat?.Dispose();
            }
            PyramidLevels.Clear();
            MeanValues.Clear();
            NormValues.Clear();
            InverseAreaValues.Clear();
            IsLearned = false;
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TemplateData()
        {
            Clear();
        }
    }
}
