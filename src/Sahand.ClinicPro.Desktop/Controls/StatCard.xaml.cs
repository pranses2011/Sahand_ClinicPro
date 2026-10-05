using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// کارت آماری داشبورد — عدد درشت + عنوان + مقایسه + آیکون با رنگ تأکیدی
/// </summary>
public partial class StatCard : UserControl
{
    /// <summary>سازنده</summary>
    public StatCard()
    {
        InitializeComponent();
    }

    /// <summary>مقدار اصلی کارت (مثل «۴۲»)</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string),
            typeof(StatCard), new PropertyMetadata("0"));

    /// <summary>عنوان زیر عدد (مثل «بیماران امروز»)</summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string),
            typeof(StatCard), new PropertyMetadata(string.Empty));

    /// <summary>متن مقایسه (مثل «↑ ۱۲٪ نسبت به دیروز»)</summary>
    public static readonly DependencyProperty CompareTextProperty =
        DependencyProperty.Register(nameof(CompareText), typeof(string),
            typeof(StatCard), new PropertyMetadata(string.Empty));

    /// <summary>آیکون کارت</summary>
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind),
            typeof(StatCard), new PropertyMetadata(PackIconKind.ChartLine));

    /// <summary>رنگ تأکیدی — نوار پایین و عدد و آیکون</summary>
    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(nameof(AccentBrush), typeof(Brush),
            typeof(StatCard),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1E, 0x88, 0xE5))));

    /// <summary>مقدار اصلی</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>عنوان</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>متن مقایسه</summary>
    public string CompareText
    {
        get => (string)GetValue(CompareTextProperty);
        set => SetValue(CompareTextProperty, value);
    }

    /// <summary>آیکون</summary>
    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    /// <summary>رنگ تأکیدی</summary>
    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }
}
