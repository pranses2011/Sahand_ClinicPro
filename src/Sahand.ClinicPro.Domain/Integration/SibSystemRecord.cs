using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// رکورد سیب - همگام‌سازی با سامانه سطح یک بهداشت
/// </summary>
public class SibSystemRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه رکورد در سیب</summary>
    public string? SibRecordId { get; set; }

    /// <summary>نوع خدمت</summary>
    public SibServiceType ServiceType { get; set; }

    /// <summary>نوع ارجاع</summary>
    public ReferralType? ReferralType { get; set; }

    /// <summary>داده خام - JSON</summary>
    public string? Data { get; set; }

    /// <summary>وضعیت همگام‌سازی</summary>
    public SyncStatus SyncStatus { get; set; }

    /// <summary>زمان همگام‌سازی</summary>
    public DateTime? SyncedAt { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
