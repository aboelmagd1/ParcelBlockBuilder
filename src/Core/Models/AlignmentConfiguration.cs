using System;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Configuration defining spatial positioning, orientation vector, and reference baseline.
    /// </summary>
    public class AlignmentConfiguration
    {
        public AlignmentMethod Method { get; set; } = AlignmentMethod.TwoPoints;

        /// <summary>
        /// Which side serves as the reference baseline during orientation and offsetting.
        /// </summary>
        public ParcelSide ReferenceSide { get; set; } = ParcelSide.SideA;

        // Baseline Start Point (Origin)
        public double OriginX { get; set; } = 0.0;
        public double OriginY { get; set; } = 0.0;

        // Baseline End Point (Vector direction)
        public double TargetX { get; set; } = 100.0;
        public double TargetY { get; set; } = 0.0;

        /// <summary>
        /// Explicit azimuth angle in degrees (clockwise from North, or counter-clockwise from X-axis depending on projection).
        /// </summary>
        public double AzimuthAngleDegrees { get; set; } = 90.0;

        /// <summary>
        /// Name or URI of the selected alignment feature/layer if aligned by existing line.
        /// </summary>
        public string SourceLineFeatureId { get; set; } = string.Empty;

        /// <summary>
        /// Spatial Reference Well-Known ID (WKID) or WKT string.
        /// </summary>
        public int SpatialReferenceWkid { get; set; } = 3857; // Default Web Mercator / WGS84

        /// <summary>
        /// Which anchor reference point on the block is placed at the origin coordinates.
        /// </summary>
        public BlockAnchorPoint AnchorPoint { get; set; } = BlockAnchorPoint.SideAStart;

        public AlignmentConfiguration Clone()
        {
            return new AlignmentConfiguration
            {
                Method = this.Method,
                ReferenceSide = this.ReferenceSide,
                AnchorPoint = this.AnchorPoint,
                OriginX = this.OriginX,
                OriginY = this.OriginY,
                TargetX = this.TargetX,
                TargetY = this.TargetY,
                AzimuthAngleDegrees = this.AzimuthAngleDegrees,
                SourceLineFeatureId = this.SourceLineFeatureId,
                SpatialReferenceWkid = this.SpatialReferenceWkid
            };
        }
    }
}
