using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sahand.ClinicPro.Desktop.Controls.Charts;

/// <summary>
/// نمودار سطحی (Area/Line) — انتخاب درست برای داده‌های «روند زمانی»:
/// درآمد روزانه، بیمار جدید ماهانه و هر سنجه پیوسته در طول زمان.
/// سطح گرادیانی زیر خط + نقاط تعاملی با Tooltip + برچسب‌های محور افقی.
/// رندر چپ‌به‌راست استاندارد نمودارهای زمانی؛ برچسب‌ها فارسی می‌مانند.
/// </summary>
public partial class AreaChart : UserControl
{
    // ═══ وابستگی‌ها ═══

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IReadOnlyList<ChartPointData>), typeof(AreaChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    /// <summary>نقاط سری — به ترتیب زمانی</summary>
    public IReadOnlyList<ChartPointData>? Points
    {
        get => (IReadOnlyList<ChartPointData>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(AreaChart), new PropertyMetadata("N0"));

    /// <summary>قالب عدد tooltip — پیش‌فرض بدون اعشار</summary>
    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public static readonly DependencyProperty ShowDotsProperty = DependencyProperty.Register(
        nameof(ShowDots), typeof(bool), typeof(AreaChart), new PropertyMetadata(true));

    /// <summary>نمایش نقاط قابل اشاره</summary>
    public bool ShowDots
    {
        get => (bool)GetValue(ShowDotsProperty);
        set => SetValue(ShowDotsProperty, value);
    }

    public AreaChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Render();
        Loaded += (_, _) => Render();
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AreaChart)d).Render();

    /// <summary>پاک‌سازی و بازسازی هندسه — هر تغییر اندازه یا داده</summary>
    private void Render()
    {
        if (PlotCanvas is null) return;
        PlotCanvas.Width = ActualWidth;
        PlotCanvas.Height = ActualHeight;
        PlotCanvas.Children.Clear();

        var pts = Points;
        EmptyHint.Visibility = pts is not { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        if (pts is null || pts.Count == 0 || ActualWidth < 40 || ActualHeight < 40) return;

        // حاشیه داخلی: بالا/چپ برای تنفس، پایین برای برچسب‌ها
        const double padL = 10, padR = 10, padT = 16, padB = 22;
        var w = ActualWidth - padL - padR;
        var h = ActualHeight - padT - padB;
        if (w <= 10 || h <= 10) return;

        var max = pts.Max(p => p.Value);
        var min = Math.Min(0, pts.Min(p => p.Value));
        var span = max - min;
        if (span <= 0) span = 1; // صفر مقیاس‌پذیر نیست — خط صاف

        double X(int i) => pts.Count == 1 ? w / 2 : padL + w * i / (pts.Count - 1);
        double Y(double v) => padT + h - (v - min) / span * h;

        // خط نقطه‌چین کف (صفر) برای خوانایی
        var baseline = new Line
        {
            X1 = padL, X2 = ActualWidth - padR, Y1 = Y(min), Y2 = Y(min),
            StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 },
            Stroke = new SolidColorBrush(Color.FromArgb(70, 128, 128, 128)),
        };
        PlotCanvas.Children.Add(baseline);

        var series = new PointCollection();
        for (var i = 0; i < pts.Count; i++)
            series.Add(new Point(X(i), Y(pts[i].Value)));

        // منحنی نرم با بزیه در نقاط میانی — ظاهر حرفه‌ای بدون کتابخانه بیرونی
        var figure = new PathFigure { StartPoint = series[0] };
        if (series.Count == 1)
        {
            figure.StartPoint = new Point(series[0].X - 4, series[0].Y);
            figure.Segments.Add(new LineSegment(new Point(series[0].X + 4, series[0].Y), true));
        }
        else
        {
            for (var i = 1; i < series.Count; i++)
            {
                var prev = series[i - 1];
                var cur = series[i];
                var mid = new Point((prev.X + cur.X) / 2, (prev.Y + cur.Y) / 2);
                figure.Segments.Add(new QuadraticBezierSegment(prev, mid, cur, true));
            }
        }

        // مسیر سطح (تا کف و بستن) — گرادیان ملایم
        var areaFigure = new PathFigure { StartPoint = new Point(series[0].X, Y(min)) };
        areaFigure.Segments.Add(new LineSegment(series[0], true));
        for (var i = 1; i < series.Count; i++)
        {
            var prev = series[i - 1];
            var cur = series[i];
            var mid = new Point((prev.X + cur.X) / 2, (prev.Y + cur.Y) / 2);
            areaFigure.Segments.Add(new QuadraticBezierSegment(prev, mid, cur, true));
        }
        areaFigure.Segments.Add(new LineSegment(new Point(series[^1].X, Y(min)), true));

        var grad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0), EndPoint = new Point(0, 1),
            GradientStops =
            {
                new GradientStop(Color.FromArgb(90, 21, 101, 192), 0),
                new GradientStop(Color.FromArgb(12, 21, 101, 192), 0.85),
            },
        };
        PlotCanvas.Children.Add(new Path
        {
            Data = new PathGeometry(new[] { areaFigure }),
            Fill = grad,
        });

        // خط اصلی سری
        PlotCanvas.Children.Add(new Path
        {
            Data = new PathGeometry(new[] { figure }),
            Stroke = ChartPalette.Primary,
            StrokeThickness = 2.2,
            StrokeLineJoin = PenLineJoin.Round,
        });

        // نقاط تعاملی + tooltip
        if (ShowDots)
            for (var i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var tip = p.Tooltip ?? $"{p.Label}: {p.Value.ToString(ValueFormat)}";
                var dot = new Ellipse
                {
                    Width = 8, Height = 8,
                    Fill = Brushes.White,
                    Stroke = ChartPalette.Primary,
                    StrokeThickness = 2,
                    Cursor = Cursors.Hand,
                    ToolTip = tip,
                };
                Canvas.SetLeft(dot, X(i) - 4);
                Canvas.SetTop(dot, Y(p.Value) - 4);
                Panel.SetZIndex(dot, 5);
                PlotCanvas.Children.Add(dot);
            }

        // برچسب‌های محور افقی — همه وقتی کم‌تر از ۹ نقطه؛ وگرنه هر n-اُم + آخری
        var step = Math.Max(1, (int)Math.Ceiling(pts.Count / 9.0));
        for (var i = 0; i < pts.Count; i += step)
        {
            var label = new TextBlock
            {
                Text = pts[i].Label,
                FontSize = 9,
                Opacity = 0.65,
                Foreground = Foreground,
            };
            label.Loaded += (_, _) =>
            {
                Canvas.SetLeft(label, Math.Clamp(X(i) - label.ActualWidth / 2, 2, Math.Max(2, ActualWidth - label.ActualWidth - 2)));
            };
            Canvas.SetTop(label, ActualHeight - padB + 5);
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, Math.Clamp(X(i) - label.DesiredSize.Width / 2, 2, Math.Max(2, ActualWidth - label.DesiredSize.Width - 2)));
            PlotCanvas.Children.Add(label);
        }
    }
}
