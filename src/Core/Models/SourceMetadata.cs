using System;
using System.Collections.Generic;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Preserves the raw GIS extraction audit trail, confidence metrics, and detected vs working comparisons.
    /// </summary>
    public class SourceMetadata
    {
        public SourceMode Mode { get; set; } = SourceMode.Manual;
        public string SourceFeatureClass { get; set; } = string.Empty;
        public string SourceLayer { get; set; } = string.Empty;
        public long? SourceParcelId { get; set; }
        public string SourceGlobalId { get; set; } = string.Empty;
        
        public ExtractionStatus Status { get; set; } = ExtractionStatus.NotExtracted;
        public double ConfidenceScore { get; set; } = 1.0; // 0.0 to 1.0
        public DateTime? ExtractionTimestamp { get; set; }

        // Raw detected dimensions (Immutable snapshot from source GIS)
        public double? DetectedFrontage { get; set; }
        public double? DetectedDepth { get; set; }
        public double? DetectedArea { get; set; }
        public int? DetectedSideACount { get; set; }
        public int? DetectedSideBCount { get; set; }
        public bool? DetectedCornerChamfer { get; set; }
        public double? DetectedChamferLength { get; set; }

        // Ambiguities and warnings discovered during inference
        public List<string> ExtractionWarnings { get; set; } = new List<string>();
        public List<string> AmbiguousProperties { get; set; } = new List<string>();

        public SourceMetadata Clone()
        {
            return new SourceMetadata
            {
                Mode = this.Mode,
                SourceFeatureClass = this.SourceFeatureClass,
                SourceLayer = this.SourceLayer,
                SourceParcelId = this.SourceParcelId,
                SourceGlobalId = this.SourceGlobalId,
                Status = this.Status,
                ConfidenceScore = this.ConfidenceScore,
                ExtractionTimestamp = this.ExtractionTimestamp,
                DetectedFrontage = this.DetectedFrontage,
                DetectedDepth = this.DetectedDepth,
                DetectedArea = this.DetectedArea,
                DetectedSideACount = this.DetectedSideACount,
                DetectedSideBCount = this.DetectedSideBCount,
                DetectedCornerChamfer = this.DetectedCornerChamfer,
                DetectedChamferLength = this.DetectedChamferLength,
                ExtractionWarnings = new List<string>(this.ExtractionWarnings),
                AmbiguousProperties = new List<string>(this.AmbiguousProperties)
            };
        }
    }
}
