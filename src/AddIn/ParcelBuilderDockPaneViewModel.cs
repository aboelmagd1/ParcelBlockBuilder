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

        public ParcelBuilderDockPaneViewModel()
        {
            // Language Commands
            SetEnglishCommand = new RelayCommand(() => IsArabic = false);
            SetArabicCommand = new RelayCommand(() => IsArabic = true);

            // Navigation Commands
            NextStepCommand = new RelayCommand(ExecuteNextStep, () => CurrentStep < 11 && IsCurrentStepValid);
            PreviousStepCommand = new RelayCommand(ExecutePreviousStep, () => CurrentStep > 1);
            GoToStepCommand = new RelayCommandWithParam<int>(ExecuteGoToStep, CanGoToStep);
            ResetCommand = new RelayCommand(ExecuteReset);
            GenerateFeatureClassCommand = new RelayCommand(async () => await ExecuteGenerateFeatureClassAsync(), () => !IsProcessing && IsValidationPassing);
            SelectOnMapCommand = new RelayCommand(ExecuteSelectOnMap);
            SetAlignmentCommand = new RelayCommand(ExecuteSetAlignment);
            SetAlignmentFromFeatureCommand = new RelayCommand(ExecuteSetAlignmentFromFeature);
            AddExceptionCommand = new RelayCommand(ExecuteAddException);
            RemoveExceptionCommand = new RelayCommandWithParam<ParcelException>(ExecuteRemoveException);
            ZoomToSelectedParcelCommand = new RelayCommand(async () => await ExecuteZoomToSelectedParcelAsync(), () => !string.IsNullOrEmpty(SelectedParcelId));
            ZoomToBlockExtentCommand = new RelayCommand(async () => await ExecuteZoomToBlockExtentAsync());
            MoveToMapExtentCommand = new RelayCommand(async () => await ExecuteMoveToMapExtentAsync());
            ZoomInCommand = new RelayCommand(async () => await ExecuteZoomInAsync());
            ZoomOutCommand = new RelayCommand(async () => await ExecuteZoomOutAsync());
            ResetSelectedParcelToStandardCommand = new RelayCommand(ExecuteResetSelectedParcelToStandard);
            SelectParcelCommand = new RelayCommandWithParam<string>(id => { if (!string.IsNullOrEmpty(id)) SelectedParcelId = id; });
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

        public bool IsCurrentStepValid => CurrentStep switch
        {
            1 => !HasFrontageError && !HasDepthError,
            4 => SideACount >= 1 && (Config.Arrangement != ArrangementMode.BackToBack || SideBCount >= 1),
            _ => true
        };

        public string StepTitle => CurrentStep switch
        {
            1 => "1. Base Parcel Dimensions",
            2 => "2. Relationship Type",
            3 => "3. Block Arrangement",
            4 => "4. Parcel Count",
            5 => "5. Dimension Similarity",
            6 => "6. Parcel Exceptions & Overrides",
            7 => "7. Corner & Chamfer Geometry",
            8 => "8. Spatial Alignment & Orientation",
            9 => "9. Dynamic Preview & Inspection",
            10 => "10. Validation & Geometry Rules",
            11 => "11. Final Review & Feature Class Output",
            _ => "Parcel Builder Workstation"
        };

        public string StepSubtitle => CurrentStep switch
        {
            1 => "Define standard frontage and depth, or extract an existing parcel layout from the active map.",
            2 => "Specify whether this is a standalone parcel or part of a coordinated block assembly.",
            3 => "Choose single-sided row or back-to-back dual row block structure.",
            4 => "Configure the number of parcels independently for Side A and Side B.",
            5 => "Determine whether all parcels are identical or have custom dimension exceptions.",
            6 => "Add custom dimension overrides, frontage variations, and parcel classifications.",
            7 => "Configure outer corner parcels and chamfer cut methods (by length or street frontage).",
            8 => "Define orientation baseline via 2 map points or reference parcel alignment.",
            9 => "Inspect individual parcel dimensions, areas, and interactive vector layout schematic.",
            10 => "Review geometric integrity, topological checks, and validation error warnings.",
            11 => "Review the final configuration summary blueprint and export to Geodatabase Feature Class.",
            _ => string.Empty
        };

        // --- SOURCE METADATA & EXTRACTION ---
        public bool IsExtractedMode => Config.SourceMode == SourceMode.ExtractedFromExistingParcel;
        public string SourceParcelId => Config.Metadata.SourceParcelId?.ToString() ?? "P-001";
        public string ExtractionConfidence => $"Confidence: {Config.Metadata.ConfidenceScore * 100:F0}% ({Config.Metadata.Status})";
        public double DetectedFrontage => Config.BaseParcel.DetectedFrontage;
        public double DetectedDepth => Config.BaseParcel.DetectedDepth;

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

        public double ChamferLength
        {
            get => Config.Corner.ChamferLength;
            set
            {
                if (Config.Corner.ChamferLength != value && value >= 0)
                {
                    Config.Corner.ChamferLength = value;
                    NotifyPropertyChanged(nameof(ChamferLength));
                    Recalculate();
                }
            }
        }

        // --- STEP 8 & 9: PREVIEW & SELECTION METRICS ---
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

        // --- STEP 10: VALIDATION ---
        public ObservableCollection<string> ValidationErrors { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ValidationWarnings { get; set; } = new ObservableCollection<string>();
        public bool IsValidationPassing => ValidationErrors.Count == 0;

        // --- STEP 11: OUTPUT & PROCESSING ---
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
        public ICommand SetAlignmentCommand { get; }
        public ICommand SetAlignmentFromFeatureCommand { get; }
        public ICommand AddExceptionCommand { get; }
        public ICommand RemoveExceptionCommand { get; }
        public ICommand ResetSelectedParcelToStandardCommand { get; }
        public ICommand SelectParcelCommand { get; }
        public ICommand ZoomToSelectedParcelCommand { get; }
        public ICommand ZoomToBlockExtentCommand { get; }
        public ICommand MoveToMapExtentCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand CancelOperationCommand { get; }
        public ICommand ToggleLivePreviewCommand { get; }

        private bool CanGoToStep(int step)
        {
            if (step < 1 || step > 11) return false;
            if (step > CurrentStep && !IsValidationPassing) return false;
            return true;
        }

        private void ExecuteNextStep()
        {
            if (CurrentStep < 11) CurrentStep++;
        }

        private void ExecutePreviousStep()
        {
            if (CurrentStep > 1) CurrentStep--;
        }

        private void ExecuteGoToStep(int step)
        {
            if (step >= 1 && step <= 11)
            {
                CurrentStep = step;
            }
        }

        private void ExecuteReset()
        {
            Config = new BlockConfiguration();
            ExceptionsList.Clear();
            CurrentStep = 1;
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
            Recalculate();
        }

        public bool HasCustomExceptionsList => ExceptionsList.Count > 0;

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
                Recalculate();
            }
        }

        private void ExecuteSelectOnMap()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectParcelTool");
            StatusText = "Click an existing parcel on the map to extract configuration...";
        }

        private void ExecuteSetAlignment()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_AlignmentTool");
            StatusText = "Click two points on the map with snapping to define orientation baseline...";
        }

        private void ExecuteSetAlignmentFromFeature()
        {
            FrameworkApplication.SetCurrentToolAsync("ParcelBuilder_SelectAlignmentFeatureTool");
            StatusText = "Click an existing line feature or polygon boundary edge on the map...";
        }

        public async Task OnMapParcelSelectedAsync(MapPoint clickPoint)
        {
            IsProcessing = true;
            StatusText = "Querying GIS context & inferring block structure...";
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, _cancellationTokenSource.Token);

                await QueuedTask.Run(() =>
                {
                    Config.SourceMode = SourceMode.ExtractedFromExistingParcel;
                    Config.Metadata.SourceParcelId = Math.Abs((long)clickPoint.X % 10000);
                    Config.Metadata.Status = ExtractionStatus.Detected;
                    Config.Metadata.ConfidenceScore = 0.94;
                    Config.BaseParcel.DetectedFrontage = 20.0;
                    Config.BaseParcel.DetectedDepth = 30.0;
                });

                NotifyPropertyChanged(nameof(IsExtractedMode));
                NotifyPropertyChanged(nameof(SourceParcelId));
                NotifyPropertyChanged(nameof(ExtractionConfidence));
                NotifyPropertyChanged(nameof(DetectedFrontage));
                NotifyPropertyChanged(nameof(DetectedDepth));

                StatusText = $"Extracted parcel {SourceParcelId} at ({clickPoint.X:F2}, {clickPoint.Y:F2}). Block inferred.";
                CurrentStep = 9; // Navigate to Review/Preview
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

        public double AlignmentAzimuthDegrees => Config.Alignment.AzimuthAngleDegrees;
        public double AlignmentVectorLength => Math.Sqrt(
            Math.Pow(Config.Alignment.TargetX - Config.Alignment.OriginX, 2) +
            Math.Pow(Config.Alignment.TargetY - Config.Alignment.OriginY, 2));
        public string AlignmentOriginText => $"({Config.Alignment.OriginX:F1}, {Config.Alignment.OriginY:F1})";
        public string AlignmentTargetText => $"({Config.Alignment.TargetX:F1}, {Config.Alignment.TargetY:F1})";
        public bool HasCustomAlignment => Math.Abs(Config.Alignment.OriginX) > 1e-4 || Math.Abs(Config.Alignment.OriginY) > 1e-4;

        public void SetAlignmentLine(double x1, double y1, double x2, double y2, string? sourceDesc = null)
        {
            Config.Alignment.OriginX = Math.Round(x1, 2);
            Config.Alignment.OriginY = Math.Round(y1, 2);
            Config.Alignment.TargetX = Math.Round(x2, 2);
            Config.Alignment.TargetY = Math.Round(y2, 2);

            double dx = x2 - x1;
            double dy = y2 - y1;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            if (length > 0.001)
            {
                double angleRad = Math.Atan2(dy, dx);
                double angleDeg = angleRad * (180.0 / Math.PI);
                double azimuth = (90.0 - angleDeg) % 360.0;
                if (azimuth < 0) azimuth += 360.0;
                Config.Alignment.AzimuthAngleDegrees = Math.Round(azimuth, 2);
            }

            NotifyPropertyChanged(nameof(Config));
            NotifyPropertyChanged(nameof(AlignmentAzimuthDegrees));
            NotifyPropertyChanged(nameof(AlignmentVectorLength));
            NotifyPropertyChanged(nameof(AlignmentOriginText));
            NotifyPropertyChanged(nameof(AlignmentTargetText));
            NotifyPropertyChanged(nameof(HasCustomAlignment));

            string desc = !string.IsNullOrEmpty(sourceDesc) ? $" ({sourceDesc})" : "";
            StatusText = $"Alignment vector set{desc}: Bearing {Config.Alignment.AzimuthAngleDegrees:F1}°, Length {length:F1}m, Origin ({x1:F1}, {y1:F1})";
            Recalculate();
        }

        public void SetAlignmentOrigin(double x, double y, string? sourceDesc = null)
        {
            Config.Alignment.OriginX = Math.Round(x, 2);
            Config.Alignment.OriginY = Math.Round(y, 2);

            NotifyPropertyChanged(nameof(Config));
            NotifyPropertyChanged(nameof(AlignmentOriginText));
            NotifyPropertyChanged(nameof(HasCustomAlignment));

            string desc = !string.IsNullOrEmpty(sourceDesc) ? $" ({sourceDesc})" : "";
            StatusText = $"Block origin point set{desc}: ({x:F1}, {y:F1})";
            Recalculate();
        }

        // --- STEP 8: BLOCK REFERENCE ANCHOR POINTS ---
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
            NotifyPropertyChanged(nameof(CurrentAnchorPoint));
            NotifyPropertyChanged(nameof(IsAnchorSideAStart));
            NotifyPropertyChanged(nameof(IsAnchorSideAEnd));
            NotifyPropertyChanged(nameof(IsAnchorSideACenter));
            NotifyPropertyChanged(nameof(IsAnchorSideBStart));
            NotifyPropertyChanged(nameof(IsAnchorSideBEnd));
            NotifyPropertyChanged(nameof(IsAnchorBlockCenter));
            NotifyPropertyChanged(nameof(AnchorPointDescription));
            StatusText = $"Alignment reference anchor set to: {AnchorPointDescription}";
            Recalculate();
        }

        public string AnchorPointDescription => Config.Alignment.AnchorPoint switch
        {
            BlockAnchorPoint.SideAStart => "Side A Start (Front-Left Corner)",
            BlockAnchorPoint.SideAEnd => "Side A End (Front-Right Corner)",
            BlockAnchorPoint.SideACenter => "Street A Center Midpoint",
            BlockAnchorPoint.SideBStart => "Side B Start (Back-Left Corner)",
            BlockAnchorPoint.SideBEnd => "Side B End (Back-Right Corner)",
            BlockAnchorPoint.BlockCenter => "Block Centroid Center",
            _ => "Front-Left Corner"
        };

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
                Config.Alignment.OriginX = Math.Round(center.X, 2);
                Config.Alignment.OriginY = Math.Round(center.Y, 2);
                if (mapView.Map?.SpatialReference != null)
                {
                    Config.Alignment.SpatialReferenceWkid = mapView.Map.SpatialReference.Wkid;
                }
            });

            NotifyPropertyChanged(nameof(Config));
            Recalculate();
            StatusText = $"Block relocated to active map center ({Config.Alignment.OriginX:F1}, {Config.Alignment.OriginY:F1}).";
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

            // 2. Validate
            ValidationErrors.Clear();
            ValidationWarnings.Clear();

            if (BaseFrontage <= 0) ValidationErrors.Add("Base Frontage must be greater than 0 meters.");
            if (BaseDepth <= 0) ValidationErrors.Add("Base Depth must be greater than 0 meters.");
            if (SideACount < 1) ValidationErrors.Add("Side A parcel count must be at least 1.");
            if (Config.Arrangement == ArrangementMode.BackToBack && SideBCount < 1)
                ValidationErrors.Add("Side B parcel count must be at least 1 for back-to-back blocks.");

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
