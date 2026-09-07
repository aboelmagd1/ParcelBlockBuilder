using System;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Represents the standard base parcel dimensions that define the template for the block.
    /// Distinguishes between Detected values from GIS extraction and Working values configured by the user.
    /// </summary>
    public class BaseParcel
    {
        private double _frontage = 20.0;
        private double _depth = 30.0;

        /// <summary>
        /// Working Frontage width in meters.
        /// </summary>
        public double Frontage
        {
            get => _frontage;
            set => _frontage = Math.Max(0.01, value);
        }

        /// <summary>
        /// Working Depth length in meters.
        /// </summary>
        public double Depth
        {
            get => _depth;
            set => _depth = Math.Max(0.01, value);
        }

        /// <summary>
        /// Raw Detected Frontage from GIS analysis.
        /// </summary>
        public double DetectedFrontage { get; set; } = 20.0;

        /// <summary>
        /// Raw Detected Depth from GIS analysis.
        /// </summary>
        public double DetectedDepth { get; set; } = 30.0;

        /// <summary>
        /// Calculated Area in square meters (Frontage * Depth for rectangular base).
        /// </summary>
        public double Area => Math.Round(Frontage * Depth, 2);

        public BaseParcel Clone()
        {
            return new BaseParcel
            {
                Frontage = this.Frontage,
                Depth = this.Depth,
                DetectedFrontage = this.DetectedFrontage,
                DetectedDepth = this.DetectedDepth
            };
        }
    }
}
