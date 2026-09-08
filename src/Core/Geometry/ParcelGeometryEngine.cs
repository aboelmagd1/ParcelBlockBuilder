using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Single Geometry Engine responsible for generating deterministic parcel polygons,
    /// handling back-to-back arrangements, asymmetric side counts, custom exceptions,
    /// independent per-corner chamfer cuts, and electric room placement with automatic clipping.
    /// </summary>
    public class ParcelGeometryEngine
    {
        public static ParcelGeometryEngine Instance { get; } = new ParcelGeometryEngine();

        /// <summary>
        /// Generates all parcel polygons and updates computed block metrics on the configuration.
        /// </summary>
        public void GenerateGeometry(BlockConfiguration config)
        {
            if (config == null) return;

            config.SideA.GeneratedParcels.Clear();
            config.SideB.GeneratedParcels.Clear();

            double currentX = 0.0;
            double sideATotalFrontage = 0.0;
            double sideATotalArea = 0.0;

            var cornerAStart = config.Corner.GetEffectiveCorner(CornerPosition.SideAStart);
            var cornerAEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideAEnd);
            var cornerBStart = config.Corner.GetEffectiveCorner(CornerPosition.SideBStart);
            var cornerBEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideBEnd);

            // --- 1. GENERATE SIDE A PARCELS ---
            int countA = config.SideA.ParcelCount;
            for (int i = 1; i <= countA; i++)
            {
                double frontage = config.GetEffectiveFrontage(ParcelSide.SideA, i);
                double depth = config.GetEffectiveDepth(ParcelSide.SideA, i);
                string parcelId = $"A-{i:D2}";

                bool isStartCorner = (i == 1) && config.Corner.HasChamfer && cornerAStart.IsEnabled;
                bool isEndCorner = (i == countA) && config.Corner.HasChamfer && cornerAEnd.IsEnabled;
                bool isCustomChamfer = config.Corner.HasChamfer && config.Corner.CustomChamferParcelIds.Contains(parcelId);
                bool hasChamfer = isStartCorner || isEndCorner || isCustomChamfer;
                bool isCorner = (i == 1) || (i == countA);

                var ring = BuildParcelRing(
                    originX: currentX,
                    originY: 0.0,
                    frontage: frontage,
                    depth: depth,
                    isSideA: true,
                    isStartCorner: isStartCorner || (isCustomChamfer && i == 1),
                    isEndCorner: isEndCorner || (isCustomChamfer && i != 1),
                    startCornerConfig: cornerAStart,
                    endCornerConfig: cornerAEnd
                );

                double area = ComputePolygonArea(ring);

                var parcel = new ParcelModel
                {
                    Id = parcelId,
                    Side = ParcelSide.SideA,
                    Sequence = i,
                    Frontage = frontage,
                    Depth = depth,
                    Area = Math.Round(area, 2),
                    IsCorner = isCorner,
                    HasChamfer = hasChamfer,
                    IsModified = config.Exceptions.Any(e => e.Side == ParcelSide.SideA && e.Sequence == i),
                    Type = isCorner ? "Corner" : "Standard",
                    PolygonRing = ring,
                    Centroid = ComputeCentroid(ring)
                };

                config.SideA.GeneratedParcels.Add(parcel);
                currentX += frontage;
                sideATotalFrontage += frontage;
                sideATotalArea += area;
            }

            config.SideA.TotalFrontage = sideATotalFrontage;
            config.SideA.TotalArea = Math.Round(sideATotalArea, 2);

            // --- 2. GENERATE SIDE B PARCELS (Back-to-Back) ---
            double sideBTotalFrontage = 0.0;
            double sideBTotalArea = 0.0;

            if (config.Arrangement == ArrangementMode.BackToBack)
            {
                currentX = 0.0;
                int countB = config.SideB.ParcelCount;
                for (int i = 1; i <= countB; i++)
                {
                    double frontage = config.GetEffectiveFrontage(ParcelSide.SideB, i);
                    double depth = config.GetEffectiveDepth(ParcelSide.SideB, i);
                    string parcelId = $"B-{i:D2}";

                    bool isStartCorner = (i == 1) && config.Corner.HasChamfer && cornerBStart.IsEnabled;
                    bool isEndCorner = (i == countB) && config.Corner.HasChamfer && cornerBEnd.IsEnabled;
                    bool isCustomChamfer = config.Corner.HasChamfer && config.Corner.CustomChamferParcelIds.Contains(parcelId);
                    bool hasChamfer = isStartCorner || isEndCorner || isCustomChamfer;
                    bool isCorner = (i == 1) || (i == countB);

                    var ring = BuildParcelRing(
                        originX: currentX,
                        originY: 0.0,
                        frontage: frontage,
                        depth: depth,
                        isSideA: false,
                        isStartCorner: isStartCorner || (isCustomChamfer && i == 1),
                        isEndCorner: isEndCorner || (isCustomChamfer && i != 1),
                        startCornerConfig: cornerBStart,
                        endCornerConfig: cornerBEnd
                    );

                    double area = ComputePolygonArea(ring);

                    var parcel = new ParcelModel
                    {
                        Id = parcelId,
                        Side = ParcelSide.SideB,
                        Sequence = i,
                        Frontage = frontage,
                        Depth = depth,
                        Area = Math.Round(area, 2),
                        IsCorner = isCorner,
                        HasChamfer = hasChamfer,
                        IsModified = config.Exceptions.Any(e => e.Side == ParcelSide.SideB && e.Sequence == i),
                        Type = isCorner ? "Corner" : "Standard",
                        PolygonRing = ring,
                        Centroid = ComputeCentroid(ring)
                    };

                    config.SideB.GeneratedParcels.Add(parcel);
                    currentX += frontage;
                    sideBTotalFrontage += frontage;
                    sideBTotalArea += area;
                }
            }

            config.SideB.TotalFrontage = sideBTotalFrontage;
            config.SideB.TotalArea = Math.Round(sideBTotalArea, 2);

            // --- 3. APPLY ELECTRIC ROOM GEOMETRY & CLIPPING ---
            if (config.ElectricRoom != null && config.ElectricRoom.HasElectricRoom)
            {
                ApplyElectricRoom(config);
            }

            // --- 4. OVERALL ESTIMATED BLOCK METRICS ---
            config.EstimatedBlockLength = Math.Max(config.SideA.TotalFrontage, config.SideB.TotalFrontage);
            config.EstimatedBlockDepth = config.Arrangement == ArrangementMode.BackToBack
                ? config.BaseParcel.Depth * 2.0
                : config.BaseParcel.Depth;
            config.EstimatedBlockArea = Math.Round(config.SideA.TotalArea + config.SideB.TotalArea, 2);
        }

        private void ApplyElectricRoom(BlockConfiguration config)
        {
            var erConfig = config.ElectricRoom;
            var targetSide = erConfig.Side;
            var parcelsList = targetSide == ParcelSide.SideA ? config.SideA.GeneratedParcels : config.SideB.GeneratedParcels;
            if (parcelsList.Count == 0) return;

            double erWidth = Math.Max(0.1, erConfig.Width);
            double erDepth = Math.Max(0.1, erConfig.Depth);
            bool isSideA = targetSide == ParcelSide.SideA;
            double depth = config.BaseParcel.Depth;
            double yStreet = isSideA ? depth : -depth;
            double yInner = isSideA ? (depth - erDepth) : (-depth + erDepth);

            double totalFrontage = targetSide == ParcelSide.SideA ? config.SideA.TotalFrontage : config.SideB.TotalFrontage;

            // Determine Electric Room X position along street frontage
            double erX1 = 0.0;
            if (erConfig.PlacementMethod == ElectricRoomPlacementMethod.InteractiveMapPlacement &&
                erConfig.ClickedMapX.HasValue && erConfig.ClickedMapY.HasValue)
            {
                var (localX, _) = ProjectMapPointToLocal(erConfig.ClickedMapX.Value, erConfig.ClickedMapY.Value, config);
                erX1 = localX - (erWidth / 2.0);
                erConfig.OffsetDistance = Math.Round(erX1, 2);
                erConfig.AnchorPoint = new Point2D(localX, yStreet);
            }
            else
            {
                erX1 = Math.Max(0.0, erConfig.OffsetDistance);
                erConfig.AnchorPoint = new Point2D(erX1 + (erWidth / 2.0), yStreet);
            }

            double erX2 = erX1 + erWidth;

            // 1. IMPORTANT GEOMETRY RULE: Strict Block Boundary Containment Check
            // Electric room must not extend outside the block boundary or into chamfer cuts
            bool isOutOfBounds = erX1 < -1e-4 || erX2 > totalFrontage + 1e-4 || erDepth > depth + 1e-4 || erWidth <= 0 || erDepth <= 0;

            if (!isOutOfBounds && config.Corner != null && config.Corner.HasChamfer)
            {
                if (isSideA)
                {
                    var cStart = config.Corner.GetEffectiveCorner(CornerPosition.SideAStart);
                    if (cStart.IsEnabled)
                    {
                        var (cutX, _) = cStart.GetEffectiveCutDistances(config.SideA.GeneratedParcels.FirstOrDefault()?.Frontage ?? 10.0, depth);
                        if (erX1 < cutX - 1e-4) isOutOfBounds = true;
                    }

                    var cEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideAEnd);
                    if (cEnd.IsEnabled)
                    {
                        var (cutX, _) = cEnd.GetEffectiveCutDistances(config.SideA.GeneratedParcels.LastOrDefault()?.Frontage ?? 10.0, depth);
                        if (erX2 > totalFrontage - cutX + 1e-4) isOutOfBounds = true;
                    }
                }
                else
                {
                    var cStart = config.Corner.GetEffectiveCorner(CornerPosition.SideBStart);
                    if (cStart.IsEnabled)
                    {
                        var (cutX, _) = cStart.GetEffectiveCutDistances(config.SideB.GeneratedParcels.FirstOrDefault()?.Frontage ?? 10.0, depth);
                        if (erX1 < cutX - 1e-4) isOutOfBounds = true;
                    }

                    var cEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideBEnd);
                    if (cEnd.IsEnabled)
                    {
                        var (cutX, _) = cEnd.GetEffectiveCutDistances(config.SideB.GeneratedParcels.LastOrDefault()?.Frontage ?? 10.0, depth);
                        if (erX2 > totalFrontage - cutX + 1e-4) isOutOfBounds = true;
                    }
                }
            }

            if (isOutOfBounds)
            {
                erConfig.IsPlacementValid = false;
                erConfig.ValidationStatusMessage = "Electric Room cannot be placed here because the configured footprint extends outside the block boundary.";
                erConfig.PlacementType = ElectricRoomPlacementType.InvalidOutsideBlock;
                erConfig.HostParcelIds.Clear();
                erConfig.PolygonRing.Clear();
                return;
            }

            // 2. Host Parcel Detection (Single Parcel vs Two Parcels)
            var normalParcels = parcelsList.Where(p => p.Type != "Electric Room" && p.Id != "ER-01").ToList();
            var hostParcels = new List<(ParcelModel Parcel, double StartX, double EndX)>();
            double curX = 0.0;
            foreach (var p in normalParcels)
            {
                double pX1 = curX;
                double pX2 = curX + p.Frontage;
                double overlap = Math.Min(erX2, pX2) - Math.Max(erX1, pX1);
                if (overlap > 1e-3)
                {
                    hostParcels.Add((p, pX1, pX2));
                }
                curX = pX2;
            }

            if (hostParcels.Count == 1)
            {
                erConfig.PlacementType = ElectricRoomPlacementType.InsideSingleParcel;
                erConfig.HostParcelIds = new List<string> { hostParcels[0].Parcel.Id };
                erConfig.IsPlacementValid = true;
                erConfig.ValidationStatusMessage = $"✓ Valid placement — Host Parcel: {hostParcels[0].Parcel.Id} (Inside Parcel)";
            }
            else if (hostParcels.Count == 2)
            {
                erConfig.PlacementType = ElectricRoomPlacementType.BetweenTwoParcels;
                erConfig.HostParcelIds = new List<string> { hostParcels[0].Parcel.Id, hostParcels[1].Parcel.Id };
                erConfig.IsPlacementValid = true;
                erConfig.ValidationStatusMessage = $"✓ Valid placement — Host Parcels: {hostParcels[0].Parcel.Id}, {hostParcels[1].Parcel.Id} (Between Two Parcels)";
            }
            else
            {
                erConfig.PlacementType = ElectricRoomPlacementType.InvalidOutsideBlock;
                erConfig.IsPlacementValid = false;
                erConfig.ValidationStatusMessage = "Electric Room placement is invalid (must be inside one parcel or between two adjacent parcels).";
                erConfig.HostParcelIds.Clear();
                erConfig.PolygonRing.Clear();
                return;
            }

            // 3. Build Electric Room Polygon Ring (oriented along street vector)
            var erRing = new List<Point2D>();
            if (isSideA)
            {
                erRing.Add(new Point2D(erX1, yInner));
                erRing.Add(new Point2D(erX1, yStreet));
                erRing.Add(new Point2D(erX2, yStreet));
                erRing.Add(new Point2D(erX2, yInner));
            }
            else
            {
                erRing.Add(new Point2D(erX1, yInner));
                erRing.Add(new Point2D(erX2, yInner));
                erRing.Add(new Point2D(erX2, yStreet));
                erRing.Add(new Point2D(erX1, yStreet));
            }

            erConfig.PolygonRing = erRing.Select(p => new Point2D(p.X, p.Y)).ToList();

            // 4. Clip / Carve host parcel(s) if enabled
            if (erConfig.ClipHostParcels)
            {
                foreach (var (parcel, pX1, pX2) in hostParcels)
                {
                    double overlapX1 = Math.Max(pX1, erX1);
                    double overlapX2 = Math.Min(pX2, erX2);

                    if (overlapX2 > overlapX1 + 1e-4)
                    {
                        parcel.PolygonRing = CarveElectricRoomFromParcel(parcel.PolygonRing, pX1, pX2, overlapX1, overlapX2, yStreet, yInner, isSideA);
                        parcel.Area = Math.Round(ComputePolygonArea(parcel.PolygonRing), 2);
                        parcel.Centroid = ComputeCentroid(parcel.PolygonRing);
                        parcel.IsModified = true;
                    }
                }
            }

            // 5. Add Electric Room Parcel as a distinct feature
            var erParcel = new ParcelModel
            {
                Id = "ER-01",
                Side = targetSide,
                Sequence = 99,
                Frontage = erWidth,
                Depth = erDepth,
                Area = Math.Round(ComputePolygonArea(erRing), 2),
                IsCorner = false,
                HasChamfer = false,
                IsModified = true,
                Type = "Electric Room",
                PolygonRing = erRing,
                Centroid = ComputeCentroid(erRing)
            };

            parcelsList.Add(erParcel);

            // Update side totals
            if (targetSide == ParcelSide.SideA)
            {
                config.SideA.TotalArea = Math.Round(config.SideA.GeneratedParcels.Sum(p => p.Area), 2);
            }
            else
            {
                config.SideB.TotalArea = Math.Round(config.SideB.GeneratedParcels.Sum(p => p.Area), 2);
            }
        }

        public static (double localX, double localY) ProjectMapPointToLocal(double gx, double gy, BlockConfiguration? config)
        {
            return BlockTransformationService.TransformMapToLocal(gx, gy, config);
        }

        private List<Point2D> CarveElectricRoomFromParcel(
            List<Point2D> originalRing,
            double pX1,
            double pX2,
            double overlapX1,
            double overlapX2,
            double yStreet,
            double yInner,
            bool isSideA)
        {
            if (originalRing == null || originalRing.Count < 3) return originalRing ?? new List<Point2D>();

            var newRing = new List<Point2D>();
            for (int i = 0; i < originalRing.Count; i++)
            {
                var pt = originalRing[i];
                var nextPt = originalRing[(i + 1) % originalRing.Count];

                bool isStreetEdge = Math.Abs(pt.Y - yStreet) < 0.05 && Math.Abs(nextPt.Y - yStreet) < 0.05;
                if (isStreetEdge)
                {
                    double segMinX = Math.Min(pt.X, nextPt.X);
                    double segMaxX = Math.Max(pt.X, nextPt.X);

                    if (overlapX1 >= segMinX - 1e-4 && overlapX2 <= segMaxX + 1e-4)
                    {
                        if (pt.X <= nextPt.X)
                        {
                            newRing.Add(pt);
                            if (overlapX1 > pt.X + 1e-4)
                            {
                                newRing.Add(new Point2D(overlapX1, yStreet));
                            }
                            newRing.Add(new Point2D(overlapX1, yInner));
                            newRing.Add(new Point2D(overlapX2, yInner));
                            if (overlapX2 < nextPt.X - 1e-4)
                            {
                                newRing.Add(new Point2D(overlapX2, yStreet));
                            }
                        }
                        else
                        {
                            newRing.Add(pt);
                            if (overlapX2 < pt.X - 1e-4)
                            {
                                newRing.Add(new Point2D(overlapX2, yStreet));
                            }
                            newRing.Add(new Point2D(overlapX2, yInner));
                            newRing.Add(new Point2D(overlapX1, yInner));
                            if (overlapX1 > nextPt.X + 1e-4)
                            {
                                newRing.Add(new Point2D(overlapX1, yStreet));
                            }
                        }
                        continue;
                    }
                }

                newRing.Add(pt);
            }

            return CleanConsecutiveDuplicates(newRing);
        }

        private List<Point2D> CleanConsecutiveDuplicates(List<Point2D> ring)
        {
            if (ring.Count < 2) return ring;
            var clean = new List<Point2D> { ring[0] };
            for (int i = 1; i < ring.Count; i++)
            {
                if (Math.Abs(ring[i].X - clean.Last().X) > 1e-4 || Math.Abs(ring[i].Y - clean.Last().Y) > 1e-4)
                {
                    clean.Add(ring[i]);
                }
            }
            return clean;
        }

        private List<Point2D> BuildParcelRing(
            double originX,
            double originY,
            double frontage,
            double depth,
            bool isSideA,
            bool isStartCorner,
            bool isEndCorner,
            SingleCornerConfig startCornerConfig,
            SingleCornerConfig endCornerConfig)
        {
            var ring = new List<Point2D>();
            var (startCutMain, startCutCross) = startCornerConfig.GetEffectiveCutDistances(frontage, depth);
            var (endCutMain, endCutCross) = endCornerConfig.GetEffectiveCutDistances(frontage, depth);

            if (isSideA)
            {
                // Side A: Spine at Y = 0, Street Frontage at Y = +depth
                double ySpine = originY;
                double yStreet = originY + depth;
                double xLeft = originX;
                double xRight = originX + frontage;

                // Point 1: Bottom-Left (Spine)
                ring.Add(new Point2D(xLeft, ySpine));

                // Point 2: Top-Left (Street A & Left Street) - Start Chamfer
                if (isStartCorner && startCutMain > 0 && startCutCross > 0)
                {
                    ring.Add(new Point2D(xLeft, yStreet - startCutCross));
                    ring.Add(new Point2D(xLeft + startCutMain, yStreet));
                }
                else
                {
                    ring.Add(new Point2D(xLeft, yStreet));
                }

                // Point 3: Top-Right (Street A & Right Street) - End Chamfer
                if (isEndCorner && endCutMain > 0 && endCutCross > 0)
                {
                    ring.Add(new Point2D(xRight - endCutMain, yStreet));
                    ring.Add(new Point2D(xRight, yStreet - endCutCross));
                }
                else
                {
                    ring.Add(new Point2D(xRight, yStreet));
                }

                // Point 4: Bottom-Right (Spine)
                ring.Add(new Point2D(xRight, ySpine));
            }
            else
            {
                // Side B: Spine at Y = 0, Street Frontage at Y = -depth
                double ySpine = originY;
                double yStreet = originY - depth;
                double xLeft = originX;
                double xRight = originX + frontage;

                // Point 1: Top-Left (Spine)
                ring.Add(new Point2D(xLeft, ySpine));

                // Point 2: Top-Right (Spine)
                ring.Add(new Point2D(xRight, ySpine));

                // Point 3: Bottom-Right (Street B & Right Street) - End Chamfer
                if (isEndCorner && endCutMain > 0 && endCutCross > 0)
                {
                    ring.Add(new Point2D(xRight, yStreet + endCutCross));
                    ring.Add(new Point2D(xRight - endCutMain, yStreet));
                }
                else
                {
                    ring.Add(new Point2D(xRight, yStreet));
                }

                // Point 4: Bottom-Left (Street B & Left Street) - Start Chamfer
                if (isStartCorner && startCutMain > 0 && startCutCross > 0)
                {
                    ring.Add(new Point2D(xLeft + startCutMain, yStreet));
                    ring.Add(new Point2D(xLeft, yStreet + startCutCross));
                }
                else
                {
                    ring.Add(new Point2D(xLeft, yStreet));
                }
            }

            return ring;
        }

        public double ComputePolygonArea(List<Point2D> ring)
        {
            if (ring == null || ring.Count < 3) return 0.0;
            double area = 0.0;
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += (ring[i].X * ring[j].Y) - (ring[j].X * ring[i].Y);
            }
            return Math.Abs(area * 0.5);
        }

        public Point2D ComputeCentroid(List<Point2D> ring)
        {
            if (ring == null || ring.Count == 0) return new Point2D(0, 0);
            double sumX = ring.Sum(p => p.X);
            double sumY = ring.Sum(p => p.Y);
            return new Point2D(sumX / ring.Count, sumY / ring.Count);
        }
    }
}
