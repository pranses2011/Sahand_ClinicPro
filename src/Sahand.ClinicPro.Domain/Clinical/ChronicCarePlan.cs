using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// طرح مراقبت بیمار مزمن — دیابت/پرفشاری/آسم/تالاسمی و... — گام R2
/// چرخه «هدف → اقدام → بازبینی دوره‌ای» را برای بیماران مزمن مستند می‌کند
/// </summary>
public class ChronicCarePlan : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>عنوان بیماری مزمن</summary>
    public string DiseaseTitle { get; set; } = null!;

    /// <summary>کد ICD-10 بیماری</summary>
    public string? Icd10Code { get; set; }

    /// <summary>وضعیت طرح</summary>
    public CarePlanStatus Status { get; set; } = CarePlanStatus.Active;

    /// <summary>تاریخ شروع طرح (میلادی)</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>تاریخ بازبینی بعدی (میلادی) — ملاک هشدار «نیازمند بازبینی»</summary>
    public DateOnly? NextReviewDate { get; set; }

    /// <summary>اهداف درمانی قابل اندازه‌گیری (HbA1c زیر ۷، ...)</summary>
    public string? Goals { get; set; }

    /// <summary>اقدامات جاری (رژیم، داروها، آموزش و...)</summary>
    public string? Actions { get; set; }

    /// <summary>اعضای تیم مراقبت (پزشک، پرستار، تغذیه)</summary>
    public string? CareTeam { get; set; }

    /// <summary>یادداشت آزاد</summary>
    public string? Notes { get; set; }

    /// <summary>بیمار والد</summary>
    public People.Patient? Patient { get; set; }
}
