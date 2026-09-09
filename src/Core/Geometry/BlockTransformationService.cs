using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Unified authoritative transformation service for converting between local block geometry
    /// and ArcGIS Pro map spatial coordinates.
    /// Strictly anchors the selected local Base Point at the target map location and rotates around it.
    /// </summary>
    public static class BlockTransformationService
    {
        /// <summary>
        /// Calculates the local schematic coordinates of the selected Base Point anchor.
        /// </summary>
        public static (double X, double Y) GetSourceBasePoint(BlockConfiguration? config)
        {
            if (config == null) return (0.0, 0.0);

            double frontageA = config.SideA.TotalFrontage;
            double frontageB = config.SideB.TotalFrontage;
            double maxFrontage = Math.Max(frontageA, frontageB);

            double depthA = config.BaseParcel.Depth;
            double depthB = config.Arrangement == ArrangementMode.BackToBack ? -config.BaseParcel.Depth : 0.0;

            var anchor = config.Alignment?.AnchorPoint ?? BlockAnchorPoint.SideAStart;

            double localX;
            double localY;

            switch (anchor)
            {
                case BlockAnchorPoint.SideAStart:
                    localX = 0.0;
                    localY = depthA;
                    break;
                case BlockAnchorPoint.SideAEnd:
                    localX = frontageA;
                    localY = depthA;
                    break;
                case BlockAnchorPoint.SideACenter:
                    localX = frontageA / 2.0;
                    localY = depthA;
                    break;
                case BlockAnchorPoint.SideBStart:
                    localX = 0.0;
                    localY = depthB;
                    break;
                case BlockAnchorPoint.SideBEnd:
                    localX = frontageB;
                    localY = depthB;
                    break;
                case BlockAnchorPoint.BlockCenter:
                    localX = maxFrontage / 2.0;
                    localY = config.Arrangement == ArrangementMode.BackToBack ? 0.0 : depthA / 2.0;
                    break;
                default:
                    localX = 0.0;
                    localY = depthA;
                    break;
            }

            if (config.Alignment != null)
            {
                config.Alignment.SourceBasePointX = localX;
                config.Alignment.SourceBasePointY = localY;
            }

            return (localX, localY);
        }

        /// <summary>
        /// Transforms a local block coordinate (x, y) to map projected coordinates (X, Y).
        /// Enforces transformation order:
        /// 1. Offset relative to local source Base Point.
        /// 2. Rotate by orientation Azimuth angle around the Base Point.
        /// 3. Translate to target map coordinate.
        /// Guarantee: The Base Point lands exactly on the target map point and remains fixed under rotation.
        /// </summary>
        public static (double MapX, double MapY) TransformLocalToMap(double localX, double localY, BlockConfiguration? config)
        {
            if (config == null || config.Alignment == null || !config.Alignment.IsBasePointPlaced)
            {
                return (localX, localY);
            }

            var alignment = config.Alignment;
            var (sourceX, sourceY) = GetSourceBasePoint(config);

            double targetX = alignment.TargetMapPointX ?? 0.0;
            double targetY = alignment.TargetMapPointY ?? 0.0;

            // 1. Vector from source Base Point to current local vertex
            double relX = localX - sourceX;
            double relY = localY - sourceY;

            // 2. Rotate around the Base Point
            // Azimuth is clockwise from North (0° = North, 90° = East)
            // Cartesian angle counter-clockwise from East: theta = 90° - azimuth
            double angleRad = (90.0 - alignment.AzimuthAngleDegrees) * (Math.PI / 180.0);
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double rotX = (relX * cosA) - (relY * sinA);
            double rotY = (relX * sinA) + (relY * cosA);

            // 3. Anchor at Target Map Point
            return (targetX + rotX, targetY + rotY);
        }

        /// <summary>
        /// Transforms a map projected coordinate (X, Y) back into local block schematic coordinates (x, y).
        /// </summary>
        public static (double LocalX, double LocalY) TransformMapToLocal(double mapX, double mapY, BlockConfiguration? config)
        {
            if (config == null || config.Alignment == null || !config.Alignment.IsBasePointPlaced)
            {
                return (mapX, mapY);
            }

            var alignment = config.Alignment;
            var (sourceX, sourceY) = GetSourceBasePoint(config);

            double targetX = alignment.TargetMapPointX ?? 0.0;
            double targetY = alignment.TargetMapPointY ?? 0.0;

            // 1. Vector from target map point to clicked map coordinate
            double relMapX = mapX - targetX;
            double relMapY = mapY - targetY;

            // 2. Inverse rotation (-theta)
            double angleRad = (90.0 - alignment.AzimuthAngleDegrees) * (Math.PI / 180.0);
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double unrotX = (relMapX * cosA) + (relMapY * sinA);
            double unrotY = (-relMapX * sinA) + (relMapY * cosA);

            // 3. Add source local Base Point
            return (sourceX + unrotX, sourceY + unrotY);
        }

        /// <summary>
        /// Computes Azimuth bearing in degrees (0° to 360°, clockwise from North) from vector P1 -> P2.
        /// </summary>
        public static double CalculateAzimuth(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            if (length < 1e-6)
            {
                return 90.0; // Default East
            }

            double angleRad = Math.Atan2(dy, dx);
            double angleDeg = angleRad * (180.0 / Math.PI);
            double azimuth = (90.0 - angleDeg) % 360.0;
            if (azimuth < 0) azimuth += 360.0;

            return Math.Round(azimuth, 2);
        }

        /// <summary>
        /// Validates alignment configuration and populates validation status and messages.
        /// </summary>
        public static (bool IsValid, List<string> Errors, List<string> Warnings, List<string> Infos) ValidateAlignment(BlockConfiguration? config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            if (config == null || config.Alignment == null)
            {
                errors.Add("Alignment configuration is missing.");
                return (false, errors, warnings, infos);
            }

            var al = config.Alignment;

            // 1. Base Point checks
            if (!al.IsBasePointPlaced)
            {
                errors.Add("Target Base Point map location has not been selected.");
            }
            else
            {
                infos.Add($"Base Point anchored at ({al.TargetMapPointX:F2}, {al.TargetMapPointY:F2}).");
            }

            // 2. Orientation checks
            if (al.Method == AlignmentMethod.TwoPoints)
            {
                if (!al.TwoPointStartX.HasValue || !al.TwoPointStartY.HasValue ||
                    !al.TwoPointEndX.HasValue || !al.TwoPointEndY.HasValue)
                {
                    warnings.Add("Two orientation points have not been defined on the map (using default 90.0° bearing).");
                }
                else
                {
                    double dist = Math.Sqrt(Math.Pow(al.TwoPointEndX.Value - al.TwoPointStartX.Value, 2) +
                                            Math.Pow(al.TwoPointEndY.Value - al.TwoPointStartY.Value, 2));
                    if (dist < 0.001)
                    {
                        errors.Add("Two orientation points are identical (zero length vector).");
                    }
                    else if (dist < 0.5)
                    {
                        warnings.Add("Orientation baseline vector is extremely short (< 0.5m).");
                    }
                    else
                    {
                        infos.Add($"Orientation defined via 2 points (Bearing: {al.AzimuthAngleDegrees:F1}°).");
                    }
                }
            }
            else if (al.Method == AlignmentMethod.MapSegment)
            {
                if (string.IsNullOrWhiteSpace(al.SelectedSegmentDescription) ||
                    !al.SegmentStartX.HasValue || !al.SegmentEndX.HasValue ||
                    !al.SegmentStartY.HasValue || !al.SegmentEndY.HasValue)
                {
                    warnings.Add("Map segment has not been selected (using default 90.0° bearing).");
                }
                else
                {
                    double dist = Math.Sqrt(Math.Pow(al.SegmentEndX.Value - al.SegmentStartX.Value, 2) +
                                            Math.Pow(al.SegmentEndY.Value - al.SegmentStartY.Value, 2));
                    if (dist < 0.001)
                    {
                        errors.Add("Selected map segment has zero length.");
                    }
                    else
                    {
                        infos.Add($"Orientation aligned to segment '{al.SelectedSegmentDescription}' (Bearing: {al.AzimuthAngleDegrees:F1}°).");
                    }
                }
            }

            bool isValid = errors.Count == 0;
            al.IsValid = isValid;
            al.ValidationMessages = errors.Concat(warnings).ToList();

            return (isValid, errors, warnings, infos);
        }
    }
}
