using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// حالت خالی لیست‌ها — آیکون + پیام «موردی یافت نشد»
/// </summary>
public partial class EmptyState : UserControl
{
    /// <summary>سازنده</summary>
    public EmptyState()
    {
        InitializeComponent();
    }

    /// <summary>پیام نمایش</summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string),
            typeof(EmptyState), new PropertyMetadata("موردی یافت نشد"));

    /// <summary>آیکون</summary>
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind),
            typeof(EmptyState), new PropertyMetadata(PackIconKind.Magnify));

    /// <summary>پیام</summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>آیکون</summary>
    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }
}
