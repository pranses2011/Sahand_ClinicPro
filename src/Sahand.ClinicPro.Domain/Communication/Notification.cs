using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// اعلان درون‌برنامه‌ای - اعلان‌های سیستم و کسب‌وکار
/// </summary>
public class Notification : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کاربر گیرنده</summary>
    public long UserId { get; set; }

    /// <summary>عنوان اعلان</summary>
    public string Title { get; set; } = null!;

    /// <summary>متن اعلان</summary>
    public string? Message { get; set; }

    /// <summary>نوع اعلان</summary>
    public NotificationType Type { get; set; }

    /// <summary>اولویت</summary>
    public NotificationPriority Priority { get; set; }

    /// <summary>پیوند مقصد در صورت کلیک</summary>
    public string? Link { get; set; }

    /// <summary>خوانده شده</summary>
    public bool IsRead { get; set; } = false;

    /// <summary>زمان خواندن</summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>کاربر گیرنده</summary>
    public User? User { get; set; }
}
