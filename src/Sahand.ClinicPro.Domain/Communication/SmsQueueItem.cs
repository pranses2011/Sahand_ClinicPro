using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// صف ارسال پیامک - پردازش پس‌زمینه با Hangfire
/// </summary>
public class SmsQueueItem : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره گیرنده</summary>
    public string Recipient { get; set; } = null!;

    /// <summary>متن پیام</summary>
    public string? Message { get; set; }

    /// <summary>زمان برنامه‌ریزی‌شده ارسال</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>زمان پردازش</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>تعداد تلاش</summary>
    public int Attempts { get; set; } = 0;

    /// <summary>وضعیت در صف</summary>
    public SmsStatus Status { get; set; }

    /// <summary>اولویت صف</summary>
    public byte Priority { get; set; }
}
