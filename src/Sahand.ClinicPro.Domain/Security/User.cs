namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// کاربر سیستم - حساب ورود پرسنل به نرم‌افزار
/// </summary>
/// <remarks>
/// رمز عبور هرگز به‌صورت متن ساده ذخیره نمی‌شود
/// پس از پنج تلاش ناموفق حساب به مدت سی دقیقه قفل می‌شود
/// </remarks>
public class User : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پرسنل مرتبط</summary>
    public long? EmployeeId { get; set; }

    /// <summary>نام کاربری - یکتا</summary>
    public string Username { get; set; } = null!;

    /// <summary>ایمیل - یکتا</summary>
    public string Email { get; set; } = null!;

    /// <summary>هش رمز عبور با BCrypt - ضریب ۱۲</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>مهر امنیتی برای ابطال توکن‌ها</summary>
    public string? SecurityStamp { get; set; }

    /// <summary>ورود دومرحله‌ای فعال</summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>کلید دومرحله‌ای</summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>موبایل کاربر</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>ایمیل تأیید شده</summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>موبایل تأیید شده</summary>
    public bool PhoneConfirmed { get; set; } = false;

    /// <summary>شمارش تلاش ناموفق ورود</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>پایان قفل حساب - ۳۰ دقیقه بعد از ۵ تلاش ناموفق</summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>آخرین ورود موفق</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>IP آخرین ورود</summary>
    public string? LastLoginIp { get; set; }

    /// <summary>آخرین تغییر رمز</summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>باید رمز را عوض کند</summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// هش رمز موقتِ بازیابیِ در انتظار — چرخهٔ ۸۴: رمز موقتِ ایمیل‌شده «جدا»
    /// از رمز اصلی نگه داشته می‌شود تا اگر کاربر نامه را نادیده گرفت، با
    /// همان رمز قبلی خودش وارد شود و رمز هیچ‌کس با اشتباهِ دیگری عوض نشود.
    /// فقط وقتی کاربر واقعاً با رمز موقت وارد شود، آن رمز جایگزین رمز اصلی
    /// می‌شود و تغییر رمز اجباری می‌گیرد.
    /// </summary>
    public string? PendingRecoveryPasswordHash { get; set; }

    /// <summary>انقضای رمز موقت بازیابی — پس از آن فقط رمز اصلی کار می‌کند</summary>
    public DateTime? PendingRecoveryExpiresAt { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>پرسنل مرتبط</summary>
    public Employee? Employee { get; set; }

    /// <summary>نقش‌های کاربر</summary>
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();

    /// <summary>توکن‌های تازه‌سازی</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>نشست‌های فعال</summary>
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    /// <summary>اعلان‌های کاربر</summary>
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
