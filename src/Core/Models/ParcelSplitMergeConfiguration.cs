using System;
using System.Collections.Generic;

namespace ParcelBuilder.Core.Models
{
    public enum SplitMergeMode
    {
        SplitParcel,
        MergeParcels
    }

    public enum SplitDirection
    {
        AlongFrontage,
        AlongDepth
    }

    public enum SplitMethod
    {
        EqualSplit,
        CustomSplit
    }

    /// <summary>
    /// Configuration model for interactive parcel split and merge operations.
    /// Supports live preview, dimensional validation, and cancellation.
    /// </summary>
    public class ParcelSplitMergeConfiguration
    {
        public SplitMergeMode Mode { get; set; } = SplitMergeMode.SplitParcel;

        // --- SPLIT SETTINGS ---
        public string SelectedParcelId { get; set; } = string.Empty;
        public SplitDirection SplitDirection { get; set; } = SplitDirection.AlongFrontage;
        public SplitMethod SplitMethod { get; set; } = SplitMethod.EqualSplit;
        public double SplitPosition { get; set; } = 10.0;
        public double SplitPercentage { get; set; } = 50.0;
        public double MinDimensionThreshold { get; set; } = 3.0;
        public string ChamferWarningText { get; set; } = string.Empty;
        public string StreetFrontageWarningText { get; set; } = string.Empty;

        // Temporary preview result for Split
        public List<ParcelModel> PreviewSplitParcels { get; set; } = new List<ParcelModel>();

        // --- MERGE SETTINGS ---
        public string MergeParcelId1 { get; set; } = string.Empty;
        public string MergeParcelId2 { get; set; } = string.Empty;

        /// <summary>
        /// Allows merging back-to-back parcels across different street sides (Side A and Side B) sharing the spine.
        /// Defaults to true.
        /// </summary>
        public bool AllowDifferentSideMerge { get; set; } = true;

        // Temporary preview result for Merge
        public ParcelModel? PreviewMergedParcel { get; set; }

        // --- STATUS & VALIDATION ---
        public bool IsValid { get; set; } = false;
        public string StatusMessage { get; set; } = "Select a parcel to begin.";

        public void Reset()
        {
            SelectedParcelId = string.Empty;
            MergeParcelId1 = string.Empty;
            MergeParcelId2 = string.Empty;
            SplitPosition = 10.0;
            SplitPercentage = 50.0;
            ChamferWarningText = string.Empty;
            StreetFrontageWarningText = string.Empty;
            PreviewSplitParcels.Clear();
            PreviewMergedParcel = null;
            IsValid = false;
            StatusMessage = "Select a parcel to begin.";
        }

        public ParcelSplitMergeConfiguration Clone()
        {
            return new ParcelSplitMergeConfiguration
            {
                Mode = this.Mode,
                SelectedParcelId = this.SelectedParcelId,
                SplitDirection = this.SplitDirection,
                SplitMethod = this.SplitMethod,
                SplitPosition = this.SplitPosition,
                SplitPercentage = this.SplitPercentage,
                MinDimensionThreshold = this.MinDimensionThreshold,
                ChamferWarningText = this.ChamferWarningText,
                StreetFrontageWarningText = this.StreetFrontageWarningText,
                MergeParcelId1 = this.MergeParcelId1,
                MergeParcelId2 = this.MergeParcelId2,
                IsValid = this.IsValid,
                StatusMessage = this.StatusMessage,
                PreviewSplitParcels = new List<ParcelModel>(this.PreviewSplitParcels.ConvertAll(p => p.Clone())),
                PreviewMergedParcel = this.PreviewMergedParcel?.Clone()
            };
        }
    }
}
