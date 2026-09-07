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
    /// High-performance vector canvas rendering the schematic block layout,
    /// dynamic parcel outlines, sequence labels, chamfers, hover tooltips,
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
        private readonly Brush _selectedBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));      // #00E5FF
        private readonly Brush _hoverBrush = new SolidColorBrush(Color.FromArgb(240, 77, 208, 225));   // #4DD0E1
        private readonly Pen _borderPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), 1.2);
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
            _selectedBrush.Freeze();
            _hoverBrush.Freeze();
            _borderPen.Freeze();
            _selectedPen.Freeze();
            _spinePen.Freeze();

            MouseMove += OnCanvasMouseMove;
            MouseLeave += OnCanvasMouseLeave;
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
                    Brushes.Gray,
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

            double topMargin = 34.0;
            double bottomMargin = (config.Arrangement == ArrangementMode.BackToBack) ? 34.0 : 20.0;
            double sideMargin = 32.0;

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

            // Street A Header
            var streetAText = new FormattedText(
                $"{config.SideA.StreetLabel}{(config.SideA.IsReference ? " (Reference)" : "")} — {config.SideA.TotalFrontage:F0}m",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                10,
                new SolidColorBrush(Color.FromRgb(41, 182, 246)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(streetAText, new Point((width - streetAText.Width) / 2, 8));

            // Street B Footer if Back-to-Back
            if (config.Arrangement == ArrangementMode.BackToBack && config.SideB.GeneratedParcels.Count > 0)
            {
                var streetBText = new FormattedText(
                    $"{config.SideB.StreetLabel} — {config.SideB.TotalFrontage:F0}m",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                    10,
                    new SolidColorBrush(Color.FromRgb(171, 71, 188)),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(streetBText, new Point((width - streetBText.Width) / 2, height - 22));
            }

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

                Brush fillBrush = isSelected
                    ? _selectedBrush
                    : (isHovered ? _hoverBrush : (parcel.Side == ParcelSide.SideA ? _sideABrush : _sideBBrush));
                Pen pen = isSelected ? _selectedPen : _borderPen;

                dc.DrawGeometry(fillBrush, pen, streamGeometry);

                // Sequence number label
                var center = Transform(parcel.Centroid);
                var numText = new FormattedText(
                    parcel.Sequence.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    11,
                    isSelected ? Brushes.Black : Brushes.White,
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

                // 1. Draw Inactive Candidate Handles
                foreach (var anchor in _renderedAnchors.Where(a => !a.IsActive))
                {
                    bool isHovered = _hoveredAnchor == anchor.Type;
                    if (isHovered)
                    {
                        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(220, 0, 229, 255)), new Pen(Brushes.White, 2.0), anchor.ScreenPt, 8.5, 8.5);
                        dc.DrawEllipse(Brushes.White, null, anchor.ScreenPt, 3.5, 3.5);
                    }
                    else
                    {
                        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(190, 15, 23, 42)), new Pen(new SolidColorBrush(Color.FromArgb(220, 0, 229, 255)), 1.4), anchor.ScreenPt, 6.0, 6.0);
                        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)), null, anchor.ScreenPt, 2.0, 2.0);
                    }
                }

                // 2. Draw Active Anchor (Prominent Glowing Marker + Badge)
                var activeItem = _renderedAnchors.FirstOrDefault(a => a.IsActive);
                if (activeItem.Type != 0 || activeItem.IsActive)
                {
                    // Glowing outer pulse ring
                    dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 0, 229, 255)), new Pen(new SolidColorBrush(Color.FromRgb(0, 229, 255)), 2.5), activeItem.ScreenPt, 13.0, 13.0);
                    // Inner glowing amber/gold target
                    dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 215, 0)), new Pen(Brushes.White, 2.0), activeItem.ScreenPt, 6.0, 6.0);

                    // Badge Pill Label
                    var badgeText = new FormattedText(
                        $"⚓ Anchor: {activeItem.Name}",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                        10,
                        new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    double badgeX = activeItem.ScreenPt.X + 16;
                    double badgeY = activeItem.ScreenPt.Y - 10;
                    if (badgeX + badgeText.Width + 14 > width)
                    {
                        badgeX = activeItem.ScreenPt.X - badgeText.Width - 22;
                    }
                    if (badgeY < 6) badgeY = activeItem.ScreenPt.Y + 12;

                    var bgRect = new Rect(badgeX - 6, badgeY - 3, badgeText.Width + 12, badgeText.Height + 6);
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(240, 11, 19, 43)), new Pen(new SolidColorBrush(Color.FromRgb(0, 229, 255)), 1.2), bgRect, 4, 4);
                    dc.DrawText(badgeText, new Point(badgeX, badgeY));
                }
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton != MouseButton.Left) return;

            var pos = e.GetPosition(this);

            // 1. Check Interactive Anchor Snap Hit
            if (ShowAnchorHandles && _renderedAnchors.Count > 0)
            {
                foreach (var anchor in _renderedAnchors)
                {
                    double dist = (pos - anchor.ScreenPt).Length;
                    if (dist <= SnapThreshold)
                    {
                        AnchorPoint = anchor.Type;
                        if (Configuration?.Alignment != null)
                        {
                            Configuration.Alignment.AnchorPoint = anchor.Type;
                        }
                        InvalidateVisual();
                        e.Handled = true;
                        return;
                    }
                }
            }

            // 2. Parcel Click
            var clicked = _renderedPolygons.FirstOrDefault(p => IsPointInPolygon(pos, p.ScreenPolygon));
            if (clicked.Parcel != null)
            {
                SelectedParcelId = clicked.Parcel.Id;
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(this);

            // 1. Check Anchor Snap first
            BlockAnchorPoint? nearestAnchor = null;
            string? anchorName = null;

            if (ShowAnchorHandles && _renderedAnchors.Count > 0)
            {
                foreach (var anchor in _renderedAnchors)
                {
                    double dist = (pos - anchor.ScreenPt).Length;
                    if (dist <= SnapThreshold)
                    {
                        nearestAnchor = anchor.Type;
                        anchorName = anchor.Name;
                        break;
                    }
                }
            }

            if (nearestAnchor != null)
            {
                if (_hoveredAnchor != nearestAnchor)
                {
                    _hoveredAnchor = nearestAnchor;
                    _hoveredParcelId = null;
                    Cursor = Cursors.Cross;
                    ToolTip = $"🎯 Snap Reference Anchor: {anchorName}\n(Click to align block origin P1 to this point)";
                    InvalidateVisual();
                }
                return;
            }

            if (_hoveredAnchor != null)
            {
                _hoveredAnchor = null;
                Cursor = Cursors.Arrow;
                ToolTip = null;
                InvalidateVisual();
            }

            // 2. Fallback to Parcel Hover
            var hovered = _renderedPolygons.FirstOrDefault(p => IsPointInPolygon(pos, p.ScreenPolygon));
            string? newHoveredId = hovered.Parcel?.Id;
            if (_hoveredParcelId != newHoveredId)
            {
                _hoveredParcelId = newHoveredId;
                Cursor = _hoveredParcelId != null ? Cursors.Hand : Cursors.Arrow;

                if (hovered.Parcel != null)
                {
                    ToolTip = $"Parcel #{hovered.Parcel.Sequence} ({hovered.Parcel.Side})\n" +
                              $"Frontage: {hovered.Parcel.Frontage:F1} m\n" +
                              $"Depth: {hovered.Parcel.Depth:F1} m\n" +
                              $"Area: {hovered.Parcel.Area:N0} m²\n" +
                              $"Type: {hovered.Parcel.Type}";
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
            bool needInvalidate = false;
            if (_hoveredParcelId != null)
            {
                _hoveredParcelId = null;
                needInvalidate = true;
            }
            if (_hoveredAnchor != null)
            {
                _hoveredAnchor = null;
                needInvalidate = true;
            }

            if (needInvalidate)
            {
                ToolTip = null;
                Cursor = Cursors.Arrow;
                InvalidateVisual();
            }
        }

        private static bool IsPointInPolygon(Point point, List<Point> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
