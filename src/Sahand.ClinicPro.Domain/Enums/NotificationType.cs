namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع اعلان
/// </summary>
public enum NotificationType : byte
{
    /// <summary>اطلاع</summary>
    Info = 1,
    /// <summary>هشدار</summary>
    Warning = 2,
    /// <summary>خطا</summary>
    Error = 3,
    /// <summary>موفقیت</summary>
    Success = 4,
    /// <summary>لایسنس</summary>
    License = 5,
    /// <summary>موجودی</summary>
    Stock = 6,
}
