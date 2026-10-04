using System.Globalization;
using System.Windows.Data;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل شیء غیرتهی به فعال بودن — برای دکمه‌های وابسته به انتخاب
/// </summary>
public class NotNullToBoolConverter : IValueConverter
{
    /// <summary>تبدیل شیء به بولین — تهی یعنی غیرفعال</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    /// <summary>تبدیل معکوس پشتیبانی نمی‌شود</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}