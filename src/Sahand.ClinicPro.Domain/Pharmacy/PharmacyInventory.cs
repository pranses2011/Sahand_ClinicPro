namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// موجودی داروخانه - به تفکیک بچ و تاریخ انقضا
/// </summary>
public class PharmacyInventory : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه دارو</summary>
    public long MedicationId { get; set; }

    /// <summary>شماره بچ</summary>
    public string? BatchNumber { get; set; }

    /// <summary>موجودی فعلی</summary>
    public decimal Quantity { get; set; } = 0m;

    /// <summary>حداقل موجودی</summary>
    public decimal MinStock { get; set; } = 0m;

    /// <summary>حداکثر موجودی</summary>
    public decimal MaxStock { get; set; } = 0m;

    /// <summary>نقطه سفارش مجدد</summary>
    public decimal ReorderLevel { get; set; } = 0m;

    /// <summary>تاریخ تولید</summary>
    public DateOnly? ManufactureDate { get; set; }

    /// <summary>تاریخ انقضا - ایندکس‌شده</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>قیمت خرید</summary>
    public decimal PurchasePrice { get; set; } = 0m;

    /// <summary>قیمت فروش</summary>
    public decimal SellPrice { get; set; } = 0m;

    /// <summary>محل فیزیکی در قفسه</summary>
    public string? Location { get; set; }

    /// <summary>شناسه تأمین‌کننده</summary>
    public long? SupplierId { get; set; }

    /// <summary>آخرین خرید</summary>
    public DateOnly? LastPurchaseDate { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>دارو</summary>
    public Medication? Medication { get; set; }

    /// <summary>تأمین‌کننده</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>تراکنش‌های این رکورد</summary>
    public ICollection<PharmacyTransaction> Transactions { get; set; } = new List<PharmacyTransaction>();
}
