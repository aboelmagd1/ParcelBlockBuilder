using System;
using System.Collections.Generic;
using System.Linq;

namespace ParcelBuilder.Core.Models
{
    /// <summary>
    /// Configuration for the Electric Room / Substation (غرفة المحول / الكهرباء)
    /// placed along the street frontage and carved/clipped from the host parcel(s).
    /// </summary>
    public class ElectricRoomConfiguration
    {
        public bool HasElectricRoom { get; set; } = false;

        /// <summary>
        /// Synonym for HasElectricRoom to match configuration naming conventions.
        /// </summary>
        public bool Enabled
        {
            get => HasElectricRoom;
            set => HasElectricRoom = value;
        }

        /// <summary>
        /// Width along the street frontage in meters (العرض على الشارع) (e.g. 2.50m).
        /// </summary>
        public double Width { get; set; } = 2.50;

        /// <summary>
        /// Depth extending into the parcel in meters (العمق داخل القطعة) (e.g. 5.00m).
        /// </summary>
        public double Depth { get; set; } = 5.00;

        /// <summary>
        /// Calculated area of electric room (Width * Depth) in square meters.
        /// </summary>
        public double Area => Math.Round(Width * Depth, 2);

        /// <summary>
        /// Street side where the electric room is situated (Side A or Side B).
        /// Controls orientation and inward vector.
        /// </summary>
        public ParcelSide Side { get; set; } = ParcelSide.SideA;

        /// <summary>
        /// Synonym for Side.
        /// </summary>
        public ParcelSide PlacementSide
        {
            get => Side;
            set => Side = value;
        }

        /// <summary>
        /// Primary placement method: interactive map click or frontage offset distance.
        /// </summary>
        public ElectricRoomPlacementMethod PlacementMethod { get; set; } = ElectricRoomPlacementMethod.InteractiveMapPlacement;

        /// <summary>
        /// Relationship to host parcels (inside a single parcel or spanning two adjacent parcels).
        /// </summary>
        public ElectricRoomPlacementType PlacementType { get; set; } = ElectricRoomPlacementType.InsideSingleParcel;

        /// <summary>
        /// Offset in meters from the start of the block frontage along the street.
        /// </summary>
        public double OffsetDistance { get; set; } = 10.0;

        /// <summary>
        /// Clicked map point X coordinate in spatial reference (if placed via map interaction).
        /// </summary>
        public double? ClickedMapX { get; set; }

        /// <summary>
        /// Clicked map point Y coordinate in spatial reference (if placed via map interaction).
        /// </summary>
        public double? ClickedMapY { get; set; }

        /// <summary>
        /// Optional anchor point along the frontage segment.
        /// </summary>
        public Point2D? AnchorPoint { get; set; }

        /// <summary>
        /// Detected host parcel identifiers (e.g. ["A-05"] or ["A-05", "A-06"]).
        /// </summary>
        public List<string> HostParcelIds { get; set; } = new List<string>();

        /// <summary>
        /// If true, host parcel geometry is automatically carved / clipped around the electric room polygon.
        /// </summary>
        public bool ClipHostParcels { get; set; } = true;

        /// <summary>
        /// Generated local 2D polygon ring coordinates for the electric room footprint.
        /// </summary>
        public List<Point2D> PolygonRing { get; set; } = new List<Point2D>();

        /// <summary>
        /// Synonym for PolygonRing.
        /// </summary>
        public List<Point2D> Geometry
        {
            get => PolygonRing;
            set => PolygonRing = value;
        }

        /// <summary>
        /// Whether the current placement is topologically and geometrically valid and completely inside block boundary.
        /// </summary>
        public bool IsPlacementValid { get; set; } = true;

        /// <summary>
        /// Detailed validation / placement status message.
        /// </summary>
        public string ValidationStatusMessage { get; set; } = "Ready for placement";

        public string ValidationStatus
        {
            get => ValidationStatusMessage;
            set => ValidationStatusMessage = value;
        }

        public ElectricRoomConfiguration Clone()
        {
            return new ElectricRoomConfiguration
            {
                HasElectricRoom = this.HasElectricRoom,
                Width = this.Width,
                Depth = this.Depth,
                Side = this.Side,
                PlacementMethod = this.PlacementMethod,
                PlacementType = this.PlacementType,
                OffsetDistance = this.OffsetDistance,
                ClickedMapX = this.ClickedMapX,
                ClickedMapY = this.ClickedMapY,
                AnchorPoint = this.AnchorPoint.HasValue ? new Point2D(this.AnchorPoint.Value.X, this.AnchorPoint.Value.Y) : null,
                HostParcelIds = new List<string>(this.HostParcelIds ?? new List<string>()),
                ClipHostParcels = this.ClipHostParcels,
                PolygonRing = this.PolygonRing?.Select(p => new Point2D(p.X, p.Y)).ToList() ?? new List<Point2D>(),
                IsPlacementValid = this.IsPlacementValid,
                ValidationStatusMessage = this.ValidationStatusMessage
            };
        }
    }
}
