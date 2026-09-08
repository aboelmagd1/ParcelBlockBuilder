using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.AddIn.Services
{
    /// <summary>
    /// Enterprise preview service providing real-time, ephemeral MapView overlays
    /// with zero Table-of-Contents (TOC) pollution, dynamic highlighting, and smooth zoom window navigation.
    /// Visualizes transformed parcel polygons, the fixed Base Point anchor, and orientation reference vectors.
    /// </summary>
    public class ParcelPreviewService
    {
        private static readonly Lazy<ParcelPreviewService> _instance = new Lazy<ParcelPreviewService>(() => new ParcelPreviewService());
        public static ParcelPreviewService Instance => _instance.Value;

        private readonly List<IDisposable> _activeOverlays = new List<IDisposable>();
        private readonly object _lock = new object();

        private ParcelPreviewService() { }

        /// <summary>
        /// Clears all temporary graphic overlays from the active MapView.
        /// </summary>
        public async Task ClearPreviewAsync()
        {
            await QueuedTask.Run(() =>
            {
                lock (_lock)
                {
                    foreach (var handle in _activeOverlays)
                    {
                        handle?.Dispose();
                    }
                    _activeOverlays.Clear();
                }
            });
        }

        /// <summary>
        /// Updates the live map preview with current block configuration, highlights the selected parcel,
        /// and renders the Base Point anchor and orientation reference vector.
        /// </summary>
        public async Task UpdateMapPreviewAsync(BlockConfiguration config, string? selectedParcelId = null)
        {
            if (config == null)
            {
                await ClearPreviewAsync();
                return;
            }

            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null || mapView.Map == null) return;

                var spatialReference = mapView.Map.SpatialReference ?? SpatialReferences.WGS84;

                lock (_lock)
                {
                    foreach (var handle in _activeOverlays)
                    {
                        handle?.Dispose();
                    }
                    _activeOverlays.Clear();

                    var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).ToList();
                    if (allParcels.Count == 0) return;

                    // 1. Symbols definition
                    var sideASymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(41, 182, 246, 80), // #29B6F6 with 30% alpha
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(2, 136, 209), 1.5, SimpleLineStyle.Solid)
                    ).MakeSymbolReference();

                    var sideBSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(171, 71, 188, 80), // #AB47BC with 30% alpha
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(123, 31, 162), 1.5, SimpleLineStyle.Solid)
                    ).MakeSymbolReference();

                    var electricRoomSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(255, 145, 0, 140), // Glowing Amber with 55% alpha
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(255, 215, 64), 2.0, SimpleLineStyle.Solid)
                    ).MakeSymbolReference();

                    var highlightSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(0, 229, 255, 140), // Glowing Cyan with 55% alpha
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(255, 255, 255), 2.5, SimpleLineStyle.Solid)
                    ).MakeSymbolReference();

                    // 2. Draw Parcel Polygons & Centroid Labels
                    foreach (var parcel in allParcels)
                    {
                        if (parcel.PolygonRing.Count < 3) continue;

                        var points = parcel.PolygonRing.Select(p =>
                        {
                            var (gx, gy) = TransformPointToMap(p.X, p.Y, config);
                            return MapPointBuilderEx.CreateMapPoint(gx, gy, spatialReference);
                        }).ToList();
                        var polygon = PolygonBuilderEx.CreatePolygon(points, spatialReference);

                        bool isSelected = !string.IsNullOrEmpty(selectedParcelId) && parcel.Id == selectedParcelId;
                        bool isElectricRoom = parcel.Type == "Electric Room" || parcel.Id == "ER-01";
                        var symbolRef = isSelected ? highlightSymbol : (isElectricRoom ? electricRoomSymbol : (parcel.Side == ParcelSide.SideA ? sideASymbol : sideBSymbol));

                        var polygonOverlay = mapView.AddOverlay(polygon, symbolRef);
                        if (polygonOverlay != null)
                        {
                            _activeOverlays.Add(polygonOverlay);
                        }

                        // Centroid label overlay
                        var (gcx, gcy) = TransformPointToMap(parcel.Centroid.X, parcel.Centroid.Y, config);
                        var centerPt = MapPointBuilderEx.CreateMapPoint(gcx, gcy, spatialReference);
                        var labelTextSymbol = SymbolFactory.Instance.ConstructTextSymbol(
                            isSelected ? CIMColor.CreateRGBColor(0, 229, 255) : CIMColor.CreateRGBColor(255, 255, 255),
                            10.0,
                            "Segoe UI",
                            "Bold"
                        );
                        labelTextSymbol.HaloSize = 1.2;
                        labelTextSymbol.HaloSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(CIMColor.CreateRGBColor(11, 19, 43, 220));

                        var textOverlay = mapView.AddOverlay(centerPt, labelTextSymbol.MakeSymbolReference());
                        if (textOverlay != null)
                        {
                            _activeOverlays.Add(textOverlay);
                        }
                    }

                    // 3. Draw Fixed Base Point Anchor Marker
                    if (config.Alignment != null && config.Alignment.IsBasePointPlaced)
                    {
                        double targetX = config.Alignment.TargetMapPointX!.Value;
                        double targetY = config.Alignment.TargetMapPointY!.Value;
                        var baseMapPoint = MapPointBuilderEx.CreateMapPoint(targetX, targetY, spatialReference);

                        // Glowing Anchor Circle
                        var anchorPointSymbol = SymbolFactory.Instance.ConstructPointSymbol(
                            CIMColor.CreateRGBColor(0, 229, 255),
                            14.0,
                            SimpleMarkerStyle.Circle
                        );
                        var basePointOverlay = mapView.AddOverlay(baseMapPoint, anchorPointSymbol.MakeSymbolReference());
                        if (basePointOverlay != null)
                        {
                            _activeOverlays.Add(basePointOverlay);
                        }

                        // Anchor label
                        var anchorLabelSymbol = SymbolFactory.Instance.ConstructTextSymbol(
                            CIMColor.CreateRGBColor(0, 229, 255),
                            9.0,
                            "Segoe UI",
                            "Bold"
                        );
                        anchorLabelSymbol.HaloSize = 1.0;
                        anchorLabelSymbol.HaloSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(CIMColor.CreateRGBColor(11, 19, 43, 220));
                        anchorLabelSymbol.OffsetX = 12.0;
                        anchorLabelSymbol.OffsetY = 12.0;

                        var anchorLabelOverlay = mapView.AddOverlay(baseMapPoint, anchorLabelSymbol.MakeSymbolReference());
                        if (anchorLabelOverlay != null)
                        {
                            _activeOverlays.Add(anchorLabelOverlay);
                        }
                    }

                    // 4. Draw Orientation Reference Overlay
                    if (config.Alignment != null)
                    {
                        var al = config.Alignment;
                        if (al.Method == AlignmentMethod.TwoPoints &&
                            al.TwoPointStartX.HasValue && al.TwoPointStartY.HasValue &&
                            al.TwoPointEndX.HasValue && al.TwoPointEndY.HasValue)
                        {
                            var p1 = MapPointBuilderEx.CreateMapPoint(al.TwoPointStartX.Value, al.TwoPointStartY.Value, spatialReference);
                            var p2 = MapPointBuilderEx.CreateMapPoint(al.TwoPointEndX.Value, al.TwoPointEndY.Value, spatialReference);
                            var line = PolylineBuilderEx.CreatePolyline(new[] { p1, p2 }, spatialReference);

                            var lineSymbol = SymbolFactory.Instance.ConstructLineSymbol(
                                CIMColor.CreateRGBColor(255, 215, 64),
                                2.0,
                                SimpleLineStyle.Dash
                            );
                            var lineOverlay = mapView.AddOverlay(line, lineSymbol.MakeSymbolReference());
                            if (lineOverlay != null) _activeOverlays.Add(lineOverlay);
                        }
                        else if (al.Method == AlignmentMethod.MapSegment &&
                                 al.SegmentStartX.HasValue && al.SegmentStartY.HasValue &&
                                 al.SegmentEndX.HasValue && al.SegmentEndY.HasValue)
                        {
                            var s1 = MapPointBuilderEx.CreateMapPoint(al.SegmentStartX.Value, al.SegmentStartY.Value, spatialReference);
                            var s2 = MapPointBuilderEx.CreateMapPoint(al.SegmentEndX.Value, al.SegmentEndY.Value, spatialReference);
                            var segLine = PolylineBuilderEx.CreatePolyline(new[] { s1, s2 }, spatialReference);

                            var segLineSymbol = SymbolFactory.Instance.ConstructLineSymbol(
                                CIMColor.CreateRGBColor(0, 191, 165),
                                2.5,
                                SimpleLineStyle.Solid
                            );
                            var segOverlay = mapView.AddOverlay(segLine, segLineSymbol.MakeSymbolReference());
                            if (segOverlay != null) _activeOverlays.Add(segOverlay);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Highlights a specific parcel in the active overlay stack.
        /// </summary>
        public async Task HighlightParcelAsync(BlockConfiguration config, string? selectedParcelId)
        {
            await UpdateMapPreviewAsync(config, selectedParcelId);
        }

        /// <summary>
        /// Zooms the active MapView to fit the geometry of the specified parcel.
        /// </summary>
        public async Task ZoomToParcelAsync(BlockConfiguration config, string parcelId)
        {
            if (config == null || string.IsNullOrEmpty(parcelId)) return;

            var targetParcel = config.SideA.GeneratedParcels
                .Concat(config.SideB.GeneratedParcels)
                .FirstOrDefault(p => p.Id == parcelId);

            if (targetParcel == null || targetParcel.PolygonRing.Count < 3) return;

            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null || mapView.Map == null) return;

                var spatialReference = mapView.Map.SpatialReference ?? SpatialReferences.WGS84;
                var points = targetParcel.PolygonRing.Select(p =>
                {
                    var (gx, gy) = TransformPointToMap(p.X, p.Y, config);
                    return MapPointBuilderEx.CreateMapPoint(gx, gy, spatialReference);
                }).ToList();
                var polygon = PolygonBuilderEx.CreatePolygon(points, spatialReference);

                var expandedExtent = polygon.Extent.Expand(1.35, 1.35, true);
                mapView.ZoomTo(expandedExtent, TimeSpan.FromMilliseconds(350));
            });
        }

        /// <summary>
        /// Zooms the active MapView to the full extent of all generated block parcels.
        /// </summary>
        public async Task ZoomToBlockExtentAsync(BlockConfiguration config)
        {
            if (config == null) return;

            var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).ToList();
            if (allParcels.Count == 0) return;

            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null || mapView.Map == null) return;

                var spatialReference = mapView.Map.SpatialReference ?? SpatialReferences.WGS84;
                var allPoints = allParcels
                    .SelectMany(p => p.PolygonRing)
                    .Select(pt =>
                    {
                        var (gx, gy) = TransformPointToMap(pt.X, pt.Y, config);
                        return MapPointBuilderEx.CreateMapPoint(gx, gy, spatialReference);
                    })
                    .ToList();

                if (allPoints.Count < 3) return;

                var fullPolygon = PolygonBuilderEx.CreatePolygon(allPoints, spatialReference);
                var expandedExtent = fullPolygon.Extent.Expand(1.25, 1.25, true);
                mapView.ZoomTo(expandedExtent, TimeSpan.FromMilliseconds(400));
            });
        }

        /// <summary>
        /// Authoritative forward transformation delegating to BlockTransformationService.
        /// </summary>
        public static (double X, double Y) TransformPointToMap(double localX, double localY, BlockConfiguration? config)
        {
            return BlockTransformationService.TransformLocalToMap(localX, localY, config);
        }

        /// <summary>
        /// Smoothly zooms in on the active MapView.
        /// </summary>
        public async Task ZoomInAsync()
        {
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null) return;
                mapView.ZoomTo(mapView.Extent.Expand(0.75, 0.75, true), TimeSpan.FromMilliseconds(200));
            });
        }

        /// <summary>
        /// Smoothly zooms out on the active MapView.
        /// </summary>
        public async Task ZoomOutAsync()
        {
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null) return;
                mapView.ZoomTo(mapView.Extent.Expand(1.35, 1.35, true), TimeSpan.FromMilliseconds(200));
            });
        }
    }
}
