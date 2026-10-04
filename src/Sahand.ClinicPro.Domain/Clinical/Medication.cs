namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// دارو - کاتالوگ مرجع داروها با کد IRC
/// </summary>
public class Medication : AuditableEntity, ISoftDeletable
{
    /// <summary>کد IRC دارو - یکتا</summary>
    public string? IrcCode { get; set; }

    /// <summary>نام ژنریک انگلیسی</summary>
    public string GenericName { get; set; } = null!;

    /// <summary>نام ژنریک فارسی</summary>
    public string? GenericNameFa { get; set; }

    /// <summary>نام تجاری</summary>
    public string? BrandName { get; set; }

    /// <summary>کد ATC طبقه‌بندی دارویی</summary>
    public string? AtcCode { get; set; }

    /// <summary>فرم دارویی مثل قرص یا شربت</summary>
    public string? Form { get; set; }

    /// <summary>غلظت مثل 500 میلی‌گرم</summary>
    public string? Strength { get; set; }

    /// <summary>شرکت سازنده</summary>
    public string? Manufacturer { get; set; }

    /// <summary>کشور سازنده</summary>
    public string? Country { get; set; }

    /// <summary>بارکد</summary>
    public string? Barcode { get; set; }

    /// <summary>دسته دارویی</summary>
    public string? Category { get; set; }

    /// <summary>بدون نیاز به نسخه</summary>
    public bool IsOtc { get; set; } = false;

    /// <summary>مواد مخدر</summary>
    public bool IsNarcotic { get; set; } = false;

    /// <summary>نیازمند یخچال</summary>
    public bool IsRefrigerated { get; set; } = false;

    /// <summary>عوارض جانبی</summary>
    public string? SideEffects { get; set; }

    /// <summary>موارد منع مصرف</summary>
    public string? Contraindications { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>اقلام نسخه با این دارو</summary>
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();

    /// <summary>رکوردهای موجودی این دارو</summary>
    public ICollection<PharmacyInventory> InventoryItems { get; set; } = new List<PharmacyInventory>();
}
