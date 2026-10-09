using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sahand.ClinicPro.Desktop.Controls.Charts;

/// <summary>
/// نمودار دونات — انتخاب درست برای داده‌های «سهم از کل» (part-of-whole):
/// سهم هر پزشک از درآمد، ترکیب خدمات، تفکیک روش‌های پرداخت، جنسیت بیماران.
/// قطاع‌ها با ArcSegment از بالای حلقه در جهت ساعتگرد + راهنمای درصدی کنار حلقه.
/// </summary>
public partial class DoughnutChart : UserControl
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IReadOnlyList<ChartPointData>), typeof(DoughnutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    /// <summary>قطاع‌ها — سهم هر مقوله</summary>
    public IReadOnlyList<ChartPointData>? Points
    {
        get => (IReadOnlyList<ChartPointData>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty CenterTextProperty = DependencyProperty.Register(
        nameof(CenterText), typeof(string), typeof(DoughnutChart), new PropertyMetadata(""));

    /// <summary>متن مرکز حلقه — معمولاً جمع کل</summary>
    public string CenterText
    {
        get => (string)GetValue(CenterTextProperty);
        set => SetValue(CenterTextProperty, value);
    }

    public static readonly DependencyProperty CenterSubtextProperty = DependencyProperty.Register(
        nameof(CenterSubtext), typeof(string), typeof(DoughnutChart), new PropertyMetadata(""));

    /// <summary>زیرنویس مرکز — مثلاً واحد</summary>
    public string CenterSubtext
    {
        get => (string)GetValue(CenterSubtextProperty);
        set => SetValue(CenterSubtextProperty, value);
    }

    public static readonly DependencyProperty RingSizeProperty = DependencyProperty.Register(
        nameof(RingSize), typeof(double), typeof(DoughnutChart), new PropertyMetadata(150.0, OnRingChanged));

    /// <summary>قطر بیرونی حلقه (px)</summary>
    public double RingSize
    {
        get => (double)GetValue(RingSizeProperty);
        set => SetValue(RingSizeProperty, value);
    }

    public DoughnutChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DoughnutChart)d).Render();

    private static void OnRingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DoughnutChart)d).Render();

    private void Render()
    {
        if (RingCanvas is null || LegendPanel is null) return;
        RingCanvas.Children.Clear();
        LegendPanel.Children.Clear();

        var pts = Points?.Where(p => p.Value > 0).ToList();
        var any = pts is { Count: > 0 };
        EmptyHint.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
        CenterValue.Text = CenterText;
        CenterLabel.Text = CenterSubtext;
        if (!any) return;

        var points = pts!; // رفع CS8602: نمونه غیرتهی پس از گارد بالا
        var total = points.Sum(p => p.Value);
        var size = Math.Max(60, RingSize);
        var outer = size / 2 - 2;
        var inner = outer * 0.62; // نسبت دونات ۶۲٪
        var cx = size / 2;
        var cy = size / 2;
        RingCanvas.Width = size;
        RingCanvas.Height = size;

        double start = -90; // از بالای حلقه
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var sweep = p.Value / total * 360;
            var end = start + Math.Min(sweep, 359.999);

            var figure = new PathFigure { StartPoint = Polar(cx, cy, outer, start) };
            figure.Segments.Add(new ArcSegment(Polar(cx, cy, outer, end), new Size(outer, outer),
                0, sweep > 180, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(Polar(cx, cy, inner, end), true));
            figure.Segments.Add(new ArcSegment(Polar(cx, cy, inner, start), new Size(inner, inner),
                0, sweep > 180, SweepDirection.Counterclockwise, true));
            figure.IsClosed = true;

            var pct = p.Value / total * 100;
            RingCanvas.Children.Add(new Path
            {
                Data = new PathGeometry(new[] { figure }),
                Fill = ChartPalette.Brush(i),
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                ToolTip = p.Tooltip ?? $"{p.Label}: {pct:0.#}٪ ({p.Value:N0})",
            });

            // راهنما: نقطه رنگ + برچسب + درصد
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3), ToolTip = p.Tooltip };
            row.Children.Add(new Border
            {
                Width = 11, Height = 11, CornerRadius = new CornerRadius(3),
                Background = ChartPalette.Brush(i),
                VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock
            {
                Text = p.Label, FontSize = 11, Margin = new Thickness(7, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
            });
            row.Children.Add(new TextBlock
            {
                Text = pct.ToString("0.#") + "٪", FontSize = 10.5, Opacity = 0.6,
                // چرخهٔ ۲۶: فونت هاردکد Consolas حذف — ارقام فارسی fallback می‌گرفت؛
                // اکنون فونت وزیرمتن جاسازی‌شدهٔ برنامه (قانون «فونت روی ارقام فارسی»)
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = Sahand.ClinicPro.Desktop.Helpers.FontAssets.Vazirmatn,
            });
            LegendPanel.Children.Add(row);

            start = end + 0.0001;
        }
    }

    /// <summary>مختصات قطبی → دکارتی</summary>
    private static Point Polar(double cx, double cy, double r, double deg)
    {
        var rad = deg * Math.PI / 180.0;
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }
}
