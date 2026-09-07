using System;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Configuration for block corners and chamfers.
    /// </summary>
    public class CornerConfiguration
    {
        public bool HasChamfer { get; set; } = true;
        public CornerType ChamferMethod { get; set; } = CornerType.ChamferByLength;

        /// <summary>
        /// Direct chamfer cut length along diagonal in meters (e.g. 5.0m).
        /// </summary>
        public double ChamferLength { get; set; } = 5.0;

        /// <summary>
        /// Setback along Street Frontage A in meters (for ChamferByFrontage).
        /// </summary>
        public double StreetASetback { get; set; } = 3.5;

        /// <summary>
        /// Setback along Cross Street B in meters (for ChamferByFrontage).
        /// </summary>
        public double StreetBSetback { get; set; } = 3.5;

        /// <summary>
        /// Apply chamfer to the start of the block (first parcels).
        /// </summary>
        public bool ApplyToStart { get; set; } = true;

        /// <summary>
        /// Apply chamfer to the end of the block (last parcels).
        /// </summary>
        public bool ApplyToEnd { get; set; } = true;

        public CornerConfiguration Clone()
        {
            return new CornerConfiguration
            {
                HasChamfer = this.HasChamfer,
                ChamferMethod = this.ChamferMethod,
                ChamferLength = this.ChamferLength,
                StreetASetback = this.StreetASetback,
                StreetBSetback = this.StreetBSetback,
                ApplyToStart = this.ApplyToStart,
                ApplyToEnd = this.ApplyToEnd
            };
        }
    }
}
