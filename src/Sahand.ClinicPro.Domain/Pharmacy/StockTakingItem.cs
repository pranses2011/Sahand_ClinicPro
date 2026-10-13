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

    /// <summary>مغایرت شمارش با سیستم (CountedQuantity - SystemQuantity) — مثبت = شمارش بیشتر از سیستم (زیادگی/اضافه انبار)، منفی = کمتر (کسری).
    /// ⚠️ چرخهٔ ۶۸: ویژگی محاسباتی است و در دیتابیس ذخیره/نگاشت نمی‌شود؛ بنابراین استفاده از آن در کوئری‌های سمت-سرور
    /// (پروجکشن/Where روی IQueryable) با خطای «Translation failed» شکست می‌خورد — عبارت معادل روی ستون‌ها بنویسید.</summary>
    public decimal Difference => CountedQuantity - SystemQuantity;

    /// <summary>جلسه والد</summary>
    public StockTaking? StockTaking { get; set; }

    /// <summary>بچ موجودی مرتبط</summary>
    public PharmacyInventory? Inventory { get; set; }
}
