using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// نظرسنجی - پرسشنامه رضایت بیماران
/// </summary>
public class Survey : AuditableEntity, ISoftDeletable
{
    /// <summary>عنوان نظرسنجی</summary>
    public string Title { get; set; } = null!;

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>نوع نظرسنجی</summary>
    public SurveyType SurveyType { get; set; }

    /// <summary>سوالات - JSON</summary>
    public string? Questions { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>پاسخ‌های نظرسنجی</summary>
    public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}
