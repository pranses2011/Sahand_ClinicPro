using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// تراکنش داروخانه - ورود و خروج و تحویل دارو
/// </summary>
public class PharmacyTransaction : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره تراکنش - یکتا</summary>
    public string? TransactionNumber { get; set; }

    /// <summary>شناسه رکورد موجودی</summary>
    public long InventoryId { get; set; }

    /// <summary>نوع تراکنش</summary>
    public PharmacyTransactionType TransactionType { get; set; }

    /// <summary>تعداد</summary>
    public decimal Quantity { get; set; } = 0m;

    /// <summary>قیمت واحد ثبت‌شده برای تراکنش (قیمت فروش/ورود)</summary>
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>عکس‌فوری بهای خرید واحد برای محاسبهٔ سود مصرفی‌ها (ریال)</summary>
    public decimal? PurchaseUnitCost { get; set; }

    /// <summary>قیمت کل</summary>
    public decimal TotalPrice { get; set; } = 0m;

    /// <summary>تاریخ تراکنش</summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>منشأ تراکنش</summary>
    public PharmacyReferenceType ReferenceType { get; set; }

    /// <summary>شناسه رکورد منشأ</summary>
    public long? ReferenceId { get; set; }

    /// <summary>شناسه نسخه در تحویل نسخه</summary>
    public long? PrescriptionId { get; set; }

    /// <summary>شناسه کاربر انجام‌دهنده</summary>
    public long? UserId { get; set; }

    /// <summary>توضیحات</summary>
    public string? Notes { get; set; }

    /// <summary>موجودی مرتبط</summary>
    public PharmacyInventory? Inventory { get; set; }
}
