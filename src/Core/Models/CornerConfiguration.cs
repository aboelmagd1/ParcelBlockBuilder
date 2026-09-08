using System;
using System.Collections.Generic;

namespace ParcelBuilder.Core.Models
{
    public enum CornerPosition
    {
        SideAStart = 0, // Front-Left (Street A & Left Street)
        SideAEnd = 1,   // Front-Right (Street A & Right Street)
        SideBStart = 2, // Back-Left (Street B & Left Street)
        SideBEnd = 3    // Back-Right (Street B & Right Street)
    }

    /// <summary>
    /// Configuration for an individual corner chamfer cut.
    /// </summary>
    public class SingleCornerConfig
    {
        public CornerPosition Position { get; set; }
        public string Name { get; set; } = "Corner";
        public bool IsEnabled { get; set; } = true;
        public ChamferMode Mode { get; set; } = ChamferMode.StreetSetbacks;

        /// <summary>
        /// 1. Direct diagonal cut length in meters (e.g. 5.0m).
        /// </summary>
        public double ChamferLength { get; set; } = 5.0;

        /// <summary>
        /// 2. Setback along Main Street (Street A or B) in meters.
        /// </summary>
        public double MainStreetSetback { get; set; } = 4.0;

        /// <summary>
        /// 2. Setback along Cross Street (Left or Right Street) in meters.
        /// </summary>
        public double CrossStreetSetback { get; set; } = 4.0;

        /// <summary>
        /// 3. Chamfer angle in degrees (default 45°).
        /// </summary>
        public double ChamferAngleDegrees { get; set; } = 45.0;

        /// <summary>
        /// 4. Primary setback for SetbackAndAngle mode.
        /// </summary>
        public double PrimarySetback { get; set; } = 4.0;

        /// <summary>
        /// Computes effective cut setbacks along X (Main Street frontage) and Y (Cross Street depth).
        /// </summary>
        public (double CutMainStreet, double CutCrossStreet) GetEffectiveCutDistances(double frontage, double depth)
        {
            if (!IsEnabled) return (0.0, 0.0);

            double cutX = 0.0;
            double cutY = 0.0;

            switch (Mode)
            {
                case ChamferMode.DirectCutLength:
                    double cut = ChamferLength / Math.Sqrt(2.0);
                    cutX = cut;
                    cutY = cut;
                    break;

                case ChamferMode.StreetSetbacks:
                    cutX = MainStreetSetback;
                    cutY = CrossStreetSetback;
                    break;

                case ChamferMode.CutLengthAndAngle:
                    double radA = ChamferAngleDegrees * (Math.PI / 180.0);
                    cutX = Math.Abs(ChamferLength * Math.Cos(radA));
                    cutY = Math.Abs(ChamferLength * Math.Sin(radA));
                    break;

                case ChamferMode.SetbackAndAngle:
                    double radB = ChamferAngleDegrees * (Math.PI / 180.0);
                    cutX = PrimarySetback;
                    cutY = Math.Abs(PrimarySetback * Math.Tan(radB));
                    break;
            }

            double maxCutX = Math.Max(0.1, frontage * 0.45);
            double maxCutY = Math.Max(0.1, depth * 0.45);

            cutX = Math.Min(cutX, maxCutX);
            cutY = Math.Min(cutY, maxCutY);

            return (Math.Round(cutX, 3), Math.Round(cutY, 3));
        }

        public double GetCalculatedChamferLength(double frontage, double depth)
        {
            var (cutX, cutY) = GetEffectiveCutDistances(frontage, depth);
            return Math.Round(Math.Sqrt((cutX * cutX) + (cutY * cutY)), 2);
        }

        public SingleCornerConfig Clone()
        {
            return new SingleCornerConfig
            {
                Position = this.Position,
                Name = this.Name,
                IsEnabled = this.IsEnabled,
                Mode = this.Mode,
                ChamferLength = this.ChamferLength,
                MainStreetSetback = this.MainStreetSetback,
                CrossStreetSetback = this.CrossStreetSetback,
                ChamferAngleDegrees = this.ChamferAngleDegrees,
                PrimarySetback = this.PrimarySetback
            };
        }
    }

    /// <summary>
    /// Master Corner & Chamfer configuration supporting individual per-corner settings.
    /// </summary>
    public class CornerConfiguration
    {
        public bool HasChamfer { get; set; } = true;
        public bool IsCustomPerCorner { get; set; } = false;

        // Shared / Uniform Default Corner
        public SingleCornerConfig DefaultCorner { get; set; } = new SingleCornerConfig
        {
            Position = CornerPosition.SideAStart,
            Name = "Default / All Corners",
            IsEnabled = true,
            Mode = ChamferMode.StreetSetbacks,
            MainStreetSetback = 4.0,
            CrossStreetSetback = 4.0,
            ChamferLength = 5.0
        };

        // 4 Individual Corners (Top-Left, Top-Right, Bottom-Left, Bottom-Right)
        public SingleCornerConfig CornerAStart { get; set; } = new SingleCornerConfig
        {
            Position = CornerPosition.SideAStart,
            Name = "Front-Left (Street A & Left Street)",
            IsEnabled = true,
            Mode = ChamferMode.StreetSetbacks,
            MainStreetSetback = 4.0,
            CrossStreetSetback = 4.0,
            ChamferLength = 5.0
        };

