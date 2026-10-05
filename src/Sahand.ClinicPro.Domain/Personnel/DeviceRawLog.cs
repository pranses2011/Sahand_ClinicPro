using System.ComponentModel.DataAnnotations;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// لاگ خام ضربه دستگاه — پیش از هر پردازشی، عیناً از سخت‌افزار دریافت و نگه‌داری می‌شود
/// اصل «منبع حقیقت»: هیچ لاگی حذف نمی‌شود؛ پس از تطبیق به رکورد حضور تبدیل یا نادیده گرفته می‌شود
/// </summary>
public class DeviceRawLog : AuditableEntity
{
    /// <summary>شناسه دستگاه فرستنده</summary>
    public long DeviceId { get; set; }

    /// <summary>شناسه ثبت‌شده روی دستگاه (کارت/اثر انگشت)</summary>
    public string EnrollNumber { get; set; } = null!;

    /// <summary>زمان ضربه به وقت دستگاه (UTC هنگام ذخیره)</summary>
    public DateTime PunchTime { get; set; }

    /// <summary>نوع ضربه</summary>
    public PunchState PunchState { get; set; } = PunchState.Undefined;

    /// <summary>کد خام وضعیت از پروتکل دستگاه — برای عیب‌یابی</summary>
    public byte? RawStateCode { get; set; }

    /// <summary>روش تأیید: ۱=اثر انگشت، ۲=کارت، ۱۵=چهره و ...</summary>
    public byte VerifyMode { get; set; }

    /// <summary>شماره دستگاه فرعی/درگاه (InOutPort)</summary>
    public byte? InOutPort { get; set; }

    /// <summary>وضعیت پردازش</summary>
    public RawLogStatus ProcessedStatus { get; set; } = RawLogStatus.Pending;

    /// <summary>زمان پردازش (UTC)</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>شناسه رکورد حضور حاصل از پردازش</summary>
    public long? AttendanceId { get; set; }

    /// <summary>پیام خطا در صورت شکست پردازش</summary>
    [StringLength(300)]
    public string? ErrorMessage { get; set; }

    /// <summary>شناسه نوبت همگام‌سازی — گروه‌بندی لاگ‌های یک Pull</summary>
    public Guid? SyncBatchId { get; set; }

    /// <summary>دستگاه مبدأ</summary>
    public AttendanceDevice? Device { get; set; }
}
