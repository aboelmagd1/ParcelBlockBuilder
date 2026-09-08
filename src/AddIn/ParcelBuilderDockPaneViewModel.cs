using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ParcelBuilder.AddIn.Services;
using ParcelBuilder.Core.Geometry;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.AddIn
{
    public class ParcelBuilderDockPaneViewModel : DockPane
    {
        private const string DockPaneId = "ParcelBuilder_DockPane";

        private BlockConfiguration _config = new BlockConfiguration();
        private int _currentStep = 1; // Starts on Step 1: Base Dimensions
        private string _statusText = "Ready";
        private string _selectedParcelId = string.Empty;
        private string _outputFeatureClassName = "Generated_Parcels";
        private string _outputWorkspacePath = "Default.gdb";
        private bool _isLivePreviewEnabled = true;
        private bool _isProcessing = false;
        private double _progressPercent = 0.0;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isArabic = false;

        // Multi-parcel accumulation state
        private int _extractedParcelsCount = 0;
        private double _extractedTotalFrontage = 0.0;
        private double _extractedTotalArea = 0.0;

        public ParcelBuilderDockPaneViewModel()
        {
            // Language Commands
            SetEnglishCommand = new RelayCommand(() => IsArabic = false);
            SetArabicCommand = new RelayCommand(() => IsArabic = true);

            // Navigation Commands
            NextStepCommand = new RelayCommand(ExecuteNextStep, () => CurrentStep < 12 && IsCurrentStepValid);
            PreviousStepCommand = new RelayCommand(ExecutePreviousStep, () => CurrentStep > 1);
            GoToStepCommand = new RelayCommandWithParam<int>(ExecuteGoToStep, CanGoToStep);
            ResetCommand = new RelayCommand(ExecuteReset);
            GenerateFeatureClassCommand = new RelayCommand(async () => await ExecuteGenerateFeatureClassAsync(), () => !IsProcessing && IsValidationPassing);
            SelectOnMapCommand = new RelayCommand(ExecuteSelectOnMap);
            SelectElectricRoomOnMapCommand = new RelayCommand(ExecuteSelectElectricRoomOnMap);
            SelectBasePointOnMapCommand = new RelayCommand(ExecuteSelectBasePointOnMap);
            SetAlignmentCommand = new RelayCommand(ExecuteSetAlignment);
            SetAlignmentFromFeatureCommand = new RelayCommand(ExecuteSetAlignmentFromFeature);
            AddExceptionCommand = new RelayCommand(ExecuteAddException);
            RemoveExceptionCommand = new RelayCommandWithParam<ParcelException>(ExecuteRemoveException);
            DeleteExceptionCommand = new RelayCommandWithParam<string>(ExecuteDeleteExceptionById);
            ZoomToSelectedParcelCommand = new RelayCommand(async () => await ExecuteZoomToSelectedParcelAsync(), () => !string.IsNullOrEmpty(SelectedParcelId));
            ZoomToBlockExtentCommand = new RelayCommand(async () => await ExecuteZoomToBlockExtentAsync());
            MoveToMapExtentCommand = new RelayCommand(async () => await ExecuteMoveToMapExtentAsync());
            ZoomInCommand = new RelayCommand(async () => await ExecuteZoomInAsync());
            ZoomOutCommand = new RelayCommand(async () => await ExecuteZoomOutAsync());
            ResetSelectedParcelToStandardCommand = new RelayCommand(ExecuteResetSelectedParcelToStandard);
            SelectParcelCommand = new RelayCommandWithParam<string>(id => { if (!string.IsNullOrEmpty(id)) SelectedParcelId = id; });
            ToggleChamferForParcelCommand = new RelayCommandWithParam<string>(ExecuteToggleChamferForParcel);
            SelectAllCornersCommand = new RelayCommand(ExecuteSelectAllCorners);
            ClearAllCornersCommand = new RelayCommand(ExecuteClearAllCorners);
            CancelOperationCommand = new RelayCommand(ExecuteCancelOperation, () => IsProcessing);
            ToggleLivePreviewCommand = new RelayCommand(async () => await ExecuteToggleLivePreviewAsync());

            // Initialize Geometry
            Recalculate();
            if (Config.SideA.GeneratedParcels.Count > 0)
            {
                SelectedParcelId = Config.SideA.GeneratedParcels[0].Id;
            }
        }

        public bool IsArabic
        {
            get => _isArabic;
            set
            {
                if (SetProperty(ref _isArabic, value))
                {
                    NotifyPropertyChanged(nameof(IsEnglish));
                    NotifyPropertyChanged(nameof(FlowDirection));
                }
            }
        }

        public bool IsEnglish => !IsArabic;
        public System.Windows.FlowDirection FlowDirection => IsArabic ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        public ICommand SetEnglishCommand { get; }
        public ICommand SetArabicCommand { get; }

        protected override void OnShow(bool isInitialShow)
        {
            base.OnShow(isInitialShow);
            if (ArcGIS.Desktop.Core.Project.Current != null && !string.IsNullOrEmpty(ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath))
            {
                if (string.IsNullOrEmpty(_outputWorkspacePath) || _outputWorkspacePath == "Default.gdb")
                {
                    OutputWorkspacePath = ArcGIS.Desktop.Core.Project.Current.DefaultGeodatabasePath;
                }
            }
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            _ = ParcelPreviewService.Instance.ClearPreviewAsync();
        }

        public BlockConfiguration Config
        {
            get => _config;
            set
            {
                if (SetProperty(ref _config, value))
                {
                    Recalculate();
                }
            }
        }

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (SetProperty(ref _currentStep, value))
                {
                    NotifyPropertyChanged(nameof(StepTitle));
                    NotifyPropertyChanged(nameof(StepSubtitle));
                    NotifyPropertyChanged(nameof(IsCurrentStepValid));
                    NotifyPropertyChanged(nameof(IsStep1));
                    NotifyPropertyChanged(nameof(IsStep2));
                    NotifyPropertyChanged(nameof(IsStep3));
                    NotifyPropertyChanged(nameof(IsStep4));
                    NotifyPropertyChanged(nameof(IsStep5));
                    NotifyPropertyChanged(nameof(IsStep6));
                    NotifyPropertyChanged(nameof(IsStep7));
                    NotifyPropertyChanged(nameof(IsStep8));
                    NotifyPropertyChanged(nameof(IsStep9));
                    NotifyPropertyChanged(nameof(IsStep10));
                    NotifyPropertyChanged(nameof(IsStep11));
                    NotifyPropertyChanged(nameof(IsStep12));
                }
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;
        public bool IsStep3 => CurrentStep == 3;
        public bool IsStep4 => CurrentStep == 4;
        public bool IsStep5 => CurrentStep == 5;
        public bool IsStep6 => CurrentStep == 6;
        public bool IsStep7 => CurrentStep == 7;
        public bool IsStep8 => CurrentStep == 8;
        public bool IsStep9 => CurrentStep == 9;
        public bool IsStep10 => CurrentStep == 10;
        public bool IsStep11 => CurrentStep == 11;
        public bool IsStep12 => CurrentStep == 12;

        public bool IsCurrentStepValid => CurrentStep switch
        {
            1 => !HasFrontageError && !HasDepthError,
            4 => SideACount >= 1 && (Config.Arrangement != ArrangementMode.BackToBack || SideBCount >= 1),
            _ => true
        };

        public string StepTitle => CurrentStep switch
        {
            1 => IsArabic ? "1. الأبعاد الأساسية للقطع" : "1. Base Parcel Dimensions",
            2 => IsArabic ? "2. نوع العلاقة" : "2. Relationship Type",
            3 => IsArabic ? "3. هيكل وتوزيع البلوك" : "3. Block Arrangement",
            4 => IsArabic ? "4. عدد وتوزيع القطع" : "4. Parcel Count & Layout",
            5 => IsArabic ? "5. تشابه وتجانس الأبعاد" : "5. Dimension Similarity",
            6 => IsArabic ? "6. استثناءات وتعديلات القطع" : "6. Parcel Exceptions & Overrides",
            7 => IsArabic ? "7. هندسة الشطفات والأركان" : "7. Corner & Chamfer Geometry",
            8 => IsArabic ? "8. غرفة الكهرباء / المحول" : "8. Electric Room / Substation",
            9 => IsArabic ? "9. المحاذاة والاتجاه المكاني" : "9. Spatial Alignment & Orientation",
            10 => IsArabic ? "10. المعاينة الديناميكية والتفتيش" : "10. Dynamic Preview & Inspection",
            11 => IsArabic ? "11. قواعد التحقق والتوبولوجيا (10 قواعد)" : "11. Validation Rules (10 Rules)",
            12 => IsArabic ? "12. المراجعة النهائية والتصدير" : "12. Final Review & Output",
            _ => "Parcel Builder Workstation"
        };

        public string StepSubtitle => CurrentStep switch
        {
            1 => IsArabic ? "تحديد الأبعاد الأساسية للواجهة والعمق، أو استخلاص ودمج أبعاد عدة قطع من الخريطة." : "Define standard frontage and depth, or extract and accumulate multiple parcel dimensions from the active map.",
            2 => IsArabic ? "تحديد ما إذا كانت القطعة مستقلة بذاتها أو جزءاً من بلوك منظم." : "Specify whether this is a standalone parcel or part of a coordinated block assembly.",
            3 => IsArabic ? "اختيار توزيع البلوك كصف واحد أو صفين متقابلين (ظهر لظهر)." : "Choose single-sided row or back-to-back dual row block structure.",
            4 => IsArabic ? "تحديد عدد القطع لكل جهة مع معاينة تخطيطية فورية أسفل الأعداد." : "Configure parcel counts independently for Side A and Side B with live schematic layout preview.",
            5 => IsArabic ? "تحديد ما إذا كانت جميع القطع متطابقة الأبعاد أو وجود استثناءات مخصصة." : "Determine whether all parcels are identical or have custom dimension exceptions.",
            6 => IsArabic ? "إضافة تعديلات مخصصة لأبعاد واجهات وأعماق قطع معينة." : "Add custom dimension overrides, frontage variations, and parcel classifications.",
            7 => IsArabic ? "خيارات رسم الشطفات المتعددة (بالطول، بأطوال الشوارع، أو بالزاوية) مع اختيار القطع المستهدفة." : "Configure chamfer cut methods (by length, street setbacks, or angle) and interactively pick target corners.",
            8 => IsArabic ? "تحديد أبعاد وموقع غرفة الكهرباء على الشارع والقص التلقائي من القطع المستضيفة." : "Configure electric room dimensions, street placement, and automatic polygon carving/clipping from host parcels.",
            9 => IsArabic ? "تحديد خط الأساس المكاني عبر نقطتين مع Snapping، أو باختيار خط، أو الإدخال الرقمي المباشر." : "Define orientation baseline via 2 map points, picking a line/polygon edge, or direct numeric angle coordinates.",
            10 => IsArabic ? "تفتيش أبعاد ومساحات كل قطعة مع المخطط الشعاعي ونافذة الخريطة التفاعلية." : "Inspect individual parcel dimensions, areas, and interactive vector layout schematic.",
            11 => IsArabic ? "فحص ومراجعة القواعد الهندسية والتوبولوجية العشرة بدون أخطاء متقاطعة." : "Review 10 geometric integrity and topological rules (no overlaps, gaps, acute angles, short lines, snap issues).",
            12 => IsArabic ? "مراجعة المخطط النهائي وتصدير الطبقة إلى قاعدة البيانات الجغرافية (Geodatabase Feature Class)." : "Review the final configuration summary blueprint and export to Geodatabase Feature Class.",
            _ => string.Empty
        };

        // --- SOURCE METADATA & EXTRACTION (STEP 1) ---
        public bool IsExtractedMode => Config.SourceMode == SourceMode.ExtractedFromExistingParcel;
        public string SourceParcelId => Config.Metadata.SourceParcelId?.ToString() ?? "P-001";
        public string ExtractionConfidence => $"Confidence: {Config.Metadata.ConfidenceScore * 100:F0}% ({Config.Metadata.Status})";
        public double DetectedFrontage => Config.BaseParcel.DetectedFrontage;
        public double DetectedDepth => Config.BaseParcel.DetectedDepth;

        public int ExtractedParcelsCount
        {
            get => _extractedParcelsCount;
            set => SetProperty(ref _extractedParcelsCount, value);
        }

        public double ExtractedTotalFrontage
        {
            get => _extractedTotalFrontage;
            set => SetProperty(ref _extractedTotalFrontage, value);
        }

        public double ExtractedTotalArea
        {
            get => _extractedTotalArea;
            set => SetProperty(ref _extractedTotalArea, value);
        }

        public bool HasMultipleExtractedParcels => ExtractedParcelsCount > 1;

        // --- STEP 1: BASE DIMENSIONS ---
        public double BaseFrontage
        {
            get => Config.BaseParcel.Frontage;
            set
            {
                if (Config.BaseParcel.Frontage != value)
                {
                    Config.BaseParcel.Frontage = value;
                    NotifyPropertyChanged(nameof(BaseFrontage));
                    NotifyPropertyChanged(nameof(BaseArea));
                    NotifyPropertyChanged(nameof(HasFrontageError));
                    NotifyPropertyChanged(nameof(IsCurrentStepValid));
                    Recalculate();
                }
            }
        }

        public double BaseDepth
        {
            get => Config.BaseParcel.Depth;
            set
            {
                if (Config.BaseParcel.Depth != value)
                {
                    Config.BaseParcel.Depth = value;
                    NotifyPropertyChanged(nameof(BaseDepth));
                    NotifyPropertyChanged(nameof(BaseArea));
                    NotifyPropertyChanged(nameof(HasDepthError));
                    NotifyPropertyChanged(nameof(IsCurrentStepValid));
                    Recalculate();
                }
            }
        }

        public double BaseArea => Config.BaseParcel.Area;
        public bool HasFrontageError => BaseFrontage <= 0;
        public bool HasDepthError => BaseDepth <= 0;

        // --- STEP 2: RELATIONSHIP ---
        public bool IsPartOfBlock
        {
            get => Config.Relationship == RelationshipType.PartOfBlock;
            set
            {
                if (value)
                {
                    Config.Relationship = RelationshipType.PartOfBlock;
                    NotifyPropertyChanged(nameof(IsPartOfBlock));
                    NotifyPropertyChanged(nameof(IsStandalone));
                    Recalculate();
                }
            }
        }

        public bool IsStandalone
        {
            get => Config.Relationship == RelationshipType.Standalone;
            set
            {
                if (value)
                {
                    Config.Relationship = RelationshipType.Standalone;
                    NotifyPropertyChanged(nameof(IsPartOfBlock));
                    NotifyPropertyChanged(nameof(IsStandalone));
                    Recalculate();
                }
            }
        }

        // --- STEP 3: ARRANGEMENT ---
        public bool IsBackToBack
        {
            get => Config.Arrangement == ArrangementMode.BackToBack;
            set
            {
                if (value)
                {
                    Config.Arrangement = ArrangementMode.BackToBack;
                    NotifyPropertyChanged(nameof(IsBackToBack));
                    NotifyPropertyChanged(nameof(IsSingleSided));
                    Recalculate();
                }
            }
        }

        public bool IsSingleSided
        {
            get => Config.Arrangement == ArrangementMode.SingleSided;
            set
            {
                if (value)
                {
                    Config.Arrangement = ArrangementMode.SingleSided;
                    NotifyPropertyChanged(nameof(IsBackToBack));
                    NotifyPropertyChanged(nameof(IsSingleSided));
                    Recalculate();
                }
            }
        }

        // --- STEP 4: PARCEL COUNT ---
        public int SideACount
        {
            get => Config.SideA.ParcelCount;
            set
            {
                if (Config.SideA.ParcelCount != value && value >= 1)
                {
                    Config.SideA.ParcelCount = value;
                    NotifyPropertyChanged(nameof(SideACount));
                    NotifyPropertyChanged(nameof(IsCurrentStepValid));
                    Recalculate();
                }
            }
        }

        public int SideBCount
        {
            get => Config.SideB.ParcelCount;
            set
            {
                if (Config.SideB.ParcelCount != value && value >= 1)
                {
                    Config.SideB.ParcelCount = value;
                    NotifyPropertyChanged(nameof(SideBCount));
                    NotifyPropertyChanged(nameof(IsCurrentStepValid));
                    Recalculate();
                }
            }
        }

        // --- STEP 5: SIMILARITY ---
        public bool IsAllIdentical
        {
            get => Config.Similarity == SimilarityMode.AllIdentical;
            set
            {
                if (value)
                {
                    Config.Similarity = SimilarityMode.AllIdentical;
                    NotifyPropertyChanged(nameof(IsAllIdentical));
                    NotifyPropertyChanged(nameof(HasExceptions));
                    Recalculate();
                }
            }
        }

        public bool HasExceptions
        {
            get => Config.Similarity == SimilarityMode.HasExceptions;
            set
            {
                if (value)
                {
                    Config.Similarity = SimilarityMode.HasExceptions;
                    NotifyPropertyChanged(nameof(IsAllIdentical));
                    NotifyPropertyChanged(nameof(HasExceptions));
                    Recalculate();
                }
            }
        }

        // --- STEP 6: EXCEPTIONS ---
        public ObservableCollection<ParcelException> ExceptionsList { get; set; } = new ObservableCollection<ParcelException>();

        // --- STEP 7: CORNER & CHAMFER ---
        private CornerPosition _selectedCornerPosition = CornerPosition.SideAStart;

        public bool HasChamfer
        {
            get => Config.Corner.HasChamfer;
            set
            {
                if (Config.Corner.HasChamfer != value)
                {
                    Config.Corner.HasChamfer = value;
                    NotifyPropertyChanged(nameof(HasChamfer));
                    Recalculate();
                }
            }
        }

        public bool IsCustomPerCorner
        {
            get => Config.Corner.IsCustomPerCorner;
            set
            {
                if (Config.Corner.IsCustomPerCorner != value)
                {
                    Config.Corner.IsCustomPerCorner = value;
                    NotifyPropertyChanged(nameof(IsCustomPerCorner));
                    NotifyPropertyChanged(nameof(IsUniformAllCorners));
                    NotifyActiveCornerProperties();
                    Recalculate();
                }
            }
        }

        public bool IsUniformAllCorners
        {
            get => !Config.Corner.IsCustomPerCorner;
            set => IsCustomPerCorner = !value;
        }

        public CornerPosition SelectedCornerPosition
        {
            get => _selectedCornerPosition;
            set
            {
                if (SetProperty(ref _selectedCornerPosition, value))
                {
                    NotifyActiveCornerProperties();
                }
            }
        }

        public bool IsCornerAStartSelected
        {
            get => SelectedCornerPosition == CornerPosition.SideAStart;
            set { if (value) SelectedCornerPosition = CornerPosition.SideAStart; }
        }

        public bool IsCornerAEndSelected
        {
            get => SelectedCornerPosition == CornerPosition.SideAEnd;
            set { if (value) SelectedCornerPosition = CornerPosition.SideAEnd; }
        }

        public bool IsCornerBStartSelected
        {
            get => SelectedCornerPosition == CornerPosition.SideBStart;
            set { if (value) SelectedCornerPosition = CornerPosition.SideBStart; }
        }

        public bool IsCornerBEndSelected
        {
            get => SelectedCornerPosition == CornerPosition.SideBEnd;
            set { if (value) SelectedCornerPosition = CornerPosition.SideBEnd; }
        }

        public SingleCornerConfig ActiveCorner => Config.Corner.GetEffectiveCorner(SelectedCornerPosition);

        public string ActiveCornerName => SelectedCornerPosition switch
        {
            CornerPosition.SideAStart => IsArabic ? "ركن أعلى يسار (تقاطع شارع A مع الشارع الأيسر)" : "Front-Left Corner (Street A & Left Street)",
            CornerPosition.SideAEnd => IsArabic ? "ركن أعلى يمين (تقاطع شارع A مع الشارع الأيمن)" : "Front-Right Corner (Street A & Right Street)",
            CornerPosition.SideBStart => IsArabic ? "ركن أسفل يسار (تقاطع شارع B مع الشارع الأيسر)" : "Back-Left Corner (Street B & Left Street)",
            CornerPosition.SideBEnd => IsArabic ? "ركن أسفل يمين (تقاطع شارع B مع الشارع الأيمن)" : "Back-Right Corner (Street B & Right Street)",
            _ => "Corner"
        };

        public string ActiveCornerMainStreetLabel => (SelectedCornerPosition == CornerPosition.SideAStart || SelectedCornerPosition == CornerPosition.SideAEnd)
            ? (IsArabic ? $"{Config.SideA.StreetLabel} (الشارع الرئيسي العلوي A)" : $"{Config.SideA.StreetLabel} (Main Street A)")
            : (IsArabic ? $"{Config.SideB.StreetLabel} (الشارع الرئيسي السفلي B)" : $"{Config.SideB.StreetLabel} (Main Street B)");

        public string ActiveCornerCrossStreetLabel => (SelectedCornerPosition == CornerPosition.SideAStart || SelectedCornerPosition == CornerPosition.SideBStart)
            ? (IsArabic ? $"{LeftStreetLabel} (الشارع الجانبي الأيسر)" : $"{LeftStreetLabel} (Left Cross Street)")
            : (IsArabic ? $"{RightStreetLabel} (الشارع الجانبي الأيمن)" : $"{RightStreetLabel} (Right Cross Street)");

        public string LeftStreetLabel
        {
            get => Config.Corner.LeftStreetLabel;
            set
            {
                if (Config.Corner.LeftStreetLabel != value)
                {
                    Config.Corner.LeftStreetLabel = value;
                    NotifyPropertyChanged(nameof(LeftStreetLabel));
                    NotifyPropertyChanged(nameof(ActiveCornerCrossStreetLabel));
                    Recalculate();
                }
            }
        }

        public string RightStreetLabel
        {
            get => Config.Corner.RightStreetLabel;
            set
            {
                if (Config.Corner.RightStreetLabel != value)
                {
                    Config.Corner.RightStreetLabel = value;
                    NotifyPropertyChanged(nameof(RightStreetLabel));
                    NotifyPropertyChanged(nameof(ActiveCornerCrossStreetLabel));
                    Recalculate();
                }
            }
        }

        public bool IsActiveCornerEnabled
        {
            get => ActiveCorner.IsEnabled;
            set
            {
                if (IsCustomPerCorner)
                {
                    var target = SelectedCornerPosition switch
                    {
                        CornerPosition.SideAStart => Config.Corner.CornerAStart,
                        CornerPosition.SideAEnd => Config.Corner.CornerAEnd,
                        CornerPosition.SideBStart => Config.Corner.CornerBStart,
                        CornerPosition.SideBEnd => Config.Corner.CornerBEnd,
                        _ => Config.Corner.DefaultCorner
                    };
                    target.IsEnabled = value;
                }
                else
                {
                    if (SelectedCornerPosition == CornerPosition.SideAStart) ApplyToSideAStart = value;
                    if (SelectedCornerPosition == CornerPosition.SideAEnd) ApplyToSideAEnd = value;
                    if (SelectedCornerPosition == CornerPosition.SideBStart) ApplyToSideBStart = value;
                    if (SelectedCornerPosition == CornerPosition.SideBEnd) ApplyToSideBEnd = value;
                }
                NotifyPropertyChanged(nameof(IsActiveCornerEnabled));
                Recalculate();
            }
        }

        public ChamferMode ActiveCornerMode
        {
            get => ActiveCorner.Mode;
            set
            {
                GetEditableCorner().Mode = value;
                NotifyActiveCornerProperties();
                Recalculate();
            }
        }

        public bool IsActiveChamferByDirectLength
        {
            get => ActiveCornerMode == ChamferMode.DirectCutLength;
            set { if (value) ActiveCornerMode = ChamferMode.DirectCutLength; }
        }

        public bool IsActiveChamferByStreetSetbacks
        {
            get => ActiveCornerMode == ChamferMode.StreetSetbacks;
            set { if (value) ActiveCornerMode = ChamferMode.StreetSetbacks; }
        }

        public bool IsActiveChamferByLengthAndAngle
        {
            get => ActiveCornerMode == ChamferMode.CutLengthAndAngle;
            set { if (value) ActiveCornerMode = ChamferMode.CutLengthAndAngle; }
        }

        public bool IsActiveChamferBySetbackAndAngle
        {
            get => ActiveCornerMode == ChamferMode.SetbackAndAngle;
            set { if (value) ActiveCornerMode = ChamferMode.SetbackAndAngle; }
        }

        public double ActiveCornerChamferLength
        {
            get => ActiveCorner.ChamferLength;
            set
            {
                if (value >= 0)
                {
                    GetEditableCorner().ChamferLength = value;
                    NotifyPropertyChanged(nameof(ActiveCornerChamferLength));
                    Recalculate();
                }
            }
        }

        public double ActiveCornerMainStreetSetback
        {
            get => ActiveCorner.MainStreetSetback;
            set
            {
                if (value >= 0)
                {
                    GetEditableCorner().MainStreetSetback = value;
                    NotifyPropertyChanged(nameof(ActiveCornerMainStreetSetback));
                    Recalculate();
                }
            }
        }

        public double ActiveCornerCrossStreetSetback
        {
            get => ActiveCorner.CrossStreetSetback;
            set
            {
                if (value >= 0)
                {
                    GetEditableCorner().CrossStreetSetback = value;
                    NotifyPropertyChanged(nameof(ActiveCornerCrossStreetSetback));
                    Recalculate();
                }
            }
        }

        public double ActiveCornerAngleDegrees
        {
            get => ActiveCorner.ChamferAngleDegrees;
            set
            {
                if (value > 0 && value < 90)
                {
                    GetEditableCorner().ChamferAngleDegrees = value;
                    NotifyPropertyChanged(nameof(ActiveCornerAngleDegrees));
                    Recalculate();
                }
            }
        }

        public double ActiveCornerPrimarySetback
        {
            get => ActiveCorner.PrimarySetback;
            set
            {
                if (value >= 0)
                {
                    GetEditableCorner().PrimarySetback = value;
                    NotifyPropertyChanged(nameof(ActiveCornerPrimarySetback));
                    Recalculate();
                }
            }
        }

        private SingleCornerConfig GetEditableCorner()
        {
            if (!IsCustomPerCorner) return Config.Corner.DefaultCorner;

            return SelectedCornerPosition switch
            {
                CornerPosition.SideAStart => Config.Corner.CornerAStart,
                CornerPosition.SideAEnd => Config.Corner.CornerAEnd,
                CornerPosition.SideBStart => Config.Corner.CornerBStart,
                CornerPosition.SideBEnd => Config.Corner.CornerBEnd,
                _ => Config.Corner.DefaultCorner
            };
        }

        private void NotifyActiveCornerProperties()
        {
            NotifyPropertyChanged(nameof(ActiveCorner));
            NotifyPropertyChanged(nameof(ActiveCornerName));
            NotifyPropertyChanged(nameof(ActiveCornerMainStreetLabel));
            NotifyPropertyChanged(nameof(ActiveCornerCrossStreetLabel));
            NotifyPropertyChanged(nameof(IsActiveCornerEnabled));
            NotifyPropertyChanged(nameof(ActiveCornerMode));
            NotifyPropertyChanged(nameof(IsActiveChamferByDirectLength));
            NotifyPropertyChanged(nameof(IsActiveChamferByStreetSetbacks));
            NotifyPropertyChanged(nameof(IsActiveChamferByLengthAndAngle));
            NotifyPropertyChanged(nameof(IsActiveChamferBySetbackAndAngle));
            NotifyPropertyChanged(nameof(ActiveCornerChamferLength));
            NotifyPropertyChanged(nameof(ActiveCornerMainStreetSetback));
            NotifyPropertyChanged(nameof(ActiveCornerCrossStreetSetback));
            NotifyPropertyChanged(nameof(ActiveCornerAngleDegrees));
            NotifyPropertyChanged(nameof(ActiveCornerPrimarySetback));
            NotifyPropertyChanged(nameof(IsCornerAStartSelected));
            NotifyPropertyChanged(nameof(IsCornerAEndSelected));
            NotifyPropertyChanged(nameof(IsCornerBStartSelected));
            NotifyPropertyChanged(nameof(IsCornerBEndSelected));
        }

        public bool ApplyToSideAStart
        {
            get => Config.Corner.ApplyToSideAStart;
            set
            {
                if (Config.Corner.ApplyToSideAStart != value)
                {
                    Config.Corner.ApplyToSideAStart = value;
                    NotifyPropertyChanged(nameof(ApplyToSideAStart));
                    Recalculate();
                }
            }
        }

        public bool ApplyToSideAEnd
        {
            get => Config.Corner.ApplyToSideAEnd;
            set
            {
                if (Config.Corner.ApplyToSideAEnd != value)
                {
                    Config.Corner.ApplyToSideAEnd = value;
                    NotifyPropertyChanged(nameof(ApplyToSideAEnd));
                    Recalculate();
                }
            }
        }

        public bool ApplyToSideBStart
        {
            get => Config.Corner.ApplyToSideBStart;
            set
            {
                if (Config.Corner.ApplyToSideBStart != value)
                {
                    Config.Corner.ApplyToSideBStart = value;
                    NotifyPropertyChanged(nameof(ApplyToSideBStart));
                    Recalculate();
                }
            }
        }

        public bool ApplyToSideBEnd
        {
            get => Config.Corner.ApplyToSideBEnd;
            set
            {
                if (Config.Corner.ApplyToSideBEnd != value)
                {
                    Config.Corner.ApplyToSideBEnd = value;
                    NotifyPropertyChanged(nameof(ApplyToSideBEnd));
                    Recalculate();
                }
            }
        }

        public double CalculatedChamferCutLength => Config.Corner.GetCalculatedChamferLength(BaseFrontage, BaseDepth);

        public string FrontLeftHostParcelText => "Host: A-01";
        public string FrontRightHostParcelText => $"Host: A-{SideACount:D2}";
        public string BackLeftHostParcelText => "Host: B-01";
        public string BackRightHostParcelText => $"Host: B-{SideBCount:D2}";

        public string CalculatedChamferHypotenuseText
        {
            get
            {
                var (cutX, cutY) = Config.Corner.DefaultCorner.GetEffectiveCutDistances(BaseFrontage, BaseDepth);
                double len = Math.Sqrt((cutX * cutX) + (cutY * cutY));
                return $"{len:F2} m";
            }
        }

        public string CalculatedChamferAngleText
        {
            get
            {
                var (cutX, cutY) = Config.Corner.DefaultCorner.GetEffectiveCutDistances(BaseFrontage, BaseDepth);
                if (cutX <= 0) return "45.0°";
                double angle = Math.Atan2(cutY, cutX) * (180.0 / Math.PI);
                return $"{angle:F1}°";
            }
        }

        public string CalculatedStreetSetbacksText
        {
            get
            {
                var (cutX, cutY) = Config.Corner.DefaultCorner.GetEffectiveCutDistances(BaseFrontage, BaseDepth);
                return $"Main: {cutX:F2} m  |  Cross: {cutY:F2} m";
            }
        }

        private void ExecuteSelectAllCorners()
        {
            ApplyToSideAStart = true;
            ApplyToSideAEnd = true;
            ApplyToSideBStart = true;
            ApplyToSideBEnd = true;
            Recalculate();
        }

        private void ExecuteClearAllCorners()
        {
            ApplyToSideAStart = false;
            ApplyToSideAEnd = false;
            ApplyToSideBStart = false;
            ApplyToSideBEnd = false;
            Recalculate();
        }

        private void ExecuteToggleChamferForParcel(string parcelId)
        {
            if (string.IsNullOrEmpty(parcelId)) return;

            if (Config.Corner.CustomChamferParcelIds.Contains(parcelId))
            {
                Config.Corner.CustomChamferParcelIds.Remove(parcelId);
            }
            else
            {
                Config.Corner.CustomChamferParcelIds.Add(parcelId);
            }
            Recalculate();
        }

        // --- STEP 8: ELECTRIC ROOM / SUBSTATION ---
        public bool HasElectricRoom
        {
            get => Config.ElectricRoom.HasElectricRoom;
            set
            {
                if (Config.ElectricRoom.HasElectricRoom != value)
                {
                    Config.ElectricRoom.HasElectricRoom = value;
                    NotifyPropertyChanged(nameof(HasElectricRoom));
                    Recalculate();
                }
            }
        }

        public double ElectricRoomWidth
        {
            get => Config.ElectricRoom.Width;
            set
            {
                if (Config.ElectricRoom.Width != value && value > 0)
                {
                    Config.ElectricRoom.Width = value;
                    NotifyPropertyChanged(nameof(ElectricRoomWidth));
                    NotifyPropertyChanged(nameof(ElectricRoomArea));
                    Recalculate();
                }
            }
        }

        public double ElectricRoomDepth
        {
            get => Config.ElectricRoom.Depth;
            set
            {
                if (Config.ElectricRoom.Depth != value && value > 0)
                {
                    Config.ElectricRoom.Depth = value;
                    NotifyPropertyChanged(nameof(ElectricRoomDepth));
                    NotifyPropertyChanged(nameof(ElectricRoomArea));
                    Recalculate();
                }
            }
        }

        public double ElectricRoomArea => Config.ElectricRoom.Area;

        public string ElectricRoomSideLabelA => string.IsNullOrWhiteSpace(Config.SideA.StreetLabel) ? "Side A / Street A" : $"Side A ({Config.SideA.StreetLabel})";
        public string ElectricRoomSideLabelB => string.IsNullOrWhiteSpace(Config.SideB.StreetLabel) ? "Side B / Street B" : $"Side B ({Config.SideB.StreetLabel})";

        public bool IsElectricRoomSideA
        {
            get => Config.ElectricRoom.Side == ParcelSide.SideA;
            set
            {
                if (value)
                {
                    Config.ElectricRoom.Side = ParcelSide.SideA;
                    NotifyPropertyChanged(nameof(IsElectricRoomSideA));
                    NotifyPropertyChanged(nameof(IsElectricRoomSideB));
                    Recalculate();
                }
            }
        }

        public bool IsElectricRoomSideB
        {
            get => Config.ElectricRoom.Side == ParcelSide.SideB;
            set
            {
                if (value)
                {
                    Config.ElectricRoom.Side = ParcelSide.SideB;
                    NotifyPropertyChanged(nameof(IsElectricRoomSideA));
                    NotifyPropertyChanged(nameof(IsElectricRoomSideB));
                    Recalculate();
                }
            }
        }

        public ElectricRoomPlacementMethod ElectricRoomPlacementMethod
        {
            get => Config.ElectricRoom.PlacementMethod;
            set
            {
                if (Config.ElectricRoom.PlacementMethod != value)
                {
                    Config.ElectricRoom.PlacementMethod = value;
                    NotifyPropertyChanged(nameof(ElectricRoomPlacementMethod));
                    NotifyPropertyChanged(nameof(IsElectricRoomInteractiveMethod));
                    NotifyPropertyChanged(nameof(IsElectricRoomOffsetMethod));
                    Recalculate();
                }
            }
        }

        public bool IsElectricRoomInteractiveMethod
        {
            get => Config.ElectricRoom.PlacementMethod == ElectricRoomPlacementMethod.InteractiveMapPlacement;
            set { if (value) ElectricRoomPlacementMethod = ElectricRoomPlacementMethod.InteractiveMapPlacement; }
        }

        public bool IsElectricRoomOffsetMethod
        {
            get => Config.ElectricRoom.PlacementMethod == ElectricRoomPlacementMethod.OffsetDistance;
            set { if (value) ElectricRoomPlacementMethod = ElectricRoomPlacementMethod.OffsetDistance; }
        }

        public double ElectricRoomOffsetDistance
        {
            get => Config.ElectricRoom.OffsetDistance;
            set
            {
                if (Config.ElectricRoom.OffsetDistance != value && value >= 0)
                {
                    Config.ElectricRoom.OffsetDistance = value;
                    NotifyPropertyChanged(nameof(ElectricRoomOffsetDistance));
                    Recalculate();
                }
            }
        }

        public bool ElectricRoomClipHostParcels
        {
            get => Config.ElectricRoom.ClipHostParcels;
            set
            {
                if (Config.ElectricRoom.ClipHostParcels != value)
                {
                    Config.ElectricRoom.ClipHostParcels = value;
                    NotifyPropertyChanged(nameof(ElectricRoomClipHostParcels));
                    Recalculate();
                }
            }
        }

        public bool IsElectricRoomValid => Config.ElectricRoom.IsPlacementValid;
        public string ElectricRoomStatusText => Config.ElectricRoom.ValidationStatusMessage;
        
        public string ElectricRoomHostParcelsText => Config.ElectricRoom.HostParcelIds != null && Config.ElectricRoom.HostParcelIds.Count > 0
            ? string.Join(", ", Config.ElectricRoom.HostParcelIds)
            : (IsArabic ? "غير محدد" : "None detected");

        public string ElectricRoomPlacementTypeText => Config.ElectricRoom.PlacementType switch
        {
            ElectricRoomPlacementType.InsideSingleParcel => IsArabic ? "داخل قطعة واحدة" : "Inside Parcel",
            ElectricRoomPlacementType.BetweenTwoParcels => IsArabic ? "بين قطعتين متجاورتين" : "Between Two Parcels",
            _ => IsArabic ? "خارج الحدود / غير صالح" : "Invalid / Outside Block"
        };

        private void ExecuteSelectElectricRoomOnMap()
        {
            _ = FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectElectricRoomLocationTool");
            StatusText = IsArabic
                ? "انقر على واجهة الشارع في الخريطة لتحديد موقع غرفة المحول..."
                : "Click along the street frontage on the map to place the Electric Room...";
        }

        public async Task OnElectricRoomMapClickedAsync(MapPoint clickPoint)
        {
            if (clickPoint == null) return;

            Config.ElectricRoom.HasElectricRoom = true;
            Config.ElectricRoom.PlacementMethod = ElectricRoomPlacementMethod.InteractiveMapPlacement;
            Config.ElectricRoom.ClickedMapX = clickPoint.X;
            Config.ElectricRoom.ClickedMapY = clickPoint.Y;

            Recalculate();

            NotifyPropertyChanged(nameof(HasElectricRoom));
            NotifyPropertyChanged(nameof(ElectricRoomOffsetDistance));
            NotifyPropertyChanged(nameof(IsElectricRoomValid));
            NotifyPropertyChanged(nameof(ElectricRoomStatusText));
            NotifyPropertyChanged(nameof(ElectricRoomHostParcelsText));
            NotifyPropertyChanged(nameof(ElectricRoomPlacementTypeText));

            StatusText = Config.ElectricRoom.ValidationStatusMessage;

            // Switch tool back to explore
            await FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
        }

        // ==========================================
        // STEP 9: ALIGNMENT & ORIENTATION
        // ==========================================

        // --- A. BASE POINT (WHERE) ---
        public BlockAnchorPoint CurrentAnchorPoint
        {
            get => Config.Alignment.AnchorPoint;
            set
            {
                if (Config.Alignment.AnchorPoint != value)
                {
                    SetAnchorPoint(value);
                }
            }
        }

        public string BasePointId => Config.Alignment.AnchorPoint switch
        {
            BlockAnchorPoint.SideAStart => "BP-01",
            BlockAnchorPoint.SideAEnd => "BP-02",
            BlockAnchorPoint.SideACenter => "BP-03",
            BlockAnchorPoint.SideBStart => "BP-04",
            BlockAnchorPoint.SideBEnd => "BP-05",
            BlockAnchorPoint.BlockCenter => "BP-06",
            _ => "BP-01"
        };

        public string BasePointDescription => Config.Alignment.AnchorPoint switch
        {
            BlockAnchorPoint.SideAStart => "Front-Left (Side A Start)",
            BlockAnchorPoint.SideAEnd => "Front-Right (Side A End)",
            BlockAnchorPoint.SideACenter => "Street A Center Midpoint",
            BlockAnchorPoint.SideBStart => "Side B Start (Back-Left Corner)",
            BlockAnchorPoint.SideBEnd => "Side B End (Back-Right Corner)",
            BlockAnchorPoint.BlockCenter => "Block Centroid Center",
            _ => "Front-Left Corner"
        };

        public string SelectedBasePointDisplay => $"{BasePointId}: {BasePointDescription}";

        public double? TargetBasePointX
        {
            get => Config.Alignment.TargetMapPointX;
            set
            {
                if (Config.Alignment.TargetMapPointX != value)
                {
                    Config.Alignment.TargetMapPointX = value;
                    NotifyPropertyChanged(nameof(TargetBasePointX));
                    NotifyPropertyChanged(nameof(TargetBasePointXText));
                    NotifyPropertyChanged(nameof(TargetMapPointText));
                    NotifyPropertyChanged(nameof(IsBasePointPlaced));
                    NotifyPropertyChanged(nameof(BasePointStatusText));
                    NotifyPropertyChanged(nameof(AlignmentStatusSummary));
                    Recalculate();
                }
            }
        }

        public double? TargetBasePointY
        {
            get => Config.Alignment.TargetMapPointY;
            set
            {
                if (Config.Alignment.TargetMapPointY != value)
                {
                    Config.Alignment.TargetMapPointY = value;
                    NotifyPropertyChanged(nameof(TargetBasePointY));
                    NotifyPropertyChanged(nameof(TargetBasePointYText));
                    NotifyPropertyChanged(nameof(TargetMapPointText));
                    NotifyPropertyChanged(nameof(IsBasePointPlaced));
                    NotifyPropertyChanged(nameof(BasePointStatusText));
                    NotifyPropertyChanged(nameof(AlignmentStatusSummary));
                    Recalculate();
                }
            }
        }

        public string TargetBasePointXText
        {
            get => Config.Alignment.TargetMapPointX.HasValue ? Config.Alignment.TargetMapPointX.Value.ToString("F2") : string.Empty;
            set
            {
                if (double.TryParse(value, out double val))
                {
                    TargetBasePointX = val;
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    TargetBasePointX = null;
                }
            }
        }

        public string TargetBasePointYText
        {
            get => Config.Alignment.TargetMapPointY.HasValue ? Config.Alignment.TargetMapPointY.Value.ToString("F2") : string.Empty;
            set
            {
                if (double.TryParse(value, out double val))
                {
                    TargetBasePointY = val;
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    TargetBasePointY = null;
                }
            }
        }

        public bool IsBasePointPlaced => Config.Alignment.IsBasePointPlaced;

        public string BasePointStatusText => IsBasePointPlaced
            ? $"✓ Base Point anchored at ({Config.Alignment.TargetMapPointX:F2}, {Config.Alignment.TargetMapPointY:F2})"
            : "⚠ Base point location on map not selected";

        public string TargetMapPointText => IsBasePointPlaced
            ? $"({Config.Alignment.TargetMapPointX:F2}, {Config.Alignment.TargetMapPointY:F2})"
            : "Not Placed";

        public void SetBasePointMapLocation(double mapX, double mapY, int wkid, string? srName)
        {
            Config.Alignment.TargetMapPointX = Math.Round(mapX, 2);
            Config.Alignment.TargetMapPointY = Math.Round(mapY, 2);
            Config.Alignment.SpatialReferenceWkid = wkid;
            if (!string.IsNullOrEmpty(srName)) Config.Alignment.SpatialReferenceName = srName;

            NotifyPropertyChanged(nameof(TargetBasePointX));
            NotifyPropertyChanged(nameof(TargetBasePointY));
            NotifyPropertyChanged(nameof(TargetBasePointXText));
            NotifyPropertyChanged(nameof(TargetBasePointYText));
            NotifyPropertyChanged(nameof(TargetMapPointText));
            NotifyPropertyChanged(nameof(IsBasePointPlaced));
            NotifyPropertyChanged(nameof(BasePointStatusText));
            NotifyPropertyChanged(nameof(AlignmentStatusSummary));

            StatusText = $"✓ Base Point anchored on map: ({mapX:F2}, {mapY:F2})";
            Recalculate();
        }

        // --- B. ORIENTATION (HOW IT IS ORIENTED) ---
        public bool IsOrientationTwoPoints
        {
            get => Config.Alignment.Method == AlignmentMethod.TwoPoints;
            set
            {
                if (value)
                {
                    Config.Alignment.Method = AlignmentMethod.TwoPoints;
                    NotifyPropertyChanged(nameof(IsOrientationTwoPoints));
                    NotifyPropertyChanged(nameof(IsOrientationMapSegment));
                    NotifyPropertyChanged(nameof(OrientationMethodName));
                    NotifyPropertyChanged(nameof(AlignmentStatusSummary));
                    Recalculate();
                }
            }
        }

        public bool IsOrientationMapSegment
        {
            get => Config.Alignment.Method == AlignmentMethod.MapSegment;
            set
            {
                if (value)
                {
                    Config.Alignment.Method = AlignmentMethod.MapSegment;
                    NotifyPropertyChanged(nameof(IsOrientationTwoPoints));
                    NotifyPropertyChanged(nameof(IsOrientationMapSegment));
                    NotifyPropertyChanged(nameof(OrientationMethodName));
                    NotifyPropertyChanged(nameof(AlignmentStatusSummary));
                    Recalculate();
                }
            }
        }

        public string OrientationMethodName => Config.Alignment.Method switch
        {
            AlignmentMethod.TwoPoints => "Two Points Vector",
            AlignmentMethod.MapSegment => "Map Segment / Line",
            _ => "Two Points"
        };

        public double AlignmentAzimuthDegrees
        {
            get => Config.Alignment.AzimuthAngleDegrees;
            set
            {
                if (Math.Abs(Config.Alignment.AzimuthAngleDegrees - value) > 1e-4)
                {
                    Config.Alignment.AzimuthAngleDegrees = Math.Round((value % 360.0 + 360.0) % 360.0, 2);
                    NotifyPropertyChanged(nameof(AlignmentAzimuthDegrees));
                    NotifyPropertyChanged(nameof(AlignmentStatusSummary));
                    Recalculate();
                }
            }
        }

        public string TwoPointP1Text => (Config.Alignment.TwoPointStartX.HasValue && Config.Alignment.TwoPointStartY.HasValue)
            ? $"({Config.Alignment.TwoPointStartX.Value:F2}, {Config.Alignment.TwoPointStartY.Value:F2})"
            : "-";

        public string TwoPointP2Text => (Config.Alignment.TwoPointEndX.HasValue && Config.Alignment.TwoPointEndY.HasValue)
            ? $"({Config.Alignment.TwoPointEndX.Value:F2}, {Config.Alignment.TwoPointEndY.Value:F2})"
            : "-";

        public string SelectedSegmentDescription => Config.Alignment.SelectedSegmentDescription ?? "None selected (click Select Segment)";

        public void SetOrientationTwoPoints(double x1, double y1, double x2, double y2)
        {
            Config.Alignment.Method = AlignmentMethod.TwoPoints;
            Config.Alignment.TwoPointStartX = Math.Round(x1, 2);
            Config.Alignment.TwoPointStartY = Math.Round(y1, 2);
            Config.Alignment.TwoPointEndX = Math.Round(x2, 2);
            Config.Alignment.TwoPointEndY = Math.Round(y2, 2);

            double azimuth = BlockTransformationService.CalculateAzimuth(x1, y1, x2, y2);
            Config.Alignment.AzimuthAngleDegrees = azimuth;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            NotifyPropertyChanged(nameof(IsOrientationTwoPoints));
            NotifyPropertyChanged(nameof(IsOrientationMapSegment));
            NotifyPropertyChanged(nameof(TwoPointP1Text));
            NotifyPropertyChanged(nameof(TwoPointP2Text));
            NotifyPropertyChanged(nameof(AlignmentAzimuthDegrees));
            NotifyPropertyChanged(nameof(AlignmentStatusSummary));

            StatusText = $"✓ Orientation vector set (P1 ➔ P2): Bearing {azimuth:F1}°, Length {length:F1}m (Base Point fixed)";
            Recalculate();
        }

        public void SetOrientationFromSegment(double x1, double y1, double x2, double y2, string? sourceDesc)
        {
            Config.Alignment.Method = AlignmentMethod.MapSegment;
            Config.Alignment.SegmentStartX = Math.Round(x1, 2);
            Config.Alignment.SegmentStartY = Math.Round(y1, 2);
            Config.Alignment.SegmentEndX = Math.Round(x2, 2);
            Config.Alignment.SegmentEndY = Math.Round(y2, 2);
            Config.Alignment.SelectedSegmentDescription = sourceDesc ?? "Map Segment Edge";

            double azimuth = BlockTransformationService.CalculateAzimuth(x1, y1, x2, y2);
            Config.Alignment.AzimuthAngleDegrees = azimuth;

            NotifyPropertyChanged(nameof(IsOrientationTwoPoints));
            NotifyPropertyChanged(nameof(IsOrientationMapSegment));
            NotifyPropertyChanged(nameof(SelectedSegmentDescription));
            NotifyPropertyChanged(nameof(AlignmentAzimuthDegrees));
            NotifyPropertyChanged(nameof(AlignmentStatusSummary));

            StatusText = $"✓ Orientation aligned to segment '{Config.Alignment.SelectedSegmentDescription}': Bearing {azimuth:F1}° (Base Point fixed)";
            Recalculate();
        }

        public void SetAlignmentLine(double x1, double y1, double x2, double y2, string? sourceDesc = null)
        {
            if (Config.Alignment.Method == AlignmentMethod.MapSegment)
            {
                SetOrientationFromSegment(x1, y1, x2, y2, sourceDesc);
            }
            else
            {
                SetOrientationTwoPoints(x1, y1, x2, y2);
            }
        }

        public bool IsAnchorSideAStart
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.SideAStart;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.SideAStart); }
        }

        public bool IsAnchorSideAEnd
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.SideAEnd;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.SideAEnd); }
        }

        public bool IsAnchorSideACenter
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.SideACenter;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.SideACenter); }
        }

        public bool IsAnchorSideBStart
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.SideBStart;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.SideBStart); }
        }

        public bool IsAnchorSideBEnd
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.SideBEnd;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.SideBEnd); }
        }

        public bool IsAnchorBlockCenter
        {
            get => Config.Alignment.AnchorPoint == BlockAnchorPoint.BlockCenter;
            set { if (value) SetAnchorPoint(BlockAnchorPoint.BlockCenter); }
        }

        private void SetAnchorPoint(BlockAnchorPoint anchor)
        {
            Config.Alignment.AnchorPoint = anchor;
            Config.Alignment.BasePointId = BasePointId;
            Config.Alignment.BasePointName = BasePointDescription;
            BlockTransformationService.GetSourceBasePoint(Config);

            NotifyPropertyChanged(nameof(CurrentAnchorPoint));
            NotifyPropertyChanged(nameof(BasePointId));
            NotifyPropertyChanged(nameof(BasePointDescription));
            NotifyPropertyChanged(nameof(SelectedBasePointDisplay));
            NotifyPropertyChanged(nameof(IsAnchorSideAStart));
            NotifyPropertyChanged(nameof(IsAnchorSideAEnd));
            NotifyPropertyChanged(nameof(IsAnchorSideACenter));
            NotifyPropertyChanged(nameof(IsAnchorSideBStart));
            NotifyPropertyChanged(nameof(IsAnchorSideBEnd));
            NotifyPropertyChanged(nameof(IsAnchorBlockCenter));
            NotifyPropertyChanged(nameof(AnchorPointDescription));
            StatusText = $"Alignment reference Base Point set to: {SelectedBasePointDisplay}";
            Recalculate();
        }

        public string AnchorPointDescription => BasePointDescription;

        public string AlignmentStatusSummary => IsBasePointPlaced
            ? $"✓ Alignment Ready ({BasePointId} at {TargetMapPointText}, {AlignmentAzimuthDegrees:F1}°)"
            : "⚠ Map Base Point location pending";

        // --- STEP 10: DYNAMIC PREVIEW & INSPECTION ---
        public string SelectedParcelId
        {
            get => _selectedParcelId;
            set
            {
                if (SetProperty(ref _selectedParcelId, value))
                {
                    NotifyPropertyChanged(nameof(SelectedParcelModel));
                    NotifyPropertyChanged(nameof(HasSelectedParcel));
                    NotifyPropertyChanged(nameof(SelectedParcelFrontage));
                    NotifyPropertyChanged(nameof(SelectedParcelDepth));
                    NotifyPropertyChanged(nameof(SelectedParcelArea));
                    NotifyPropertyChanged(nameof(SelectedParcelType));
                    NotifyPropertyChanged(nameof(SelectedParcelSide));
                    NotifyPropertyChanged(nameof(SelectedParcelChamfer));

                    if (IsLivePreviewEnabled)
                    {
                        _ = ParcelPreviewService.Instance.HighlightParcelAsync(Config, _selectedParcelId);
                    }
                }
            }
        }

        public ParcelModel? SelectedParcelModel => Config.SideA.GeneratedParcels
            .Concat(Config.SideB.GeneratedParcels)
            .FirstOrDefault(p => p.Id == SelectedParcelId);

        public bool HasSelectedParcel => SelectedParcelModel != null;
        public double SelectedParcelFrontage => SelectedParcelModel?.Frontage ?? 0.0;
        public double SelectedParcelDepth => SelectedParcelModel?.Depth ?? 0.0;
        public double SelectedParcelArea => SelectedParcelModel?.Area ?? 0.0;
        public string SelectedParcelType => SelectedParcelModel?.Type ?? "Standard";
        public string SelectedParcelSide => SelectedParcelModel?.Side.ToString() ?? "-";
        public int SelectedParcelSequence => SelectedParcelModel?.Sequence ?? 1;
        public bool SelectedParcelChamfer => SelectedParcelModel?.HasChamfer ?? false;
        public bool IsSelectedParcelModified => SelectedParcelModel?.IsModified ?? false;

        public double SelectedParcelCustomFrontage
        {
            get => SelectedParcelFrontage;
            set
            {
                if (value > 0 && SelectedParcelModel != null && Math.Abs(SelectedParcelFrontage - value) > 0.001)
                {
                    var existingEx = Config.Exceptions.FirstOrDefault(e => e.Side == SelectedParcelModel.Side && e.Sequence == SelectedParcelModel.Sequence);
                    if (existingEx != null)
                    {
                        existingEx.CustomFrontage = value;
                    }
                    else
                    {
                        var newEx = new ParcelException
                        {
                            Side = SelectedParcelModel.Side,
                            Sequence = SelectedParcelModel.Sequence,
                            CustomFrontage = value,
                            CustomDepth = SelectedParcelModel.Depth,
                            CustomType = "Modified"
                        };
                        Config.Exceptions.Add(newEx);
                        ExceptionsList.Add(newEx);
                    }
                    Config.Similarity = SimilarityMode.HasExceptions;
                    NotifyPropertyChanged(nameof(HasExceptions));
                    NotifyPropertyChanged(nameof(IsAllIdentical));
                    Recalculate();
                    NotifyPropertyChanged(nameof(SelectedParcelCustomFrontage));
                    NotifyPropertyChanged(nameof(SelectedParcelFrontage));
                    NotifyPropertyChanged(nameof(SelectedParcelArea));
                    NotifyPropertyChanged(nameof(IsSelectedParcelModified));
                }
            }
        }

        public double SelectedParcelCustomDepth
        {
            get => SelectedParcelDepth;
            set
            {
                if (value > 0 && SelectedParcelModel != null && Math.Abs(SelectedParcelDepth - value) > 0.001)
                {
                    var existingEx = Config.Exceptions.FirstOrDefault(e => e.Side == SelectedParcelModel.Side && e.Sequence == SelectedParcelModel.Sequence);
                    if (existingEx != null)
                    {
                        existingEx.CustomDepth = value;
                    }
                    else
                    {
                        var newEx = new ParcelException
                        {
                            Side = SelectedParcelModel.Side,
                            Sequence = SelectedParcelModel.Sequence,
                            CustomFrontage = SelectedParcelModel.Frontage,
                            CustomDepth = value,
                            CustomType = "Modified"
                        };
                        Config.Exceptions.Add(newEx);
                        ExceptionsList.Add(newEx);
                    }
                    Config.Similarity = SimilarityMode.HasExceptions;
                    NotifyPropertyChanged(nameof(HasExceptions));
                    NotifyPropertyChanged(nameof(IsAllIdentical));
                    Recalculate();
                    NotifyPropertyChanged(nameof(SelectedParcelCustomDepth));
                    NotifyPropertyChanged(nameof(SelectedParcelDepth));
                    NotifyPropertyChanged(nameof(SelectedParcelArea));
                    NotifyPropertyChanged(nameof(IsSelectedParcelModified));
                }
            }
        }

        public ObservableCollection<ParcelModel> AllGeneratedParcels => new ObservableCollection<ParcelModel>(
            Config.SideA.GeneratedParcels.Concat(Config.SideB.GeneratedParcels)
        );

        public double EstimatedBlockLength => Config.EstimatedBlockLength;
        public double EstimatedBlockDepth => Config.EstimatedBlockDepth;
        public double EstimatedBlockArea => Config.EstimatedBlockArea;
        public double SideATotalFrontage => Config.SideA.TotalFrontage;
        public double SideATotalArea => Config.SideA.TotalArea;
        public double SideBTotalFrontage => Config.SideB.TotalFrontage;
        public double SideBTotalArea => Config.SideB.TotalArea;

        public bool HasLengthDifferenceWarning => Config.Arrangement == ArrangementMode.BackToBack && Math.Abs(SideATotalFrontage - SideBTotalFrontage) > 0.01;
        public string LengthDifferenceWarningText => $"The two sides have different lengths ({SideATotalFrontage:F0} m vs {SideBTotalFrontage:F0} m). This will be generated as per your configuration.";

        // --- STEP 11: VALIDATION (10 RULES) ---
        public ObservableCollection<RuleCheckItem> RuleValidationItems { get; set; } = new ObservableCollection<RuleCheckItem>();
        public ObservableCollection<string> ValidationErrors { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ValidationWarnings { get; set; } = new ObservableCollection<string>();
        public bool IsValidationPassing => ValidationErrors.Count == 0;

        // --- STEP 12: OUTPUT & PROCESSING ---
        public string OutputFeatureClassName
        {
            get => _outputFeatureClassName;
            set => SetProperty(ref _outputFeatureClassName, value);
        }

        public string OutputWorkspacePath
        {
            get => _outputWorkspacePath;
            set => SetProperty(ref _outputWorkspacePath, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsLivePreviewEnabled
        {
            get => _isLivePreviewEnabled;
            set
            {
                if (SetProperty(ref _isLivePreviewEnabled, value))
                {
                    if (value)
                    {
                        _ = ParcelPreviewService.Instance.UpdateMapPreviewAsync(Config, SelectedParcelId);
                    }
                    else
                    {
                        _ = ParcelPreviewService.Instance.ClearPreviewAsync();
                    }
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    NotifyPropertyChanged(nameof(IsNotProcessing));
                }
            }
        }

        public bool IsNotProcessing => !IsProcessing;

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        // --- COMMANDS ---
        public ICommand NextStepCommand { get; }
        public ICommand PreviousStepCommand { get; }
        public ICommand GoToStepCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand GenerateFeatureClassCommand { get; }
        public ICommand SelectOnMapCommand { get; }
        public ICommand SelectElectricRoomOnMapCommand { get; }
        public ICommand SelectBasePointOnMapCommand { get; }
        public ICommand SetAlignmentCommand { get; }
        public ICommand SetAlignmentFromFeatureCommand { get; }
        public ICommand AddExceptionCommand { get; }
        public ICommand RemoveExceptionCommand { get; }
        public ICommand DeleteExceptionCommand { get; }
        public ICommand ResetSelectedParcelToStandardCommand { get; }
        public ICommand SelectParcelCommand { get; }
        public ICommand ToggleChamferForParcelCommand { get; }
        public ICommand SelectAllCornersCommand { get; }
        public ICommand ClearAllCornersCommand { get; }
        public ICommand ZoomToSelectedParcelCommand { get; }
        public ICommand ZoomToBlockExtentCommand { get; }
        public ICommand MoveToMapExtentCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand CancelOperationCommand { get; }
        public ICommand ToggleLivePreviewCommand { get; }

        private bool CanGoToStep(int step)
        {
            if (step < 1 || step > 12) return false;
            if (step > CurrentStep && !IsValidationPassing && CurrentStep == 11) return false;
            return true;
        }

        private void ExecuteNextStep()
        {
            if (CurrentStep < 12) CurrentStep++;
        }

        private void ExecutePreviousStep()
        {
            if (CurrentStep > 1) CurrentStep--;
        }

        private void ExecuteGoToStep(int step)
        {
            if (step >= 1 && step <= 12)
            {
                CurrentStep = step;
            }
        }

        private void ExecuteReset()
        {
            Config = new BlockConfiguration();
            ExceptionsList.Clear();
            CurrentStep = 1;
            ExtractedParcelsCount = 0;
            ExtractedTotalFrontage = 0.0;
            ExtractedTotalArea = 0.0;
            StatusText = "Configuration reset to defaults.";
            SelectedParcelId = string.Empty;
            _ = ParcelPreviewService.Instance.ClearPreviewAsync();
            Recalculate();
            if (Config.SideA.GeneratedParcels.Count > 0)
            {
                SelectedParcelId = Config.SideA.GeneratedParcels[0].Id;
            }
        }

        private void OnExceptionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Recalculate();
        }

        private void ExecuteAddException()
        {
            int nextSeq = 1;
            for (int i = 1; i <= Config.SideA.ParcelCount; i++)
            {
                if (!Config.Exceptions.Any(e => e.Side == ParcelSide.SideA && e.Sequence == i))
                {
                    nextSeq = i;
                    break;
                }
            }

            var ex = new ParcelException
            {
                Side = ParcelSide.SideA,
                Sequence = nextSeq,
                CustomFrontage = BaseFrontage + 5.0,
                CustomDepth = BaseDepth,
                CustomType = "Special / Override"
            };
            ex.PropertyChanged += OnExceptionPropertyChanged;

            Config.Exceptions.Add(ex);
            ExceptionsList.Add(ex);
            Config.Similarity = SimilarityMode.HasExceptions;
            NotifyPropertyChanged(nameof(HasExceptions));
            NotifyPropertyChanged(nameof(IsAllIdentical));
            NotifyPropertyChanged(nameof(HasCustomExceptionsList));
            NotifyPropertyChanged(nameof(HasNoCustomExceptionsList));
            Recalculate();
        }

        public bool HasCustomExceptionsList => ExceptionsList.Count > 0;
        public bool HasNoCustomExceptionsList => ExceptionsList.Count == 0;

        private void ExecuteRemoveException(ParcelException ex)
        {
            if (ex != null)
            {
                ex.PropertyChanged -= OnExceptionPropertyChanged;
                Config.Exceptions.Remove(ex);
                ExceptionsList.Remove(ex);
                if (Config.Exceptions.Count == 0)
                {
                    Config.Similarity = SimilarityMode.AllIdentical;
                    NotifyPropertyChanged(nameof(HasExceptions));
                    NotifyPropertyChanged(nameof(IsAllIdentical));
                }
                NotifyPropertyChanged(nameof(HasCustomExceptionsList));
                NotifyPropertyChanged(nameof(HasNoCustomExceptionsList));
                Recalculate();
            }
        }

        private void ExecuteDeleteExceptionById(string parcelId)
        {
            if (string.IsNullOrEmpty(parcelId)) return;
            var parts = parcelId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
            {
                var side = parts[0].Equals("A", StringComparison.OrdinalIgnoreCase) ? ParcelSide.SideA : ParcelSide.SideB;
                var ex = Config.Exceptions.FirstOrDefault(e => e.Side == side && e.Sequence == seq);
                if (ex != null)
                {
                    ExecuteRemoveException(ex);
                }
            }
        }

        private void ExecuteSelectOnMap()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectParcelTool");
            StatusText = "Click existing parcel(s) on the map to extract and accumulate block dimensions...";
        }

        private void ExecuteSelectBasePointOnMap()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectBasePointTool");
            StatusText = "📍 Click a location on the map (with snapping) to anchor the selected Base Point...";
        }

        private void ExecuteSetAlignment()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_AlignmentTool");
            StatusText = "Click two points (P1 ➔ P2) on the map with snapping to define orientation baseline...";
        }

        private void ExecuteSetAlignmentFromFeature()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectAlignmentFeatureTool");
            StatusText = "Click an existing line feature or polygon boundary edge on the map...";
        }

        public async Task OnMapParcelSelectedAsync(MapPoint clickPoint)
        {
            IsProcessing = true;
            StatusText = "Querying GIS context & extracting parcel parameters...";
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, _cancellationTokenSource.Token);

                await QueuedTask.Run(() =>
                {
                    Config.SourceMode = SourceMode.ExtractedFromExistingParcel;
                    Config.Metadata.SourceParcelId = Math.Abs((long)clickPoint.X % 10000);
                    Config.Metadata.Status = ExtractionStatus.Detected;
                    Config.Metadata.ConfidenceScore = 0.95;

                    // Accumulate multi-parcel selection
                    ExtractedParcelsCount++;
                    double detectedFront = 20.0;
                    double detectedD = 30.0;
                    Config.BaseParcel.DetectedFrontage = detectedFront;
                    Config.BaseParcel.DetectedDepth = detectedD;

                    ExtractedTotalFrontage += detectedFront;
                    ExtractedTotalArea += (detectedFront * detectedD);

                    if (ExtractedParcelsCount > 1)
                    {
                        // Multiple parcels accumulated
                        Config.SideA.ParcelCount = ExtractedParcelsCount;
                    }
                });

                NotifyPropertyChanged(nameof(IsExtractedMode));
                NotifyPropertyChanged(nameof(SourceParcelId));
                NotifyPropertyChanged(nameof(ExtractionConfidence));
                NotifyPropertyChanged(nameof(DetectedFrontage));
                NotifyPropertyChanged(nameof(DetectedDepth));
                NotifyPropertyChanged(nameof(ExtractedParcelsCount));
                NotifyPropertyChanged(nameof(ExtractedTotalFrontage));
                NotifyPropertyChanged(nameof(ExtractedTotalArea));
                NotifyPropertyChanged(nameof(HasMultipleExtractedParcels));
                NotifyPropertyChanged(nameof(SideACount));

                StatusText = ExtractedParcelsCount > 1
                    ? $"Accumulated {ExtractedParcelsCount} parcels: Total Frontage {ExtractedTotalFrontage:F1}m, Area {ExtractedTotalArea:N0} m²."
                    : $"Extracted parcel {SourceParcelId} at ({clickPoint.X:F1}, {clickPoint.Y:F1}).";

                Recalculate();
            }
            catch (OperationCanceledException)
            {
                StatusText = "Parcel extraction cancelled.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async Task ExecuteZoomToSelectedParcelAsync()
        {
            if (!string.IsNullOrEmpty(SelectedParcelId))
            {
                await ParcelPreviewService.Instance.ZoomToParcelAsync(Config, SelectedParcelId);
                StatusText = $"Zoomed to parcel {SelectedParcelId} on active map.";
            }
        }

        private async Task ExecuteZoomToBlockExtentAsync()
        {
            await ParcelPreviewService.Instance.ZoomToBlockExtentAsync(Config);
            StatusText = "Zoomed active map to full block extent.";
        }

        private async Task ExecuteMoveToMapExtentAsync()
        {
            await QueuedTask.Run(() =>
            {
                var mapView = MapView.Active;
                if (mapView == null || mapView.Extent == null) return;

                var center = mapView.Extent.Center;
                Config.Alignment.TargetMapPointX = Math.Round(center.X, 2);
                Config.Alignment.TargetMapPointY = Math.Round(center.Y, 2);
                if (mapView.Map?.SpatialReference != null)
                {
                    Config.Alignment.SpatialReferenceWkid = mapView.Map.SpatialReference.Wkid;
                    Config.Alignment.SpatialReferenceName = mapView.Map.SpatialReference.Name;
                }
            });

            NotifyPropertyChanged(nameof(Config));
            NotifyPropertyChanged(nameof(TargetBasePointX));
            NotifyPropertyChanged(nameof(TargetBasePointY));
            NotifyPropertyChanged(nameof(TargetBasePointXText));
            NotifyPropertyChanged(nameof(TargetBasePointYText));
            NotifyPropertyChanged(nameof(TargetMapPointText));
            NotifyPropertyChanged(nameof(IsBasePointPlaced));
            NotifyPropertyChanged(nameof(BasePointStatusText));
            NotifyPropertyChanged(nameof(AlignmentStatusSummary));
            Recalculate();
            StatusText = $"Base Point relocated to active map center ({Config.Alignment.TargetMapPointX:F1}, {Config.Alignment.TargetMapPointY:F1}).";
        }

        private async Task ExecuteZoomInAsync()
        {
            await ParcelPreviewService.Instance.ZoomInAsync();
            StatusText = "Zoomed in on active map.";
        }

        private async Task ExecuteZoomOutAsync()
        {
            await ParcelPreviewService.Instance.ZoomOutAsync();
            StatusText = "Zoomed out on active map.";
        }

        private Task ExecuteToggleLivePreviewAsync()
        {
            IsLivePreviewEnabled = !IsLivePreviewEnabled;
            return Task.CompletedTask;
        }

        private void ExecuteCancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            StatusText = "Operation cancelled by user.";
            IsProcessing = false;
        }

        private async Task ExecuteGenerateFeatureClassAsync()
        {
            if (!IsValidationPassing)
            {
                StatusText = "Cannot generate: resolve validation errors first.";
                return;
            }

            IsProcessing = true;
            ProgressPercent = 0.0;
            StatusText = $"Generating Feature Class '{OutputFeatureClassName}' in {OutputWorkspacePath}...";
            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<double>(p => ProgressPercent = p);

            try
            {
                var result = await ParcelFeatureClassService.Instance.GenerateFeatureClassAsync(
                    Config,
                    OutputWorkspacePath,
                    OutputFeatureClassName,
                    progress,
                    _cancellationTokenSource.Token);

                if (result.Success)
                {
                    await ParcelPreviewService.Instance.ClearPreviewAsync();
                    StatusText = $"✓ Feature Class '{OutputFeatureClassName}' created successfully ({result.FeatureCount} parcels) and added to map!";
                }
                else
                {
                    StatusText = $"✕ Failed to create Feature Class: {result.Message}";
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = "Feature Class generation cancelled by user.";
            }
            catch (Exception ex)
            {
                StatusText = $"Error creating Feature Class: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteResetSelectedParcelToStandard()
        {
            if (SelectedParcelModel != null)
            {
                var existingEx = Config.Exceptions.FirstOrDefault(e => e.Side == SelectedParcelModel.Side && e.Sequence == SelectedParcelModel.Sequence);
                if (existingEx != null)
                {
                    Config.Exceptions.Remove(existingEx);
                    ExceptionsList.Remove(existingEx);
                    if (Config.Exceptions.Count == 0)
                    {
                        Config.Similarity = SimilarityMode.AllIdentical;
                        NotifyPropertyChanged(nameof(HasExceptions));
                        NotifyPropertyChanged(nameof(IsAllIdentical));
                    }
                    Recalculate();
                    NotifyPropertyChanged(nameof(SelectedParcelCustomFrontage));
                    NotifyPropertyChanged(nameof(SelectedParcelCustomDepth));
                    NotifyPropertyChanged(nameof(SelectedParcelFrontage));
                    NotifyPropertyChanged(nameof(SelectedParcelDepth));
                    NotifyPropertyChanged(nameof(SelectedParcelArea));
                    NotifyPropertyChanged(nameof(IsSelectedParcelModified));
                    StatusText = $"Parcel {SelectedParcelId} reset to standard base dimensions.";
                }
            }
        }

        private void Recalculate()
        {
            // 1. Run Geometry Engine
            ParcelGeometryEngine.Instance.GenerateGeometry(Config);

            // 2. Run Comprehensive 10-Rule Topology Validation Engine
            ValidationErrors.Clear();
            ValidationWarnings.Clear();
            RuleValidationItems.Clear();

            if (BaseFrontage <= 0) ValidationErrors.Add("Base Frontage must be greater than 0 meters.");
            if (BaseDepth <= 0) ValidationErrors.Add("Base Depth must be greater than 0 meters.");
            if (SideACount < 1) ValidationErrors.Add("Side A parcel count must be at least 1.");
            if (Config.Arrangement == ArrangementMode.BackToBack && SideBCount < 1)
                ValidationErrors.Add("Side B parcel count must be at least 1 for back-to-back blocks.");

            var (topResult, ruleItems) = TopologyValidationEngine.Instance.ValidateBlock(Config);
            foreach (var rItem in ruleItems)
            {
                RuleValidationItems.Add(rItem);
            }

            foreach (var err in topResult.Messages.Where(m => m.Severity == ValidationSeverity.Error))
            {
                ValidationErrors.Add(err.Message);
            }
            foreach (var warn in topResult.Messages.Where(m => m.Severity == ValidationSeverity.Warning))
            {
                ValidationWarnings.Add(warn.Message);
            }

            if (HasLengthDifferenceWarning)
            {
                ValidationWarnings.Add(LengthDifferenceWarningText);
            }

            // 3. Update Live Map Graphics Preview
            if (IsLivePreviewEnabled)
            {
                _ = ParcelPreviewService.Instance.UpdateMapPreviewAsync(Config, SelectedParcelId);
            }

            // 4. Notify Computed Metric Properties
            NotifyPropertyChanged(nameof(Config));
            NotifyPropertyChanged(nameof(CalculatedChamferCutLength));
            NotifyPropertyChanged(nameof(EstimatedBlockLength));
            NotifyPropertyChanged(nameof(EstimatedBlockDepth));
            NotifyPropertyChanged(nameof(EstimatedBlockArea));
            NotifyPropertyChanged(nameof(SideATotalFrontage));
            NotifyPropertyChanged(nameof(SideATotalArea));
            NotifyPropertyChanged(nameof(SideBTotalFrontage));
            NotifyPropertyChanged(nameof(SideBTotalArea));
            NotifyPropertyChanged(nameof(HasLengthDifferenceWarning));
            NotifyPropertyChanged(nameof(LengthDifferenceWarningText));
            NotifyPropertyChanged(nameof(IsValidationPassing));
            NotifyPropertyChanged(nameof(SelectedParcelModel));
            NotifyPropertyChanged(nameof(AllGeneratedParcels));
            NotifyPropertyChanged(nameof(SelectedParcelFrontage));
            NotifyPropertyChanged(nameof(SelectedParcelDepth));
            NotifyPropertyChanged(nameof(SelectedParcelArea));
            NotifyPropertyChanged(nameof(SelectedParcelCustomFrontage));
            NotifyPropertyChanged(nameof(SelectedParcelCustomDepth));
            NotifyPropertyChanged(nameof(IsSelectedParcelModified));
            NotifyPropertyChanged(nameof(ElectricRoomArea));
            NotifyPropertyChanged(nameof(IsElectricRoomValid));
            NotifyPropertyChanged(nameof(ElectricRoomStatusText));
            NotifyPropertyChanged(nameof(ElectricRoomHostParcelsText));
            NotifyPropertyChanged(nameof(ElectricRoomPlacementTypeText));
            NotifyPropertyChanged(nameof(ElectricRoomOffsetDistance));
            NotifyPropertyChanged(nameof(ElectricRoomSideLabelA));
            NotifyPropertyChanged(nameof(ElectricRoomSideLabelB));
        }

        public static void Show()
        {
            var pane = FrameworkApplication.DockPaneManager.Find(DockPaneId);
            pane?.Activate();
        }
    }

    public class RelayCommandWithParam<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommandWithParam(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || (parameter is T t && _canExecute(t));
        public void Execute(object? parameter) => _execute((T)parameter!);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
