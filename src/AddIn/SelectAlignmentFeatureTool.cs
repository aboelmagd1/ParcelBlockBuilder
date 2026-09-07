using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// Interactive map tool allowing the user to click any Line feature or Polygon boundary segment on the map
    /// to extract and snap the alignment baseline vector and azimuth angle without crashing.
    /// </summary>
    internal class SelectAlignmentFeatureTool : MapTool
    {
        public SelectAlignmentFeatureTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Point;
            SketchOutputMode = SketchOutputMode.Map;
            UseSnapping = true;
        }

        protected override Task OnToolActivateAsync(bool hasMapViewChanged)
        {
            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;
            if (dockPane != null)
            {
                dockPane.StatusText = "Click any line feature or polygon boundary edge on the map...";
            }
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint clickPoint) return false;

            await ProcessFeatureSelectionAsync(clickPoint);
            return true;
        }

        private async Task ProcessFeatureSelectionAsync(MapPoint clickPoint)
        {
            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;

            try
            {
                await QueuedTask.Run(() =>
                {
                    var mapView = MapView.Active;
                    if (mapView?.Map == null) return;

                    var mapSr = mapView.Map.SpatialReference ?? SpatialReferences.WGS84;
                    var projectedClick = GeometryEngine.Instance.Project(clickPoint, mapSr) as MapPoint ?? clickPoint;

                    // Calculate a reasonable search tolerance based on current view extent
                    double extentWidth = mapView.Extent != null ? Math.Abs(mapView.Extent.Width) : 100.0;
                    double searchTolerance = Math.Max(0.0001, extentWidth * 0.03);

                    Geometry? searchGeom = null;
                    try
                    {
                        searchGeom = GeometryEngine.Instance.Buffer(projectedClick, searchTolerance);
                    }
                    catch
                    {
                        // Fallback envelope if buffer fails
                        searchGeom = EnvelopeBuilderEx.CreateEnvelope(
                            projectedClick.X - searchTolerance,
                            projectedClick.Y - searchTolerance,
                            projectedClick.X + searchTolerance,
                            projectedClick.Y + searchTolerance,
                            mapSr);
                    }

                    if (searchGeom == null) return;

                    MapPoint? bestStart = null;
                    MapPoint? bestEnd = null;
                    double minDistance = double.MaxValue;
                    string featureDesc = "Map Feature Edge";

                    // Retrieve only valid visible FeatureLayers that have Polyline or Polygon geometries
                    var layers = mapView.Map.GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .Where(l => l.IsVisible && (l.ShapeType == esriGeometryType.esriGeometryPolyline || l.ShapeType == esriGeometryType.esriGeometryPolygon))
                        .ToList();

                    foreach (var layer in layers)
                    {
                        try
                        {
                            var spatialFilter = new SpatialQueryFilter
                            {
                                FilterGeometry = searchGeom,
                                SpatialRelationship = SpatialRelationship.Intersects
                            };

                            using var rowCursor = layer.Search(spatialFilter);
                            if (rowCursor == null) continue;

                            while (rowCursor.MoveNext())
                            {
                                using var feature = rowCursor.Current as Feature;
                                if (feature == null) continue;

                                Geometry? rawGeom = null;
                                try
                                {
                                    rawGeom = feature.GetShape();
                                }
                                catch
                                {
                                    continue;
                                }

                                if (rawGeom == null || rawGeom.IsEmpty) continue;

                                // Always project feature geometry to Map Spatial Reference for consistent distance calculations
                                var geom = GeometryEngine.Instance.Project(rawGeom, mapSr);
                                if (geom == null || geom.IsEmpty) continue;

                                if (geom is Polyline polyline)
                                {
                                    ProcessPolylineSegments(polyline, projectedClick, mapSr, layer.Name, ref bestStart, ref bestEnd, ref minDistance, ref featureDesc);
                                }
                                else if (geom is Polygon polygon)
                                {
                                    ProcessPolygonSegments(polygon, projectedClick, mapSr, layer.Name, ref bestStart, ref bestEnd, ref minDistance, ref featureDesc);
                                }
                            }
                        }
                        catch
                        {
                            // Skip layer gracefully if locked, remote, or inaccessible
                            continue;
                        }
                    }

                    if (bestStart != null && bestEnd != null)
                    {
                        if (dockPane != null)
                        {
                            dockPane.Activate();
                            dockPane.SetAlignmentLine(bestStart.X, bestStart.Y, bestEnd.X, bestEnd.Y, featureDesc);
                        }
                    }
                    else
                    {
                        if (dockPane != null)
                        {
                            dockPane.StatusText = "No line or polygon edge found within click tolerance. Try clicking closer to a boundary.";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                if (dockPane != null)
                {
                    dockPane.StatusText = $"Alignment selection notice: {ex.Message}";
                }
            }
        }

        private static void ProcessPolylineSegments(
            Polyline polyline,
            MapPoint clickPoint,
            SpatialReference mapSr,
            string layerName,
            ref MapPoint? bestStart,
            ref MapPoint? bestEnd,
            ref double minDistance,
            ref string featureDesc)
        {
            var points = polyline.Points;
            if (points == null || points.Count < 2) return;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];
                if (p1 == null || p2 == null) continue;

                var segLine = PolylineBuilderEx.CreatePolyline(new[] { p1, p2 }, mapSr);
                double dist = GeometryEngine.Instance.Distance(clickPoint, segLine);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    // Orient vector from point closest to click towards the other endpoint, or in drawing order
                    bestStart = p1;
                    bestEnd = p2;
                    featureDesc = $"Line on '{layerName}'";
                }
            }
        }

        private static void ProcessPolygonSegments(
            Polygon polygon,
            MapPoint clickPoint,
            SpatialReference mapSr,
            string layerName,
            ref MapPoint? bestStart,
            ref MapPoint? bestEnd,
            ref double minDistance,
            ref string featureDesc)
        {
            var points = polygon.Points;
            if (points == null || points.Count < 3) return;

            for (int i = 0; i < points.Count; i++)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];
                if (p1 == null || p2 == null) continue;

                // Skip closing segment if it's the exact same point
                if (Math.Abs(p1.X - p2.X) < 1e-7 && Math.Abs(p1.Y - p2.Y) < 1e-7) continue;

                var segLine = PolylineBuilderEx.CreatePolyline(new[] { p1, p2 }, mapSr);
                double dist = GeometryEngine.Instance.Distance(clickPoint, segLine);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestStart = p1;
                    bestEnd = p2;
                    featureDesc = $"Polygon edge on '{layerName}'";
                }
            }
        }
    }
}
