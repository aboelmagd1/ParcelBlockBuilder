using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace ParcelBuilder.AddIn
{
    internal class ShowParcelBuilderButton : Button
    {
        protected override void OnClick()
        {
            var dockPane = FrameworkApplication.DockPaneManager.Find("ParcelBuilder_DockPane");
            if (dockPane != null)
            {
                dockPane.Activate();
            }
        }
    }
}
