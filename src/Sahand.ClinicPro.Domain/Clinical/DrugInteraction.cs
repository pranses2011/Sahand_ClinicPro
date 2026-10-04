using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// تداخل دارویی - جفت داروهای خطرناک با شدت تداخل
/// </summary>
public class DrugInteraction : AuditableEntity
{
    /// <summary>شناسه داروی اول</summary>
    public long Medication1Id { get; set; }

    /// <summary>شناسه داروی دوم</summary>
    public long Medication2Id { get; set; }

    /// <summary>شدت تداخل</summary>
    public DrugInteractionSeverity Severity { get; set; }

    /// <summary>شرح تداخل</summary>
    public string? Description { get; set; }

    /// <summary>سازوکار تداخل</summary>
    public string? Mechanism { get; set; }

    /// <summary>توصیه بالینی</summary>
    public string? Recommendation { get; set; }

    /// <summary>داروی اول</summary>
    public Medication? Medication1 { get; set; }

    /// <summary>داروی دوم</summary>
    public Medication? Medication2 { get; set; }
}
