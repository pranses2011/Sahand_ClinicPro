using System.Globalization;
using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>خواندن اعداد بوم با ارقام فارسی، عربی یا لاتین بدون پذیرش گروه‌بندی مبهم.</summary>
public static class FreeTemplateInput
{
    private static string Normalize(string? text) => Money.PersianDigitsToLatin((text ?? "").Trim()).Replace('٫', '.');

    public static bool TryNumber(string? text, out double value)
    {
        if (double.TryParse(Normalize(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value) && double.IsFinite(value)) return true;
        value = 0;
        return false;
    }

    public static bool TryInteger(string? text, out int value)
        => int.TryParse(Normalize(text), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}
