using System;
using System.Collections.Generic;
using System.Linq;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Master aggregate root and single source of truth for Parcel Builder.
    /// Drives GUI, Schematic Canvas, GIS Map Preview, Geometry Engine, Validation, and Feature Class Output.
    /// </summary>
    public class BlockConfiguration
    {
        public string ConfigurationId { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "New Parcel Block";
        
        // Workflow Mode & GIS Audit Metadata
        public SourceMode SourceMode { get; set; } = SourceMode.Manual;
        public SourceMetadata Metadata { get; set; } = new SourceMetadata();

        // Architectural Relationships
        public RelationshipType Relationship { get; set; } = RelationshipType.PartOfBlock;
        public ArrangementMode Arrangement { get; set; } = ArrangementMode.BackToBack;
        public SimilarityMode Similarity { get; set; } = SimilarityMode.AllIdentical;

        // Base Template Parcel
        public BaseParcel BaseParcel { get; set; } = new BaseParcel();

        // Sides Configuration (Dual sides for BackToBack, Single side for SingleSided)
        public SideConfiguration SideA { get; set; } = new SideConfiguration
        {
            Side = ParcelSide.SideA,
            Name = "Side A",
            StreetLabel = "Street A",
            IsReference = true,
            ParcelCount = 7
        };

        public SideConfiguration SideB { get; set; } = new SideConfiguration
        {
            Side = ParcelSide.SideB,
            Name = "Side B",
            StreetLabel = "Street B",
            IsReference = false,
            ParcelCount = 5
        };

        // Per-Parcel Overrides / Exceptions
        public List<ParcelException> Exceptions { get; set; } = new List<ParcelException>();

        // Corners, Chamfers & Alignment Settings
        public CornerConfiguration Corner { get; set; } = new CornerConfiguration();
        public AlignmentConfiguration Alignment { get; set; } = new AlignmentConfiguration();

        // Computed Dimensions Summary
        public double EstimatedBlockLength { get; set; }
        public double EstimatedBlockDepth { get; set; }
        public double EstimatedBlockArea { get; set; }

        public BlockConfiguration Clone()
        {
            var clone = new BlockConfiguration
            {
                ConfigurationId = this.ConfigurationId,
                Name = this.Name,
                SourceMode = this.SourceMode,
                Metadata = this.Metadata?.Clone() ?? new SourceMetadata(),
                Relationship = this.Relationship,
                Arrangement = this.Arrangement,
                Similarity = this.Similarity,
                BaseParcel = this.BaseParcel?.Clone() ?? new BaseParcel(),
                SideA = this.SideA?.Clone() ?? new SideConfiguration(),
                SideB = this.SideB?.Clone() ?? new SideConfiguration(),
                Corner = this.Corner?.Clone() ?? new CornerConfiguration(),
                Alignment = this.Alignment?.Clone() ?? new AlignmentConfiguration(),
                EstimatedBlockLength = this.EstimatedBlockLength,
                EstimatedBlockDepth = this.EstimatedBlockDepth,
                EstimatedBlockArea = this.EstimatedBlockArea,
                Exceptions = this.Exceptions?.Select(e => e.Clone()).ToList() ?? new List<ParcelException>()
            };

            return clone;
        }

        /// <summary>
        /// Retrieves effective frontage for a given side and 1-based sequence index, factoring in exceptions.
        /// </summary>
        public double GetEffectiveFrontage(ParcelSide side, int sequence)
        {
            var ex = Exceptions.FirstOrDefault(e => e.Side == side && e.Sequence == sequence);
            return (ex != null && ex.CustomFrontage.HasValue) ? ex.CustomFrontage.Value : BaseParcel.Frontage;
        }

        /// <summary>
        /// Retrieves effective depth for a given side and 1-based sequence index, factoring in exceptions.
        /// </summary>
        public double GetEffectiveDepth(ParcelSide side, int sequence)
        {
            var ex = Exceptions.FirstOrDefault(e => e.Side == side && e.Sequence == sequence);
            return (ex != null && ex.CustomDepth.HasValue) ? ex.CustomDepth.Value : BaseParcel.Depth;
        }
    }
}
