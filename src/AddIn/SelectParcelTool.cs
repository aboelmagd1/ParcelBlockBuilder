using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// Interactive map tool allowing user to click any existing parcel on the map
    /// to detect and infer the base parcel dimensions and block configuration.
    /// </summary>
    internal class SelectParcelTool : MapTool
    {
        public SelectParcelTool()
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
                dockPane.StatusText = "Click any existing parcel polygon on the map to extract configuration...";
            }
            return Task.CompletedTask;
        }

        protected override async Task<bool> OnSketchCompleteAsync(Geometry geometry)
        {
            if (geometry is not MapPoint clickPoint) return false;

            try
            {
                var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane") as ParcelBuilderDockPaneViewModel;
                if (dockPane != null)
                {
                    dockPane.Activate();
                    await dockPane.OnMapParcelSelectedAsync(clickPoint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectParcelTool notice: {ex.Message}");
            }

            return true;
        }
    }
}
