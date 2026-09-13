using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Computational geometry service for splitting a parcel into two valid parcels.
    /// Supports rectangular, chamfered, and irregular parcels in local coordinates.
    /// </summary>
    public class ParcelSplitService
    {
        public static ParcelSplitService Instance { get; } = new ParcelSplitService();

        /// <summary>
        /// Attempts to split the source parcel according to direction, method, and position.
        /// Returns two valid ParcelModels, warnings, or an error validation message.
        /// </summary>
        public (bool Success, string Message, string ChamferWarning, string FrontageWarning, ParcelModel? Parcel1, ParcelModel? Parcel2) SplitParcel(
            ParcelModel sourceParcel,
            SplitDirection direction,
            SplitMethod method,
            double customPosition,
            double minDimensionThreshold = 3.0,
            int totalSideParcelCount = 1)
        {
            if (sourceParcel == null || sourceParcel.PolygonRing == null || sourceParcel.PolygonRing.Count < 3)
            {
                return (false, "The selected parcel geometry is invalid or empty.", string.Empty, string.Empty, null, null);
            }

            var ring = sourceParcel.PolygonRing;
            double minX = ring.Min(p => p.X);
            double maxX = ring.Max(p => p.X);
            double minY = ring.Min(p => p.Y);
            double maxY = ring.Max(p => p.Y);

            double width = maxX - minX;
            double height = maxY - minY;

            double targetDimension = direction == SplitDirection.AlongFrontage ? sourceParcel.Frontage : sourceParcel.Depth;
            if (targetDimension <= 0.001)
            {
                targetDimension = direction == SplitDirection.AlongFrontage ? width : height;
            }

            double effectiveSplitPos = 0.0;
            if (method == SplitMethod.EqualSplit)
            {
                effectiveSplitPos = targetDimension / 2.0;
            }
            else
            {
                effectiveSplitPos = customPosition;
            }

            // Minimum dimension validation (do not silently clamp)
            double dim1 = effectiveSplitPos;
            double dim2 = targetDimension - effectiveSplitPos;

            if (dim1 < minDimensionThreshold || dim2 < minDimensionThreshold)
            {
                double smaller = Math.Min(dim1, dim2);
                return (false, $"Split blocked: resulting parcel dimension ({smaller:F2}m) is below the minimum allowed dimension ({minDimensionThreshold:F1}m).", string.Empty, string.Empty, null, null);
            }

            if (effectiveSplitPos <= 0.1 || effectiveSplitPos >= targetDimension - 0.1)
            {
                return (false, $"Split position must be within (0.1m, {targetDimension - 0.1:F1}m).", string.Empty, string.Empty, null, null);
            }

            List<Point2D> poly1Ring;
            List<Point2D> poly2Ring;

            string chamferWarning = string.Empty;
            string frontageWarning = string.Empty;

            bool p1HasChamfer = false;
            bool p2HasChamfer = false;
            bool p1IsCorner = false;
            bool p2IsCorner = false;
            bool p1HasStreetFrontage = true;
            bool p2HasStreetFrontage = true;

            if (direction == SplitDirection.AlongFrontage)
            {
                // Along Frontage: Vertical dividing line along parcel local X
                double cutX = minX + effectiveSplitPos;
                poly1Ring = ClipPolygonByHalfPlane(ring, p => p.X <= cutX, (p1, p2) => IntersectLineX(p1, p2, cutX));
                poly2Ring = ClipPolygonByHalfPlane(ring, p => p.X >= cutX, (p1, p2) => IntersectLineX(p1, p2, cutX));

                p1HasStreetFrontage = true;
                p2HasStreetFrontage = true;

                // Chamfer preservation for frontage split
                if (sourceParcel.HasChamfer)
                {
                    bool isStartCorner = sourceParcel.Sequence == 1;
                    bool isEndCorner = sourceParcel.Sequence >= totalSideParcelCount && totalSideParcelCount > 1;

                    if (isStartCorner)
                    {
                        p1HasChamfer = true;
                        p1IsCorner = true;
                        p2HasChamfer = false;
                        p2IsCorner = false;
                        chamferWarning = "Corner chamfer preserved on the outer corner parcel (Parcel 1) and cleared from inner parcel (Parcel 2).";
                    }
                    else if (isEndCorner)
                    {
                        p1HasChamfer = false;
                        p1IsCorner = false;
                        p2HasChamfer = true;
                        p2IsCorner = true;
                        chamferWarning = "Corner chamfer preserved on the outer corner parcel (Parcel 2) and cleared from inner parcel (Parcel 1).";
                    }
                    else
                    {
                        // Default to preserving on the larger or first parcel
                        p1HasChamfer = true;
                        p2HasChamfer = false;
                        chamferWarning = "Corner chamfer assigned to Parcel 1 and cleared from Parcel 2.";
                    }
                }
            }
            else
            {
                // Along Depth: Dividing line perpendicular to depth (horizontal cut)
                double cutY;
                if (sourceParcel.Side == ParcelSide.SideA)
                {
                    // Street is at maxY (+depth), Spine is at minY (0)
                    cutY = minY + effectiveSplitPos;
                    // poly1: spine to cutY (rear child, no street frontage)
                    // poly2: cutY to maxY (front child, street facing)
                    poly1Ring = ClipPolygonByHalfPlane(ring, p => p.Y <= cutY, (p1, p2) => IntersectLineY(p1, p2, cutY));
                    poly2Ring = ClipPolygonByHalfPlane(ring, p => p.Y >= cutY, (p1, p2) => IntersectLineY(p1, p2, cutY));

                    p1HasStreetFrontage = false; // Rear parcel
                    p2HasStreetFrontage = true;  // Front parcel
                    frontageWarning = "⚠️ Rear child parcel (Parcel 1) has no direct street frontage.";

                    if (sourceParcel.HasChamfer)
                    {
                        p2HasChamfer = true; // Street facing child keeps chamfer
                        p2IsCorner = sourceParcel.IsCorner;
                        p1HasChamfer = false;
                        p1IsCorner = false;
                        chamferWarning = "Corner chamfer preserved on the street-facing child (Parcel 2) and cleared from rear child (Parcel 1).";
                    }
                }
                else
                {
                    // Side B: Street is at minY (-depth), Spine is at maxY (0)
                    cutY = maxY - effectiveSplitPos;
                    poly1Ring = ClipPolygonByHalfPlane(ring, p => p.Y >= cutY, (p1, p2) => IntersectLineY(p1, p2, cutY)); // Rear child
                    poly2Ring = ClipPolygonByHalfPlane(ring, p => p.Y <= cutY, (p1, p2) => IntersectLineY(p1, p2, cutY)); // Front child

                    p1HasStreetFrontage = false; // Rear parcel
                    p2HasStreetFrontage = true;  // Front parcel
                    frontageWarning = "⚠️ Rear child parcel (Parcel 1) has no direct street frontage.";

                    if (sourceParcel.HasChamfer)
                    {
                        p2HasChamfer = true; // Street facing child keeps chamfer
                        p2IsCorner = sourceParcel.IsCorner;
                        p1HasChamfer = false;
                        p1IsCorner = false;
                        chamferWarning = "Corner chamfer preserved on the street-facing child (Parcel 2) and cleared from rear child (Parcel 1).";
                    }
                }
            }

            poly1Ring = CleanRing(poly1Ring);
            poly2Ring = CleanRing(poly2Ring);

            if (poly1Ring.Count < 3 || poly2Ring.Count < 3)
            {
                return (false, "The selected parcel cannot be split using the current configuration.", string.Empty, string.Empty, null, null);
            }

            double rawArea1 = ParcelGeometryEngine.Instance.ComputePolygonArea(poly1Ring);
            double rawArea2 = ParcelGeometryEngine.Instance.ComputePolygonArea(poly2Ring);

            if (rawArea1 <= 0.1 || rawArea2 <= 0.1)
            {
                return (false, "Split rejected: one of the resulting parcels has negligible or zero area.", string.Empty, string.Empty, null, null);
            }

            // Total Area Conservation: Area1 + Area2 == sourceParcel.Area exactly
            double area1 = Math.Round(rawArea1, 2);
            double area2 = Math.Round(sourceParcel.Area - area1, 2);
            if (area2 <= 0.0)
            {
                area2 = Math.Round(rawArea2, 2);
            }

            // Derive frontage & depth for new parcels
            double f1, f2, d1, d2;
            if (direction == SplitDirection.AlongFrontage)
            {
                f1 = Math.Round(effectiveSplitPos, 2);
                f2 = Math.Round(targetDimension - effectiveSplitPos, 2);
                d1 = sourceParcel.Depth;
                d2 = sourceParcel.Depth;
            }
            else
            {
                f1 = sourceParcel.Frontage;
                f2 = sourceParcel.Frontage;
                d1 = Math.Round(effectiveSplitPos, 2);
                d2 = Math.Round(targetDimension - effectiveSplitPos, 2);
            }

            string baseId = sourceParcel.Id;
            string id1 = $"{baseId}A";
            string id2 = $"{baseId}B";

            var parcel1 = new ParcelModel
            {
                Id = id1,
                Side = sourceParcel.Side,
                Sequence = sourceParcel.Sequence,
                Frontage = f1,
                Depth = d1,
                Area = area1,
                IsCorner = p1IsCorner,
                HasChamfer = p1HasChamfer,
                HasStreetFrontage = p1HasStreetFrontage,
                ParentParcelId = sourceParcel.Id,
                IsModified = true,
                Type = "Split",
                PolygonRing = poly1Ring,
                Centroid = ParcelGeometryEngine.Instance.ComputeCentroid(poly1Ring)
            };

            var parcel2 = new ParcelModel
            {
                Id = id2,
                Side = sourceParcel.Side,
                Sequence = sourceParcel.Sequence + 1,
                Frontage = f2,
                Depth = d2,
                Area = area2,
                IsCorner = p2IsCorner,
                HasChamfer = p2HasChamfer,
                HasStreetFrontage = p2HasStreetFrontage,
                ParentParcelId = sourceParcel.Id,
                IsModified = true,
                Type = "Split",
                PolygonRing = poly2Ring,
                Centroid = ParcelGeometryEngine.Instance.ComputeCentroid(poly2Ring)
            };

            return (true, "✓ Valid split configuration.", chamferWarning, frontageWarning, parcel1, parcel2);
        }

        private static List<Point2D> ClipPolygonByHalfPlane(
            List<Point2D> polygon,
            Func<Point2D, bool> isInside,
            Func<Point2D, Point2D, Point2D> intersect)
        {
            var outputList = new List<Point2D>();
            if (polygon == null || polygon.Count == 0) return outputList;

            var s = polygon.Last();
            foreach (var p in polygon)
            {
                if (isInside(p))
                {
                    if (isInside(s))
                    {
                        outputList.Add(p);
                    }
                    else
                    {
                        outputList.Add(intersect(s, p));
                        outputList.Add(p);
                    }
                }
                else if (isInside(s))
                {
                    outputList.Add(intersect(s, p));
                }
                s = p;
            }

            return outputList;
        }

        private static Point2D IntersectLineX(Point2D p1, Point2D p2, double cutX)
        {
            double dx = p2.X - p1.X;
            if (Math.Abs(dx) < 1e-7) return new Point2D(cutX, p1.Y);
            double t = (cutX - p1.X) / dx;
            double y = p1.Y + t * (p2.Y - p1.Y);
            return new Point2D(cutX, y);
        }

        private static Point2D IntersectLineY(Point2D p1, Point2D p2, double cutY)
        {
            double dy = p2.Y - p1.Y;
            if (Math.Abs(dy) < 1e-7) return new Point2D(p1.X, cutY);
            double t = (cutY - p1.Y) / dy;
            double x = p1.X + t * (p2.X - p1.X);
            return new Point2D(x, cutY);
        }

        private static List<Point2D> CleanRing(List<Point2D> ring)
        {
            if (ring == null || ring.Count < 3) return ring ?? new List<Point2D>();
            var clean = new List<Point2D> { ring[0] };
            for (int i = 1; i < ring.Count; i++)
            {
                if (Math.Abs(ring[i].X - clean.Last().X) > 1e-4 || Math.Abs(ring[i].Y - clean.Last().Y) > 1e-4)
                {
                    clean.Add(ring[i]);
                }
            }
            // Ensure first and last are not duplicated in memory list representation
            if (clean.Count > 1 && Math.Abs(clean[0].X - clean.Last().X) < 1e-4 && Math.Abs(clean[0].Y - clean.Last().Y) < 1e-4)
            {
                clean.RemoveAt(clean.Count - 1);
            }
            return clean;
        }
    }
}
