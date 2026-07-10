namespace FastestImageMatching.Models
{
    /// <summary>
    /// Configuration parameters for pattern matching
    /// </summary>
    public class MatchConfig
    {
        /// <summary>
        /// Maximum number of patterns to find
        /// </summary>
        public int TargetNumber { get; set; } = 5;

        /// <summary>
        /// Maximum overlap ratio between detected instances (0-1)
        /// </summary>
        public double MaxOverlapRatio { get; set; } = 0.8;

        /// <summary>
        /// Minimum match score threshold (0-1)
        /// </summary>
        public double ScoreThreshold { get; set; } = 0.8;

        /// <summary>
        /// Rotation tolerance in degrees (0-360)
        /// For example: 180 = search from -180° to +180°
        /// </summary>
        public double ToleranceAngle { get; set; } = 180.0;

        /// <summary>
        /// Minimum reduced area at pyramid top level
        /// </summary>
        public int MinReducedArea { get; set; } = 256;

        /// <summary>
        /// Scale ratio for image pyramid (typically 1.2-1.5)
        /// </summary>
        public double ScaleRatio { get; set; } = 1.25;

        /// <summary>
        /// Enable SIMD optimization if available
        /// </summary>
        public bool EnableSIMD { get; set; } = true;
    }
}
