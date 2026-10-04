namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت رکورد پرداخت
/// </summary>
public enum PaymentRecordStatus : byte
{
    /// <summary>در انتظار</summary>
    Pending = 1,
    /// <summary>تکمیل شده</summary>
    Completed = 2,
    /// <summary>ناموفق</summary>
    Failed = 3,
    /// <summary>برگشتی</summary>
    Refunded = 4,
}
