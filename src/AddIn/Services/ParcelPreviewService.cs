using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.AddIn.Services
{
    /// <summary>
    /// Enterprise preview service providing real-time, ephemeral MapView overlays
    /// with zero Table-of-Contents (TOC) pollution, dynamic highlighting, and smooth zoom window navigation.
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
        /// Updates the live map preview with current block configuration and highlights the selected parcel.
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

                    // Symbols definition with SimpleFillStyle
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

                    var highlightSymbol = SymbolFactory.Instance.ConstructPolygonSymbol(
                        CIMColor.CreateRGBColor(0, 229, 255, 140), // Glowing Cyan with 55% alpha
                        SimpleFillStyle.Solid,
                        SymbolFactory.Instance.ConstructStroke(CIMColor.CreateRGBColor(255, 255, 255), 2.5, SimpleLineStyle.Solid)
                    ).MakeSymbolReference();

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
                        var symbolRef = isSelected ? highlightSymbol : (parcel.Side == ParcelSide.SideA ? sideASymbol : sideBSymbol);

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
        /// Spatial translation & azimuth rotation helper converting local coordinates to MapView coordinates using AnchorPoint.
        /// </summary>
        public static (double X, double Y) TransformPointToMap(double localX, double localY, BlockConfiguration? config)
        {
            if (config == null || config.Alignment == null ||
                (Math.Abs(config.Alignment.OriginX) < 1e-6 && Math.Abs(config.Alignment.OriginY) < 1e-6))
            {
                return (localX, localY);
            }

            var alignment = config.Alignment;
            double anchorX = 0.0;
            double anchorY = 0.0;

            double frontageA = config.SideA.TotalFrontage;
            double frontageB = config.SideB.TotalFrontage;
            double maxFrontage = Math.Max(frontageA, frontageB);

            double depthA = config.BaseParcel.Depth;
            double depthB = config.Arrangement == ArrangementMode.BackToBack ? -config.BaseParcel.Depth : 0.0;

            switch (alignment.AnchorPoint)
            {
                case BlockAnchorPoint.SideAStart:
                    anchorX = 0.0;
                    anchorY = depthA;
                    break;
                case BlockAnchorPoint.SideAEnd:
                    anchorX = frontageA;
                    anchorY = depthA;
                    break;
                case BlockAnchorPoint.SideACenter:
                    anchorX = frontageA / 2.0;
                    anchorY = depthA;
                    break;
                case BlockAnchorPoint.SideBStart:
                    anchorX = 0.0;
                    anchorY = depthB;
                    break;
                case BlockAnchorPoint.SideBEnd:
                    anchorX = frontageB;
                    anchorY = depthB;
                    break;
                case BlockAnchorPoint.BlockCenter:
                    anchorX = maxFrontage / 2.0;
                    anchorY = config.Arrangement == ArrangementMode.BackToBack ? 0.0 : depthA / 2.0;
                    break;
            }

            double relX = localX - anchorX;
            double relY = localY - anchorY;

            double angleRad = (90.0 - alignment.AzimuthAngleDegrees) * (Math.PI / 180.0);
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double rotX = (relX * cosA) - (relY * sinA);
            double rotY = (relX * sinA) + (relY * cosA);

            return (alignment.OriginX + rotX, alignment.OriginY + rotY);
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
