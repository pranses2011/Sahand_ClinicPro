using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل کد رنگ HEX به قلم‌وی SolidColorBrush — چرخهٔ بیست‌ونهم (گزارش
/// هجدهم مالک): «توسعهٔ تنظیمات تم تک‌عنصری حرفه‌ای‌تر». نمونهٔ رنگ
/// (Swatch) کنار کشوهای «رنگ منوی کناری» و «رنگ اصلی» در تنظیمات تم،
/// رنگ مؤثر فعلی را نشان می‌دهد — مقدار تهی/خالی قلم‌وی شفاف برمی‌گرداند
/// (مخفی از طریق پارامتر Inverse در XAML مدیریت نمی‌شود چون همیشه یک
/// خانهٔ رنگی رزرو شده است).
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    /// <summary>کد HEX → قلم‌وی؛ تهی/نامعتبر → شفاف</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value?.ToString();
        if (string.IsNullOrWhiteSpace(hex))
            return Brushes.Transparent;
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
        catch (FormatException)
        {
            return Brushes.Transparent;
        }
    }

    /// <summary>بدون پشتیبانی — فقط نمایشی است</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
