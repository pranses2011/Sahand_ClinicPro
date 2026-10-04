using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد تکمیل ویزیت</summary>
public sealed record VisitCompletedEvent(long VisitId, long PatientId, long DoctorId) : DomainEventBase;
