using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Pharmacy;

/// <summary>
/// تأمین‌کننده دارو - شرکت‌های پخش و سازنده
/// </summary>
public class Supplier : AuditableEntity, ISoftDeletable
{
    /// <summary>نام شرکت</summary>
    public string CompanyName { get; set; } = null!;

    /// <summary>کد یکتا</summary>
    public string? Code { get; set; }

    /// <summary>نوع تأمین‌کننده</summary>
    public SupplierType SupplierType { get; set; }

    /// <summary>شخص رابط</summary>
    public string? ContactPerson { get; set; }

    /// <summary>تلفن</summary>
    public string? Phone { get; set; }

    /// <summary>موبایل</summary>
    public string? Mobile { get; set; }

    /// <summary>ایمیل</summary>
    public string? Email { get; set; }

    /// <summary>نشانی</summary>
    public string? Address { get; set; }

    /// <summary>کد اقتصادی</summary>
    public string? EconomicCode { get; set; }

    /// <summary>شناسه ملی شرکت</summary>
    public string? NationalId { get; set; }

    /// <summary>حساب بانکی</summary>
    public string? BankAccount { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>داروهای تأمین‌شده</summary>
    public ICollection<PharmacyInventory> InventoryItems { get; set; } = new List<PharmacyInventory>();

    /// <summary>سفارش‌های خرید</summary>
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
