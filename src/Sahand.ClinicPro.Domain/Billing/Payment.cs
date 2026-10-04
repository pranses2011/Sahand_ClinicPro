using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// پرداخت - دریافتی نقدی یا کارتی یا آنلاین از بیمار
/// </summary>
public class Payment : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره سند پرداخت - یکتا</summary>
    public string? PaymentNumber { get; set; }

    /// <summary>شناسه فاکتور مرتبط</summary>
    public long? InvoiceId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>مبلغ پرداختی به ریال</summary>
    public decimal Amount { get; set; }

    /// <summary>روش پرداخت</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>تاریخ پرداخت</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>شماره ارجاع تراکنش</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>چهار رقم آخر کارت</summary>
    public string? CardNumber { get; set; }

    /// <summary>نام بانک</summary>
    public string? BankName { get; set; }

    /// <summary>شناسه پایانه کارت‌خوان</summary>
    public string? TerminalId { get; set; }

    /// <summary>شماره پیگیری</summary>
    public string? TraceNumber { get; set; }

    /// <summary>وضعیت پرداخت</summary>
    public PaymentRecordStatus Status { get; set; } = Domain.Enums.PaymentRecordStatus.Completed;

    /// <summary>شناسه صندوقدار دریافت‌کننده</summary>
    public long? CashierId { get; set; }

    /// <summary>شناسه صندوق</summary>
    public long? CashRegisterId { get; set; }

    /// <summary>پرداخت برگشتی</summary>
    public bool IsReturned { get; set; } = false;

    /// <summary>فاکتور مرتبط</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>صندوق دریافت</summary>
    public CashRegister? CashRegister { get; set; }
}
