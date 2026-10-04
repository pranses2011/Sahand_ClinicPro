using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>
/// قالب پیامک - الگوی آماده با متغیرها
/// </summary>
public class SmsTemplate : AuditableEntity, ISoftDeletable
{
    /// <summary>نام قالب</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتای قالب</summary>
    public string? Code { get; set; }

    /// <summary>نوع قالب</summary>
    public SmsTemplateType TemplateType { get; set; }

    /// <summary>متن قالب با متغیرهایی مثل نام بیمار</summary>
    public string? Content { get; set; }

    /// <summary>تعریف متغیرها - JSON</summary>
    public string? Variables { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>پیامک‌های ارسال‌شده با این قالب</summary>
    public ICollection<SmsMessage> Messages { get; set; } = new List<SmsMessage>();
}
