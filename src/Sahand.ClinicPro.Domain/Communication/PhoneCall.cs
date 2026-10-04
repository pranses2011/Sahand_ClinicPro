using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// تماس تلفنی - ثبت تماس‌های مرتبط با بیماران
/// </summary>
public class PhoneCall : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره تماس‌گیرنده</summary>
    public string? CallerNumber { get; set; }

    /// <summary>شناسه بیمار مرتبط</summary>
    public long? PatientId { get; set; }

    /// <summary>شناسه کاربر ثبت‌کننده</summary>
    public long? UserId { get; set; }

    /// <summary>جهت تماس</summary>
    public CallDirection Direction { get; set; }

    /// <summary>شروع تماس</summary>
    public DateTime? CallStart { get; set; }

    /// <summary>پایان تماس</summary>
    public DateTime? CallEnd { get; set; }

    /// <summary>مدت به ثانیه</summary>
    public int? Duration { get; set; }

    /// <summary>نتیجه تماس</summary>
    public CallResult Result { get; set; }

    /// <summary>خلاصه گفتگو</summary>
    public string? Notes { get; set; }

    /// <summary>مسیر فایل صوت</summary>
    public string? RecordingPath { get; set; }

    /// <summary>بیمار مرتبط</summary>
    public Patient? Patient { get; set; }
}
