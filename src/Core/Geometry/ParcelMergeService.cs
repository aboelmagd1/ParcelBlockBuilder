using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Computational geometry service for merging two adjacent parcels sharing a common boundary
    /// into a single contiguous parcel polygon.
    /// </summary>
    public class ParcelMergeService
    {
        public static ParcelMergeService Instance { get; } = new ParcelMergeService();

        /// <summary>
        /// Validates whether two parcels can be merged.
        /// Checks chamfers, shared boundary, and matching dimensions (depth for frontage merge, frontage for depth merge).
        /// </summary>
        public (bool CanMerge, string Reason) CanMergeParcels(
            ParcelModel parcel1,
            ParcelModel parcel2,
            bool allowDifferentSideMerge = true)
        {
            if (parcel1 == null || parcel2 == null)
            {
                return (false, "Please select two valid parcels to merge.");
            }

            if (parcel1.Id == parcel2.Id)
            {
                return (false, "Cannot merge a parcel with itself.");
            }

            // Check side compatibility
            if (parcel1.Side != parcel2.Side && !allowDifferentSideMerge)
            {
                return (false, "Cannot merge parcels from different sides. Merging is currently set to only allow adjacent parcels on the same street side.");
            }

            // Block merging Electric Room parcels
            if (parcel1.Type == "Electric Room" || parcel2.Type == "Electric Room" ||
                parcel1.Id == "ER-01" || parcel2.Id == "ER-01")
            {
                return (false, "Merge blocked: An Electric Room parcel cannot be merged with standard parcels.");
            }

            // Block if either parcel has an active corner chamfer
            if (parcel1.HasChamfer || parcel2.HasChamfer)
            {
                string chamferedId = parcel1.HasChamfer ? parcel1.Id : parcel2.Id;
                return (false, $"Merge blocked: Parcel {chamferedId} has an active corner chamfer. Please clear the corner chamfer before merging.");
            }

            if (parcel1.PolygonRing == null || parcel1.PolygonRing.Count < 3 ||
                parcel2.PolygonRing == null || parcel2.PolygonRing.Count < 3)
            {
                return (false, "One or both selected parcels have invalid geometry.");
            }

            // Check for common boundary (shared linear segment >= 0.01m)
            var (hasCommonBoundary, sharedLength) = FindSharedBoundary(parcel1.PolygonRing, parcel2.PolygonRing);
            if (!hasCommonBoundary || sharedLength < 0.01)
            {
                return (false, parcel1.Side == parcel2.Side
                    ? "These parcels cannot be merged because they do not share a common boundary."
                    : "These parcels cannot be merged because they do not share a common spine boundary between Side A and Side B.");
            }

            // Check matching shared dimension:
            // For side-by-side parcels sharing a depth edge: Depths must match
            // For front-back parcels sharing a frontage edge: Frontages must match
            bool isDepthMatch = Math.Abs(parcel1.Depth - parcel2.Depth) <= 0.05;
            bool isFrontageMatch = Math.Abs(parcel1.Frontage - parcel2.Frontage) <= 0.05;

            if (parcel1.Side == parcel2.Side)
            {
                if (!isDepthMatch)
                {
                    return (false, $"Merge blocked: Shared dimensions do not match (Depths: {parcel1.Depth:F1}m vs {parcel2.Depth:F1}m). Side-by-side merging requires identical depth dimensions.");
                }
            }
            else
            {
                // Different sides (Spine / Back-to-Back through-lot merge)
                if (!isFrontageMatch && sharedLength < Math.Min(parcel1.Frontage, parcel2.Frontage) - 0.1)
                {
                    return (false, $"Merge blocked: Back-to-back parcels on different sides must share matching frontage along the spine (Frontages: {parcel1.Frontage:F1}m vs {parcel2.Frontage:F1}m; Shared: {sharedLength:F1}m).");
                }
            }

            return (true, "Parcels are valid for merge.");
        }

        /// <summary>
        /// Attempts to merge two selected parcels.
        /// Verifies shared boundary and returns a single unified ParcelModel.
        /// </summary>
        public (bool Success, string Message, ParcelModel? MergedParcel) MergeParcels(
            ParcelModel parcel1,
            ParcelModel parcel2,
            bool allowDifferentSideMerge = true)
        {
            var (canMerge, reason) = CanMergeParcels(parcel1, parcel2, allowDifferentSideMerge);
            if (!canMerge)
            {
                return (false, reason, null);
            }

            // 2. Perform 2D polygon union
            var unionRing = UnionAdjacentPolygons(parcel1.PolygonRing, parcel2.PolygonRing);
            if (unionRing == null || unionRing.Count < 3)
            {
                return (false, "Failed to compute a valid unified polygon geometry for the selected parcels.", null);
            }

            double area = Math.Round(ParcelGeometryEngine.Instance.ComputePolygonArea(unionRing), 2);
            if (area <= 0.1)
            {
                return (false, "Merge rejected: resulting parcel has negligible or zero area.", null);
            }

            // Exact Area Conservation: Area == parcel1.Area + parcel2.Area
            double expectedArea = Math.Round(parcel1.Area + parcel2.Area, 2);
            if (Math.Abs(area - expectedArea) <= 1.0)
            {
                area = expectedArea;
            }

            // Derive new frontage & depth
            double minX = unionRing.Min(p => p.X);
            double maxX = unionRing.Max(p => p.X);
            double minY = unionRing.Min(p => p.Y);
            double maxY = unionRing.Max(p => p.Y);

            double frontage = Math.Round(maxX - minX, 2);
            double depth = Math.Round(maxY - minY, 2);

            bool isCrossSide = parcel1.Side != parcel2.Side;
            ParcelSide targetSide = isCrossSide
                ? ParcelSide.Both
                : parcel1.Side;

            int targetSeq = parcel1.Sequence;

            string mergedId;
            if (isCrossSide)
            {
                var pA = parcel1.Side == ParcelSide.SideA ? parcel1 : parcel2;
                var pB = parcel1.Side == ParcelSide.SideB ? parcel1 : parcel2;
                mergedId = $"{pA.Id}/{pB.Id}";
            }
            else
            {
                mergedId = $"{parcel1.Id}M";
                if (parcel1.Id.Length > 5 || parcel2.Id.Length > 5)
                {
                    mergedId = $"{parcel1.Id}_{parcel2.Id}";
                }
            }

            var mergedParcel = new ParcelModel
            {
                Id = mergedId,
                Side = targetSide,
                Sequence = targetSeq,
                Frontage = frontage,
                Depth = depth,
                Area = area,
                IsCorner = parcel1.IsCorner || parcel2.IsCorner,
                HasChamfer = false,
                HasStreetFrontage = true,
                IsModified = true,
                Type = isCrossSide ? "Through Parcel" : "Modified",
                PolygonRing = unionRing,
                Centroid = ParcelGeometryEngine.Instance.ComputeCentroid(unionRing)
            };

            return (true, $"✓ Successfully merged {parcel1.Id} and {parcel2.Id} into {mergedParcel.Id}.", mergedParcel);
        }

        /// <summary>
        /// Detects whether two polygons share an overlapping linear segment.
        /// </summary>
        public (bool HasSharedBoundary, double SharedLength) FindSharedBoundary(List<Point2D> poly1, List<Point2D> poly2)
        {
            double totalShared = 0.0;
            const double tolerance = 0.01;

            int n1 = poly1.Count;
            int n2 = poly2.Count;

            for (int i = 0; i < n1; i++)
            {
                var a1 = poly1[i];
                var a2 = poly1[(i + 1) % n1];

                for (int j = 0; j < n2; j++)
                {
                    var b1 = poly2[j];
                    var b2 = poly2[(j + 1) % n2];

                    double overlap = SegmentOverlap(a1, a2, b1, b2, tolerance);
                    if (overlap > tolerance)
                    {
                        totalShared += overlap;
                    }
                }
            }

            return (totalShared >= tolerance, totalShared);
        }

        private static double SegmentOverlap(Point2D a1, Point2D a2, Point2D b1, Point2D b2, double tol)
        {
            // Check if segments are collinear
            bool aIsVertical = Math.Abs(a2.X - a1.X) < tol;
            bool bIsVertical = Math.Abs(b2.X - b1.X) < tol;

            if (aIsVertical && bIsVertical)
            {
                if (Math.Abs(a1.X - b1.X) > tol) return 0.0;
                double aMinY = Math.Min(a1.Y, a2.Y);
                double aMaxY = Math.Max(a1.Y, a2.Y);
                double bMinY = Math.Min(b1.Y, b2.Y);
                double bMaxY = Math.Max(b1.Y, b2.Y);

                double overlapMin = Math.Max(aMinY, bMinY);
                double overlapMax = Math.Min(aMaxY, bMaxY);
                return Math.Max(0.0, overlapMax - overlapMin);
            }

            bool aIsHorizontal = Math.Abs(a2.Y - a1.Y) < tol;
            bool bIsHorizontal = Math.Abs(b2.Y - b1.Y) < tol;

            if (aIsHorizontal && bIsHorizontal)
            {
                if (Math.Abs(a1.Y - b1.Y) > tol) return 0.0;
                double aMinX = Math.Min(a1.X, a2.X);
                double aMaxX = Math.Max(a1.X, a2.X);
                double bMinX = Math.Min(b1.X, b2.X);
                double bMaxX = Math.Max(b1.X, b2.X);

                double overlapMin = Math.Max(aMinX, bMinX);
                double overlapMax = Math.Min(aMaxX, bMaxX);
                return Math.Max(0.0, overlapMax - overlapMin);
            }

            // General 2D collinear check
            return 0.0;
        }

        /// <summary>
        /// Ensures a polygon ring has a Counter-Clockwise (CCW) winding order.
        /// </summary>
        private static List<Point2D> EnsureCounterClockwise(List<Point2D> ring)
        {
            if (ring == null || ring.Count < 3) return ring ?? new List<Point2D>();
            double signedArea = 0.0;
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                var p1 = ring[i];
                var p2 = ring[(i + 1) % n];
                signedArea += (p1.X * p2.Y) - (p2.X * p1.Y);
            }
            if (signedArea < 0)
            {
                var reversed = new List<Point2D>(ring);
                reversed.Reverse();
                return reversed;
            }
            return ring;
        }

        /// <summary>
        /// Computes the union ring of two touching adjacent polygons.
        /// </summary>
        public List<Point2D> UnionAdjacentPolygons(List<Point2D> ring1, List<Point2D> ring2)
        {
            // Normalize winding order to Counter-Clockwise so shared seam edges run in opposite directions
            ring1 = EnsureCounterClockwise(ring1);
            ring2 = EnsureCounterClockwise(ring2);

            // Collect all directed boundary edges from both polygons
            var edges1 = GetDirectedEdges(ring1);
            var edges2 = GetDirectedEdges(ring2);

            var outerEdges = new List<(Point2D From, Point2D To)>();

            // An edge is an internal seam if an edge in poly1 is equal and opposite (or collinear overlap) in poly2
            foreach (var e1 in edges1)
            {
                var remaining = SubtractOverlappingEdge(e1, edges2);
                outerEdges.AddRange(remaining);
            }

            foreach (var e2 in edges2)
            {
                var remaining = SubtractOverlappingEdge(e2, edges1);
                outerEdges.AddRange(remaining);
            }

            // Chain the outer edges into a closed contiguous polygon loop
            var result = ChainEdgesToPolygon(outerEdges);
            return CleanRing(result);
        }

        private static List<(Point2D From, Point2D To)> GetDirectedEdges(List<Point2D> ring)
        {
            var list = new List<(Point2D, Point2D)>();
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                list.Add((ring[i], ring[(i + 1) % n]));
            }
            return list;
        }

        private static List<(Point2D From, Point2D To)> SubtractOverlappingEdge(
            (Point2D From, Point2D To) edge,
            List<(Point2D From, Point2D To)> otherEdges)
        {
            const double tol = 0.01;
            bool isVertical = Math.Abs(edge.To.X - edge.From.X) < tol;
            bool isHorizontal = Math.Abs(edge.To.Y - edge.From.Y) < tol;

            foreach (var other in otherEdges)
            {
                if (isVertical && Math.Abs(other.To.X - other.From.X) < tol && Math.Abs(edge.From.X - other.From.X) < tol)
                {
                    double eMinY = Math.Min(edge.From.Y, edge.To.Y);
                    double eMaxY = Math.Max(edge.From.Y, edge.To.Y);
                    double oMinY = Math.Min(other.From.Y, other.To.Y);
                    double oMaxY = Math.Max(other.From.Y, other.To.Y);

                    // Check if opposite direction and overlapping
                    if (Math.Max(eMinY, oMinY) < Math.Min(eMaxY, oMaxY) - tol)
                    {
                        // Seam edge - dissolve it
                        return new List<(Point2D, Point2D)>();
                    }
                }
                else if (isHorizontal && Math.Abs(other.To.Y - other.From.Y) < tol && Math.Abs(edge.From.Y - other.From.Y) < tol)
                {
                    double eMinX = Math.Min(edge.From.X, edge.To.X);
                    double eMaxX = Math.Max(edge.From.X, edge.To.X);
                    double oMinX = Math.Min(other.From.X, other.To.X);
                    double oMaxX = Math.Max(other.From.X, other.To.X);

                    if (Math.Max(eMinX, oMinX) < Math.Min(eMaxX, oMaxX) - tol)
                    {
                        // Seam edge - dissolve it
                        return new List<(Point2D, Point2D)>();
                    }
                }
            }

            return new List<(Point2D, Point2D)> { edge };
        }

        private static List<Point2D> ChainEdgesToPolygon(List<(Point2D From, Point2D To)> edges)
        {
            if (edges.Count == 0) return new List<Point2D>();

            var chained = new List<Point2D>();
            var unused = new List<(Point2D From, Point2D To)>(edges);

            var current = unused[0];
            chained.Add(current.From);
            unused.RemoveAt(0);

            const double tol = 0.02;

            while (unused.Count > 0)
            {
                var nextIndex = unused.FindIndex(e =>
                    Math.Abs(e.From.X - current.To.X) < tol && Math.Abs(e.From.Y - current.To.Y) < tol);

                bool needFlip = false;
                if (nextIndex == -1)
                {
                    nextIndex = unused.FindIndex(e =>
                        Math.Abs(e.To.X - current.To.X) < tol && Math.Abs(e.To.Y - current.To.Y) < tol);
                    if (nextIndex != -1)
                    {
                        needFlip = true;
                    }
                }

                if (nextIndex == -1)
                {
                    // Fallback to closest vertex if floating point precision differs
                    var best = unused
                        .Select((e, idx) =>
                        {
                            double dFrom = Math.Pow(e.From.X - current.To.X, 2) + Math.Pow(e.From.Y - current.To.Y, 2);
                            double dTo = Math.Pow(e.To.X - current.To.X, 2) + Math.Pow(e.To.Y - current.To.Y, 2);
                            return new { idx, dist = Math.Min(dFrom, dTo), flip = dTo < dFrom };
                        })
                        .OrderBy(x => x.dist)
                        .First();

                    nextIndex = best.idx;
                    needFlip = best.flip;
                }

                var nextEdge = unused[nextIndex];
                if (needFlip)
                {
                    current = (nextEdge.To, nextEdge.From);
                }
                else
                {
                    current = nextEdge;
                }

                chained.Add(current.From);
                unused.RemoveAt(nextIndex);
            }

            return chained;
        }

        private static List<Point2D> CleanRing(List<Point2D> ring)
        {
            if (ring == null || ring.Count < 3) return ring ?? new List<Point2D>();

            // 1. Remove duplicate consecutive vertices
            var deduped = new List<Point2D> { ring[0] };
            for (int i = 1; i < ring.Count; i++)
            {
                if (Math.Abs(ring[i].X - deduped.Last().X) > 1e-4 || Math.Abs(ring[i].Y - deduped.Last().Y) > 1e-4)
                {
                    deduped.Add(ring[i]);
                }
            }
            if (deduped.Count > 1 && Math.Abs(deduped[0].X - deduped.Last().X) < 1e-4 && Math.Abs(deduped[0].Y - deduped.Last().Y) < 1e-4)
            {
                deduped.RemoveAt(deduped.Count - 1);
            }

            if (deduped.Count < 3) return deduped;

            // 2. Remove redundant collinear vertices along straight line segments (~180°)
            var clean = new List<Point2D>();
            for (int i = 0; i < deduped.Count; i++)
            {
                var prev = deduped[(i - 1 + deduped.Count) % deduped.Count];
                var curr = deduped[i];
                var next = deduped[(i + 1) % deduped.Count];

                double crossProduct = (curr.X - prev.X) * (next.Y - curr.Y) - (curr.Y - prev.Y) * (next.X - curr.X);
                double dotProduct = (curr.X - prev.X) * (next.X - curr.X) + (curr.Y - prev.Y) * (next.Y - curr.Y);

                // If cross product is near zero and dot product > 0, curr is collinear on the segment between prev and next
                if (Math.Abs(crossProduct) < 1e-4 && dotProduct > 0)
                {
                    continue; // Skip redundant collinear vertex
                }

                clean.Add(curr);
            }

            return clean;
        }
    }
}
