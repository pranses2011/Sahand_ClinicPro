using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Shared.Growth;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پرونده بالینی پیشرفته — گام R2
/// شامل رسم نمودار رشد روی Canvas با منحنی‌های مرجع WHO (±۱/۲/۳ SD)
/// و نقاط اندازه‌گیری بیمار با رنگ‌بندی وضعیت
/// </summary>
public partial class AdvancedEmrView : UserControl
{
    /// <summary>حاشیه داخلی نمودار</summary>
    private const double ChartPadding = 42;

    /// <summary>رنگ منحنی‌های مرجع — از مرکز به حاشیه</summary>
    private static readonly SolidColorBrush[] BandBrushes =
    {
        new(Color.FromRgb(120, 144, 156)), // +3/-3
        new(Color.FromRgb(129, 199, 132)), // +2/-2
        new(Color.FromRgb(79, 195, 247))   // +1/-1
    };

    /// <summary>سازنده — اتصال به تغییرات ViewModel برای رسم مجدد</summary>
    public AdvancedEmrView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => RedrawChart();
        Unloaded += (_, _) =>
        {
            if (DataContext is INotifyPropertyChanged npc)
                npc.PropertyChanged -= OnViewModelPropertyChanged;
        };
    }

    /// <summary>بازخورد تغییر DataContext — اشتراک در تغییرات</summary>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldNpc)
            oldNpc.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is INotifyPropertyChanged newNpc)
            newNpc.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>رسم مجدد هنگام تغییر داده یا شاخص یا جنسیت</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AdvancedEmrViewModel.ChartVersion)
            or nameof(AdvancedEmrViewModel.IsBusy))
            RedrawChart();
    }

    /// <summary>رسم مجدد هنگام تغییر اندازه بوم</summary>
    private void OnChartSizeChanged(object sender, SizeChangedEventArgs e) => RedrawChart();

    /// <summary>رسم کامل نمودار رشد</summary>
    private void RedrawChart()
    {
        if (GrowthChart is null || DataContext is not AdvancedEmrViewModel vm) return;

        var canvas = GrowthChart;
        canvas.Children.Clear();
        var width = canvas.ActualWidth;
        var height = canvas.ActualHeight;
        if (width < 80 || height < 80) return;

        var indicator = IndexToIndicator(vm.IndicatorIndex);
        var (fromMonth, toMonth) = WhoGrowthReference.AgeRange(indicator);

        // محدوده مقیاس عمودی از داده مرجع + داده بیمار (کمی حاشیه)
        double min = double.MaxValue, max = double.MinValue;
        for (var m = fromMonth; m <= toMonth; m += Math.Max(1, (toMonth - fromMonth) / 24))
        {
            if (WhoGrowthReference.Reference(indicator, vm.IsMale, m) is not { } r) continue;
            min = Math.Min(min, r.Median - 3.2 * r.Sd);
            max = Math.Max(max, r.Median + 3.2 * r.Sd);
        }

        foreach (var row in vm.GrowthRows)
        {
            var value = ValueFor(row, vm.IndicatorIndex);
            if (value is null) continue;
            min = Math.Min(min, value.Value - 1);
            max = Math.Max(max, value.Value + 1);
        }
        if (min >= max) return;

        var plotW = width - 2 * ChartPadding;
        var plotH = height - 2 * ChartPadding;
        double X(double month) => ChartPadding + (month - fromMonth) * plotW / (toMonth - fromMonth);
        double Y(double value) => height - ChartPadding - (value - min) * plotH / (max - min);

        // ── شبکه و محورها ──
        DrawAxes(canvas, width, height, min, max, fromMonth, toMonth, X, Y);

        // ── منحنی‌های مرجع: میانه + ±1/2/3 ──
        for (var level = 3; level >= 1; level--)
            DrawReferenceCurve(canvas, indicator, vm.IsMale, fromMonth, toMonth,
                level, min, max, X, Y, level == 1);
        DrawReferenceCurve(canvas, indicator, vm.IsMale, fromMonth, toMonth,
            0, min, max, X, Y, true, median: true);

        // ── خط اتصال نقاط بیمار ──
        var patientPoints = vm.GrowthRows
            .Select(r => (Row: r, Value: ValueFor(r, vm.IndicatorIndex)))
            .Where(p => p.Value is not null && p.Row.AgeMonths >= fromMonth && p.Row.AgeMonths <= toMonth)
            .OrderBy(p => p.Row.AgeMonths)
            .ToList();

        if (patientPoints.Count > 1)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(67, 160, 71)),
                StrokeThickness = 2.2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            foreach (var (row, value) in patientPoints)
                polyline.Points.Add(new Point(X(row.AgeMonths), Y(value!.Value)));
            canvas.Children.Add(polyline);
        }

        // ── نقاط بیمار با رنگ وضعیت ──
        foreach (var (row, value) in patientPoints)
        {
            var z = WhoGrowthReference.ZScore(indicator, vm.IsMale, row.AgeMonths, value!.Value);
            var color = z is null
                ? Color.FromRgb(120, 120, 120)
                : (Color?)ColorFromHex(WhoGrowthReference.Classify(z.Value, indicator).HexColor)
                  ?? Color.FromRgb(120, 120, 120);

            var dot = new Ellipse
            {
                Width = 11, Height = 11,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White, StrokeThickness = 1.6,
                ToolTip = $"{row.DateShamsi}\nسن: {row.AgeMonths} ماه\nمقدار: {value:0.#}"
            };
            Canvas.SetLeft(dot, X(row.AgeMonths) - 5.5);
            Canvas.SetTop(dot, Y(value.Value) - 5.5);
            canvas.Children.Add(dot);
        }

        // راهنما — پایین نمودار
        AddLegend(canvas, width, height, indicator);
    }

    /// <summary>محورهای افقی/عمودی با برچسب‌های عددی</summary>
    private void DrawAxes(Canvas canvas, double width, double height,
        double min, double max, int fromMonth, int toMonth,
        Func<double, double> x, Func<double, double> y)
    {
        var axisBrush = new SolidColorBrush(Color.FromRgb(176, 190, 197));
        var textBrush = new SolidColorBrush(Color.FromRgb(96, 108, 118));

        // محور عمودی — ۶分割
        for (var i = 0; i <= 6; i++)
        {
            var value = min + (max - min) * i / 6;
            var yy = y(value);
            var line = new Line
            {
                X1 = ChartPadding, X2 = width - ChartPadding,
                Y1 = yy, Y2 = yy,
                Stroke = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
                StrokeThickness = 1
            };
            canvas.Children.Add(line);

            var label = new TextBlock
            {
                Text = value.ToString("0.#"),
                FontSize = 10, Foreground = textBrush
            };
            Canvas.SetLeft(label, width - ChartPadding + 4);
            Canvas.SetTop(label, yy - 7);
            canvas.Children.Add(label);
        }

        // محور افقی — هر ۶ ماه
        var step = toMonth > 40 ? 6 : 3;
        for (var m = fromMonth; m <= toMonth; m += step)
        {
            var xx = x(m);
            var line = new Line
            {
                X1 = xx, X2 = xx,
                Y1 = ChartPadding, Y2 = height - ChartPadding,
                Stroke = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
                StrokeThickness = 1
            };
            canvas.Children.Add(line);

            var label = new TextBlock
            {
                Text = m.ToString(),
                FontSize = 10, Foreground = textBrush
            };
            Canvas.SetLeft(label, xx - 8);
            Canvas.SetTop(label, height - ChartPadding + 4);
            canvas.Children.Add(label);
        }

        // قاب
        var frame = new Rectangle
        {
            Width = width - 2 * ChartPadding,
            Height = height - 2 * ChartPadding,
            Stroke = axisBrush, StrokeThickness = 1
        };
        Canvas.SetLeft(frame, ChartPadding);
        Canvas.SetTop(frame, ChartPadding);
        canvas.Children.Add(frame);
    }

    /// <summary>رسم یک منحنی مرجع — سطح ±z یا میانه</summary>
    private void DrawReferenceCurve(Canvas canvas, GrowthIndicator indicator, bool isMale,
        int fromMonth, int toMonth, int zLevel, double min, double max,
        Func<double, double> x, Func<double, double> y, bool prominent = false, bool median = false)
    {
        var polyline = new Polyline
        {
            StrokeThickness = median ? 2 : prominent ? 1.4 : 1,
            Stroke = median
                ? new SolidColorBrush(Color.FromRgb(69, 90, 100))
                : BandBrushes[Math.Clamp(zLevel - 1, 0, 2)],
            StrokeDashArray = median ? null : new DoubleCollection { 5, 3 }
        };
        if (median)
            polyline.StrokeDashArray = null;

        var step = Math.Max(1, (toMonth - fromMonth) / 60);
        for (var m = fromMonth; m <= toMonth; m += step)
        {
            if (WhoGrowthReference.Reference(indicator, isMale, m) is not { } r) continue;
            var value = zLevel == 0 ? r.Median : r.Median + zLevel * r.Sd;
            polyline.Points.Add(new Point(x(m), y(value)));
        }

        canvas.Children.Add(polyline);
    }

    /// <summary>راهنمای پایین نمودار — رنگ نقاط بیمار</summary>
    private void AddLegend(Canvas canvas, double width, double height, GrowthIndicator indicator)
    {
        var hint = new TextBlock
        {
            Text = $"مرجع WHO ۲۰۰۶ — خط آبی مرکزی میانه، خط‌چین‌ها ±۱/۲/۳ انحراف معیار ({IndicatorName(indicator)}) — " +
                   "نقاط بیمار: سبز نرمال، زرد مرز هشدار، نارنجی هشدار، قرمز بحرانی",
            FontSize = 10.5,
            Opacity = 0.8,
            Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122))
        };
        Canvas.SetLeft(hint, ChartPadding);
        Canvas.SetTop(hint, 6);
        canvas.Children.Add(hint);
    }

    /// <summary>نام فارسی شاخص</summary>
    private static string IndicatorName(GrowthIndicator indicator) => indicator switch
    {
        GrowthIndicator.WeightForAge => "وزن برای سن",
        GrowthIndicator.HeightForAge => "قد برای سن",
        GrowthIndicator.HeadCircumferenceForAge => "محیط سر برای سن",
        _ => "BMI برای سن"
    };

    /// <summary>تبدیل اندیس انتخابی به شاخص</summary>
    private static GrowthIndicator IndexToIndicator(int index) => index switch
    {
        1 => GrowthIndicator.HeightForAge,
        2 => GrowthIndicator.HeadCircumferenceForAge,
        3 => GrowthIndicator.BmiForAge,
        _ => GrowthIndicator.WeightForAge
    };

    /// <summary>مقدار ردیف متناسب با شاخص انتخابی</summary>
    private static double? ValueFor(AdvancedEmrViewModel.GrowthRow row, int indicatorIndex)
        => indicatorIndex switch
        {
            0 => row.WeightKg is null ? null : (double)row.WeightKg,
            1 => row.HeightCm is null ? null : (double)row.HeightCm,
            2 => row.HeadCirc is null ? null : (double)row.HeadCirc,
            // BMI = وزن بر مجذور قد متر
            _ => row.WeightKg is null || row.HeightCm is null || row.HeightCm == 0
                ? null
                : Math.Round((double)row.WeightKg / Math.Pow((double)row.HeightCm / 100.0, 2), 2)
        };

    /// <summary>رنگ از کد hex</summary>
    private static Color? ColorFromHex(string hex)
    {
        try
        {
            var offset = hex.StartsWith("#") ? 1 : 0;
            return Color.FromRgb(
                Convert.ToByte(hex.Substring(offset, 2), 16),
                Convert.ToByte(hex.Substring(offset + 2, 2), 16),
                Convert.ToByte(hex.Substring(offset + 4, 2), 16));
        }
        catch
        {
            return null;
        }
    }
}
