using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ParcelBuilder.Core.Models;

namespace ParcelBuilder.AddIn.Controls
{
    /// <summary>
    /// High-performance vector canvas rendering the schematic block layout with 4 surrounding streets,
    /// dynamic parcel outlines, sequence labels, chamfers, electric rooms, hover tooltips,
    /// interactive parcel selection, and interactive anchor point snapping.
    /// </summary>
    public class SchematicCanvasControl : FrameworkElement
    {
        public static readonly DependencyProperty ConfigurationProperty =
            DependencyProperty.Register(
                nameof(Configuration),
                typeof(BlockConfiguration),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedParcelIdProperty =
            DependencyProperty.Register(
                nameof(SelectedParcelId),
                typeof(string),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedParcelId2Property =
            DependencyProperty.Register(
                nameof(SelectedParcelId2),
                typeof(string),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedCornerPositionProperty =
            DependencyProperty.Register(
                nameof(SelectedCornerPosition),
                typeof(CornerPosition?),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AnchorPointProperty =
            DependencyProperty.Register(
                nameof(AnchorPoint),
                typeof(BlockAnchorPoint?),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowAnchorHandlesProperty =
            DependencyProperty.Register(
                nameof(ShowAnchorHandles),
                typeof(bool),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowElectricRoomHandlesProperty =
            DependencyProperty.Register(
                nameof(ShowElectricRoomHandles),
                typeof(bool),
                typeof(SchematicCanvasControl),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectElectricRoomLocationCommandProperty =
            DependencyProperty.Register(
                nameof(SelectElectricRoomLocationCommand),
                typeof(ICommand),
                typeof(SchematicCanvasControl),
                new PropertyMetadata(null));

        public BlockConfiguration Configuration
        {
            get => (BlockConfiguration)GetValue(ConfigurationProperty);
            set => SetValue(ConfigurationProperty, value);
        }

        public string SelectedParcelId
        {
            get => (string)GetValue(SelectedParcelIdProperty);
            set => SetValue(SelectedParcelIdProperty, value);
        }

        public string SelectedParcelId2
        {
            get => (string)GetValue(SelectedParcelId2Property);
            set => SetValue(SelectedParcelId2Property, value);
        }

        public CornerPosition? SelectedCornerPosition
        {
            get => (CornerPosition?)GetValue(SelectedCornerPositionProperty);
            set => SetValue(SelectedCornerPositionProperty, value);
        }

        public BlockAnchorPoint? AnchorPoint
        {
            get => (BlockAnchorPoint?)GetValue(AnchorPointProperty);
            set => SetValue(AnchorPointProperty, value);
        }

        public bool ShowAnchorHandles
        {
            get => (bool)GetValue(ShowAnchorHandlesProperty);
            set => SetValue(ShowAnchorHandlesProperty, value);
        }

        public bool ShowElectricRoomHandles
        {
            get => (bool)GetValue(ShowElectricRoomHandlesProperty);
            set => SetValue(ShowElectricRoomHandlesProperty, value);
        }

        public ICommand? SelectElectricRoomLocationCommand
        {
            get => (ICommand?)GetValue(SelectElectricRoomLocationCommandProperty);
            set => SetValue(SelectElectricRoomLocationCommandProperty, value);
        }

        private const double SnapThreshold = 18.0;

        private readonly Brush _sideABrush = new SolidColorBrush(Color.FromArgb(200, 41, 182, 246)); // #29B6F6
        private readonly Brush _sideBBrush = new SolidColorBrush(Color.FromArgb(200, 171, 71, 188)); // #AB47BC
        private readonly Brush _electricRoomBrush = new SolidColorBrush(Color.FromArgb(230, 255, 145, 0)); // Amber/Orange #FF9100
        private readonly Brush _throughParcelBrush = new SolidColorBrush(Color.FromArgb(210, 0, 191, 165)); // Teal #00BFA5
        private readonly Brush _selectedBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));      // #00E5FF
        private readonly Brush _hoverBrush = new SolidColorBrush(Color.FromArgb(240, 77, 208, 225));   // #4DD0E1
        private readonly Pen _borderPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), 1.2);
        private readonly Pen _electricRoomPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 64)), 1.8);
        private readonly Pen _selectedPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 2.5);
        private readonly Pen _spinePen = new Pen(new SolidColorBrush(Color.FromArgb(140, 0, 229, 255)), 1.2)
        {
            DashStyle = DashStyles.Dash
        };

        private string? _hoveredParcelId;
        private BlockAnchorPoint? _hoveredAnchor;
        private ElectricRoomHandleInfo? _hoveredElectricRoomHandle;
        private readonly List<(ParcelModel Parcel, List<Point> ScreenPolygon)> _renderedPolygons = new();
        private readonly List<(BlockAnchorPoint Type, string Name, Point ScreenPt, bool IsActive)> _renderedAnchors = new();
        private readonly List<ElectricRoomHandleInfo> _renderedElectricRoomHandles = new();

