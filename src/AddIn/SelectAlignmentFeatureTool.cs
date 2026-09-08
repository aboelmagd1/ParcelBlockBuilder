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
                double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
                string? featureDesc = null;
                bool found = false;

                await QueuedTask.Run(() =>
                {
                    var mapView = MapView.Active;
                    if (mapView?.Map == null) return;

                    var mapSr = mapView.Map.SpatialReference ?? SpatialReferences.WGS84;
                    var projectedClick = GeometryEngine.Instance.Project(clickPoint, mapSr) as MapPoint ?? clickPoint;

                    // Calculate a reasonable search tolerance based on current view extent
                    double extentWidth = mapView.Extent != null ? Math.Abs(mapView.Extent.Width) : 100.0;
                    double searchTolerance = Math.Max(0.0001, extentWidth * 0.05);

                    var searchEnv = EnvelopeBuilderEx.CreateEnvelope(
                        projectedClick.X - searchTolerance,
                        projectedClick.Y - searchTolerance,
                        projectedClick.X + searchTolerance,
                        projectedClick.Y + searchTolerance,
                        mapSr);

                    MapPoint? bestStart = null;
                    MapPoint? bestEnd = null;
                    double minDistance = double.MaxValue;
                    string desc = "Map Feature Edge";

                    // 1. Primary Search using MapView.Active.GetFeatures (instant hit-testing)
                    try
                    {
                        var selectionResult = mapView.GetFeatures(searchEnv);
                        if (selectionResult != null && selectionResult.Count > 0)
                        {
                            var dict = selectionResult.ToDictionary();
                            foreach (var kvp in dict)
                            {
                                var layer = kvp.Key;
                                var oids = kvp.Value;
                                if (layer is not BasicFeatureLayer basicLayer || oids == null || oids.Count == 0) continue;

                                using var rowCursor = basicLayer.Search(new QueryFilter { ObjectIDs = oids });
                                if (rowCursor == null) continue;

                                while (rowCursor.MoveNext())
                                {
                                    using var feat = rowCursor.Current as Feature;
                                    if (feat == null) continue;

                                    var rawGeom = feat.GetShape();
                                    if (rawGeom == null || rawGeom.IsEmpty) continue;

                                    var geom = GeometryEngine.Instance.Project(rawGeom, mapSr);
                                    if (geom == null || geom.IsEmpty) continue;

                                    if (geom is Polyline polyline)
                                    {
                                        ProcessPolylineSegments(polyline, projectedClick, mapSr, basicLayer.Name, ref bestStart, ref bestEnd, ref minDistance, ref desc);
                                    }
                                    else if (geom is Polygon polygon)
                                    {
                                        ProcessPolygonSegments(polygon, projectedClick, mapSr, basicLayer.Name, ref bestStart, ref bestEnd, ref minDistance, ref desc);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to layer-by-layer spatial query
                    }

                    // 2. Fallback Search across all visible FeatureLayers if GetFeatures returned no segments
                    if (bestStart == null || bestEnd == null)
                    {
                        var layers = mapView.Map.GetLayersAsFlattenedList()
                            .OfType<BasicFeatureLayer>()
                            .Where(l => l.IsVisible)
                            .ToList();

                        foreach (var layer in layers)
                        {
                            try
                            {
                                var spatialFilter = new SpatialQueryFilter
                                {
                                    FilterGeometry = searchEnv,
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

                                    var geom = GeometryEngine.Instance.Project(rawGeom, mapSr);
                                    if (geom == null || geom.IsEmpty) continue;

                                    if (geom is Polyline polyline)
                                    {
                                        ProcessPolylineSegments(polyline, projectedClick, mapSr, layer.Name, ref bestStart, ref bestEnd, ref minDistance, ref desc);
                                    }
                                    else if (geom is Polygon polygon)
                                    {
                                        ProcessPolygonSegments(polygon, projectedClick, mapSr, layer.Name, ref bestStart, ref bestEnd, ref minDistance, ref desc);
                                    }
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    if (bestStart != null && bestEnd != null)
                    {
                        x1 = bestStart.X;
                        y1 = bestStart.Y;
                        x2 = bestEnd.X;
                        y2 = bestEnd.Y;
                        featureDesc = desc;
                        found = true;
                    }
                });

                if (found && dockPane != null)
                {
                    dockPane.Activate();
                    dockPane.SetOrientationFromSegment(x1, y1, x2, y2, featureDesc);
                }
                else if (!found && dockPane != null)
                {
                    dockPane.StatusText = "⚠ No line or polygon edge found within click tolerance. Try clicking directly on a visible feature boundary.";
                }
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
