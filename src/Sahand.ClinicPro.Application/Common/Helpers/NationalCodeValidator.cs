using Sahand.ClinicPro.Domain.ValueObjects;

namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// اعتبارسنج کد ملی ۱۰ رقمی ایرانی — الگوریتم رقم کنترلی
/// </summary>
public static class NationalCodeValidator
{
    /// <summary>
    /// بررسی صحت کد ملی با الگوریتم رقم کنترلی
    /// </summary>
    public static bool IsValid(string? code) => NationalCode.IsValid(code);

    /// <summary>
    /// پیام خطای فارسی برای کد ملی نامعتبر
    /// </summary>
    public const string InvalidMessage = "کد ملی نامعتبر است";
}
