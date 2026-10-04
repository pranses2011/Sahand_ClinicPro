using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// پیامک ارسالی - رکورد کامل همراه وضعیت تحویل
/// </summary>
public class SmsMessage : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره گیرنده - ۱۱ رقم</summary>
    public string Recipient { get; set; } = null!;

    /// <summary>نوع گیرنده</summary>
    public SmsRecipientType RecipientType { get; set; }

    /// <summary>شناسه رکورد گیرنده مثل بیمار</summary>
    public long? RecipientId { get; set; }

    /// <summary>متن پیامک</summary>
    public string? Message { get; set; }

    /// <summary>شناسه قالب استفاده‌شده</summary>
    public long? TemplateId { get; set; }

    /// <summary>نام پنل ارسال‌کننده</summary>
    public string? Provider { get; set; }

    /// <summary>شناسه پیام در پنل</summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>وضعیت ارسال</summary>
    public SmsStatus Status { get; set; }

    /// <summary>زمان ارسال</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>زمان تحویل به گوشی</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>هزینه ارسال</summary>
    public decimal Cost { get; set; } = 0m;

    /// <summary>خطای ارسال</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>نوع پیام</summary>
    public SmsMessageType MessageType { get; set; }

    /// <summary>منشأ پیام</summary>
    public SmsReferenceType? ReferenceType { get; set; }

    /// <summary>شناسه رکورد منشأ</summary>
    public long? ReferenceId { get; set; }

    /// <summary>قالب پیامک</summary>
    public SmsTemplate? Template { get; set; }
}
