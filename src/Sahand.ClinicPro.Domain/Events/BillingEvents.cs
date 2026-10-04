using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد صدور فاکتور</summary>
public sealed record InvoiceCreatedEvent(long InvoiceId, long PatientId, decimal TotalAmount) : DomainEventBase;

/// <summary>رویداد تسویه کامل فاکتور</summary>
public sealed record InvoicePaidEvent(long InvoiceId, long PatientId, decimal PaidAmount) : DomainEventBase;

/// <summary>رویداد ثبت پرداخت</summary>
public sealed record PaymentReceivedEvent(long PaymentId, long InvoiceId, decimal Amount) : DomainEventBase;
