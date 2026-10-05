using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// قلم انبارگردانی - رکورد شمارش یک بچ موجودی در جلسه انبارگردانی — گام P2 (چرخه ششم)
/// موجودیت سیستمی هنگام شروع ثبت می‌شود (Snapshot) و شمارش واقعی به‌تدریج وارد می‌شود
/// </summary>
public class StockTakingItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه جلسه انبارگردانی والد</summary>
    public long StockTakingId { get; set; }

    /// <summary>شناسه رکورد موجودی (بچ مشخص دارو)</summary>
    public long InventoryId { get; set; }

    /// <summary>موجودی سیستم هنگام شمارش — عکس فوری از Quantity</summary>
    public decimal SystemQuantity { get; set; } = 0m;

    /// <summary>مقدار شمارش‌شده واقعی</summary>
    public decimal CountedQuantity { get; set; } = 0m;

    /// <summary>آیا این قلم شمارش شده است؟ (اقلام شمارش‌نشده مغایرت اعمال نمی‌کنند)</summary>
    public bool IsCounted { get; set; } = false;

    /// <summary>یادداشت شمارنده (مثلاً «بچ شکسته» یا «قلم ناقص»)</summary>
    public string? Notes { get; set; }

    /// <summary>مغایرت مثبت = کسری انبار، منفی = زیادگی — ویژگی محاسباتی و در دیتابیس ذخیره نمی‌شود</summary>
    public decimal Difference => CountedQuantity - SystemQuantity;

    /// <summary>جلسه والد</summary>
    public StockTaking? StockTaking { get; set; }

    /// <summary>بچ موجودی مرتبط</summary>
    public PharmacyInventory? Inventory { get; set; }
}
