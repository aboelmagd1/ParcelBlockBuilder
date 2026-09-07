using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Single Geometry Engine responsible for generating deterministic parcel polygons,
    /// handling back-to-back arrangements, asymmetric side counts, custom exceptions, and corner chamfers.
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

            // --- 1. GENERATE SIDE A PARCELS ---
            int countA = config.SideA.ParcelCount;
            for (int i = 1; i <= countA; i++)
            {
                double frontage = config.GetEffectiveFrontage(ParcelSide.SideA, i);
                double depth = config.GetEffectiveDepth(ParcelSide.SideA, i);
                bool isStartCorner = (i == 1) && config.Corner.HasChamfer && config.Corner.ApplyToStart;
                bool isEndCorner = (i == countA) && config.Corner.HasChamfer && config.Corner.ApplyToEnd;
                bool isCorner = isStartCorner || isEndCorner;

                var ring = BuildParcelRing(
                    originX: currentX,
                    originY: 0.0,
                    frontage: frontage,
                    depth: depth,
                    isSideA: true,
                    isStartCorner: isStartCorner,
                    isEndCorner: isEndCorner,
                    cornerConfig: config.Corner
                );

                double area = ComputePolygonArea(ring);

                var parcel = new ParcelModel
                {
                    Id = $"A-{i:D2}",
                    Side = ParcelSide.SideA,
                    Sequence = i,
                    Frontage = frontage,
                    Depth = depth,
                    Area = Math.Round(area, 2),
                    IsCorner = isCorner,
                    HasChamfer = isCorner,
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
                    bool isStartCorner = (i == 1) && config.Corner.HasChamfer && config.Corner.ApplyToStart;
                    bool isEndCorner = (i == countB) && config.Corner.HasChamfer && config.Corner.ApplyToEnd;
                    bool isCorner = isStartCorner || isEndCorner;

                    var ring = BuildParcelRing(
                        originX: currentX,
                        originY: 0.0,
                        frontage: frontage,
                        depth: depth,
                        isSideA: false,
                        isStartCorner: isStartCorner,
                        isEndCorner: isEndCorner,
                        cornerConfig: config.Corner
                    );

                    double area = ComputePolygonArea(ring);

                    var parcel = new ParcelModel
                    {
                        Id = $"B-{i:D2}",
                        Side = ParcelSide.SideB,
                        Sequence = i,
                        Frontage = frontage,
                        Depth = depth,
                        Area = Math.Round(area, 2),
                        IsCorner = isCorner,
                        HasChamfer = isCorner,
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

            // --- 3. OVERALL ESTIMATED BLOCK METRICS ---
            config.EstimatedBlockLength = Math.Max(sideATotalFrontage, sideBTotalFrontage);
            config.EstimatedBlockDepth = config.Arrangement == ArrangementMode.BackToBack
                ? config.BaseParcel.Depth * 2.0
                : config.BaseParcel.Depth;
            config.EstimatedBlockArea = Math.Round(sideATotalArea + sideBTotalArea, 2);
        }

        private List<Point2D> BuildParcelRing(
            double originX,
            double originY,
            double frontage,
            double depth,
            bool isSideA,
            bool isStartCorner,
            bool isEndCorner,
            CornerConfiguration cornerConfig)
        {
            var ring = new List<Point2D>();
            double chamfer = Math.Min(cornerConfig.ChamferLength, Math.Min(frontage * 0.4, depth * 0.4));

            if (isSideA)
            {
                // Side A: Spine at Y = 0, Street Frontage at Y = +depth
                double ySpine = originY;
                double yStreet = originY + depth;
                double xLeft = originX;
                double xRight = originX + frontage;

                // Point 1: Bottom-Left (Spine)
                ring.Add(new Point2D(xLeft, ySpine));

                // Point 2: Top-Left (Street) - Check for Start Chamfer
                if (isStartCorner)
                {
                    ring.Add(new Point2D(xLeft, yStreet - chamfer));
                    ring.Add(new Point2D(xLeft + chamfer, yStreet));
                }
                else
                {
                    ring.Add(new Point2D(xLeft, yStreet));
                }

                // Point 3: Top-Right (Street) - Check for End Chamfer
                if (isEndCorner)
                {
                    ring.Add(new Point2D(xRight - chamfer, yStreet));
                    ring.Add(new Point2D(xRight, yStreet - chamfer));
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

                // Point 3: Bottom-Right (Street) - Check for End Chamfer
                if (isEndCorner)
                {
                    ring.Add(new Point2D(xRight, yStreet + chamfer));
                    ring.Add(new Point2D(xRight - chamfer, yStreet));
                }
                else
                {
                    ring.Add(new Point2D(xRight, yStreet));
                }

                // Point 4: Bottom-Left (Street) - Check for Start Chamfer
                if (isStartCorner)
                {
                    ring.Add(new Point2D(xLeft + chamfer, yStreet));
                    ring.Add(new Point2D(xLeft, yStreet + chamfer));
                }
                else
                {
                    ring.Add(new Point2D(xLeft, yStreet));
                }
            }

            return ring;
        }

        private double ComputePolygonArea(List<Point2D> ring)
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

        private Point2D ComputeCentroid(List<Point2D> ring)
        {
            if (ring == null || ring.Count == 0) return new Point2D(0, 0);
            double sumX = ring.Sum(p => p.X);
            double sumY = ring.Sum(p => p.Y);
            return new Point2D(sumX / ring.Count, sumY / ring.Count);
        }
    }
}
