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

        private const double SnapThreshold = 18.0;

        private readonly Brush _sideABrush = new SolidColorBrush(Color.FromArgb(200, 41, 182, 246)); // #29B6F6
        private readonly Brush _sideBBrush = new SolidColorBrush(Color.FromArgb(200, 171, 71, 188)); // #AB47BC
        private readonly Brush _electricRoomBrush = new SolidColorBrush(Color.FromArgb(230, 255, 145, 0)); // Amber/Orange #FF9100
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
        private readonly List<(ParcelModel Parcel, List<Point> ScreenPolygon)> _renderedPolygons = new();
        private readonly List<(BlockAnchorPoint Type, string Name, Point ScreenPt, bool IsActive)> _renderedAnchors = new();

        public SchematicCanvasControl()
        {
            ClipToBounds = true;
            _sideABrush.Freeze();
            _sideBBrush.Freeze();
            _electricRoomBrush.Freeze();
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
                    SelectedParcelId = item.Parcel.Id;

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

            if (ShowAnchorHandles)
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

            if (foundAnchor == null)
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

            if (foundAnchor == null && foundParcelId == null)
            {
                Cursor = Cursors.Arrow;
            }

            if (_hoveredParcelId != foundParcelId || _hoveredAnchor != foundAnchor)
            {
                _hoveredParcelId = foundParcelId;
                _hoveredAnchor = foundAnchor;
                InvalidateVisual();
            }
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            _hoveredParcelId = null;
            _hoveredAnchor = null;
            Cursor = Cursors.Arrow;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            _renderedPolygons.Clear();
            _renderedAnchors.Clear();

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

            var allParcels = config.SideA.GeneratedParcels.Concat(config.SideB.GeneratedParcels).ToList();
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

                bool isSelected = parcel.Id == SelectedParcelId;
                bool isHovered = parcel.Id == _hoveredParcelId && _hoveredAnchor == null;
                bool isElectricRoom = parcel.Type == "Electric Room" || parcel.Id == "ER-01";

                Brush fillBrush = isSelected
                    ? _selectedBrush
                    : (isHovered
                        ? _hoverBrush
                        : (isElectricRoom
                            ? _electricRoomBrush
                            : (parcel.Side == ParcelSide.SideA ? _sideABrush : _sideBBrush)));

                Pen pen = isSelected ? _selectedPen : (isElectricRoom ? _electricRoomPen : _borderPen);

                dc.DrawGeometry(fillBrush, pen, streamGeometry);

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
}
