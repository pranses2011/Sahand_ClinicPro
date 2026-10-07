using System.Globalization;
using System.Windows.Data;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Converters;

/// <summary>
/// مبدل مقدار ریالی VM ↔ مقدار نمایشی کادر مبلغ — چرخه بیستم (گزارش چهاردهم مالک).
/// برای اتصال دوطرفهٔ کادرهای ورود مبلغ (تخفیف/دریافتی/حقوق/پرداخت) که با رفتار
/// MoneyInput قالب‌بندی زندهٔ هزارگان دارند:
///   • Convert (ریال ← نمایش): در حالت تومان مقدار بر ۱۰ تقسیم می‌شود
///   • ConvertBack (نمایش ← ریال): در حالت تومان × ۱۰ — محاسبات ریالی می‌مانند
/// در حالت ریال، مبدل همانی است و فقط قالب‌بندی جداکننده اثر دارد.
/// </summary>
public sealed class MoneyRialConverter : IValueConverter
{
    /// <summary>ریال ذخیره‌شده → مقدار نمایشی برای MoneyInput.NumericValue</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal m => Money.ToDisplay(m),
            long l => Money.ToDisplay(l),
            int i => Money.ToDisplay(i),
            double d => Money.ToDisplay(System.Convert.ToDecimal(d)),
            _ => 0m,
        };

    /// <summary>مقدار نمایشی ورودی کاربر → ریال برای ذخیره در VM</summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            decimal m => Money.FromDisplayInput(m),
            long l => Money.FromDisplayInput(l),
            int i => Money.FromDisplayInput(i),
            double d => Money.FromDisplayInput(System.Convert.ToDecimal(d)),
            _ => 0m,
        };
}
