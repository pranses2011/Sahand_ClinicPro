using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// نسخه الکترونیک - رکورد ارسال نسخه به سامانه سلامت یا تأمین
/// </summary>
public class ElectronicPrescription : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه نسخه محلی</summary>
    public long PrescriptionId { get; set; }

    /// <summary>سامانه مقصد - سلامت یا تأمین</summary>
    public InsuranceSystemType SystemType { get; set; }

    /// <summary>کد رهگیری - یکتا</summary>
    public string? TrackingCode { get; set; }

    /// <summary>کد ثبت در سامانه</summary>
    public string? RegistrationCode { get; set; }

    /// <summary>وضعیت</summary>
    public ElectronicPrescriptionStatus Status { get; set; }

    /// <summary>پاسخ خام سامانه</summary>
    public string? Response { get; set; }

    /// <summary>زمان ارسال</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>ارسال موفق</summary>
    public bool IsSuccess { get; set; } = false;

    /// <summary>کد خطای سامانه</summary>
    public string? ErrorCode { get; set; }

    /// <summary>نسخه مرتبط</summary>
    public Prescription? Prescription { get; set; }
}
