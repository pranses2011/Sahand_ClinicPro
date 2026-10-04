using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Events;

/// <summary>رویداد آماده شدن نتیجه آزمایش</summary>
public sealed record LabResultReadyEvent(long LabOrderId, long PatientId, string Mobile) : DomainEventBase;
