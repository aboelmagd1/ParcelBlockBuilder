using System;
using System.Linq;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;
using Xunit;

namespace ParcelBuilder.Tests
{
    public class ElectricRoomGeometryTests
    {
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

            // 1. Verify Electric Room parcel created
            var er = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            Assert.Equal(2.50, er.Frontage);
            Assert.Equal(5.00, er.Depth);
            Assert.Equal(12.50, er.Area);
            Assert.Equal("Electric Room", er.Type);

            // 2. Verify Placement Type and Host Parcel
            Assert.True(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(ElectricRoomPlacementType.InsideSingleParcel, config.ElectricRoom.PlacementType);
            Assert.Single(config.ElectricRoom.HostParcelIds);
            Assert.Equal("A-01", config.ElectricRoom.HostParcelIds[0]);

            // 3. Verify Host Parcel 1 was carved (Original: 20*30 = 600m², Carved: 600 - 12.5 = 587.5 m²)
            var hostParcel = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "A-01");
            Assert.NotNull(hostParcel);
            Assert.Equal(587.5, hostParcel.Area, 1);
            Assert.True(hostParcel.IsModified);
        }

        [Fact]
        public void ElectricRoom_BetweenTwoParcels_DetectsBothHostsAndCarvesBoth()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5;
            config.SideB.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = false;

            // Parcel 1 spans [0, 20], Parcel 2 spans [20, 40].
            // ER width = 3.0m, offset = 18.5m -> spans [18.5, 21.5]
            // Overlap with Parcel 1: [18.5, 20.0] (1.5m * 5m = 7.5 m²)
            // Overlap with Parcel 2: [20.0, 21.5] (1.5m * 5m = 7.5 m²)
            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 3.00,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 18.5,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // 1. Verify single logical Electric Room parcel created
            var erParcels = config.SideA.GeneratedParcels.Where(p => p.Id == "ER-01").ToList();
            Assert.Single(erParcels);
            var er = erParcels[0];
            Assert.Equal(3.00, er.Frontage);
            Assert.Equal(5.00, er.Depth);
            Assert.Equal(15.00, er.Area);

            // 2. Verify Placement Type and Dual Host Parcels
            Assert.True(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(ElectricRoomPlacementType.BetweenTwoParcels, config.ElectricRoom.PlacementType);
            Assert.Equal(2, config.ElectricRoom.HostParcelIds.Count);
            Assert.Contains("A-01", config.ElectricRoom.HostParcelIds);
            Assert.Contains("A-02", config.ElectricRoom.HostParcelIds);

            // 3. Verify Both Host Parcels were cleanly carved
            var parcel1 = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "A-01");
            var parcel2 = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "A-02");
            Assert.NotNull(parcel1);
            Assert.NotNull(parcel2);

            Assert.Equal(592.5, parcel1.Area, 1); // 600 - 7.5 = 592.5 m²
            Assert.Equal(592.5, parcel2.Area, 1); // 600 - 7.5 = 592.5 m²
        }

        [Fact]
        public void ElectricRoom_OutOfBounds_PlacementRejected()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = false;

            // Total frontage = 100m. Offset = 99m, width = 2.5m -> spans [99, 101.5] (exceeds 100m)
            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 2.50,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 99.0
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            Assert.False(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(ElectricRoomPlacementType.InvalidOutsideBlock, config.ElectricRoom.PlacementType);
            Assert.Contains("extends outside the block boundary", config.ElectricRoom.ValidationStatusMessage);

            // Validation engine should also flag this as an error
            var (valResult, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);
            Assert.False(valResult.IsValid);
            Assert.Contains(ruleItems, i => i.RuleNumber == 11 && !i.Passed);
        }

        [Fact]
        public void ElectricRoom_SideB_OrientationAndPlacement()
        {
            var config = new BlockConfiguration();
            config.SideB.ParcelCount = 4;
            config.BaseParcel.Frontage = 25.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = false;

            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 4.00,
                Depth = 6.00,
                Side = ParcelSide.SideB,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 10.0,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            var er = config.SideB.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            Assert.Equal(ParcelSide.SideB, er.Side);
            Assert.Equal(24.00, er.Area); // 4 * 6 = 24 m²
            Assert.Single(config.ElectricRoom.HostParcelIds);
            Assert.Equal("B-01", config.ElectricRoom.HostParcelIds[0]);

            // Ring coordinates should be placed on negative Y (-30 to -24)
            Assert.All(er.PolygonRing, pt => Assert.True(pt.Y <= -23.99 && pt.Y >= -30.01));
        }

        [Fact]
        public void ElectricRoom_AreaCalculation_IsAccurate()
        {
            var er = new ElectricRoomConfiguration
            {
                Width = 2.50,
                Depth = 5.00
            };

            Assert.Equal(12.50, er.Area); // 2.50 * 5.00 = 12.50 m²

            er.Width = 3.50;
            er.Depth = 4.00;
            Assert.Equal(14.00, er.Area); // 3.50 * 4.00 = 14.00 m²
        }

        [Fact]
        public void ElectricRoom_DefaultConfiguration_HasZeroOffsetAndTopLeftAnchor()
        {
            var er = new ElectricRoomConfiguration();
            Assert.Equal(0.0, er.OffsetDistance);
            Assert.Equal(ElectricRoomAnchorPoint.TopLeft, er.RoomAnchor);
        }

        [Theory]
        [InlineData(ElectricRoomAnchorPoint.TopLeft, 20.0, 20.0, 22.5)]
        [InlineData(ElectricRoomAnchorPoint.Center, 20.0, 18.75, 21.25)]
        [InlineData(ElectricRoomAnchorPoint.TopRight, 20.0, 17.5, 20.0)]
        public void ElectricRoom_AnchorPoints_AlignCorrectly(ElectricRoomAnchorPoint anchor, double offset, double expectedX1, double expectedX2)
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
                OffsetDistance = offset,
                RoomAnchor = anchor,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            var er = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            double minX = er.PolygonRing.Min(pt => pt.X);
            double maxX = er.PolygonRing.Max(pt => pt.X);
            Assert.Equal(expectedX1, minX, 2);
            Assert.Equal(expectedX2, maxX, 2);
        }

        [Fact]
        public void ElectricRoom_WithCornerChamfer_DefaultOffsetZero_StartsAtStreetFrontageAfterChamfer()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5;
            config.SideB.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Corner.HasChamfer = true;
            config.Corner.IsCustomPerCorner = true;
            config.Corner.CornerAStart.IsEnabled = true;
            config.Corner.CornerAStart.Mode = ChamferMode.StreetSetbacks;
            config.Corner.CornerAStart.MainStreetSetback = 5.0;
            config.Corner.CornerAStart.CrossStreetSetback = 5.0;

            config.ElectricRoom = new ElectricRoomConfiguration
            {
                HasElectricRoom = true,
                Width = 2.50,
                Depth = 5.00,
                Side = ParcelSide.SideA,
                PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance,
                OffsetDistance = 0.0,
                ClipHostParcels = true
            };

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            var er = config.SideA.GeneratedParcels.FirstOrDefault(p => p.Id == "ER-01");
            Assert.NotNull(er);
            Assert.True(config.ElectricRoom.IsPlacementValid);
            Assert.Equal(5.0, er.PolygonRing.Min(pt => pt.X), 2);
            Assert.Equal(7.5, er.PolygonRing.Max(pt => pt.X), 2);
        }
    }
}
