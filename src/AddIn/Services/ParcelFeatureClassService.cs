using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.AddIn.Services
{
    /// <summary>
    /// Enterprise Cadastre service responsible for creating Geodatabase Feature Classes,
    /// defining cadastre schema fields, calculating spatial coordinates, and adding layers to active MapView.
    /// </summary>
    public class ParcelFeatureClassService
    {
        private static readonly Lazy<ParcelFeatureClassService> _instance = new(() => new ParcelFeatureClassService());
        public static ParcelFeatureClassService Instance => _instance.Value;

        private ParcelFeatureClassService() { }

        public async Task<(bool Success, string Message, int FeatureCount)> GenerateFeatureClassAsync(
            BlockConfiguration config,
            string targetGdbPath,
            string targetFcName,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (config == null)
            {
                return (false, "Configuration is null.", 0);
            }

            var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).ToList();
            if (allParcels.Count == 0)
            {
                return (false, "No generated parcels found to export.", 0);
            }

            // 1. Resolve workspace path and clean feature class name
            string gdbPath = ResolveGdbPath(targetGdbPath);
            string fcName = CleanFeatureClassName(targetFcName);

            progress?.Report(10.0);
            cancellationToken.ThrowIfCancellationRequested();

            // 2. Determine Spatial Reference from active map or default to WGS84
            SpatialReference spatialRef = SpatialReferences.WGS84;
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView?.Map?.SpatialReference != null)
                {
                    spatialRef = mapView.Map.SpatialReference;
                }
            });

            // 3. Ensure File Geodatabase exists
            if (!Directory.Exists(gdbPath))
            {
                string? parentDir = Path.GetDirectoryName(gdbPath);
                string gdbName = Path.GetFileName(gdbPath);
                if (!string.IsNullOrEmpty(parentDir) && !string.IsNullOrEmpty(gdbName))
                {
                    var gdbParams = Geoprocessing.MakeValueArray(parentDir, gdbName);
                    await Geoprocessing.ExecuteToolAsync("management.CreateFileGDB", gdbParams, null, cancellationToken);
                }
            }

            progress?.Report(30.0);
            cancellationToken.ThrowIfCancellationRequested();

            // 4. Create Feature Class via Geoprocessing
            var createParams = Geoprocessing.MakeValueArray(gdbPath, fcName, "POLYGON", "", "DISABLED", "DISABLED", spatialRef);
            var createResult = await Geoprocessing.ExecuteToolAsync("management.CreateFeatureclass", createParams, null, cancellationToken);

            if (createResult.IsFailed)
            {
                return (false, $"Failed to create Feature Class: {createResult.ErrorMessages}", 0);
            }

            progress?.Report(50.0);
            cancellationToken.ThrowIfCancellationRequested();

            // 5. Add Schema Fields
            string targetFcPath = Path.Combine(gdbPath, fcName);
            var fieldDefinitions = new (string Name, string Type, int Length, string Alias)[]
            {
                ("ParcelID", "TEXT", 30, "Parcel Identifier"),
                ("Side", "TEXT", 15, "Block Side"),
                ("Sequence", "LONG", 0, "Sequence Number"),
                ("Frontage", "DOUBLE", 0, "Frontage Width (m)"),
                ("Depth", "DOUBLE", 0, "Parcel Depth (m)"),
                ("Area_sqm", "DOUBLE", 0, "Calculated Area (sq m)"),
                ("ParcelType", "TEXT", 30, "Parcel Classification"),
                ("IsCorner", "SHORT", 0, "Is Corner Parcel (1/0)"),
                ("StreetLabel", "TEXT", 100, "Street Name / Label"),
                ("BlockLength", "DOUBLE", 0, "Total Block Length (m)"),
                ("Arrangement", "TEXT", 30, "Block Arrangement Mode")
            };

            foreach (var field in fieldDefinitions)
            {
                var addFieldParams = Geoprocessing.MakeValueArray(targetFcPath, field.Name, field.Type, "", "", field.Length > 0 ? field.Length : "", field.Alias);
                await Geoprocessing.ExecuteToolAsync("management.AddField", addFieldParams, null, cancellationToken);
            }

            progress?.Report(70.0);
            cancellationToken.ThrowIfCancellationRequested();

            // 6. Insert Features with Geometry and Attributes
            int insertedCount = 0;
            await QueuedTask.Run(() =>
            {
                using var gdb = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(gdbPath)));
                using var fc = gdb.OpenDataset<FeatureClass>(fcName);
                var fcDef = fc.GetDefinition();
                string shapeFieldName = fcDef.GetShapeField();

                foreach (var parcel in allParcels)
                {
                    if (parcel.PolygonRing.Count < 3) continue;

                    var points = parcel.PolygonRing.Select(p =>
                    {
                        var (gx, gy) = ParcelPreviewService.TransformPointToMap(p.X, p.Y, config);
                        return MapPointBuilderEx.CreateMapPoint(gx, gy, spatialRef);
                    }).ToList();

                    var polygon = PolygonBuilderEx.CreatePolygon(points, spatialRef);

                    using var rowBuffer = fc.CreateRowBuffer();
                    rowBuffer[shapeFieldName] = polygon;

                    SetRowValue(rowBuffer, fcDef, "ParcelID", parcel.Id);
                    SetRowValue(rowBuffer, fcDef, "Side", parcel.Side.ToString());
                    SetRowValue(rowBuffer, fcDef, "Sequence", parcel.Sequence);
                    SetRowValue(rowBuffer, fcDef, "Frontage", Math.Round(parcel.Frontage, 2));
                    SetRowValue(rowBuffer, fcDef, "Depth", Math.Round(parcel.Depth, 2));
                    SetRowValue(rowBuffer, fcDef, "Area_sqm", Math.Round(parcel.Area, 2));
                    SetRowValue(rowBuffer, fcDef, "ParcelType", parcel.Type);
                    SetRowValue(rowBuffer, fcDef, "IsCorner", parcel.IsCorner ? (short)1 : (short)0);
                    SetRowValue(rowBuffer, fcDef, "StreetLabel", parcel.Side == ParcelSide.SideA ? config.SideA.StreetLabel : config.SideB.StreetLabel);
                    SetRowValue(rowBuffer, fcDef, "BlockLength", Math.Round(config.EstimatedBlockLength, 2));
                    SetRowValue(rowBuffer, fcDef, "Arrangement", config.Arrangement.ToString());

                    using var row = fc.CreateRow(rowBuffer);
                    insertedCount++;
                }
            });

            progress?.Report(90.0);
            cancellationToken.ThrowIfCancellationRequested();

            // 7. Add Feature Layer to Active Map and Zoom
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView?.Map != null)
                {
                    var layerUri = new Uri(targetFcPath);
                    var newLayer = LayerFactory.Instance.CreateLayer(layerUri, mapView.Map, 0, fcName);
                    if (newLayer != null)
                    {
                        mapView.ZoomTo(newLayer);
                    }
                }
            });

            progress?.Report(100.0);
            return (true, $"Feature Class '{fcName}' created with {insertedCount} parcels.", insertedCount);
        }

        private static void SetRowValue(RowBuffer rowBuffer, FeatureClassDefinition fcDef, string fieldName, object value)
        {
            int fieldIndex = fcDef.FindField(fieldName);
            if (fieldIndex >= 0)
            {
                rowBuffer[fieldIndex] = value;
            }
        }

        private static string ResolveGdbPath(string? inputPath)
        {
            if (!string.IsNullOrWhiteSpace(inputPath) && inputPath.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase))
            {
                return inputPath.Trim();
            }

            if (Project.Current != null && !string.IsNullOrEmpty(Project.Current.DefaultGeodatabasePath))
            {
                return Project.Current.DefaultGeodatabasePath;
            }

            string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsDir, "ArcGIS", "ParcelBuilder_Output.gdb");
        }

        private static string CleanFeatureClassName(string? inputName)
        {
            if (string.IsNullOrWhiteSpace(inputName))
            {
                return "Generated_Parcels";
            }

            string clean = new string(inputName.Trim().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            if (string.IsNullOrEmpty(clean)) clean = "Generated_Parcels";
            if (char.IsDigit(clean[0]))
            {
                clean = "PB_" + clean;
            }
            return clean;
        }
    }
}
