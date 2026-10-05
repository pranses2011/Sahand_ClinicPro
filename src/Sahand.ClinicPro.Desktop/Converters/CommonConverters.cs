using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل تاریخ میلادی به شمسی برای نمایش در جدول‌ها — «۱۴۰۴/۰۴/۰۱»
/// ورودی پذیرفته‌شده: DateTime یا DateTime? یا رشته تاریخ
/// </summary>
public sealed class ShamsiDateConverter : IValueConverter
{
    /// <summary>تبدیل به شمسی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateTime dt => PersianDateHelper.ToShamsi(dt),
            DateTimeOffset dto => PersianDateHelper.ToShamsi(dto.DateTime),
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var parsed)
                => PersianDateHelper.ToShamsi(parsed),
            _ => "—",
        };
    }

    /// <summary>تبدیل معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل تاریخ + ساعت شمسی — «۱۴۰۴/۰۴/۰۱ ۱۰:۳۰»
/// </summary>
public sealed class ShamsiDateTimeConverter : IValueConverter
{
    /// <summary>تبدیل به شمسی با ساعت</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dt ? PersianDateHelper.ToShamsiWithTime(dt) : "—";

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل تهی بودن به دیدپذیری — برای EmptyState و عناصر اختیاری
/// پارامتر «Inverse» حالت را وارونه می‌کند
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    /// <summary>تهی یا صفر یا خالی → مخفی؛ مقدار → نمایان (یا وارونه با پارامتر)</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null
                     || (value is string s && string.IsNullOrWhiteSpace(s))
                     || (value is System.Collections.ICollection c && c.Count == 0)
                     || (value is int i && i == 0);
        var inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);
        var visible = inverse ? isNull : !isNull;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل بولی به دیدپذیری — False → Collapsed
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>True → نمایان؛ False → مخفی</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// مبدل مبلغ ریالی با جداکننده هزارگان — «۳٬۰۰۰٬۰۰۰»
/// </summary>
public sealed class CurrencyRialConverter : IValueConverter
{
    /// <summary>قالب‌بندی مبلغ</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal m => m.ToString("N0"),
            long l => l.ToString("N0"),
            int i => i.ToString("N0"),
            double d => d.ToString("N0"),
            _ => "0",
        };

    /// <summary>معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