        public SchematicCanvasControl()
        {
            ClipToBounds = true;
            _sideABrush.Freeze();
            _sideBBrush.Freeze();
            _electricRoomBrush.Freeze();
            _throughParcelBrush.Freeze();
            _selectedBrush.Freeze();
            _hoverBrush.Freeze();
            _borderPen.Freeze();
            _electricRoomPen.Freeze();
            _selectedPen.Freeze();
            _spinePen.Freeze();

            MouseMove += OnCanvasMouseMove;
            MouseLeave += OnCanvasMouseLeave;
            MouseDown += OnCanvasMouseDown;
        }

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);

            // 0. Check Electric Room handle click if visible
            if (ShowElectricRoomHandles)
            {
                foreach (var handle in _renderedElectricRoomHandles)
                {
                    double dist = Math.Sqrt(Math.Pow(pos.X - handle.ScreenPt.X, 2) + Math.Pow(pos.Y - handle.ScreenPt.Y, 2));
                    if (dist <= SnapThreshold)
                    {
                        if (Configuration?.ElectricRoom != null)
                        {
                            Configuration.ElectricRoom.Side = handle.Side;
                            Configuration.ElectricRoom.OffsetDistance = handle.Offset;
                            Configuration.ElectricRoom.PlacementMethod = ElectricRoomPlacementMethod.OffsetDistance;
                            Configuration.ElectricRoom.ClickedMapX = null;
                            Configuration.ElectricRoom.ClickedMapY = null;
                        }

                        if (SelectElectricRoomLocationCommand != null && SelectElectricRoomLocationCommand.CanExecute((handle.Side, handle.Offset)))
                        {
                            SelectElectricRoomLocationCommand.Execute((handle.Side, handle.Offset));
                        }

                        InvalidateVisual();
                        return;
                    }
                }
            }

            // 1. Check anchor handle click if handles visible
            if (ShowAnchorHandles)
            {
                foreach (var anchor in _renderedAnchors)
                {
                    double dist = Math.Sqrt(Math.Pow(pos.X - anchor.ScreenPt.X, 2) + Math.Pow(pos.Y - anchor.ScreenPt.Y, 2));
                    if (dist <= SnapThreshold)
                    {
                        AnchorPoint = anchor.Type;
                        if (Configuration?.Alignment != null)
                        {
                            Configuration.Alignment.AnchorPoint = anchor.Type;
                        }
                        InvalidateVisual();
                        return;
                    }
                }
            }

            // 2. Check parcel polygon click
            for (int i = _renderedPolygons.Count - 1; i >= 0; i--)
            {
                var item = _renderedPolygons[i];
                if (IsPointInsidePolygon(pos, item.ScreenPolygon))
                {
                    bool isDualSelectionMode = System.Windows.Data.BindingOperations.GetBindingExpression(this, SelectedParcelId2Property) != null
                                               || !string.IsNullOrEmpty(SelectedParcelId2);

                    if (isDualSelectionMode)
                    {
                        string clickedId = item.Parcel.Id;
                        if (string.IsNullOrEmpty(SelectedParcelId))
                        {
                            SelectedParcelId = clickedId;
                        }
                        else if (string.IsNullOrEmpty(SelectedParcelId2) && clickedId != SelectedParcelId)
                        {
                            SelectedParcelId2 = clickedId;
                        }
                        else if (clickedId == SelectedParcelId)
                        {
                            SelectedParcelId = SelectedParcelId2 ?? string.Empty;
                            SelectedParcelId2 = string.Empty;
                        }
                        else if (clickedId == SelectedParcelId2)
                        {
                            SelectedParcelId2 = string.Empty;
                        }
                        else
                        {
                            SelectedParcelId = clickedId;
                            SelectedParcelId2 = string.Empty;
                        }
                    }
                    else
                    {
                        SelectedParcelId = item.Parcel.Id;
                    }

                    // Automatically determine if this clicked parcel corresponds to a corner
                    if (item.Parcel.Side == ParcelSide.SideA && item.Parcel.Sequence == 1)
                        SelectedCornerPosition = CornerPosition.SideAStart;
                    else if (item.Parcel.Side == ParcelSide.SideA && item.Parcel.Sequence == (Configuration?.SideA.ParcelCount ?? 1))
                        SelectedCornerPosition = CornerPosition.SideAEnd;
                    else if (item.Parcel.Side == ParcelSide.SideB && item.Parcel.Sequence == 1)
                        SelectedCornerPosition = CornerPosition.SideBStart;
                    else if (item.Parcel.Side == ParcelSide.SideB && item.Parcel.Sequence == (Configuration?.SideB.ParcelCount ?? 1))
                        SelectedCornerPosition = CornerPosition.SideBEnd;

                    InvalidateVisual();
                    return;
                }
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);
            string? foundParcelId = null;
            BlockAnchorPoint? foundAnchor = null;
            ElectricRoomHandleInfo? foundERHandle = null;

            if (ShowElectricRoomHandles)
            {
                foreach (var handle in _renderedElectricRoomHandles)
                {
                    double dist = Math.Sqrt(Math.Pow(pos.X - handle.ScreenPt.X, 2) + Math.Pow(pos.Y - handle.ScreenPt.Y, 2));
                    if (dist <= SnapThreshold)
                    {
                        foundERHandle = handle;
                        Cursor = Cursors.Hand;
                        break;
                    }
                }
            }

            if (foundERHandle == null && ShowAnchorHandles)
            {
                foreach (var anchor in _renderedAnchors)
                {
                    double dist = Math.Sqrt(Math.Pow(pos.X - anchor.ScreenPt.X, 2) + Math.Pow(pos.Y - anchor.ScreenPt.Y, 2));
                    if (dist <= SnapThreshold)
                    {
                        foundAnchor = anchor.Type;
                        Cursor = Cursors.Hand;
                        break;
                    }
                }
            }

            if (foundERHandle == null && foundAnchor == null)
            {
                for (int i = _renderedPolygons.Count - 1; i >= 0; i--)
                {
                    var item = _renderedPolygons[i];
                    if (IsPointInsidePolygon(pos, item.ScreenPolygon))
                    {
                        foundParcelId = item.Parcel.Id;
                        Cursor = Cursors.Hand;
                        break;
                    }
                }
            }

            if (foundERHandle == null && foundAnchor == null && foundParcelId == null)
            {
                Cursor = Cursors.Arrow;
            }

            if (_hoveredParcelId != foundParcelId || _hoveredAnchor != foundAnchor || _hoveredElectricRoomHandle != foundERHandle)
            {
                _hoveredParcelId = foundParcelId;
                _hoveredAnchor = foundAnchor;
                _hoveredElectricRoomHandle = foundERHandle;

                if (_hoveredElectricRoomHandle != null)
                {
                    ToolTip = $"📍 {_hoveredElectricRoomHandle.Description}\nClick to place Electric Room here";
                }
                else if (foundAnchor != null)
                {
                    ToolTip = $"Anchor Point: {foundAnchor.Value}";
                }
                else
                {
                    ToolTip = null;
                }

                InvalidateVisual();
            }
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            _hoveredParcelId = null;
            _hoveredAnchor = null;
            _hoveredElectricRoomHandle = null;
            ToolTip = null;
            Cursor = Cursors.Arrow;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            _renderedPolygons.Clear();
            _renderedAnchors.Clear();
            _renderedElectricRoomHandles.Clear();

            var config = Configuration;
            if (config == null || (config.SideA.GeneratedParcels.Count == 0 && config.SideB.GeneratedParcels.Count == 0))
            {
                var text = new FormattedText(
                    "No parcel geometry generated",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    12,
                    new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2));
                return;
            }

            double width = ActualWidth;
            double height = ActualHeight;
            if (width < 10 || height < 10) return;

            var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).Distinct().ToList();
            double minX = allParcels.SelectMany(p => p.PolygonRing).Min(pt => pt.X);
            double maxX = allParcels.SelectMany(p => p.PolygonRing).Max(pt => pt.X);
            double minY = allParcels.SelectMany(p => p.PolygonRing).Min(pt => pt.Y);
            double maxY = allParcels.SelectMany(p => p.PolygonRing).Max(pt => pt.Y);

            double geomWidth = Math.Max(1.0, maxX - minX);
            double geomHeight = Math.Max(1.0, maxY - minY);

            // Generous margins to display street labels on all 4 sides (Top, Bottom, Left, Right)
            double topMargin = 28.0;
            double bottomMargin = (config.Arrangement == ArrangementMode.BackToBack) ? 28.0 : 18.0;
            double sideMargin = 45.0; // Margin for left and right cross streets

            double availWidth = Math.Max(10.0, width - (sideMargin * 2));
            double availHeight = Math.Max(10.0, height - (topMargin + bottomMargin));

            double scale = Math.Min(availWidth / geomWidth, availHeight / geomHeight);
            double drawWidth = geomWidth * scale;
            double drawHeight = geomHeight * scale;

            double startX = sideMargin + ((availWidth - drawWidth) / 2.0);
            double startY = topMargin + ((availHeight - drawHeight) / 2.0);

            Point Transform(Point2D pt)
            {
                double px = startX + ((pt.X - minX) * scale);
                double py = startY + ((maxY - pt.Y) * scale);
                return new Point(px, py);
            }

            // 1. Street A Header (Top Street)
            var streetAText = new FormattedText(
                $"▲ {config.SideA.StreetLabel}{(config.SideA.IsReference ? " (Reference)" : "")} — {config.SideA.TotalFrontage:F0}m",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                10,
                new SolidColorBrush(Color.FromRgb(41, 182, 246)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(streetAText, new Point((width - streetAText.Width) / 2, 6));

            // 2. Street B Footer (Bottom Street)
            if (config.Arrangement == ArrangementMode.BackToBack && config.SideB.GeneratedParcels.Count > 0)
            {
                var streetBText = new FormattedText(
                    $"▼ {config.SideB.StreetLabel} — {config.SideB.TotalFrontage:F0}m",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    10,
                    new SolidColorBrush(Color.FromRgb(171, 71, 188)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(streetBText, new Point((width - streetBText.Width) / 2, height - 20));
            }

            // 3. Left Cross Street (West / Left Street)
            string leftStreetName = !string.IsNullOrWhiteSpace(config.Corner?.LeftStreetLabel) ? config.Corner.LeftStreetLabel : "Left Cross St (الشارع الأيسر)";
            var leftStreetText = new FormattedText(
                $"◀ {leftStreetName}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                9,
                new SolidColorBrush(Color.FromRgb(0, 191, 165)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Draw left street rotated vertically
            dc.PushTransform(new RotateTransform(-90, 12, startY + (drawHeight / 2)));
            dc.DrawText(leftStreetText, new Point(12 - (leftStreetText.Width / 2), startY + (drawHeight / 2) - 6));
            dc.Pop();

            // 4. Right Cross Street (East / Right Street)
            string rightStreetName = !string.IsNullOrWhiteSpace(config.Corner?.RightStreetLabel) ? config.Corner.RightStreetLabel : "Right Cross St (الشارع الأيمن)";
            var rightStreetText = new FormattedText(
                $"{rightStreetName} ▶",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                9,
                new SolidColorBrush(Color.FromRgb(0, 191, 165)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Draw right street rotated vertically
            dc.PushTransform(new RotateTransform(90, width - 12, startY + (drawHeight / 2)));
            dc.DrawText(rightStreetText, new Point((width - 12) - (rightStreetText.Width / 2), startY + (drawHeight / 2) - 6));
            dc.Pop();

            // Draw Parcels
            foreach (var parcel in allParcels)
            {
                if (parcel.PolygonRing.Count < 3) continue;

                var screenPts = parcel.PolygonRing.Select(Transform).ToList();
                _renderedPolygons.Add((parcel, screenPts));

                var streamGeometry = new StreamGeometry();
                using (var ctx = streamGeometry.Open())
                {
                    ctx.BeginFigure(screenPts[0], true, true);
                    for (int i = 1; i < screenPts.Count; i++)
                    {
                        ctx.LineTo(screenPts[i], true, false);
                    }
                }
                streamGeometry.Freeze();

                bool isSelected = parcel.Id == SelectedParcelId || (!string.IsNullOrEmpty(SelectedParcelId2) && parcel.Id == SelectedParcelId2);
                bool isHovered = parcel.Id == _hoveredParcelId && _hoveredAnchor == null;
                bool isElectricRoom = parcel.Type == "Electric Room" || parcel.Id == "ER-01";
                bool isThrough = parcel.Side == ParcelSide.Both || parcel.Type == "Through Parcel";

                Brush fillBrush = isSelected
                    ? _selectedBrush
                    : (isHovered
                        ? _hoverBrush
                        : (isElectricRoom
                            ? _electricRoomBrush
                            : (isThrough
                                ? _throughParcelBrush
                                : (parcel.Side == ParcelSide.SideA ? _sideABrush : _sideBBrush))));

                Pen pen = isSelected ? _selectedPen : (isElectricRoom ? _electricRoomPen : _borderPen);

                dc.DrawGeometry(fillBrush, pen, streamGeometry);

                // Render split preview dividing lines or sub-parcels if active on this parcel
                if (isSelected && config.SplitMerge != null && config.SplitMerge.Mode == SplitMergeMode.SplitParcel &&
                    config.SplitMerge.SelectedParcelId == parcel.Id && config.SplitMerge.PreviewSplitParcels.Count == 2)
                {
                    var p1 = config.SplitMerge.PreviewSplitParcels[0];
                    var p2 = config.SplitMerge.PreviewSplitParcels[1];

                    var splitPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 215, 64)), 2.0)
                    {
                        DashStyle = DashStyles.Dash
                    };

                    // Draw sub-boundary lines
                    var sPts1 = p1.PolygonRing.Select(Transform).ToList();
                    var sPts2 = p2.PolygonRing.Select(Transform).ToList();

                    var splitGeo = new StreamGeometry();
                    using (var sCtx = splitGeo.Open())
                    {
                        sCtx.BeginFigure(sPts1[0], true, true);
                        for (int k = 1; k < sPts1.Count; k++) sCtx.LineTo(sPts1[k], true, false);
                    }
                    splitGeo.Freeze();
                    dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(90, 0, 229, 255)), splitPen, splitGeo);

                    var splitGeo2 = new StreamGeometry();
                    using (var sCtx2 = splitGeo2.Open())
                    {
                        sCtx2.BeginFigure(sPts2[0], true, true);
                        for (int k = 1; k < sPts2.Count; k++) sCtx2.LineTo(sPts2[k], true, false);
                    }
                    splitGeo2.Freeze();
                    dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(90, 0, 191, 165)), splitPen, splitGeo2);
                }

                // Render merge preview highlight on the selected parcels
                if (isSelected && config.SplitMerge != null && config.SplitMerge.Mode == SplitMergeMode.MergeParcels &&
                    config.SplitMerge.PreviewMergedParcel != null &&
                    (parcel.Id == config.SplitMerge.MergeParcelId1 || parcel.Id == config.SplitMerge.MergeParcelId2))
                {
                    var mergePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 229, 255)), 2.0)
                    {
                        DashStyle = DashStyles.Dash
                    };
                    dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(40, 0, 229, 255)), mergePen, streamGeometry);
                }

                // Sequence number label or ER label (ensure high contrast: dark navy on cyan selection, crisp white otherwise)
                var center = Transform(parcel.Centroid);
                string labelText = isElectricRoom ? "⚡ ER" : parcel.Sequence.ToString();
                var numText = new FormattedText(
                    labelText,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    isElectricRoom ? 10 : 11,
                    isSelected ? new SolidColorBrush(Color.FromRgb(11, 19, 43)) : Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(numText, new Point(center.X - (numText.Width / 2), center.Y - (numText.Height / 2)));
            }

            // Draw Anchor Points & Visual Indicators
            if (ShowAnchorHandles)
            {
                double frontageA = config.SideA.TotalFrontage;
                double frontageB = config.SideB.TotalFrontage;
                double maxFrontage = Math.Max(frontageA, frontageB);
                double depthA = config.BaseParcel.Depth;
                double depthB = config.Arrangement == ArrangementMode.BackToBack ? -config.BaseParcel.Depth : 0.0;

                var activeAnchor = AnchorPoint ?? config.Alignment?.AnchorPoint ?? BlockAnchorPoint.SideAStart;

                _renderedAnchors.Add((BlockAnchorPoint.SideAStart, "Front-Left (Side A Start)", Transform(new Point2D(0, depthA)), activeAnchor == BlockAnchorPoint.SideAStart));
                _renderedAnchors.Add((BlockAnchorPoint.SideAEnd, "Front-Right (Side A End)", Transform(new Point2D(frontageA, depthA)), activeAnchor == BlockAnchorPoint.SideAEnd));
                _renderedAnchors.Add((BlockAnchorPoint.SideACenter, "Street A Center", Transform(new Point2D(frontageA / 2.0, depthA)), activeAnchor == BlockAnchorPoint.SideACenter));

                if (config.Arrangement == ArrangementMode.BackToBack)
                {
                    _renderedAnchors.Add((BlockAnchorPoint.SideBStart, "Back-Left (Side B Start)", Transform(new Point2D(0, depthB)), activeAnchor == BlockAnchorPoint.SideBStart));
                    _renderedAnchors.Add((BlockAnchorPoint.SideBEnd, "Back-Right (Side B End)", Transform(new Point2D(frontageB, depthB)), activeAnchor == BlockAnchorPoint.SideBEnd));
                }

                double centroidY = config.Arrangement == ArrangementMode.BackToBack ? 0.0 : depthA / 2.0;
                _renderedAnchors.Add((BlockAnchorPoint.BlockCenter, "Block Centroid", Transform(new Point2D(maxFrontage / 2.0, centroidY)), activeAnchor == BlockAnchorPoint.BlockCenter));

                foreach (var anchor in _renderedAnchors)
                {
                    bool isHovered = _hoveredAnchor == anchor.Type;
                    double radius = anchor.IsActive ? 8.0 : (isHovered ? 7.0 : 5.0);

                    Brush anchorFill = anchor.IsActive
                        ? new SolidColorBrush(Color.FromRgb(0, 229, 255))
                        : (isHovered ? new SolidColorBrush(Color.FromRgb(255, 215, 64)) : new SolidColorBrush(Color.FromArgb(200, 14, 23, 54)));
                    Pen anchorPen = new Pen(anchor.IsActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(0, 229, 255)), anchor.IsActive ? 2.0 : 1.2);

                    dc.DrawEllipse(anchorFill, anchorPen, anchor.ScreenPt, radius, radius);

                    if (anchor.IsActive)
                    {
                        dc.DrawEllipse(Brushes.White, null, anchor.ScreenPt, 3.0, 3.0);
                    }
                }
            }

            // --- Draw Dedicated Electric Room Footprint Overlay ---
            if (config.ElectricRoom != null && (config.ElectricRoom.HasElectricRoom || ShowElectricRoomHandles))
            {
                var er = config.ElectricRoom;
                double erWidth = Math.Max(0.5, er.Width);
                double erDepth = Math.Max(0.5, er.Depth);
                bool isSideA = er.Side == ParcelSide.SideA;
                double depth = config.BaseParcel.Depth;
                double yStreet = isSideA ? depth : (config.Arrangement == ArrangementMode.BackToBack ? -depth : 0.0);
                double yInner = isSideA ? (depth - erDepth) : (yStreet + erDepth);

                double targetX = er.OffsetDistance;
                double startCutX = 0.0;
                if (config.Corner != null && config.Corner.HasChamfer)
                {
                    var cStart = isSideA
                        ? config.Corner.GetEffectiveCorner(CornerPosition.SideAStart)
                        : config.Corner.GetEffectiveCorner(CornerPosition.SideBStart);
                    if (cStart.IsEnabled)
                    {
                        var firstP = (isSideA ? config.SideA.GeneratedParcels : config.SideB.GeneratedParcels).FirstOrDefault();
                        var (cutX, _) = cStart.GetEffectiveCutDistances(firstP?.Frontage ?? 10.0, depth);
                        startCutX = cutX;
                    }
                }

                if (er.PlacementMethod == ElectricRoomPlacementMethod.OffsetDistance && er.OffsetDistance < 1e-4 && startCutX > 0)
                {
                    targetX = startCutX;
                }

                double erX1 = er.RoomAnchor switch
                {
                    ElectricRoomAnchorPoint.TopRight or ElectricRoomAnchorPoint.BottomRight => targetX - erWidth,
                    ElectricRoomAnchorPoint.Center => targetX - (erWidth / 2.0),
                    _ => targetX
                };
                double erX2 = erX1 + erWidth;

                // Screen points
                var pInnerLeft = Transform(new Point2D(erX1, yInner));
                var pStreetLeft = Transform(new Point2D(erX1, yStreet));
                var pStreetRight = Transform(new Point2D(erX2, yStreet));
                var pInnerRight = Transform(new Point2D(erX2, yInner));

                // Guarantee a minimum visual footprint size (at least 18px wide x 18px high) so it is always clearly noticeable
                double sWidth = Math.Abs(pStreetRight.X - pStreetLeft.X);
                double sHeight = Math.Abs(pInnerLeft.Y - pStreetLeft.Y);

                if (sWidth < 18.0)
                {
                    double diff = 18.0 - sWidth;
                    pStreetRight.X += diff;
                    pInnerRight.X += diff;
                }
                if (sHeight < 18.0)
                {
                    double diff = (18.0 - sHeight) * (isSideA ? 1 : -1);
                    pInnerLeft.Y += diff;
                    pInnerRight.Y += diff;
                }

                var erScreenPts = new List<Point> { pInnerLeft, pStreetLeft, pStreetRight, pInnerRight };

                var erGeom = new StreamGeometry();
                using (var ctx = erGeom.Open())
                {
                    ctx.BeginFigure(erScreenPts[0], true, true);
                    for (int k = 1; k < erScreenPts.Count; k++)
                    {
                        ctx.LineTo(erScreenPts[k], true, false);
                    }
                }
                erGeom.Freeze();

                // 1. Glowing outer amber halo / shadow
                var erGlowPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 255, 171, 0)), 5.0);
                erGlowPen.Freeze();
                dc.DrawGeometry(null, erGlowPen, erGeom);

                // 2. High-contrast vibrant amber fill
                var erFillBrush = new SolidColorBrush(Color.FromRgb(255, 160, 0)); // Amber #FFA000
                erFillBrush.Freeze();
                var erBorderPen = new Pen(Brushes.White, 2.0);
                erBorderPen.Freeze();
                dc.DrawGeometry(erFillBrush, erBorderPen, erGeom);

                // 3. Technical dashed inner line (hazard/equipment motif)
                var innerHatchPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 30, 30, 30)), 1.2)
                {
                    DashStyle = DashStyles.Dash
                };
                innerHatchPen.Freeze();
                dc.DrawGeometry(null, innerHatchPen, erGeom);

                // 4. Center Label "⚡ ER"
                Point centerPt = new Point(
                    (pInnerLeft.X + pStreetRight.X) / 2.0,
                    (pStreetLeft.Y + pInnerRight.Y) / 2.0
                );

                var erText = new FormattedText(
                    "⚡ ER",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.ExtraBold, FontStretches.Normal),
                    10,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(erText, new Point(centerPt.X - (erText.Width / 2.0), centerPt.Y - (erText.Height / 2.0)));

                // 5. Room Anchor Point glowing marker (indicates where the room is pinned)
                Point anchorPt = er.AnchorPoint.HasValue
                    ? Transform(er.AnchorPoint.Value)
                    : Transform(new Point2D(erX1, yStreet));

                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0, 229, 255)), new Pen(Brushes.White, 1.5), anchorPt, 4.0, 4.0);
            }

            // Draw Interactive Electric Room Handles (Small, delicate red dots along street frontages and parcel boundaries)
            if (ShowElectricRoomHandles)
            {
                double depthA = config.BaseParcel.Depth;
                double depthB = config.Arrangement == ArrangementMode.BackToBack ? -config.BaseParcel.Depth : 0.0;
                double activeOffset = config.ElectricRoom?.OffsetDistance ?? 0.0;
                bool isEROnA = (config.ElectricRoom?.Side ?? ParcelSide.SideA) == ParcelSide.SideA;
                bool isEROnB = (config.ElectricRoom?.Side ?? ParcelSide.SideA) == ParcelSide.SideB;

                // --- Street A Handles ---
                var sideAParcels = config.SideA.GeneratedParcels.Where(p => p.Type != "Electric Room" && p.Id != "ER-01").ToList();
                double startCutXA = 0.0;
                double endCutXA = 0.0;
                if (config.Corner != null && config.Corner.HasChamfer)
                {
                    var cStart = config.Corner.GetEffectiveCorner(CornerPosition.SideAStart);
                    if (cStart.IsEnabled)
                    {
                        var (cutX, _) = cStart.GetEffectiveCutDistances(sideAParcels.FirstOrDefault()?.Frontage ?? 10.0, depthA);
                        startCutXA = cutX;
                    }
                    var cEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideAEnd);
                    if (cEnd.IsEnabled)
                    {
                        var (cutX, _) = cEnd.GetEffectiveCutDistances(sideAParcels.LastOrDefault()?.Frontage ?? 10.0, depthA);
                        endCutXA = cutX;
                    }
                }

                double curXA = 0.0;
                bool isStartAActive = isEROnA && (Math.Abs(activeOffset - 0.0) < 0.1 || Math.Abs(activeOffset - startCutXA) < 0.1);
                _renderedElectricRoomHandles.Add(new ElectricRoomHandleInfo
                {
                    Side = ParcelSide.SideA,
                    Offset = 0.0,
                    ScreenPt = Transform(new Point2D(startCutXA, depthA)),
                    Description = startCutXA > 0 ? $"Street A Frontage Start ({startCutXA:F1} m)" : "Street A Start (0.0 m)",
                    IsActive = isStartAActive
                });

                for (int i = 0; i < sideAParcels.Count; i++)
                {
                    var p = sideAParcels[i];
                    curXA += p.Frontage;
                    double ptX = curXA;
                    if (i == sideAParcels.Count - 1 && endCutXA > 0)
                    {
                        ptX = Math.Max(0.0, curXA - endCutXA);
                    }
                    bool isAct = isEROnA && Math.Abs(activeOffset - curXA) < 0.1;
                    string desc = (i == sideAParcels.Count - 1)
                        ? $"Street A End ({ptX:F1} m)"
                        : $"Boundary between A-{p.Sequence:D2} & A-{(p.Sequence + 1):D2} ({curXA:F1} m)";

                    _renderedElectricRoomHandles.Add(new ElectricRoomHandleInfo
                    {
                        Side = ParcelSide.SideA,
                        Offset = Math.Round(curXA, 2),
                        ScreenPt = Transform(new Point2D(ptX, depthA)),
                        Description = desc,
                        IsActive = isAct
                    });
                }

                // --- Street B Handles (if BackToBack) ---
                if (config.Arrangement == ArrangementMode.BackToBack)
                {
                    var sideBParcels = config.SideB.GeneratedParcels.Where(p => p.Type != "Electric Room" && p.Id != "ER-01").ToList();
                    double startCutXB = 0.0;
                    double endCutXB = 0.0;
                    if (config.Corner != null && config.Corner.HasChamfer)
                    {
                        var cStart = config.Corner.GetEffectiveCorner(CornerPosition.SideBStart);
                        if (cStart.IsEnabled)
                        {
                            var (cutX, _) = cStart.GetEffectiveCutDistances(sideBParcels.FirstOrDefault()?.Frontage ?? 10.0, depthB);
                            startCutXB = cutX;
                        }
                        var cEnd = config.Corner.GetEffectiveCorner(CornerPosition.SideBEnd);
                        if (cEnd.IsEnabled)
                        {
                            var (cutX, _) = cEnd.GetEffectiveCutDistances(sideBParcels.LastOrDefault()?.Frontage ?? 10.0, depthB);
                            endCutXB = cutX;
                        }
                    }

                    double curXB = 0.0;
                    bool isStartBActive = isEROnB && (Math.Abs(activeOffset - 0.0) < 0.1 || Math.Abs(activeOffset - startCutXB) < 0.1);
                    _renderedElectricRoomHandles.Add(new ElectricRoomHandleInfo
                    {
                        Side = ParcelSide.SideB,
                        Offset = 0.0,
                        ScreenPt = Transform(new Point2D(startCutXB, depthB)),
                        Description = startCutXB > 0 ? $"Street B Frontage Start ({startCutXB:F1} m)" : "Street B Start (0.0 m)",
                        IsActive = isStartBActive
                    });

                    for (int i = 0; i < sideBParcels.Count; i++)
                    {
                        var p = sideBParcels[i];
                        curXB += p.Frontage;
                        double ptX = curXB;
                        if (i == sideBParcels.Count - 1 && endCutXB > 0)
                        {
                            ptX = Math.Max(0.0, curXB - endCutXB);
                        }
                        bool isAct = isEROnB && Math.Abs(activeOffset - curXB) < 0.1;
                        string desc = (i == sideBParcels.Count - 1)
                            ? $"Street B End ({ptX:F1} m)"
                            : $"Boundary between B-{p.Sequence:D2} & B-{(p.Sequence + 1):D2} ({curXB:F1} m)";

                        _renderedElectricRoomHandles.Add(new ElectricRoomHandleInfo
                        {
                            Side = ParcelSide.SideB,
                            Offset = Math.Round(curXB, 2),
                            ScreenPt = Transform(new Point2D(ptX, depthB)),
                            Description = desc,
                            IsActive = isAct
                        });
                    }
                }

                // Render each handle as a small, delicate red dot with white border and subtle hover halo
                var redFill = new SolidColorBrush(Color.FromRgb(255, 23, 68)); // #FF1744
                redFill.Freeze();
                var redHover = new SolidColorBrush(Color.FromRgb(255, 82, 82)); // #FF5252
                redHover.Freeze();
                var goldActive = new SolidColorBrush(Color.FromRgb(255, 215, 64)); // #FFD740
                goldActive.Freeze();
                var whitePen = new Pen(Brushes.White, 1.0);
                whitePen.Freeze();
                var goldPen = new Pen(goldActive, 1.5);
                goldPen.Freeze();

                foreach (var handle in _renderedElectricRoomHandles)
                {
                    bool isHovered = _hoveredElectricRoomHandle != null &&
                                     _hoveredElectricRoomHandle.Side == handle.Side &&
                                     Math.Abs(_hoveredElectricRoomHandle.Offset - handle.Offset) < 0.01;

                    // Smaller point radius: 4.5px active, 4.0px hover, 3.0px normal
                    double r = handle.IsActive ? 4.5 : (isHovered ? 4.0 : 3.0);

                    if (handle.IsActive)
                    {
                        var activeHalo = new SolidColorBrush(Color.FromArgb(90, 255, 23, 68));
                        dc.DrawEllipse(activeHalo, goldPen, handle.ScreenPt, r + 2.5, r + 2.5);
                    }
                    else if (isHovered)
                    {
                        var hoverHalo = new SolidColorBrush(Color.FromArgb(70, 255, 82, 82));
                        dc.DrawEllipse(hoverHalo, null, handle.ScreenPt, r + 2.0, r + 2.0);
                    }

                    Brush fill = isHovered ? redHover : redFill;
                    Pen pen = handle.IsActive ? goldPen : whitePen;
                    dc.DrawEllipse(fill, pen, handle.ScreenPt, r, r);

                    if (handle.IsActive)
                    {
                        dc.DrawEllipse(goldActive, null, handle.ScreenPt, 1.8, 1.8);
                    }
                    else
                    {
                        dc.DrawEllipse(Brushes.White, null, handle.ScreenPt, 1.0, 1.0);
                    }
                }
            }
        }

        private static bool IsPointInsidePolygon(Point pt, List<Point> poly)
        {
            if (poly.Count < 3) return false;
            bool inside = false;
            int j = poly.Count - 1;
            for (int i = 0; i < poly.Count; i++)
            {
                if (((poly[i].Y <= pt.Y && pt.Y < poly[j].Y) || (poly[j].Y <= pt.Y && pt.Y < poly[i].Y)) &&
                    (pt.X < (poly[j].X - poly[i].X) * (pt.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                {
                    inside = !inside;
                }
                j = i;
            }
            return inside;
        }
    }

    public class ElectricRoomHandleInfo
    {
        public ParcelSide Side { get; set; }
        public double Offset { get; set; }
        public Point ScreenPt { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
