using System.Globalization;
using System.Windows.Data;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل «پهنای ستونی» کارت‌های داشبورد — ورودی‌ها: (Span, پهنای کل نوار)
/// و پارامتر مبدل = تعداد ستون‌های نوار (کارت‌های KPI: ۵، بخش‌ها: ۲).
/// خروجی: پهنای آیتم طوری که Span ستون از نوار را بگیرد؛ حاشیهٔ بین
/// کارت‌ها از پهنا کم می‌شود تا ردیف سرریز نکند.
/// </summary>
public sealed class SpanWidthConverter : IMultiValueConverter
{
    /// <summary>محاسبهٔ پهنا از روی Span و پهنای کل</summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var span = values.Length > 0 && values[0] is int s ? Math.Clamp(s, 1, 5) : 1;
        var total = values.Length > 1 && values[1] is double w && w > 0 ? w : 1000;
        var columns = parameter is string ps && int.TryParse(ps, out var c) && c > 0 ? c : 5;
        var width = total / columns * span - 6;
        return Math.Max(110, width);
    }

    /// <summary>برگشت پشتیبانی نمی‌شود</summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
