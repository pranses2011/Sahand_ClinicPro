namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منشأ تراکنش داروخانه
/// </summary>
public enum PharmacyReferenceType : byte
{
    /// <summary>نسخه</summary>
    Prescription = 1,
    /// <summary>سفارش خرید</summary>
    PurchaseOrder = 2,
    /// <summary>انبارگردانی</summary>
    StockTaking = 3,
    /// <summary>دستی</summary>
    Manual = 4,
}
