using System;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace ParcelBuilder.AddIn
{
    /// <summary>
    /// ArcGIS Pro Module entry point for Parcel Builder.
    /// </summary>
    internal class ParcelBuilderModule : Module
    {
        private static ParcelBuilderModule? _this = null;

        public static ParcelBuilderModule Current => _this ??= (ParcelBuilderModule)FrameworkApplication.FindModule("ParcelBuilder_Module");

        protected override bool CanUnload()
        {
            return true;
        }
    }
}
