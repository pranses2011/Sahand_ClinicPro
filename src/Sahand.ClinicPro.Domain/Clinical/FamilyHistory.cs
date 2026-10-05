using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// سابقه پزشکی خانواده — بیماری‌های ارثی و خانوادگی بیمار — گام R2
/// برای غربالگری بیماری‌های ژنتیک و ارزیابی خطر ازدواج خویشاوندی کاربرد دارد
/// </summary>
public class FamilyHistory : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>نسبت خویشاوند مبتلا</summary>
    public FamilyRelative Relative { get; set; }

    /// <summary>عنوان بیماری (دیابت نوع ۲، پرفشاری خون و...)</summary>
    public string Disease { get; set; } = null!;

    /// <summary>کد ICD-10 بیماری در صورت ثبت</summary>
    public string? Icd10Code { get; set; }

    /// <summary>سن شروع بیماری در خویشاوند</summary>
    public int? OnsetAge { get; set; }

    /// <summary>آیا بیماری ژنتیک/ارثی شناخته‌شده است</summary>
    public bool IsGenetic { get; set; } = false;

    /// <summary>آیا خویشاوند فوت کرده است</summary>
    public bool IsDeceased { get; set; } = false;

    /// <summary>سن فوت در صورت فوت</summary>
    public int? DeathAge { get; set; }

    /// <summary>یادداشت آزاد</summary>
    public string? Notes { get; set; }

    /// <summary>بیمار والد</summary>
    public People.Patient? Patient { get; set; }
}
