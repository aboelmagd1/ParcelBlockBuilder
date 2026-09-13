using System;
using System.Linq;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;
using Xunit;

namespace ParcelBuilder.Tests
{
    public class GeometryAndValidationTests
    {
        [Fact]
        public void ChamferModes_ComputeAccurateCutDistances()
        {
            var corner = new CornerConfiguration
            {
                HasChamfer = true,
                Mode = ChamferMode.StreetSetbacks,
                StreetASetback = 4.0,
                StreetBSetback = 3.0
            };

            var (cutX, cutY) = corner.GetEffectiveCutDistances(20.0, 30.0);
            Assert.Equal(4.0, cutX);
            Assert.Equal(3.0, cutY);

            double diagonal = corner.GetCalculatedChamferLength(20.0, 30.0);
            Assert.Equal(5.0, diagonal); // 3-4-5 right triangle
        }

        [Fact]
        public void ElectricRoom_InsideSingleParcel_DetectsHostAndCarvesCleanly()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5;
            config.SideB.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = false;

            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 2.50,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 5.0,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Verify Electric Room was generated
            var er = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            Assert.Equal(2.50, er.Frontage);
            Assert.Equal(5.00, er.Depth);
            Assert.Equal(12.50, er.Area); // 2.50 * 5.00 = 12.50 m²
            Assert.Equal("Electric Room", er.Type);

            // Verify single host detection
            Assert.True(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(ElectricRoomPlacementType.InsideSingleParcel, config.ElectricRoom.PlacementType);
            Assert.Single(config.ElectricRoom.HostParcelIds);
            Assert.Equal("A-01", config.ElectricRoom.HostParcelIds[0]);

            // Verify Host parcel (A-01) was carved
            var hostParcel = config.SideA.GeneratedParcels.First(p => p.Id == "A-01");
            Assert.Equal(587.50, hostParcel.Area); // 600 - 12.50 = 587.50 m²
            Assert.True(hostParcel.PolygonRing.Count >= 6); // Carved polygon ring

            // Verify Topology Validation
            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ElectricRoom_BetweenTwoParcels_DetectsBothHostsAndCarvesBothCleanly()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5; // each 20m wide: A-01 is [0..20], A-02 is [20..40]
            config.SideB.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = false;

            // Place ER at offset 19.0m with width 2.5m -> spans [19.0 .. 21.5]
            // A-01 overlap is [19.0 .. 20.0] (1.0m width)
            // A-02 overlap is [20.0 .. 21.5] (1.5m width)
            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 2.50,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 19.0,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Verify Two-Parcel Host Detection
            Assert.True(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(ElectricRoomPlacementType.BetweenTwoParcels, config.ElectricRoom.PlacementType);
            Assert.Equal(2, config.ElectricRoom.HostParcelIds.Count);
            Assert.Contains("A-01", config.ElectricRoom.HostParcelIds);
            Assert.Contains("A-02", config.ElectricRoom.HostParcelIds);

            // Verify ER parcel is one single polygon
            var er = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            Assert.Equal(12.50, er.Area);

            // Verify A-01 carved by 1.0 * 5.0 = 5.0 m² -> 600 - 5 = 595 m²
            var p1 = config.SideA.GeneratedParcels.First(p => p.Id == "A-01");
            Assert.Equal(595.0, p1.Area);

            // Verify A-02 carved by 1.5 * 5.0 = 7.5 m² -> 600 - 7.5 = 592.5 m²
            var p2 = config.SideA.GeneratedParcels.First(p => p.Id == "A-02");
            Assert.Equal(592.50, p2.Area);

            // Verify total combined area
            double totalSideAArea = config.SideA.GeneratedParcels.Sum(p => p.Area);
            Assert.Equal(5 * 600.0, totalSideAArea); // Total remains 3000 m²

            // Verify Topology Validation
            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ElectricRoom_OutsideBlockBoundary_RejectsWithRequiredErrorMessage()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5; // Total frontage = 100m
            config.SideB.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;

            // Offset 99.0m with width 2.5m -> extends to 101.5m (outside 100m block!)
            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 2.50,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 99.0,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            Assert.False(config.ElectricRoom.IsPlacementValid);
            Assert.Equal("Electric Room cannot be placed here because the configured footprint extends outside the block boundary.", config.ElectricRoom.ValidationStatusMessage);
            Assert.Equal(ElectricRoomPlacementType.InvalidOutsideBlock, config.ElectricRoom.PlacementType);
            Assert.Empty(config.ElectricRoom.HostParcelIds);

            // Ensure no invalid ER parcel added
            Assert.DoesNotContain(config.SideA.GeneratedParcels, p => p.Id == "ER-01");

            // Verify Validation Engine catches out of bounds
            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);
            Assert.False(result.IsValid);
            Assert.Contains(result.Messages, m => m.Message.Contains("extends outside the block boundary"));
        }

        [Fact]
        public void TopologyValidation_ChecksAll10RulesSuccessfully()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 6;
            config.SideB.ParcelCount = 6;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);

            Assert.Equal(10, ruleItems.Count);
            Assert.True(ruleItems.All(r => r.Passed));
            Assert.True(result.IsValid);

