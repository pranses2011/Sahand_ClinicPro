using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// نشست کاربر - ردیابی دستگاه‌های متصل
/// </summary>
public class UserSession : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>توکن نشست</summary>
    public string? SessionToken { get; set; }

    /// <summary>زمان ورود</summary>
    public DateTime? LoginAt { get; set; }

    /// <summary>زمان خروج</summary>
    public DateTime? LogoutAt { get; set; }

    /// <summary>آخرین فعالیت</summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>نشانی IP</summary>
    public string? IpAddress { get; set; }

    /// <summary>عامل کاربر مرورگر</summary>
    public string? UserAgent { get; set; }

    /// <summary>نوع دستگاه</summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>نشست فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>کاربر</summary>
    public User? User { get; set; }
}
