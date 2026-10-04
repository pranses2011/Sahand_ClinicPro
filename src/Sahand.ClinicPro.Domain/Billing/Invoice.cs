using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// فاکتور - صورت‌حساب خدمات ارائه‌شده به بیمار
/// </summary>
public class Invoice : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره فاکتور - یکتا</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پذیرش مرتبط</summary>
    public long? ReceptionId { get; set; }

    /// <summary>شناسه ویزیت مرتبط</summary>
    public long? VisitId { get; set; }

    /// <summary>تاریخ صدور فاکتور</summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>تاریخ شمسی فاکتور</summary>
    public string? InvoiceDateShamsi { get; set; }

    /// <summary>نوع فاکتور</summary>
    public InvoiceType InvoiceType { get; set; }

    /// <summary>بیمهگر فاکتور</summary>
    public long? InsuranceCompanyId { get; set; }

    /// <summary>بیمه تکمیلی فاکتور</summary>
    public long? SupplementaryInsuranceId { get; set; }

    /// <summary>جمع کل قبل از تخفیف</summary>
    public decimal SubTotal { get; set; } = 0m;

    /// <summary>مبلغ تخفیف</summary>
    public decimal DiscountAmount { get; set; } = 0m;

    /// <summary>درصد تخفیف</summary>
    public decimal DiscountPercent { get; set; } = 0m;

    /// <summary>سهم بیمه اصلی</summary>
    public decimal InsuranceDiscount { get; set; } = 0m;

    /// <summary>سهم بیمه تکمیلی</summary>
    public decimal SupplementaryDiscount { get; set; } = 0m;

    /// <summary>مالیات</summary>
    public decimal Tax { get; set; } = 0m;

    /// <summary>مبلغ نهایی</summary>
    public decimal TotalAmount { get; set; } = 0m;

    /// <summary>مبلغ پرداخت‌شده</summary>
    public decimal PaidAmount { get; set; } = 0m;

    /// <summary>مانده حساب</summary>
    public decimal RemainingAmount { get; set; } = 0m;

    /// <summary>وضعیت فاکتور</summary>
    public InvoiceStatus Status { get; set; } = Domain.Enums.InvoiceStatus.Issued;

    /// <summary>وضعیت پرداخت</summary>
    public PaymentStatus PaymentStatus { get; set; } = Domain.Enums.PaymentStatus.Unpaid;

    /// <summary>شناسه صندوقدار</summary>
    public long? CashierId { get; set; }

    /// <summary>شناسه صندوق</summary>
    public long? CashRegisterId { get; set; }

    /// <summary>توضیحات فاکتور</summary>
    public string? Description { get; set; }

    /// <summary>چاپ شده</summary>
    public bool IsPrinted { get; set; } = false;

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پذیرش مرتبط</summary>
    public Reception? Reception { get; set; }

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمهگر</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }

    /// <summary>اقلام فاکتور</summary>
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    /// <summary>پرداخت‌های فاکتور</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
