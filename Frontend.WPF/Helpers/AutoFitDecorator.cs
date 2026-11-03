using System.Windows;
using System.Windows.Controls;

namespace Frontend.WPF.Helpers
{
    /// <summary>
    /// Hosts a TextBlock and gives it the biggest FontSize at which its text still fits the available area.
    /// Sizes are snapped to a ladder so slightly longer/shorter text keeps the same size.
    /// </summary>
    public class AutoFitDecorator : Decorator
    {
        public static readonly DependencyProperty MinFontSizeProperty =
            DependencyProperty.Register(nameof(MinFontSize), typeof(double), typeof(AutoFitDecorator),
                new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLimitChanged));

        public static readonly DependencyProperty MaxFontSizeProperty =
            DependencyProperty.Register(nameof(MaxFontSize), typeof(double), typeof(AutoFitDecorator),
                new FrameworkPropertyMetadata(160.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLimitChanged));

        /// <summary>Each size step is this many percent bigger than the one below. Higher = steadier size, 0 = always exact fit.</summary>
        public static readonly DependencyProperty SnapPercentProperty =
            DependencyProperty.Register(nameof(SnapPercent), typeof(double), typeof(AutoFitDecorator),
                new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLimitChanged));

        public double MinFontSize
        {
            get => (double)GetValue(MinFontSizeProperty);
            set => SetValue(MinFontSizeProperty, value);
        }

        public double MaxFontSize
        {
            get => (double)GetValue(MaxFontSizeProperty);
            set => SetValue(MaxFontSizeProperty, value);
        }

        public double SnapPercent
        {
            get => (double)GetValue(SnapPercentProperty);
            set => SetValue(SnapPercentProperty, value);
        }

        // Last fit result. While only the highlight moves the text is unchanged,
        // so the size is reused and never jumps between portions.
        private bool hasCache;
        private string? cachedText;
        private TextWrapping cachedWrap;
        private Size cachedBounds;
        private double cachedSize;

        private static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((AutoFitDecorator)d).hasCache = false;

        protected override Size MeasureOverride(Size constraint)
        {
            if (Child is not TextBlock tb ||
                double.IsInfinity(constraint.Width) || double.IsInfinity(constraint.Height))
                return base.MeasureOverride(constraint);

            string? text = GetText(tb);

            bool reuse = hasCache
                         && text == cachedText
                         && tb.TextWrapping == cachedWrap
                         && constraint == cachedBounds;

            double size = reuse ? cachedSize : FindBestSize(tb, constraint);

            if (!reuse)
            {
                hasCache = true;
                cachedText = text;
                cachedWrap = tb.TextWrapping;
                cachedBounds = constraint;
                cachedSize = size;
            }

            tb.FontSize = size;
            MeasureChild(tb, constraint.Width);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is UIElement child)
            {
                double height = Math.Min(child.DesiredSize.Height, finalSize.Height);
                double top = (finalSize.Height - height) / 2;   // vertically centred
                child.Arrange(new Rect(0, top, finalSize.Width, height));
            }
            return finalSize;
        }

        private double FindBestSize(TextBlock tb, Size bounds)
        {
            double min = Math.Max(1, MinFontSize);
            double lo = min;
            double hi = Math.Max(lo, MaxFontSize);

            if (Fits(tb, hi, bounds))          // short text: just use the cap
                return hi;

            while (hi - lo > 0.5)              // binary search for the largest size that fits
            {
                double mid = (lo + hi) / 2;
                if (Fits(tb, mid, bounds)) lo = mid; else hi = mid;
            }

            if (SnapPercent <= 0)
                return lo;

            // Snap down to the nearest step of the ladder: min, min*r, min*r^2, ...
            double ratio = 1 + SnapPercent / 100.0;
            double steps = Math.Floor(Math.Log(lo / min) / Math.Log(ratio));
            return min * Math.Pow(ratio, steps);
        }

        private static bool Fits(TextBlock tb, double fontSize, Size bounds)
        {
            tb.FontSize = fontSize;
            MeasureChild(tb, bounds.Width);
            return tb.DesiredSize.Height <= bounds.Height
                && tb.DesiredSize.Width <= bounds.Width + 0.5;
        }

        private static void MeasureChild(TextBlock tb, double width)
        {
            // NoWrap text must be measured unconstrained, otherwise WPF clamps DesiredSize
            // to the available width and an overflowing line would look like it fits.
            double measureWidth = tb.TextWrapping == TextWrapping.NoWrap ? double.PositiveInfinity : width;
            tb.Measure(new Size(measureWidth, double.PositiveInfinity));
        }

        private static string? GetText(TextBlock tb)
            => tb is HighlightableTextBlock h ? h.Text : tb.Text;
    }
}