using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Billing;

/// <summary>ردیف فهرست فاکتورها</summary>
public record InvoiceListDto(
    long Id,
    string? InvoiceNumber,
    string PatientName,
    string DateShamsi,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status);

/// <summary>قلم فاکتور برای ثبت</summary>
public record InvoiceItemDto(
    long? ServiceId,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    long? DoctorId,
    decimal DoctorSharePercent,
    long? PerformedById = null,
    // چرخهٔ ۳۹ — معافیت بیمه‌ای قلم: برای اقلام کالایی (لوازم مصرفی درمانگاه)
    // که سهم بیمه ندارند و ۱۰۰٪ بر عهدهٔ بیمار است؛ پیش‌فرض false یعنی رفتار
    // پیشین (سهم بیمه از درصد شرکت بیمه) کاملاً دست‌نخورده می‌ماند
    bool IsInsuranceExempt = false);

/// <summary>درخواست صدور فاکتور</summary>
public record CreateInvoiceDto(
    long PatientId,
    long? ReceptionId,
    long? VisitId,
    InvoiceType Type,
    long? InsuranceCompanyId,
    decimal TaxPercent,
    List<InvoiceItemDto> Items,
    string? Description = null);

/// <summary>قلم فاکتور در پاسخ</summary>
public record InvoiceItemDetailDto(
    long? ServiceId,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal PatientShare,
    decimal InsuranceShare,
    decimal TotalPrice);

/// <summary>جزئیات فاکتور</summary>
public record InvoiceDetailDto(
    long Id,
    string? InvoiceNumber,
    long PatientId,
    string PatientName,
    string DateShamsi,
    InvoiceType Type,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal Tax,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus,
    InvoiceStatus Status,
    List<InvoiceItemDetailDto> Items);

/// <summary>درخواست ثبت پرداخت</summary>
public record RecordPaymentDto(
    long? InvoiceId,
    long PatientId,
    decimal Amount,
    PaymentMethod Method,
    string? ReferenceNumber = null,
    string? CardNumber = null);

/// <summary>ردیف فهرست پرداخت‌ها</summary>
public record PaymentListDto(
    long Id,
    string? PaymentNumber,
    string PatientName,
    decimal Amount,
    PaymentMethod Method,
    string DateShamsi,
    string? ReferenceNumber);
