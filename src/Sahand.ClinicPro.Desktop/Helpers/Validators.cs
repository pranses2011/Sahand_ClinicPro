namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// اعتبارسنجی‌های مشترک فرم‌ها — چرخه نوزدهم (گزارش سیزدهم مالک):
/// اعتبار کد ملی فقط با تعداد رقم سنجیده نمی‌شود؛ رقم کنترلی الگوریتم
/// استاندارد سازمان ثبت احوال هم بررسی می‌شود. برای یکدستی، هر فرمی که
/// کد ملی یا موبایل وارد می‌کند از همین‌جا استفاده می‌کند.
/// </summary>
public static class Validators
{
    /// <summary>
    /// اعتبارسنجی کامل کد ملی ایرانی — ۱۰ رقم، غیرتکراری، رقم کنترلی درست
    /// </summary>
    public static bool IsValidNationalCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;
        if (code.Length != 10 || !code.All(char.IsDigit) || code.Distinct().Count() == 1)
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);
        var remainder = sum % 11;
        var check = code[9] - '0';
        return (remainder < 2 && check == remainder) || (remainder >= 2 && check == 11 - remainder);
    }

    /// <summary>
    /// اعتبارسنجی موبایل ایرانی — ۰۹ + ۹ رقم
    /// </summary>
    public static bool IsValidMobile(string? mobile)
        => !string.IsNullOrWhiteSpace(mobile)
           && System.Text.RegularExpressions.Regex.IsMatch(mobile.Trim(), @"^09\d{9}$");
}
