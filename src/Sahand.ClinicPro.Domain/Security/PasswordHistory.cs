namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// تاریخچه رمز عبور - جلوگیری از تکرار رمزهای قبلی
/// </summary>
public class PasswordHistory : AuditableEntity
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>هش رمز پیشین</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>زمان تغییر</summary>
    public DateTime? ChangedAt { get; set; }

    /// <summary>کاربر</summary>
    public User? User { get; set; }
}
