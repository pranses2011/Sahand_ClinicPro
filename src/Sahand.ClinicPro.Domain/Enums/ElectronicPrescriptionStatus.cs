namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت نسخه الکترونیک
/// </summary>
public enum ElectronicPrescriptionStatus : byte
{
    /// <summary>در انتظار ارسال</summary>
    Pending = 1,
    /// <summary>ارسال شده</summary>
    Sent = 2,
    /// <summary>ثبت‌شده در سامانه</summary>
    Registered = 3,
    /// <summary>رد شده</summary>
    Rejected = 4,
    /// <summary>لغو</summary>
    Cancelled = 5,
}
