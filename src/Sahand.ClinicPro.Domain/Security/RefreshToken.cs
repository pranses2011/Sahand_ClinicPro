namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// توکن تازه‌سازی - چرخه عمر توکن بدون ورود مجدد
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>مقدار توکن - یکتا و هش‌شده</summary>
    public string Token { get; set; } = null!;

    /// <summary>شناسه توکن JWT مرتبط</summary>
    public string? JwtId { get; set; }

    /// <summary>ابطال شده</summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>زمان ایجاد</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>زمان انقضا</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>اطلاعات دستگاه</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>نشانی IP</summary>
    public string? IpAddress { get; set; }

    /// <summary>کاربر صاحب توکن</summary>
    public User? User { get; set; }
}
