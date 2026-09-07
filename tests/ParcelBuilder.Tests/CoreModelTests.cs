using System;
using System.Linq;
using ParcelBuilder.Core.Models;
using Xunit;

namespace ParcelBuilder.Tests
{
    public class CoreModelTests
    {
        [Fact]
        public void DefaultBlockConfiguration_InitializesWithValidDefaults()
        {
            var config = new BlockConfiguration();

            Assert.Equal(SourceMode.Manual, config.SourceMode);
            Assert.Equal(ArrangementMode.BackToBack, config.Arrangement);
            Assert.Equal(7, config.SideA.ParcelCount);
            Assert.Equal(5, config.SideB.ParcelCount);
            Assert.Equal(20.0, config.BaseParcel.Frontage);
            Assert.Equal(30.0, config.BaseParcel.Depth);
            Assert.Equal(600.0, config.BaseParcel.Area);
            Assert.True(config.Corner.HasChamfer);
            Assert.Equal(5.0, config.Corner.ChamferLength);
        }

        [Fact]
        public void BlockConfiguration_Clone_CreatesDeepIndependentCopy()
        {
            var original = new BlockConfiguration
            {
                Name = "Master Block",
                SourceMode = SourceMode.ExtractedFromExistingParcel
            };
            original.SideA.ParcelCount = 10;
            original.Exceptions.Add(new ParcelException
            {
                Side = ParcelSide.SideA,
                Sequence = 2,
                CustomFrontage = 25.0
            });

            var clone = original.Clone();

            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(10, clone.SideA.ParcelCount);
            Assert.Single(clone.Exceptions);
            Assert.Equal(25.0, clone.Exceptions[0].CustomFrontage);

            // Modify clone to ensure independence
            clone.SideA.ParcelCount = 12;
            clone.Exceptions[0].CustomFrontage = 30.0;

            Assert.Equal(10, original.SideA.ParcelCount);
            Assert.Equal(25.0, original.Exceptions[0].CustomFrontage);
        }

        [Fact]
        public void EffectiveDimensions_HandlesExceptionsCorrectly()
        {
            var config = new BlockConfiguration();
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;

            config.Exceptions.Add(new ParcelException
            {
                Side = ParcelSide.SideA,
                Sequence = 3,
                CustomFrontage = 28.5,
                CustomDepth = 35.0
            });

            // Sequence 1 (Normal)
            Assert.Equal(20.0, config.GetEffectiveFrontage(ParcelSide.SideA, 1));
            Assert.Equal(30.0, config.GetEffectiveDepth(ParcelSide.SideA, 1));

            // Sequence 3 (Exception)
            Assert.Equal(28.5, config.GetEffectiveFrontage(ParcelSide.SideA, 3));
            Assert.Equal(35.0, config.GetEffectiveDepth(ParcelSide.SideA, 3));
        }

        [Fact]
        public void SourceMetadata_PreservesDetectedVsWorkingValues()
        {
            var meta = new SourceMetadata
            {
                Mode = SourceMode.ExtractedFromExistingParcel,
                SourceFeatureClass = "Parcels_Cadastre",
                SourceParcelId = 10423,
                Status = ExtractionStatus.Detected,
                ConfidenceScore = 0.95,
                DetectedFrontage = 20.14,
                DetectedDepth = 29.87,
                DetectedArea = 601.58
            };

            var config = new BlockConfiguration
            {
                SourceMode = SourceMode.ExtractedFromExistingParcel,
                Metadata = meta
            };
            // User rounds working values
            config.BaseParcel.Frontage = 20.0;
            config.BaseParcel.Depth = 30.0;

            Assert.Equal(20.14, config.Metadata.DetectedFrontage);
            Assert.Equal(29.87, config.Metadata.DetectedDepth);
            Assert.Equal(20.0, config.BaseParcel.Frontage);
            Assert.Equal(30.0, config.BaseParcel.Depth);
        }
    }
}
