using System.Globalization;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>نمایش و دریافت عدد غیرپولی بدون تبدیل واحد یا گردکردن.</summary>
public static class PlainNumber
{
    public static bool TryParse(string text, out decimal value)
    {
        var normalized = Money.PersianDigitsToLatin(text.Trim())
            .Replace('٫', '.').Replace('/', '.').Replace('−', '-');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    public static string Format(decimal value) => Money.ToPersianDigits(value.ToString(CultureInfo.InvariantCulture));
}
