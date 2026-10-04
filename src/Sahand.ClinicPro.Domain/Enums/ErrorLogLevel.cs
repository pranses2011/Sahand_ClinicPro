namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// سطح لاگ خطا
/// </summary>
public enum ErrorLogLevel : byte
{
    /// <summary>عیب‌یابی</summary>
    Debug = 1,
    /// <summary>اطلاع</summary>
    Info = 2,
    /// <summary>هشدار</summary>
    Warning = 3,
    /// <summary>خطا</summary>
    Error = 4,
    /// <summary>مرگبار</summary>
    Fatal = 5,
}
