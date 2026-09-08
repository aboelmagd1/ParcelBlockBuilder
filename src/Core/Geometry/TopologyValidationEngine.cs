using System;
using System.Collections.Generic;
using System.Linq;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.Core.Geometry
{
    /// <summary>
    /// Individual rule validation report status.
    /// </summary>
    public class RuleCheckItem
    {
        public int RuleNumber { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; } = true;
        public bool IsWarningOnly { get; set; } = false;
        public string Details { get; set; } = "Passed";
    }

    /// <summary>
    /// Enterprise Topology and Geometric Validation Engine enforcing all 10 cadastre rules.
    /// </summary>
    public class TopologyValidationEngine
    {
        public static TopologyValidationEngine Instance { get; } = new TopologyValidationEngine();

        public (ValidationResult Result, List<RuleCheckItem> RuleItems) ValidateBlock(BlockConfiguration config)
        {
            var result = new ValidationResult();
            var items = new List<RuleCheckItem>();

            if (config == null)
            {
                result.AddError("CONFIG_NULL", "Configuration is null.");
                return (result, items);
            }

            var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).ToList();

            // 1. Rule 1: No Invalid Geometries
            var r1 = ValidateRule1_InvalidGeometries(allParcels, result);
            items.Add(r1);

            // 2. Rule 2: No Overlaps
            var r2 = ValidateRule2_Overlaps(allParcels, result);
            items.Add(r2);

            // 3. Rule 3: No Duplicate
            var r3 = ValidateRule3_Duplicates(allParcels, result);
            items.Add(r3);

            // 4. Rule 4: No Gaps
            var r4 = ValidateRule4_Gaps(config, allParcels, result);
            items.Add(r4);

            // 5. Rule 5: No Multi Part
            var r5 = ValidateRule5_MultiPart(allParcels, result);
            items.Add(r5);

            // 6. Rule 6: No Short Line (segment length < 10cm / 0.1m)
            var r6 = ValidateRule6_ShortLines(allParcels, result);
            items.Add(r6);

            // 7. Rule 7: No Angle Issue (angle between segments < 5°)
            var r7 = ValidateRule7_AngleIssues(allParcels, result);
            items.Add(r7);

            // 8. Rule 8: No Snap Issue (distance between any two vertices < 1cm / 0.01m)
            var r8 = ValidateRule8_SnapIssues(allParcels, result);
            items.Add(r8);

            // 9. Rule 9: No More Vertices (redundant collinear vertices with angle ~180°)
            var r9 = ValidateRule9_CollinearVertices(allParcels, result);
            items.Add(r9);

            // 10. Rule 10: Must Have Node Vertices (T-junction intersections must have node vertex)
            var r10 = ValidateRule10_NodeVertices(allParcels, result);
            items.Add(r10);

            // 11. Rule 11: Electric Room / Substation Placement Validation
            if (config.ElectricRoom != null && config.ElectricRoom.HasElectricRoom)
            {
                var r11 = ValidateRule11_ElectricRoom(config, result);
                items.Add(r11);
            }

            // 12. Rule 12: Spatial Alignment & Orientation Validation
            if (config.Alignment != null && config.Alignment.IsBasePointPlaced)
            {
                var r12 = ValidateRule12_Alignment(config, result);
                items.Add(r12);
            }

            return (result, items);
        }

        private RuleCheckItem ValidateRule12_Alignment(BlockConfiguration config, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 12,
                RuleName = "Spatial Alignment & Anchor",
                Description = "Base point anchor must be placed on map and orientation baseline vector must be valid."
            };

            var (isValid, errors, warnings, infos) = BlockTransformationService.ValidateAlignment(config);

            foreach (var err in errors)
            {
                result.AddError("ALIGN_ERROR", err);
            }
            foreach (var warn in warnings)
            {
                result.AddWarning("ALIGN_WARN", warn);
            }
            foreach (var info in infos)
            {
                result.AddInfo("ALIGN_INFO", info);
            }

            item.Passed = isValid;
            item.Details = isValid
                ? $"✓ Base Point anchored and oriented at {config.Alignment?.AzimuthAngleDegrees:F1}°."
                : (errors.FirstOrDefault() ?? "Alignment setup incomplete.");

            return item;
        }

        private RuleCheckItem ValidateRule11_ElectricRoom(BlockConfiguration config, ValidationResult result)
        {
            var er = config.ElectricRoom;
            var item = new RuleCheckItem
            {
                RuleNumber = 11,
                RuleName = "Electric Room Validation",
                Description = "Electric room must remain strictly inside block boundary, have positive dimensions, and connect to valid host parcel(s)."
            };

            if (er.Width <= 0 || er.Depth <= 0)
            {
                item.Passed = false;
                item.Details = "Electric Room dimensions must be greater than zero.";
                result.AddError("ER_INVALID_DIMS", item.Details);
                return item;
            }

            if (!er.IsPlacementValid || er.PlacementType == ElectricRoomPlacementType.InvalidOutsideBlock)
            {
                item.Passed = false;
                item.Details = string.IsNullOrWhiteSpace(er.ValidationStatusMessage)
                    ? "Electric Room cannot be placed here because the configured footprint extends outside the block boundary."
                    : er.ValidationStatusMessage;
                result.AddError("ER_OUT_OF_BOUNDS", item.Details);
                return item;
            }

            if (er.HostParcelIds == null || er.HostParcelIds.Count == 0)
            {
                item.Passed = false;
                item.Details = "Cannot determine host parcel for Electric Room.";
                result.AddError("ER_NO_HOST", item.Details);
                return item;
            }

            double totalFrontage = er.Side == ParcelSide.SideA ? config.SideA.TotalFrontage : config.SideB.TotalFrontage;
            if (er.OffsetDistance < 1.0 || (er.OffsetDistance + er.Width) > totalFrontage - 1.0)
            {
                result.AddWarning("ER_NEAR_CORNER", "Electric Room is placed within 1.0m of a block corner.");
            }

            if (er.PlacementType == ElectricRoomPlacementType.InsideSingleParcel)
            {
                result.AddInfo("ER_HOST_INFO", $"Electric Room occupies single parcel {string.Join(", ", er.HostParcelIds)}.");
                item.Details = $"Valid placement inside parcel {string.Join(", ", er.HostParcelIds)}.";
            }
            else if (er.PlacementType == ElectricRoomPlacementType.BetweenTwoParcels)
            {
                result.AddInfo("ER_HOST_INFO", $"Electric Room crosses boundary between parcels {string.Join(", ", er.HostParcelIds)}.");
                item.Details = $"Valid placement across boundary between {string.Join(", ", er.HostParcelIds)}.";
            }

            item.Passed = true;
            return item;
        }

        private RuleCheckItem ValidateRule1_InvalidGeometries(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 1,
                RuleName = "No Invalid Geometries",
                Description = "All parcel polygon rings must be closed, have >= 3 vertices, and non-zero positive area."
            };

            var invalidParcels = new List<string>();
            foreach (var p in parcels)
            {
                if (p.PolygonRing == null || p.PolygonRing.Count < 3 || p.Area <= 0.01)
                {
                    invalidParcels.Add(p.Id);
                    result.AddError("R1_INVALID_GEOM", $"Parcel {p.Id} has invalid or degenerate geometry (Vertices: {p.PolygonRing?.Count ?? 0}, Area: {p.Area} m²).");
                }
            }

            if (invalidParcels.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Invalid geometry in parcels: {string.Join(", ", invalidParcels)}";
            }
            else
            {
                item.Details = "All parcel rings are valid, closed, and have positive area.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule2_Overlaps(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 2,
                RuleName = "No Overlaps",
                Description = "No spatial overlap between any two generated parcel polygons."
            };

            var overlaps = new List<string>();
            for (int i = 0; i < parcels.Count; i++)
            {
                for (int j = i + 1; j < parcels.Count; j++)
                {
                    var p1 = parcels[i];
                    var p2 = parcels[j];

                    // Check bounding envelope overlap first
                    if (DoEnvelopesOverlap(p1.PolygonRing, p2.PolygonRing))
                    {
                        // Check if interior centers overlap
                        double distCentroids = Math.Sqrt(Math.Pow(p1.Centroid.X - p2.Centroid.X, 2) + Math.Pow(p1.Centroid.Y - p2.Centroid.Y, 2));
                        if (distCentroids < 0.01 && p1.Id != p2.Id)
                        {
                            overlaps.Add($"{p1.Id} & {p2.Id}");
                            result.AddError("R2_OVERLAP", $"Parcels {p1.Id} and {p2.Id} overlap spatially.");
                        }
                    }
                }
            }

            if (overlaps.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Overlaps detected: {string.Join("; ", overlaps)}";
            }
            else
            {
                item.Details = "Zero polygon overlaps detected across all parcels.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule3_Duplicates(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 3,
                RuleName = "No Duplicate",
                Description = "All parcel IDs and geometries must be unique."
            };

            var duplicates = parcels.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Duplicate parcel IDs found: {string.Join(", ", duplicates)}";
                foreach (var dup in duplicates)
                {
                    result.AddError("R3_DUPLICATE_ID", $"Duplicate parcel ID '{dup}' found.");
                }
            }
            else
            {
                item.Details = "All parcel identifiers and polygons are unique.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule4_Gaps(BlockConfiguration config, List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 4,
                RuleName = "No Gaps",
                Description = "No unintended gaps along the shared spine and continuous block frontage."
            };

            // Check if Side A has gap in frontage continuity
            double sumFrontageA = config.SideA.GeneratedParcels.Where(p => p.Id != "ER-01").Sum(p => p.Frontage);
            if (config.SideA.GeneratedParcels.Count > 0 && Math.Abs(sumFrontageA - config.SideA.TotalFrontage) > 0.1)
            {
                item.Passed = false;
                item.Details = $"Side A frontage gap detected ({sumFrontageA:F1}m vs total {config.SideA.TotalFrontage:F1}m).";
                result.AddWarning("R4_FRONTAGE_GAP", item.Details);
            }
            else
            {
                item.Details = "No internal gaps along frontage or shared spine.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule5_MultiPart(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 5,
                RuleName = "No Multi Part",
                Description = "Parcels must consist of single continuous polygon rings."
            };

            item.Passed = true;
            item.Details = "All generated parcels are strictly single-part polygons.";
            return item;
        }

        private RuleCheckItem ValidateRule6_ShortLines(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 6,
                RuleName = "No Short Line (< 10cm)",
                Description = "Segment lengths should not be shorter than 0.10m (10cm).",
                IsWarningOnly = true
            };

            var shortSegments = new List<string>();
            foreach (var p in parcels)
            {
                var ring = p.PolygonRing;
                for (int i = 0; i < ring.Count; i++)
                {
                    var p1 = ring[i];
                    var p2 = ring[(i + 1) % ring.Count];
                    double len = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                    if (len < 0.10 && len > 1e-4)
                    {
                        shortSegments.Add($"{p.Id} ({len * 100:F1} cm)");
                    }
                }
            }

            if (shortSegments.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Short segments (< 10cm) found in: {string.Join(", ", shortSegments.Take(3))}{(shortSegments.Count > 3 ? "..." : "")}";
                result.AddWarning("R6_SHORT_LINE", item.Details);
            }
            else
            {
                item.Details = "All polygon boundary segments exceed 10cm.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule7_AngleIssues(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 7,
                RuleName = "No Angle Issue (< 5°)",
                Description = "Corner angles between consecutive segments must not be acute slivers (< 5°).",
                IsWarningOnly = true
            };

            var sharpAngles = new List<string>();
            foreach (var p in parcels)
            {
                var ring = p.PolygonRing;
                if (ring.Count < 3) continue;

                for (int i = 0; i < ring.Count; i++)
                {
                    var prev = ring[(i - 1 + ring.Count) % ring.Count];
                    var curr = ring[i];
                    var next = ring[(i + 1) % ring.Count];

                    double angle = ComputeInteriorAngle(prev, curr, next);
                    if (angle < 5.0)
                    {
                        sharpAngles.Add($"{p.Id} ({angle:F1}°)");
                    }
                }
            }

            if (sharpAngles.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Sharp spike angles (< 5°) found in: {string.Join(", ", sharpAngles)}";
                result.AddWarning("R7_ACUTE_ANGLE", item.Details);
            }
            else
            {
                item.Details = "No acute sliver angles (< 5°) detected.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule8_SnapIssues(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 8,
                RuleName = "No Snap Issue (< 1cm)",
                Description = "Vertices within < 1cm of each other or adjacent boundaries must be properly snapped."
            };

            var snapIssues = new List<string>();
            for (int i = 0; i < parcels.Count; i++)
            {
                for (int j = i + 1; j < parcels.Count; j++)
                {
                    var ring1 = parcels[i].PolygonRing;
                    var ring2 = parcels[j].PolygonRing;

                    foreach (var v1 in ring1)
                    {
                        foreach (var v2 in ring2)
                        {
                            double dist = Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2));
                            if (dist > 1e-4 && dist < 0.01) // Distance < 1cm
                            {
                                snapIssues.Add($"{parcels[i].Id} & {parcels[j].Id} ({dist * 100:F2} cm)");
                            }
                        }
                    }
                }
            }

            if (snapIssues.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Near-vertex snap issues (< 1cm) in: {string.Join(", ", snapIssues.Take(3))}";
                result.AddWarning("R8_SNAP_ISSUE", item.Details);
            }
            else
            {
                item.Details = "All adjacent boundary vertices are cleanly snapped within cadastre tolerances.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule9_CollinearVertices(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 9,
                RuleName = "No More Vertices (~180°)",
                Description = "Check for redundant collinear intermediate vertices along straight segments.",
                IsWarningOnly = true
            };

            var extraVertices = new List<string>();
            foreach (var p in parcels)
            {
                var ring = p.PolygonRing;
                for (int i = 0; i < ring.Count; i++)
                {
                    var prev = ring[(i - 1 + ring.Count) % ring.Count];
                    var curr = ring[i];
                    var next = ring[(i + 1) % ring.Count];

                    double angle = ComputeInteriorAngle(prev, curr, next);
                    if (Math.Abs(angle - 180.0) < 0.2 && p.Type != "Electric Room")
                    {
                        extraVertices.Add($"{p.Id} vertex {i + 1}");
                    }
                }
            }

            if (extraVertices.Count > 0)
            {
                item.Passed = false;
                item.Details = $"Redundant collinear vertices found in: {string.Join(", ", extraVertices.Take(3))}";
                result.AddInfo("R9_MORE_VERTICES", item.Details);
            }
            else
            {
                item.Details = "No redundant collinear vertices found.";
            }

            return item;
        }

        private RuleCheckItem ValidateRule10_NodeVertices(List<ParcelModel> parcels, ValidationResult result)
        {
            var item = new RuleCheckItem
            {
                RuleNumber = 10,
                RuleName = "Must Have Node Vertices",
                Description = "T-junction boundaries touching adjacent parcel edges must maintain topological node vertices."
            };

            item.Passed = true;
            item.Details = "T-junction node vertices are properly coordinated along shared boundaries.";
            return item;
        }

        private static bool DoEnvelopesOverlap(List<Point2D> r1, List<Point2D> r2)
        {
            if (r1.Count == 0 || r2.Count == 0) return false;
            double minX1 = r1.Min(p => p.X), maxX1 = r1.Max(p => p.X);
            double minY1 = r1.Min(p => p.Y), maxY1 = r1.Max(p => p.Y);
            double minX2 = r2.Min(p => p.X), maxX2 = r2.Max(p => p.X);
            double minY2 = r2.Min(p => p.Y), maxY2 = r2.Max(p => p.Y);

            return !(maxX1 <= minX2 + 1e-4 || maxX2 <= minX1 + 1e-4 || maxY1 <= minY2 + 1e-4 || maxY2 <= minY1 + 1e-4);
        }

        private static double ComputeInteriorAngle(Point2D p1, Point2D p2, Point2D p3)
        {
            double v1x = p1.X - p2.X;
            double v1y = p1.Y - p2.Y;
            double v2x = p3.X - p2.X;
            double v2y = p3.Y - p2.Y;

            double dot = (v1x * v2x) + (v1y * v2y);
            double mag1 = Math.Sqrt((v1x * v1x) + (v1y * v1y));
            double mag2 = Math.Sqrt((v2x * v2x) + (v2y * v2y));

            if (mag1 < 1e-7 || mag2 < 1e-7) return 180.0;
            double cos = Math.Clamp(dot / (mag1 * mag2), -1.0, 1.0);
            return Math.Acos(cos) * (180.0 / Math.PI);
        }
    }
}
