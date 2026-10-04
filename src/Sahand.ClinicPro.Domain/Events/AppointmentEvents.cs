using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد رزرو نوبت — برای ارسال پیامک تأیید</summary>
public sealed record AppointmentBookedEvent(long AppointmentId, long PatientId, string Mobile) : DomainEventBase;

/// <summary>رویداد لغو نوبت</summary>
public sealed record AppointmentCancelledEvent(long AppointmentId, long PatientId, string Reason) : DomainEventBase;

/// <summary>رویداد یادآوری نوبت فردا — برای صف پیامک</summary>
public sealed record AppointmentReminderDueEvent(long AppointmentId, long PatientId, string Mobile) : DomainEventBase;
