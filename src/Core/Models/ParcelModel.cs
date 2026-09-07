using System;
using System.Collections.Generic;

namespace ParcelBuilder.Core.Models
{
    public struct Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X:F2}, {Y:F2})";
    }

    /// <summary>
    /// Represents an individual calculated or extracted parcel within the block structure.
    /// </summary>
    public class ParcelModel
    {
        public string Id { get; set; } = string.Empty;
        public ParcelSide Side { get; set; } = ParcelSide.SideA;
        public int Sequence { get; set; } = 1;
        public double Frontage { get; set; } = 20.0;
        public double Depth { get; set; } = 30.0;
        public double Area { get; set; } = 600.0;
        
        public bool IsCorner { get; set; }
        public bool HasChamfer { get; set; }
        public bool IsModified { get; set; }
        public string Type { get; set; } = "Standard";

        /// <summary>
        /// Boundary polygon vertices in local or projected map coordinates.
        /// </summary>
        public List<Point2D> PolygonRing { get; set; } = new List<Point2D>();

        /// <summary>
        /// Computed centroid for labeling and selection.
        /// </summary>
        public Point2D Centroid { get; set; }

        public ParcelModel Clone()
        {
            return new ParcelModel
            {
                Id = this.Id,
                Side = this.Side,
                Sequence = this.Sequence,
                Frontage = this.Frontage,
                Depth = this.Depth,
                Area = this.Area,
                IsCorner = this.IsCorner,
                HasChamfer = this.HasChamfer,
                IsModified = this.IsModified,
                Type = this.Type,
                Centroid = this.Centroid,
                PolygonRing = new List<Point2D>(this.PolygonRing)
            };
        }
    }
}
