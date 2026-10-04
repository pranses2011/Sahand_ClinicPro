namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع دستگاه نشست کاربر
/// </summary>
public enum DeviceType : byte
{
    /// <summary>دسکتاپ</summary>
    Desktop = 1,
    /// <summary>لپ‌تاپ</summary>
    Laptop = 2,
    /// <summary>تبلت</summary>
    Tablet = 3,
    /// <summary>سایر</summary>
    Other = 4,
}
