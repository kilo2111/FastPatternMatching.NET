using OpenCvSharp;

namespace FastestImageMatching.Models
{
    /// <summary>
    /// Represents a single pattern match result
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Location of the matched pattern (center point)
        /// </summary>
        public Point2d Location { get; set; }

        /// <summary>
        /// Match score (0-1, where 1 is perfect match)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Rotation angle in degrees (-180 to 180)
        /// </summary>
        public double Angle { get; set; }

        /// <summary>
        /// Bounding box of the matched region
        /// </summary>
        public Rect BoundingBox { get; set; }

        /// <summary>
        /// Rotated rectangle representing the match
        /// </summary>
        public RotatedRect RotatedRect { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MatchResult()
        {
        }

        /// <summary>
        /// Constructor with parameters
        /// </summary>
        public MatchResult(Point2d location, double score, double angle)
        {
            Location = location;
            Score = score;
            Angle = angle;
        }

        /// <summary>
        /// String representation of the match result
        /// </summary>
        public override string ToString()
        {
            return $"Match: Pos=({Location.X:F2}, {Location.Y:F2}), Score={Score:F4}, Angle={Angle:F2}°";
        }
    }
}
