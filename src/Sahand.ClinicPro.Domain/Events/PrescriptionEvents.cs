using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد صدور نسخه</summary>
public sealed record PrescriptionCreatedEvent(long PrescriptionId, long PatientId, long DoctorId, bool IsElectronic) : DomainEventBase;
