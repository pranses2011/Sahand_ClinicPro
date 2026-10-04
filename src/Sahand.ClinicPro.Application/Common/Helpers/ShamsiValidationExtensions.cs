using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// اعتبارسنجی تاریخ شمسی برای FluentValidation
/// </summary>
public static class ShamsiValidationExtensions
{
    /// <summary>آیا رشته تاریخ شمسی معتبر است — تهی مجاز است</summary>
    public static bool IsValidShamsi(string? shamsi)
        => string.IsNullOrEmpty(shamsi) || PersianDateHelper.ToGregorian(shamsi) is not null;
}
