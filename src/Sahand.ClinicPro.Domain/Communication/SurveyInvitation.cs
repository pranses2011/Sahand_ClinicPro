using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.Entities.Communication;

/// <summary>دعوت پاسخ‌گویی؛ فقط چکیده توکن نگهداری می‌شود، بدون شناسه بیمار یا تلفن.</summary>
public class SurveyInvitation : BaseEntity
{
    /// <summary>شناسه نظرسنجی.</summary>
    public long SurveyId { get; set; }
    /// <summary>چکیده SHA-256 توکن تصادفی، به صورت هگز.</summary>
    public string TokenHash { get; set; } = string.Empty;
    /// <summary>پایان اعتبار به وقت UTC.</summary>
    public DateTime ExpiresAt { get; set; }
    /// <summary>زمان مصرف به وقت UTC؛ مقدار تهی نشان‌دهنده توکن مصرف‌نشده است.</summary>
    public DateTime? UsedAt { get; set; }
}
