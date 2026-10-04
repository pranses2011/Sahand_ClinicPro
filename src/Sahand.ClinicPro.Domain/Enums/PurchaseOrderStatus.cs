namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت سفارش خرید
/// </summary>
public enum PurchaseOrderStatus : byte
{
    /// <summary>پیش‌نویس</summary>
    Draft = 1,
    /// <summary>ارسال شده</summary>
    Submitted = 2,
    /// <summary>دریافت جزئی</summary>
    PartiallyReceived = 3,
    /// <summary>دریافت کامل</summary>
    Received = 4,
    /// <summary>لغو</summary>
    Cancelled = 5,
}
