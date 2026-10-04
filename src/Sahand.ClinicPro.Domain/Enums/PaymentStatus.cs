namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت پرداخت
/// </summary>
public enum PaymentStatus : byte
{
    /// <summary>پرداخت‌نشده</summary>
    Unpaid = 1,
    /// <summary>پرداخت جزئی</summary>
    PartiallyPaid = 2,
    /// <summary>تسویه شده</summary>
    Paid = 3,
    /// <summary>پرداخت اضافه</summary>
    Overpaid = 4,
}
