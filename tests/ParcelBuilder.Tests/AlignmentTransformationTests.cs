using System;
using System.Linq;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;
using Xunit;

namespace ParcelBuilder.Tests
{
    public class AlignmentTransformationTests
    {
        [Fact]
        public void BasePointPlacement_TransformsExactTargetCoordinate()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 5;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideAStart; // Local: (0, 30)
            config.Alignment.TargetMapPointX = 542381.25;
            config.Alignment.TargetMapPointY = 2754381.72;
            config.Alignment.AzimuthAngleDegrees = 90.0;

            var (srcX, srcY) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(0.0, srcX);
            Assert.Equal(30.0, srcY);

            // Transform the exact source base point
            var (mapX, mapY) = BlockTransformationService.TransformLocalToMap(srcX, srcY, config);
            Assert.Equal(542381.25, mapX, 2);
            Assert.Equal(2754381.72, mapY, 2);
        }

        [Theory]
        [InlineData(0.0)]    // North
        [InlineData(45.0)]   // North-East
        [InlineData(90.0)]   // East
        [InlineData(135.0)]  // South-East
        [InlineData(180.0)]  // South
        [InlineData(270.0)]  // West
        [InlineData(315.0)]  // North-West
        public void OrientationChange_PreservesBasePointAnchor(double testAzimuth)
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 6;
            config.BaseParcel.Frontage = 15.0;
            config.BaseParcel.Depth = 25.0;
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideACenter; // Midpoint
            config.Alignment.TargetMapPointX = 600000.00;
            config.Alignment.TargetMapPointY = 3000000.00;
            config.Alignment.AzimuthAngleDegrees = testAzimuth;

            var (srcX, srcY) = BlockTransformationService.GetSourceBasePoint(config);

            // The target map location of the base point MUST remain strictly invariant under rotation
            var (mapX, mapY) = BlockTransformationService.TransformLocalToMap(srcX, srcY, config);
            Assert.Equal(600000.00, mapX, 4);
            Assert.Equal(3000000.00, mapY, 4);
        }

        [Fact]
        public void TwoPointOrientation_CalculatesAccurateAzimuthAndReversal()
        {
            // Pointing East: (0, 0) -> (100, 0) -> Azimuth 90°
            double azEast = BlockTransformationService.CalculateAzimuth(0, 0, 100, 0);
            Assert.Equal(90.0, azEast);

            // Pointing North: (0, 0) -> (0, 100) -> Azimuth 0° (or 360°)
            double azNorth = BlockTransformationService.CalculateAzimuth(0, 0, 0, 100);
            Assert.Equal(0.0, azNorth);

            // Pointing West: (0, 0) -> (-100, 0) -> Azimuth 270°
            double azWest = BlockTransformationService.CalculateAzimuth(0, 0, -100, 0);
            Assert.Equal(270.0, azWest);

            // Pointing South: (0, 0) -> (0, -100) -> Azimuth 180°
            double azSouth = BlockTransformationService.CalculateAzimuth(0, 0, 0, -100);
            Assert.Equal(180.0, azSouth);

            // Verify 180° reversal between P1->P2 and P2->P1
            double azP1P2 = BlockTransformationService.CalculateAzimuth(10, 20, 50, 60);
            double azP2P1 = BlockTransformationService.CalculateAzimuth(50, 60, 10, 20);
            double diff = Math.Abs(azP1P2 - azP2P1);
            Assert.Equal(180.0, diff, 1);
        }

        [Fact]
        public void InverseTransformation_RoundtripIsExact()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 4;
            config.BaseParcel.Frontage = 18.0;
            config.BaseParcel.Depth = 28.0;
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideAStart;
            config.Alignment.TargetMapPointX = 450123.45;
            config.Alignment.TargetMapPointY = 2890123.67;
            config.Alignment.AzimuthAngleDegrees = 123.45;

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            foreach (var parcel in config.SideA.GeneratedParcels)
            {
                foreach (var pt in parcel.PolygonRing)
                {
                    var (mapX, mapY) = BlockTransformationService.TransformLocalToMap(pt.X, pt.Y, config);
                    var (reLocalX, reLocalY) = BlockTransformationService.TransformMapToLocal(mapX, mapY, config);

                    Assert.Equal(pt.X, reLocalX, 3);
                    Assert.Equal(pt.Y, reLocalY, 3);
                }
            }
        }

        [Fact]
        public void AllSixAnchorPoints_CalculateProperSchematicLocations()
        {
            var config = new BlockConfiguration();
            config.Arrangement = ArrangementMode.BackToBack;
            config.SideA.ParcelCount = 5; // 5 * 20 = 100m frontage
            config.SideB.ParcelCount = 5; // 5 * 20 = 100m frontage
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;

            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // 1. Side A Start (Front-Left): (0, 30)
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideAStart;
            var (x1, y1) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(0.0, x1);
            Assert.Equal(30.0, y1);

            // 2. Side A End (Front-Right): (100, 30)
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideAEnd;
            var (x2, y2) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(100.0, x2);
            Assert.Equal(30.0, y2);

            // 3. Side A Center: (50, 30)
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideACenter;
            var (x3, y3) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(50.0, x3);
            Assert.Equal(30.0, y3);

            // 4. Side B Start (Back-Left): (0, -30)
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideBStart;
            var (x4, y4) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(0.0, x4);
            Assert.Equal(-30.0, y4);

            // 5. Side B End (Back-Right): (100, -30)
            config.Alignment.AnchorPoint = BlockAnchorPoint.SideBEnd;
            var (x5, y5) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(100.0, x5);
            Assert.Equal(-30.0, y5);

            // 6. Block Center: (50, 0)
            config.Alignment.AnchorPoint = BlockAnchorPoint.BlockCenter;
            var (x6, y6) = BlockTransformationService.GetSourceBasePoint(config);
            Assert.Equal(50.0, x6);
            Assert.Equal(0.0, y6);
        }

        [Fact]
        public void ValidationEngine_DetectsMissingBasePointAndValidatesPass()
        {
            var config = new BlockConfiguration();
            config.SideA.ParcelCount = 4;
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;
            ParcelGeometryEngine.Instance.GenerateGeometry(config);

            // Initially TargetMapPoint is not set - test BlockTransformationService.ValidateAlignment directly
            var (isValid1, errors1, _, _) = BlockTransformationService.ValidateAlignment(config);
            Assert.False(isValid1);
            Assert.Contains(errors1, err => err.Contains("Target Base Point map location has not been selected"));

            // Set Base Point
            config.Alignment.TargetMapPointX = 500000.0;
            config.Alignment.TargetMapPointY = 2500000.0;
            var (result2, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(config);
            Assert.True(result2.IsValid);

            var alignRule = ruleItems.FirstOrDefault(r => r.RuleNumber == 12);
            Assert.NotNull(alignRule);
            Assert.True(alignRule.Passed);
        }
    }
}