        public SingleCornerConfig CornerAEnd { get; set; } = new SingleCornerConfig
        {
            Position = CornerPosition.SideAEnd,
            Name = "Front-Right (Street A & Right Street)",
            IsEnabled = true,
            Mode = ChamferMode.StreetSetbacks,
            MainStreetSetback = 4.0,
            CrossStreetSetback = 4.0,
            ChamferLength = 5.0
        };

        public SingleCornerConfig CornerBStart { get; set; } = new SingleCornerConfig
        {
            Position = CornerPosition.SideBStart,
            Name = "Back-Left (Street B & Left Street)",
            IsEnabled = true,
            Mode = ChamferMode.StreetSetbacks,
            MainStreetSetback = 4.0,
            CrossStreetSetback = 4.0,
            ChamferLength = 5.0
        };

        public SingleCornerConfig CornerBEnd { get; set; } = new SingleCornerConfig
        {
            Position = CornerPosition.SideBEnd,
            Name = "Back-Right (Street B & Right Street)",
            IsEnabled = true,
            Mode = ChamferMode.StreetSetbacks,
            MainStreetSetback = 4.0,
            CrossStreetSetback = 4.0,
            ChamferLength = 5.0
        };

        // Surrounding cross-streets labels
        public string LeftStreetLabel { get; set; } = "Left Street (الشارع الأيسر)";
        public string RightStreetLabel { get; set; } = "Right Street (الشارع الأيمن)";

        public SingleCornerConfig GetEffectiveCorner(CornerPosition pos)
        {
            if (!IsCustomPerCorner)
            {
                var cfg = DefaultCorner.Clone();
                cfg.Position = pos;
                // Check if specific corner is disabled
                if (pos == CornerPosition.SideAStart && !ApplyToSideAStart) cfg.IsEnabled = false;
                if (pos == CornerPosition.SideAEnd && !ApplyToSideAEnd) cfg.IsEnabled = false;
                if (pos == CornerPosition.SideBStart && !ApplyToSideBStart) cfg.IsEnabled = false;
                if (pos == CornerPosition.SideBEnd && !ApplyToSideBEnd) cfg.IsEnabled = false;
                return cfg;
            }

            return pos switch
            {
                CornerPosition.SideAStart => CornerAStart,
                CornerPosition.SideAEnd => CornerAEnd,
                CornerPosition.SideBStart => CornerBStart,
                CornerPosition.SideBEnd => CornerBEnd,
                _ => DefaultCorner
            };
        }

        // Backward compatibility getters/setters
        public ChamferMode Mode
        {
            get => DefaultCorner.Mode;
            set => DefaultCorner.Mode = value;
        }

        public double ChamferLength
        {
            get => DefaultCorner.ChamferLength;
            set => DefaultCorner.ChamferLength = value;
        }

        public double StreetASetback
        {
            get => DefaultCorner.MainStreetSetback;
            set => DefaultCorner.MainStreetSetback = value;
        }

        public double StreetBSetback
        {
            get => DefaultCorner.CrossStreetSetback;
            set => DefaultCorner.CrossStreetSetback = value;
        }

        public double ChamferAngleDegrees
        {
            get => DefaultCorner.ChamferAngleDegrees;
            set => DefaultCorner.ChamferAngleDegrees = value;
        }

        public double PrimarySetback
        {
            get => DefaultCorner.PrimarySetback;
            set => DefaultCorner.PrimarySetback = value;
        }

        public bool ApplyToSideAStart
        {
            get => CornerAStart.IsEnabled;
            set => CornerAStart.IsEnabled = value;
        }

        public bool ApplyToSideAEnd
        {
            get => CornerAEnd.IsEnabled;
            set => CornerAEnd.IsEnabled = value;
        }

        public bool ApplyToSideBStart
        {
            get => CornerBStart.IsEnabled;
            set => CornerBStart.IsEnabled = value;
        }

        public bool ApplyToSideBEnd
        {
            get => CornerBEnd.IsEnabled;
            set => CornerBEnd.IsEnabled = value;
        }

        public bool ApplyToStart
        {
            get => ApplyToSideAStart && ApplyToSideBStart;
            set { ApplyToSideAStart = value; ApplyToSideBStart = value; }
        }

        public bool ApplyToEnd
        {
            get => ApplyToSideAEnd && ApplyToSideBEnd;
            set { ApplyToSideAEnd = value; ApplyToSideBEnd = value; }
        }

        public List<string> CustomChamferParcelIds { get; set; } = new List<string>();

        public (double CutX, double CutY) GetEffectiveCutDistances(double frontage, double depth)
        {
            return DefaultCorner.GetEffectiveCutDistances(frontage, depth);
        }

        public double GetCalculatedChamferLength(double frontage, double depth)
        {
            return DefaultCorner.GetCalculatedChamferLength(frontage, depth);
        }

        public CornerConfiguration Clone()
        {
            return new CornerConfiguration
            {
                HasChamfer = this.HasChamfer,
                IsCustomPerCorner = this.IsCustomPerCorner,
                DefaultCorner = this.DefaultCorner?.Clone() ?? new SingleCornerConfig(),
                CornerAStart = this.CornerAStart?.Clone() ?? new SingleCornerConfig(),
                CornerAEnd = this.CornerAEnd?.Clone() ?? new SingleCornerConfig(),
                CornerBStart = this.CornerBStart?.Clone() ?? new SingleCornerConfig(),
                CornerBEnd = this.CornerBEnd?.Clone() ?? new SingleCornerConfig(),
                LeftStreetLabel = this.LeftStreetLabel,
                RightStreetLabel = this.RightStreetLabel,
                CustomChamferParcelIds = new List<string>(this.CustomChamferParcelIds)
            };
        }
    }
}
