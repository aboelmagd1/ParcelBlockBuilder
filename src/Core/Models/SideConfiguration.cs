using System;
using System.Collections.Generic;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Configuration and state for one side of a parcel block.
    /// </summary>
    public class SideConfiguration
    {
        public ParcelSide Side { get; set; } = ParcelSide.SideA;
        public string Name { get; set; } = "Side A";
        public string StreetLabel { get; set; } = "Street A";
        public bool IsReference { get; set; } = true;

        private int _parcelCount = 7;
        public int ParcelCount
        {
            get => _parcelCount;
            set => _parcelCount = Math.Max(1, value);
        }

        public List<ParcelModel> GeneratedParcels { get; set; } = new List<ParcelModel>();

        public double TotalFrontage { get; set; }
        public double TotalArea { get; set; }

        public SideConfiguration Clone()
        {
            var clone = new SideConfiguration
            {
                Side = this.Side,
                Name = this.Name,
                StreetLabel = this.StreetLabel,
                IsReference = this.IsReference,
                ParcelCount = this.ParcelCount,
                TotalFrontage = this.TotalFrontage,
                TotalArea = this.TotalArea,
                GeneratedParcels = new List<ParcelModel>()
            };

            foreach (var p in this.GeneratedParcels)
            {
                clone.GeneratedParcels.Add(p.Clone());
            }

            return clone;
        }
    }
}
