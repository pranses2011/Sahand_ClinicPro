using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Controls.Charts;

/// <summary>
/// نمودار میله‌ای افقی — انتخاب درست برای «رتبه‌بندی/برترین‌ها»:
/// پرمراجعه‌ترین بیماران، پرمصرف‌ترین داروها، بزرگ‌ترین بدهکاران.
/// برچسب‌ها راست‌چین فارسی، میله‌ها رو به چپ رشد می‌کنند (قاعده چیدمان RTL).
/// </summary>
public partial class HorizontalBarChart : UserControl
{
    /// <summary>ردیف داخلی نمایش — برچسب + عرض نسبی + مقدار قالب‌بندی‌شده</summary>
    public sealed class BarRow : FrameworkElement
    {
        public string Label { get; init; } = "";
        public string ValueText { get; init; } = "";
        public double BarWidth { get; init; }
        public Brush Brush { get; init; } = Brushes.Gray;
    }

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IReadOnlyList<ChartPointData>), typeof(HorizontalBarChart),
        new PropertyMetadata(null, OnPointsChanged));

    /// <summary>نقاط — به ترتیب نزولی ارزش</summary>
    public IReadOnlyList<ChartPointData>? Points
    {
        get => (IReadOnlyList<ChartPointData>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty BarMaxWidthProperty = DependencyProperty.Register(
        nameof(BarMaxWidth), typeof(double), typeof(HorizontalBarChart), new PropertyMetadata(400.0));

    /// <summary>عرض پیکسلی بلندترین میله (بقیه نسبتی)</summary>
    public double BarMaxWidth
    {
        get => (double)GetValue(BarMaxWidthProperty);
        set => SetValue(BarMaxWidthProperty, value);
    }

    public ObservableCollection<BarRow> Rows { get; } = new();

    public HorizontalBarChart()
    {
        InitializeComponent();
        RowsHost.ItemsSource = Rows;
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HorizontalBarChart)d).Render();

    private void Render()
    {
        if (RowsHost is null) return;
        Rows.Clear();

        var pts = Points;
        EmptyHint.Visibility = pts is not { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        if (pts is null || pts.Count == 0) return;

        var max = pts.Max(p => p.Value);
        if (max <= 0) max = 1;

        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            var width = Math.Max(p.Value > 0 ? 4 : 0, p.Value / max * BarMaxWidth);
            Rows.Add(new BarRow
            {
                Label = p.Label,
                ValueText = p.Value.ToString("N0"),
                BarWidth = width,
                Brush = ChartPalette.Brush(i),
            });
        }
    }
}
