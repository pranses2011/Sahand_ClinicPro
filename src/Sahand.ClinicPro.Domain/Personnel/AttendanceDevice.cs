using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// دستگاه حضور و غیاب — تعریف سخت‌افزار و پارامترهای اتصال
/// هر دستگاه از طریق درایور متناسب با برند خود همگام‌سازی می‌شود
/// </summary>
public class AttendanceDevice : AuditableEntity, ISoftDeletable
{
    /// <summary>نام نمایشی دستگاه — مثلاً «ورودی طبقه اول»</summary>
    public string Name { get; set; } = null!;

    /// <summary>برند و پروتکل ارتباطی</summary>
    public DeviceBrand Brand { get; set; } = DeviceBrand.ZKTeco;

    /// <summary>مدل دستگاه — مثلاً K40 / DS-K1T342M</summary>
    public string? Model { get; set; }

    /// <summary>شماره سریال سخت‌افزار</summary>
    public string? SerialNumber { get; set; }

    /// <summary>نشانی IP روی شبکه محلی</summary>
    public string IpAddress { get; set; } = "192.168.1.201";

    /// <summary>پورت اتصال — ZKTeco: 4370 / Hikvision: 80</summary>
    public int Port { get; set; } = 4370;

    /// <summary>کلید/رمز ارتباطی دستگاه</summary>
    public string? CommunicationKey { get; set; }

    /// <summary>محل نصب فیزیکی</summary>
    public string? Location { get; set; }

    /// <summary>وضعیت لحظه‌ای اتصال</summary>
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;

    /// <summary>فعال — دستگاه غیرفعال در همگام‌سازی دوره‌ای شرکت نمی‌کند</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>بازه همگام‌سازی خودکار (دقیقه) — پیش‌فرض ۵ دقیقه</summary>
    public int SyncIntervalMinutes { get; set; } = 5;

    /// <summary>آخرین همگام‌سازی موفق (UTC)</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>تعداد لاگ دریافتی در آخرین همگام‌سازی</summary>
    public int? LastSyncLogCount { get; set; }

    /// <summary>آخرین خطای اتصال — برای نمایش در پنل پایش</summary>
    public string? LastError { get; set; }

    /// <summary>توضیحات آزاد</summary>
    public string? Notes { get; set; }

    /// <summary>لاگ‌های خام دریافتی از این دستگاه</summary>
    public ICollection<DeviceRawLog> RawLogs { get; set; } = new List<DeviceRawLog>();

    /// <summary>نگاشت کاربران ثبت‌شده روی دستگاه به پرسنل</summary>
    public ICollection<DeviceUserMapping> UserMappings { get; set; } = new List<DeviceUserMapping>();
}
