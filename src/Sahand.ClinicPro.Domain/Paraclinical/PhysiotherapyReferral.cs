using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;

namespace Sahand.ClinicPro.Domain.Entities.Paraclinical;

/// <summary>ارجاع پزشک به فیزیوتراپی؛ مستقل از جلسات درمانی.</summary>
public class PhysiotherapyReferral : BaseEntity
{
    public Guid RequestId { get; set; }
    public long VisitId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Visit? Visit { get; set; }
    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
}
