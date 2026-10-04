using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Licensing;

/// <summary>
/// لاگ اعتبارسنجی لایسنس - هر بررسی روزانه ثبت می‌شود
/// </summary>
public class LicenseCheckLog : AuditableEntity, ISoftDeletable
{
    /// <summary>زمان بررسی</summary>
    public DateTime? CheckDate { get; set; }

    /// <summary>نتیجه</summary>
    public LicenseValidationResult Result { get; set; }

    /// <summary>بررسی آنلاین بود</summary>
    public bool IsOnlineCheck { get; set; } = false;

    /// <summary>پیام نتیجه</summary>
    public string? Message { get; set; }
}
