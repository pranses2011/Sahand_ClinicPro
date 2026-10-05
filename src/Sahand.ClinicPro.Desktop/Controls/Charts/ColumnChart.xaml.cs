using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Sahand.ClinicPro.Desktop.Controls.Charts;

/// <summary>
/// نمودار ستونی عمودی — انتخاب درست برای «مقایسه مقوله‌های مجزا»:
/// تعداد مراجعه به تفکیک ساعت، درآمد ماه‌به‌ماه، رده‌های سنی و ...
/// عرض هر ستون تطبیقی با فضا؛ برچسب مقدار بالای ستون و نام مقوله زیر آن.
/// </summary>
public partial class ColumnChart : UserControl
{
    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IReadOnlyList<ChartPointData>), typeof(ColumnChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    /// <summary>نقاط — هر نقطه یک مقوله</summary>
    public IReadOnlyList<ChartPointData>? Points
    {
        get => (IReadOnlyList<ChartPointData>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(ColumnChart), new PropertyMetadata("N0"));

    /// <summary>قالب عدد برچسب‌ها</summary>
    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    /// <summary>نمایش عدد بالای ستون‌ها (پیش‌فرض روشن)</summary>
    public static readonly DependencyProperty ShowValuesProperty = DependencyProperty.Register(
        nameof(ShowValues), typeof(bool), typeof(ColumnChart), new PropertyMetadata(true));

    public bool ShowValues
    {
        get => (bool)GetValue(ShowValuesProperty);
        set => SetValue(ShowValuesProperty, value);
    }

    public ColumnChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Render();
        Loaded += (_, _) => Render();
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ColumnChart)d).Render();

    private void Render()
    {
        if (PlotCanvas is null) return;
        PlotCanvas.Children.Clear();

        var pts = Points;
        EmptyHint.Visibility = pts is not { Count: > 0 } ? Visibility.Visible : Visibility.Collapsed;
        if (pts is null || pts.Count == 0 || ActualHeight < 60 || ActualWidth < 40) return;

        const double padB = 24, padT = 18, gap = 6, sidePad = 6;
        var height = ActualHeight - padB - padT;
        if (height <= 10) return;

        // عرض کل لازم — حداقل ۳۴px برای هر ستون تا برچسب‌ها جا شوند
        var slot = Math.Max(34, (ActualWidth - sidePad * 2 - gap * (pts.Count - 1)) / pts.Count);
        var totalWidth = Math.Max(ActualWidth, sidePad * 2 + slot * pts.Count + gap * (pts.Count - 1));
        PlotCanvas.Width = totalWidth;

        var max = pts.Max(p => p.Value);
        if (max <= 0) max = 1;

        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            var colH = Math.Max(p.Value > 0 ? 3 : 1, p.Value / max * height);
            var x = sidePad + i * (slot + gap);
            var y = padT + height - colH;

            // ستون با گوشه گرد بالا — رنگ چرخی پالت
            var rect = new Border
            {
                Width = slot, Height = colH,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Background = ChartPalette.Brush(i),
                ToolTip = p.Tooltip ?? $"{p.Label}: {p.Value.ToString(ValueFormat)}",
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            PlotCanvas.Children.Add(rect);

            // مقدار بالای ستون
            if (ShowValues)
            {
                var val = new TextBlock
                {
                    Text = p.Value.ToString(ValueFormat),
                    FontSize = 8.5, Opacity = 0.75, Foreground = Foreground,
                };
                val.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(val, x + slot / 2 - val.DesiredSize.Width / 2);
                Canvas.SetTop(val, y - val.DesiredSize.Height - 2);
                PlotCanvas.Children.Add(val);
            }

            // برچسب مقوله زیر ستون
            var lbl = new TextBlock
            {
                Text = p.Label, FontSize = 9, Opacity = 0.65, Foreground = Foreground,
                TextTrimming = TextTrimming.CharacterEllipsis, Width = slot + 4, TextAlignment = TextAlignment.Center,
            };
            Canvas.SetLeft(lbl, x + slot / 2 - (slot + 4) / 2);
            Canvas.SetTop(lbl, padT + height + 4);
            PlotCanvas.Children.Add(lbl);
        }
    }
}
