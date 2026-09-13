using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;
using Xunit;

namespace ParcelBuilder.Tests
{
    public class ParcelSplitMergeTests
    {
        private ParcelModel CreateStandardParcel(string id = "A-01", double x = 0, double y = 0, double frontage = 20.0, double depth = 30.0, bool hasChamfer = false, int seq = 1, ParcelSide side = ParcelSide.SideA, string type = "Standard")
        {
            var ring = new List<Point2D>
            {
                new Point2D(x, y),
                new Point2D(x, y + depth),
                new Point2D(x + frontage, y + depth),
                new Point2D(x + frontage, y)
            };

            return new ParcelModel
            {
                Id = id,
                Side = side,
                Sequence = seq,
                Frontage = frontage,
                Depth = Math.Abs(depth),
                Area = frontage * Math.Abs(depth),
                IsCorner = hasChamfer,
                HasChamfer = hasChamfer,
                HasStreetFrontage = true,
                Type = type,
                PolygonRing = ring,
                Centroid = new Point2D(x + frontage / 2.0, y + depth / 2.0)
            };
        }

        [Fact]
        public void SplitParcel_AlongFrontage_EqualSplit_ProducesTwoValidParcels()
        {
            var parcel = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);

            var (success, msg, chamferWarn, frontageWarn, p1, p2) = ParcelSplitService.Instance.SplitParcel(
                parcel,
                SplitDirection.AlongFrontage,
                SplitMethod.EqualSplit,
                10.0,
                3.0);

            Assert.True(success, msg);
            Assert.NotNull(p1);
            Assert.NotNull(p2);

            Assert.Equal("A-01A", p1.Id);
            Assert.Equal("A-01B", p2.Id);

            Assert.Equal(10.0, p1.Frontage);
            Assert.Equal(10.0, p2.Frontage);
            Assert.Equal(30.0, p1.Depth);
            Assert.Equal(30.0, p2.Depth);

            Assert.True(p1.HasStreetFrontage);
            Assert.True(p2.HasStreetFrontage);

            Assert.Equal(300.0, p1.Area);
            Assert.Equal(300.0, p2.Area);
            Assert.Equal(600.0, p1.Area + p2.Area);
        }

        [Fact]
        public void SplitParcel_AlongFrontage_CustomSplit_ConservesExactTotalArea()
        {
            var parcel = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);

            var (success, msg, chamferWarn, frontageWarn, p1, p2) = ParcelSplitService.Instance.SplitParcel(
                parcel,
                SplitDirection.AlongFrontage,
                SplitMethod.CustomSplit,
                8.0,
                3.0);

            Assert.True(success, msg);
            Assert.NotNull(p1);
            Assert.NotNull(p2);

