using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// Interactive map tool allowing the user to click a location on the ArcGIS Pro map (with snapping enabled)
    /// to anchor the selected Base Point target map coordinates (WHERE the block is placed).
    /// </summary>
    internal class SelectBasePointTool : MapTool
    {
        public SelectBasePointTool()
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
                dockPane.StatusText = "📍 Click a location on the map (with snapping) to place the selected Base Point...";
            }
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint clickPoint) return false;

            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;

            try
            {
                double x = 0;
                double y = 0;
                int wkid = 0;
                string? srName = null;

                await QueuedTask.Run(() =>
                {
                    var mapView = MapView.Active;
                    var mapSr = mapView?.Map?.SpatialReference ?? geometry.SpatialReference ?? SpatialReferences.WGS84;
                    var projectedPoint = GeometryEngine.Instance.Project(clickPoint, mapSr) as MapPoint ?? clickPoint;
                    x = projectedPoint.X;
                    y = projectedPoint.Y;
                    wkid = mapSr.Wkid;
                    srName = mapSr.Name;
                });

                if (dockPane != null)
                {
                    dockPane.Activate();
                    dockPane.SetBasePointMapLocation(x, y, wkid, srName);
                }
            }
            catch (Exception ex)
            {
                if (dockPane != null)
                {
                    dockPane.StatusText = $"Base Point selection notice: {ex.Message}";
                }
            }

            return true;
        }
    }
}
