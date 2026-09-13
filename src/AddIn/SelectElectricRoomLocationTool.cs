using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// Interactive map tool allowing the user to click a location on the street frontage
    /// to interactively place the Electric Room / Substation.
    /// </summary>
    internal class SelectElectricRoomLocationTool : MapTool
    {
        public SelectElectricRoomLocationTool()
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
                dockPane.StatusText = "Click along the street frontage on the map to place the Electric Room...";
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
                    await dockPane.OnElectricRoomMapClickedAsync(clickPoint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectElectricRoomLocationTool notice: {ex.Message}");
            }

            return true;
        }
    }
}