            Assert.Equal(8.0, p1.Frontage);
            Assert.Equal(12.0, p2.Frontage);
            Assert.Equal(240.0, p1.Area);
            Assert.Equal(360.0, p2.Area);
            Assert.Equal(parcel.Area, p1.Area + p2.Area);
        }

        [Fact]
        public void SplitParcel_AlongDepth_SetsRearParcelNoFrontageAndWarns()
        {
            var parcel = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);

            var (success, msg, chamferWarn, frontageWarn, p1, p2) = ParcelSplitService.Instance.SplitParcel(
                parcel,
                SplitDirection.AlongDepth,
                SplitMethod.CustomSplit,
                10.0,
                3.0);

            Assert.True(success, msg);
            Assert.NotNull(p1);
            Assert.NotNull(p2);

            Assert.Equal(20.0, p1.Frontage);
            Assert.Equal(20.0, p2.Frontage);
            Assert.Equal(10.0, p1.Depth);
            Assert.Equal(20.0, p2.Depth);

            // In Side A, p1 is rear (spine side) and p2 is front (street facing)
            Assert.False(p1.HasStreetFrontage);
            Assert.True(p2.HasStreetFrontage);
            Assert.Contains("no direct street frontage", frontageWarn);

            Assert.Equal(200.0, p1.Area);
            Assert.Equal(400.0, p2.Area);
            Assert.Equal(parcel.Area, p1.Area + p2.Area);
        }

        [Fact]
        public void SplitParcel_BelowMinDimension_BlocksSplitWithoutClamping()
        {
            var parcel = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);

            // Cut at 2.0m with minDimensionThreshold = 3.0m
            var (success, msg, _, _, _, _) = ParcelSplitService.Instance.SplitParcel(
                parcel,
                SplitDirection.AlongFrontage,
                SplitMethod.CustomSplit,
                2.0,
                3.0);

            Assert.False(success);
            Assert.Contains("below the minimum allowed dimension", msg);
        }

        [Fact]
        public void SplitParcel_WithCornerChamfer_PreservesChamferOnlyOnCornerChild()
        {
            // Parcel A-01 is start corner (Seq = 1) with active chamfer
            var parcel = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0, hasChamfer: true, seq: 1);

            var (success, msg, chamferWarn, frontageWarn, p1, p2) = ParcelSplitService.Instance.SplitParcel(
                parcel,
                SplitDirection.AlongFrontage,
                SplitMethod.EqualSplit,
                10.0,
                3.0,
                totalSideParcelCount: 4);

            Assert.True(success, msg);
            Assert.NotNull(p1);
            Assert.NotNull(p2);

            // p1 is outer corner child -> keeps chamfer
            Assert.True(p1.HasChamfer);
            Assert.True(p1.IsCorner);

            // p2 is inner child -> chamfer cleared
            Assert.False(p2.HasChamfer);
            Assert.False(p2.IsCorner);

            Assert.Contains("cleared from inner parcel", chamferWarn);
        }

        [Fact]
        public void MergeParcels_AdjacentWithMatchingDepths_UnionsSuccessfully()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);
            var p2 = CreateStandardParcel("A-02", 20.0, 0, 15.0, 30.0);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);

            Assert.True(success, msg);
            Assert.NotNull(merged);
            Assert.Equal("A-01M", merged.Id);
            Assert.Equal(35.0, merged.Frontage);
            Assert.Equal(30.0, merged.Depth);
            Assert.Equal(1050.0, merged.Area); // (20 + 15) * 30 = 1050
            Assert.Equal("Modified", merged.Type);
        }

        [Fact]
        public void MergeParcels_MismatchedDepths_RejectsMergeWithoutAutoAveraging()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);
            var p2 = CreateStandardParcel("A-02", 20.0, 0, 15.0, 25.0); // Different depth!

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2);
            Assert.False(canMerge);
            Assert.Contains("Shared dimensions do not match", reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);
            Assert.False(success);
            Assert.Null(merged);
            Assert.Contains("Shared dimensions do not match", msg);
        }

        [Fact]
        public void MergeParcels_WithActiveChamfer_BlocksMerge()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0, hasChamfer: true);
            var p2 = CreateStandardParcel("A-02", 20.0, 0, 15.0, 30.0);

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2);
            Assert.False(canMerge);
            Assert.Contains("active corner chamfer", reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);
            Assert.False(success);
            Assert.Null(merged);
            Assert.Contains("active corner chamfer", msg);
        }

        [Fact]
        public void MergeParcels_DisjointParcels_RejectsWithClearError()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);
            var p2 = CreateStandardParcel("A-02", 50.0, 0, 20.0, 30.0); // Disjoint gap of 30m

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);

            Assert.False(success);
            Assert.Null(merged);
            Assert.Contains("do not share a common boundary", msg);
        }

        [Fact]
        public void MergeParcels_DifferentSides_WhenDisabled_BlocksMerge()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0, side: ParcelSide.SideA);
            var p2 = CreateStandardParcel("B-01", 0, 0, 20.0, -30.0, side: ParcelSide.SideB);

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2, allowDifferentSideMerge: false);
            Assert.False(canMerge);
            Assert.Contains("different sides", reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2, allowDifferentSideMerge: false);
            Assert.False(success);
            Assert.Null(merged);
            Assert.Contains("different sides", msg);
        }

        [Fact]
        public void MergeParcels_DifferentSides_WhenEnabled_MergesAcrossSpineSuccessfully()
        {
            var p1 = CreateStandardParcel("A-02", 20.0, 0, 20.0, 30.0, side: ParcelSide.SideA);
            var p2 = CreateStandardParcel("B-02", 20.0, 0, 20.0, -30.0, side: ParcelSide.SideB);

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2, allowDifferentSideMerge: true);
            Assert.True(canMerge, reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2, allowDifferentSideMerge: true);
            Assert.True(success, msg);
            Assert.NotNull(merged);
            Assert.Equal("A-02/B-02", merged.Id);
            Assert.Equal(ParcelSide.Both, merged.Side);
            Assert.Equal(1200.0, merged.Area);
            Assert.Equal(20.0, merged.Frontage);
            Assert.Equal(60.0, merged.Depth);
            Assert.Equal("Through Parcel", merged.Type);
        }

        [Fact]
        public void MergeParcels_ElectricRoom_BlocksMerge()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);
            var p2 = CreateStandardParcel("ER-01", 20.0, 0, 5.0, 30.0, type: "Electric Room");

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2);
            Assert.False(canMerge);
            Assert.Contains("Electric Room", reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);
            Assert.False(success);
            Assert.Null(merged);
            Assert.Contains("Electric Room", msg);
        }

        [Fact]
        public void MergeParcels_RemovesCollinearSeamVertices_YieldsCleanRectangle()
        {
            var p1 = CreateStandardParcel("A-01", 0, 0, 20.0, 30.0);
            var p2 = CreateStandardParcel("A-02", 20.0, 0, 15.0, 30.0);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);

            Assert.True(success, msg);
            Assert.NotNull(merged);
            Assert.Equal(4, merged.PolygonRing.Count); // Exactly 4 vertices, no 180° collinear seam points
            Assert.Equal(35.0, merged.Frontage);
            Assert.Equal(30.0, merged.Depth);
            Assert.Equal(1050.0, merged.Area);
        }

        [Fact]
        public void MergeParcels_SideBParcels_UnionsSuccessfully()
        {
            // Side B parcels with negative Y in local block coordinates
            var ring1 = new List<Point2D>
            {
                new Point2D(0, 0),
                new Point2D(20, 0),
                new Point2D(20, -30),
                new Point2D(0, -30)
            };
            var p1 = new ParcelModel
            {
                Id = "B-01",
                Side = ParcelSide.SideB,
                Sequence = 1,
                Frontage = 20.0,
                Depth = 30.0,
                Area = 600.0,
                PolygonRing = ring1,
                Centroid = new Point2D(10, -15)
            };

            var ring2 = new List<Point2D>
            {
                new Point2D(20, 0),
                new Point2D(35, 0),
                new Point2D(35, -30),
                new Point2D(20, -30)
            };
            var p2 = new ParcelModel
            {
                Id = "B-02",
                Side = ParcelSide.SideB,
                Sequence = 2,
                Frontage = 15.0,
                Depth = 30.0,
                Area = 450.0,
                PolygonRing = ring2,
                Centroid = new Point2D(27.5, -15)
            };

            var (canMerge, reason) = ParcelMergeService.Instance.CanMergeParcels(p1, p2);
            Assert.True(canMerge, reason);

            var (success, msg, merged) = ParcelMergeService.Instance.MergeParcels(p1, p2);
            Assert.True(success, msg);
            Assert.NotNull(merged);
            Assert.Equal(ParcelSide.SideB, merged.Side);
            Assert.Equal(35.0, merged.Frontage);
            Assert.Equal(30.0, merged.Depth);
            Assert.Equal(1050.0, merged.Area);
            Assert.Equal(4, merged.PolygonRing.Count);
        }

        [Fact]
        public void StateSync_ExceptionsPreserveSplitDimensions_WhenRegeneratingGeometry()
        {
            var config = new BlockConfiguration();
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.SideA.ParcelCount = 4;
            config.Arrangement = ArrangementMode.SingleSided;

            ParcelGeometryEngine.Instance.GenerateGeometry(config);
            Assert.Equal(4, config.SideA.GeneratedParcels.Count);

            // Simulate split of A-01 (20m into two 10m parcels)
            var (success, _, _, _, p1, p2) = ParcelSplitService.Instance.SplitParcel(
                config.SideA.GeneratedParcels[0],
                SplitDirection.AlongFrontage,
                SplitMethod.EqualSplit,
                10.0);
            Assert.True(success);

            config.SideA.GeneratedParcels.RemoveAt(0);
            config.SideA.GeneratedParcels.Insert(0, p2!);
            config.SideA.GeneratedParcels.Insert(0, p1!);

            // Renumber
            for (int i = 0; i < config.SideA.GeneratedParcels.Count; i++)
            {
                var p = config.SideA.GeneratedParcels[i];
                p.Sequence = i + 1;
                p.Id = $"A-{p.Sequence:D2}";
            }
            config.SideA.ParcelCount = config.SideA.GeneratedParcels.Count;

            // Synchronize exceptions from generated parcels
            foreach (var p in config.SideA.GeneratedParcels)
            {
                if (Math.Abs(p.Frontage - config.BaseParcel.Frontage) > 0.01 || p.IsModified)
                {
                    config.Exceptions.Add(new ParcelException
                    {
                        Side = p.Side,
                        Sequence = p.Sequence,
                        CustomFrontage = p.Frontage,
                        CustomDepth = p.Depth,
                        CustomType = p.Type
                    });
                }
            }

            Assert.Equal(2, config.Exceptions.Count);
            Assert.Equal(10.0, config.GetEffectiveFrontage(ParcelSide.SideA, 1));
            Assert.Equal(10.0, config.GetEffectiveFrontage(ParcelSide.SideA, 2));
            Assert.Equal(20.0, config.GetEffectiveFrontage(ParcelSide.SideA, 3));

            // Regenerate geometry using GenerateGeometry (as would happen if user changes Chamfer or Electric Room)
            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            Assert.Equal(5, config.SideA.GeneratedParcels.Count);
            Assert.Equal(10.0, config.SideA.GeneratedParcels[0].Frontage);
            Assert.Equal(10.0, config.SideA.GeneratedParcels[1].Frontage);
            Assert.Equal(20.0, config.SideA.GeneratedParcels[2].Frontage);
            Assert.Equal(20.0, config.SideA.GeneratedParcels[3].Frontage);
            Assert.Equal(20.0, config.SideA.GeneratedParcels[4].Frontage);
            Assert.Equal(80.0, config.SideA.TotalFrontage);
        }
    }
}
