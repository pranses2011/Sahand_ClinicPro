using System.Globalization;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>مقدار آزمایش یک عدد بالینی است، نه مبلغ پولی.</summary>
public static class LabNumericResult
{
    /// <summary>پارس اعشار با ارقام فارسی، عربی یا لاتین؛ جداکنندهٔ هزارگان پذیرفته نمی‌شود.</summary>
    public static bool TryParse(string text, out decimal value)
    {
        var normalized = Money.PersianDigitsToLatin(text.Trim())
            .Replace('٫', '.').Replace('/', '.').Replace('−', '-');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    /// <summary>نمایش مقدار بدون گردکردن یا تبدیل واحد پول.</summary>
    public static string Format(decimal value) => Money.ToPersianDigits(value.ToString(CultureInfo.InvariantCulture));
}
