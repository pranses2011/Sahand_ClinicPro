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
/// ═══ چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
/// «ویرایش حقوقی پرسنل فقط درصد را می‌آورد و حقوق ماهیانه را خالی نشان می‌دهد» —
/// ریشه: پراپرتی‌های مبلغی برخی ViewModel‌ها (مانند BaseSalary حقوق پرسنل) از نوع
/// رشته‌اند و این مبدل فقط نوع‌های عددی را می‌شناخت؛ رشته به شاخهٔ پیش‌فرض (۰)
/// می‌افتاد و کادر همیشه خالی باز می‌شد. اکنون رشتهٔ عددی (با ارقام فارسی و
/// جداکننده) هم در هر دو جهت پارس می‌شود.
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
            // چرخه بیست‌ویکم: پراپرتی رشته‌ای — «12,000,000» یا «۱۲۰۰۰۰۰۰» یا «12000000»
            string s => Money.ToDisplay(ParseSafe(s)),
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
            // چرخه بیست‌ویکم: مقدار عددی رفتار MoneyInput گاهی رشته‌وار می‌رسد
            string s => Money.FromDisplayInput(ParseSafe(s)),
            _ => 0m,
        };

    /// <summary>
    /// پارس امن رشتهٔ مبلغ — ارقام فارسی/عربی نرمال، جداکننده‌ها حذف؛
    /// نتیجهٔ نامعتبر صفر است (کادر خالی هم ۰ حساب می‌شود).
    /// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ رشتهٔ بلندتر از ۲۸ رقم،
    /// خودِ decimal.Parse را با OverflowException می‌شکست و برنامه را
    /// می‌بست؛ اکنون TryParse امن است و مقدار به سقف ستون‌های مبلغ
    /// دیتابیس (decimal(18,0)) محبوس می‌شود.
    /// </summary>
    private static decimal ParseSafe(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0m;
        var latin = Money.PersianDigitsToLatin(text);
        var digits = new string(latin.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return 0m;

        // برش تا سقف ۱۸ رقم + پارس امن — نه استثنا، نه مقدار خارج از بازه
        if (digits.Length > 18)
            digits = digits[..18];
        return decimal.TryParse(digits, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }
}