            // Verify RuleCheckItem visual helper properties
            foreach (var item in ruleItems)
            {
                Assert.Equal("✓", item.StatusIcon);
                Assert.Equal("Passed", item.StatusText);
                Assert.Equal("#00E5A3", item.StatusColor);
                Assert.Equal("#0C152E", item.CardBackground);
                Assert.False(string.IsNullOrWhiteSpace(item.Details));
            }
        }

        [Fact]
        public void TopologyValidation_DetectsDuplicateParcelIds()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 2;
            config.SideB.ParcelCount = 0;
            config.Arrangement = ArrangementMode.SingleSided;
            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Force duplicate ID
            config.SideA.GeneratedParcels[1].Id = config.SideA.GeneratedParcels[0].Id;

            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);

            var dupRule = ruleItems.FirstOrDefault(r => r.RuleNumber == 3);
            Assert.NotNull(dupRule);
            Assert.False(dupRule.Passed);
            Assert.Equal("✕", dupRule.StatusIcon);
            Assert.Equal("Failed", dupRule.StatusText);
            Assert.Equal("#FF5252", dupRule.StatusColor);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void TopologyValidation_DetectsInvalidGeometryDegenerateArea()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 2;
            config.SideB.ParcelCount = 0;
            config.Arrangement = ArrangementMode.SingleSided;
            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Force degenerate parcel
            config.SideA.GeneratedParcels[0].PolygonRing.Clear();

            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);

            var geomRule = ruleItems.FirstOrDefault(r => r.RuleNumber == 1);
            Assert.NotNull(geomRule);
            Assert.False(geomRule.Passed);
            Assert.Equal("✕", geomRule.StatusIcon);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void TopologyValidation_IncludesAlignmentRule_WhenAnchorPlaced()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 4;
            config.SideB.ParcelCount = 4;
            config.Alignment = new AlignmentConfiguration
            {
                TargetMapPointX = 500000.0,
                TargetMapPointY = 2700000.0,
                AzimuthAngleDegrees = 45.0
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            var (result, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);

            Assert.Equal(11, ruleItems.Count); // 10 Cadastre + 1 Alignment
            var alignRule = ruleItems.FirstOrDefault(r => r.RuleNumber == 12);
            Assert.NotNull(alignRule);
            Assert.True(alignRule.Passed);
            Assert.Contains("45.0°", alignRule.Details);
        }

        [Fact]
        public void ParcelDimensionOverride_ModifiesOnlyTargetParcel_LeavesOtherParcelsUntouched()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 4;
            config.SideB.ParcelCount = 4;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Arrangement = ArrangementMode.BackToBack;
            config.Corner.HasChamfer = false;

            // Apply custom override to A-02 only: Frontage 35m, Depth 40m
            config.Exceptions.Add(new ParcelException
            {
                Side = ParcelSide.SideA,
                Sequence = 2,
                CustomFrontage = 35.0,
                CustomDepth = 40.0,
                CustomType = "Modified"
            });

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Verify Side A parcels
            var a01 = config.SideA.GeneratedParcels.First(p => p.Id == "A-01");
            var a02 = config.SideA.GeneratedParcels.First(p => p.Id == "A-02");
            var a03 = config.SideA.GeneratedParcels.First(p => p.Id == "A-03");
            var a04 = config.SideA.GeneratedParcels.First(p => p.Id == "A-04");

            // A-01 must remain strictly base dimensions
            Assert.Equal(20.0, a01.Frontage);
            Assert.Equal(30.0, a01.Depth);
            Assert.Equal(600.0, a01.Area);
            Assert.False(a01.IsModified);

            // A-02 must reflect the exact customized dimensions
            Assert.Equal(35.0, a02.Frontage);
            Assert.Equal(40.0, a02.Depth);
            Assert.Equal(1400.0, a02.Area);
            Assert.True(a02.IsModified);

            // A-03 and A-04 must remain strictly base dimensions
            Assert.Equal(20.0, a03.Frontage);
            Assert.Equal(30.0, a03.Depth);
            Assert.Equal(600.0, a03.Area);
            Assert.False(a03.IsModified);

            Assert.Equal(20.0, a04.Frontage);
            Assert.Equal(30.0, a04.Depth);
            Assert.Equal(600.0, a04.Area);
            Assert.False(a04.IsModified);

            // All Side B parcels must remain strictly base dimensions
            foreach (var p in config.SideB.GeneratedParcels)
            {
                Assert.Equal(20.0, p.Frontage);
                Assert.Equal(30.0, p.Depth);
                Assert.Equal(600.0, p.Area);
                Assert.False(p.IsModified);
            }
        }

        [Fact]
        public void ParcelDimensionOverride_SideBTarget_DoesNotAffectSideA()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 3;
            config.SideB.ParcelCount = 3;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Arrangement = ArrangementMode.BackToBack;
            config.Corner.HasChamfer = false;

            // Apply custom override to B-02 only: Frontage 28m
            config.Exceptions.Add(new ParcelException
            {
                Side = ParcelSide.SideB,
                Sequence = 2,
                CustomFrontage = 28.0,
                CustomType = "Modified"
            });

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Side A must be entirely unchanged
            foreach (var p in config.SideA.GeneratedParcels)
            {
                Assert.Equal(20.0, p.Frontage);
                Assert.Equal(30.0, p.Depth);
                Assert.Equal(600.0, p.Area);
                Assert.False(p.IsModified);
            }

            // Side B: B-01 and B-03 are 20m, B-02 is 28m
            var b01 = config.SideB.GeneratedParcels.First(p => p.Id == "B-01");
            var b02 = config.SideB.GeneratedParcels.First(p => p.Id == "B-02");
            var b03 = config.SideB.GeneratedParcels.First(p => p.Id == "B-03");

            Assert.Equal(20.0, b01.Frontage);
            Assert.False(b01.IsModified);

            Assert.Equal(28.0, b02.Frontage);
            Assert.Equal(840.0, b02.Area);
            Assert.True(b02.IsModified);

            Assert.Equal(20.0, b03.Frontage);
            Assert.False(b03.IsModified);
        }
    }
}
