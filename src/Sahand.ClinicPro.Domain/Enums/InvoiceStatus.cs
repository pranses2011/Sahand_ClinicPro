namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت فاکتور
/// </summary>
public enum InvoiceStatus : byte
{
    /// <summary>پیش‌نویس</summary>
    Draft = 1,
    /// <summary>صادر شده</summary>
    Issued = 2,
    /// <summary>پرداخت جزئی</summary>
    PartiallyPaid = 3,
    /// <summary>پرداخت شده</summary>
    Paid = 4,
    /// <summary>ابطال</summary>
    Cancelled = 5,
    /// <summary>برگشت هزینه</summary>
    Refunded = 6,
}
