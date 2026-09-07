using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// Interactive map tool allowing user to click 2 points on the map with active snapping
    /// to define the block orientation baseline vector and origin.
    /// </summary>
    internal class AlignmentTool : MapTool
    {
        public AlignmentTool()
        {
            IsSketchTool = true;
            SketchType = SketchGeometryType.Line;
            SketchOutputMode = SketchOutputMode.Map;
            UseSnapping = true;
        }

        protected override Task OnToolActivateAsync(bool hasMapViewChanged)
        {
            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;
            if (dockPane != null)
            {
                dockPane.StatusText = "Click 2 points on the map (with snapping) to draw the orientation baseline...";
            }
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;

            try
            {
                if (geometry is Polyline line && line.PointCount >= 2)
                {
                    await QueuedTask.Run(() =>
                    {
                        var mapView = MapView.Active;
                        var mapSr = mapView?.Map?.SpatialReference ?? geometry.SpatialReference ?? SpatialReferences.WGS84;

                        var projLine = GeometryEngine.Instance.Project(line, mapSr) as Polyline ?? line;
                        var startPt = projLine.Points[0];
                        var endPt = projLine.Points[projLine.PointCount - 1];

                        if (dockPane != null)
                        {
                            dockPane.Activate();
                            dockPane.SetAlignmentLine(startPt.X, startPt.Y, endPt.X, endPt.Y, "2-Point Baseline");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                if (dockPane != null)
                {
                    dockPane.StatusText = $"Alignment notice: {ex.Message}";
                }
            }

            return true;
        }
    }
}
