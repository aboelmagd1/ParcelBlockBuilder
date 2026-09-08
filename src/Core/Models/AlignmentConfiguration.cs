using System;
using System.Collections.Generic;
using System.Linq;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Configuration defining spatial positioning (WHERE) and orientation (HOW) on the ArcGIS Pro map.
    /// Strictly separates Base Point placement from rotational orientation.
    /// </summary>
    public class AlignmentConfiguration
    {
        public bool Enabled { get; set; } = true;

        // --- 1. BASE POINT (WHERE) ---
        /// <summary>
        /// Which anchor reference point on the block is selected from the preview.
        /// </summary>
        public BlockAnchorPoint AnchorPoint { get; set; } = BlockAnchorPoint.SideAStart;

        public string BasePointId { get; set; } = "BP-01";
        public string BasePointName { get; set; } = "Side A Start (Front-Left)";

        /// <summary>
        /// Base point coordinates in local block schematic space.
        /// </summary>
        public double SourceBasePointX { get; set; } = 0.0;
        public double SourceBasePointY { get; set; } = 0.0;

        /// <summary>
        /// Target anchor coordinates selected by the user on the ArcGIS Pro map.
        /// </summary>
        public double? TargetMapPointX { get; set; } = null;
        public double? TargetMapPointY { get; set; } = null;

        /// <summary>
        /// Indicates whether the user has selected a valid target coordinate on the map.
        /// </summary>
        public bool IsBasePointPlaced => TargetMapPointX.HasValue && TargetMapPointY.HasValue;

        // --- 2. ORIENTATION (HOW IT IS ORIENTED) ---
        public AlignmentMethod Method { get; set; } = AlignmentMethod.TwoPoints;

        // Method 1: Two Points
        public double? TwoPointStartX { get; set; } = null;
        public double? TwoPointStartY { get; set; } = null;
        public double? TwoPointEndX { get; set; } = null;
        public double? TwoPointEndY { get; set; } = null;

        // Method 2: Map Segment
        public string? SelectedSegmentDescription { get; set; } = null;
        public double? SegmentStartX { get; set; } = null;
        public double? SegmentStartY { get; set; } = null;
        public double? SegmentEndX { get; set; } = null;
        public double? SegmentEndY { get; set; } = null;

        /// <summary>
        /// Azimuth angle in degrees clockwise from North (0° = North, 90° = East, 180° = South, 270° = West).
        /// Default is 90° (aligned along the positive X-axis).
        /// </summary>
        public double AzimuthAngleDegrees { get; set; } = 90.0;

        /// <summary>
        /// Spatial reference WKID of the map.
        /// </summary>
        public int SpatialReferenceWkid { get; set; } = 3857;
        public string SpatialReferenceName { get; set; } = "Projected";

        // Compatibility accessors for legacy callers
        public double OriginX
        {
            get => TargetMapPointX ?? 0.0;
            set => TargetMapPointX = value;
        }

        public double OriginY
        {
            get => TargetMapPointY ?? 0.0;
            set => TargetMapPointY = value;
        }

        public double TargetX
        {
            get => Method == AlignmentMethod.TwoPoints ? (TwoPointEndX ?? (OriginX + 100.0)) : (SegmentEndX ?? (OriginX + 100.0));
            set
            {
                if (Method == AlignmentMethod.TwoPoints) TwoPointEndX = value;
                else SegmentEndX = value;
            }
        }

        public double TargetY
        {
            get => Method == AlignmentMethod.TwoPoints ? (TwoPointEndY ?? OriginY) : (SegmentEndY ?? OriginY);
            set
            {
                if (Method == AlignmentMethod.TwoPoints) TwoPointEndY = value;
                else SegmentEndY = value;
            }
        }

        public string SourceLineFeatureId
        {
            get => SelectedSegmentDescription ?? string.Empty;
            set => SelectedSegmentDescription = value;
        }

        public bool IsValid { get; set; } = false;
        public List<string> ValidationMessages { get; set; } = new List<string>();

        public AlignmentConfiguration Clone()
        {
            return new AlignmentConfiguration
            {
                Enabled = this.Enabled,
                AnchorPoint = this.AnchorPoint,
                BasePointId = this.BasePointId,
                BasePointName = this.BasePointName,
                SourceBasePointX = this.SourceBasePointX,
                SourceBasePointY = this.SourceBasePointY,
                TargetMapPointX = this.TargetMapPointX,
                TargetMapPointY = this.TargetMapPointY,
                Method = this.Method,
                TwoPointStartX = this.TwoPointStartX,
                TwoPointStartY = this.TwoPointStartY,
                TwoPointEndX = this.TwoPointEndX,
                TwoPointEndY = this.TwoPointEndY,
                SelectedSegmentDescription = this.SelectedSegmentDescription,
                SegmentStartX = this.SegmentStartX,
                SegmentStartY = this.SegmentStartY,
                SegmentEndX = this.SegmentEndX,
                SegmentEndY = this.SegmentEndY,
                AzimuthAngleDegrees = this.AzimuthAngleDegrees,
                SpatialReferenceWkid = this.SpatialReferenceWkid,
                SpatialReferenceName = this.SpatialReferenceName,
                IsValid = this.IsValid,
                ValidationMessages = this.ValidationMessages.ToList()
            };
        }
    }
}
