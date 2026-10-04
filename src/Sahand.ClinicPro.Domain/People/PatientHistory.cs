using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// سابقه پزشکی بیمار - جراحی‌ها و بستری‌ها و آلرژی‌های ثبت‌شده
/// </summary>
public class PatientHistory : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>نوع سابقه</summary>
    public HistoryType HistoryType { get; set; }

    /// <summary>عنوان سابقه</summary>
    public string? Title { get; set; }

    /// <summary>شرح سابقه</summary>
    public string? Description { get; set; }

    /// <summary>تاریخ ثبت</summary>
    public DateTime? RecordedDate { get; set; }

    /// <summary>شناسه کاربر ثبت‌کننده</summary>
    public long? RecordedBy { get; set; }

    /// <summary>بیمار مرتبط</summary>
    public Patient? Patient { get; set; }
}
