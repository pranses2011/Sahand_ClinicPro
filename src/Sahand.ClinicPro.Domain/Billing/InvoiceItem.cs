using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// قلم فاکتور - یک خدمت با جزئیات سهم بیمه و پزشک
/// </summary>
public class InvoiceItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه فاکتور</summary>
    public long InvoiceId { get; set; }

    /// <summary>شناسه خدمت</summary>
    public long? ServiceId { get; set; }

    /// <summary>شرح قلم</summary>
    public string? Description { get; set; }

    /// <summary>تعداد</summary>
    public decimal Quantity { get; set; } = 1m;

    /// <summary>قیمت واحد</summary>
    public decimal UnitPrice { get; set; } = 0m;

    /// <summary>تخفیف قلم</summary>
    public decimal DiscountAmount { get; set; } = 0m;

    /// <summary>درصد تخفیف</summary>
    public decimal DiscountPercent { get; set; } = 0m;

    /// <summary>سهم بیمه</summary>
    public decimal InsuranceShare { get; set; } = 0m;

    /// <summary>درصد بیمه</summary>
    public decimal InsurancePercent { get; set; } = 0m;

    /// <summary>سهم بیمه تکمیلی</summary>
    public decimal SupplementaryShare { get; set; } = 0m;

    /// <summary>سهم بیمار</summary>
    public decimal PatientShare { get; set; } = 0m;

    /// <summary>قیمت کل قلم</summary>
    public decimal TotalPrice { get; set; } = 0m;

    /// <summary>شناسه پزشک انجام‌دهنده</summary>
    public long? DoctorId { get; set; }

    /// <summary>
    /// شناسه پرسنل انجام‌دهنده خدمت — مبنای حقوق درصدی:
    /// سود اقلامی که این فیلد آن‌ها را به پرسنل منتسب می‌کند
    /// در فیش حقوقی همان پرسنل لحاظ می‌شود.
    /// </summary>
    public long? PerformedById { get; set; }

    /// <summary>
    /// عکس‌فوری هزینه تمام‌شده هر واحد در لحظه صدور فاکتور (ریال).
    /// سود این قلم = مبلغ نهایی − (UnitCost × تعداد)؛ عکس‌فوری گرفتن
    /// تضمین می‌کند تغییر آتی هزینه خدمت، سوابق و فیش‌های گذشته را خراب نکند.
    /// </summary>
    public decimal UnitCost { get; set; } = 0m;

    /// <summary>سهم ریالی پزشک</summary>
    public decimal DoctorShareAmount { get; set; } = 0m;

    /// <summary>درصد سهم پزشک</summary>
    public decimal DoctorSharePercent { get; set; } = 0m;

    /// <summary>فاکتور والد</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>خدمت مربوطه</summary>
    public Service? Service { get; set; }

    /// <summary>پرسنل انجام‌دهنده خدمت</summary>
    public Employee? Performer { get; set; }
}
