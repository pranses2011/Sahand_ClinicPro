using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Clinical;

/// <summary>
/// نسخه پزشکی - دارویی یا پاراکلینیک یا ترکیبی
/// </summary>
public class Prescription : AuditableEntity, ISoftDeletable
{
    /// <summary>کد نسخه - فرمت RX-XXXXXXXX</summary>
    public string? PrescriptionCode { get; set; }

    /// <summary>شناسه ویزیت مرتبط</summary>
    public long? VisitId { get; set; }

    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک تجویزکننده</summary>
    public long DoctorId { get; set; }

    /// <summary>تاریخ تجویز</summary>
    public DateTime? PrescriptionDate { get; set; }

    /// <summary>نوع نسخه</summary>
    public PrescriptionType Type { get; set; } = Domain.Enums.PrescriptionType.Medication;

    /// <summary>نسخه الکترونیکی سامانه بیمه</summary>
    public bool IsElectronic { get; set; } = false;

    /// <summary>کد رهگیری نسخه الکترونیک</summary>
    public string? ElectronicTrackingCode { get; set; }

    /// <summary>سامانه مقصد نسخه الکترونیک</summary>
    public InsuranceSystemType? InsuranceSystemType { get; set; }

    /// <summary>کد تأیید بیمه</summary>
    public string? InsuranceApprovalCode { get; set; }

    /// <summary>معتبر تا تاریخ</summary>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>نسخه مواد مخدر</summary>
    public bool IsNarcotic { get; set; } = false;

    /// <summary>وضعیت نسخه</summary>
    public PrescriptionStatus Status { get; set; } = Domain.Enums.PrescriptionStatus.Issued;

    /// <summary>توضیحات پزشک</summary>
    public string? Notes { get; set; }

    /// <summary>تعداد دفعات چاپ</summary>
    public int PrintedCount { get; set; } = 0;

    /// <summary>ویزیت مرتبط</summary>
    public Visit? Visit { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>پزشک تجویزکننده</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>اقلام نسخه</summary>
    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
