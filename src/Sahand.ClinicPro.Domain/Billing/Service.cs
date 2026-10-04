using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// خدمت - فهرست خدمات قابل ارائه با قیمت پایه
/// </summary>
public class Service : AuditableEntity, ISoftDeletable
{
    /// <summary>کد خدمت - یکتا</summary>
    public string? ServiceCode { get; set; }

    /// <summary>نام خدمت</summary>
    public string ServiceName { get; set; } = null!;

    /// <summary>نام انگلیسی خدمت</summary>
    public string? ServiceNameEn { get; set; }

    /// <summary>شناسه دسته‌بندی</summary>
    public long? CategoryId { get; set; }

    /// <summary>نوع خدمت</summary>
    public ServiceType ServiceType { get; set; }

    /// <summary>واحد سنجش</summary>
    public string? Unit { get; set; }

    /// <summary>قیمت پیش‌فرض</summary>
    public decimal DefaultPrice { get; set; } = 0m;

    /// <summary>ضریب K</summary>
    public decimal KCoefficient { get; set; } = 0m;

    /// <summary>کد ملی خدمت برای بیمه</summary>
    public string? NationalCode { get; set; }

    /// <summary>نیازمند مجوز بیمه</summary>
    public bool RequiresAuthorization { get; set; } = false;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>دسته‌بندی</summary>
    public ServiceCategory? Category { get; set; }

    /// <summary>تعرفه‌های این خدمت</summary>
    public ICollection<Tariff> Tariffs { get; set; } = new List<Tariff>();

    /// <summary>اقلام فاکتور این خدمت</summary>
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
