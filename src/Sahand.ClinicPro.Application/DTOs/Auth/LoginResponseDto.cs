namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// پاسخ موفق ورود — توکن‌ها و اطلاعات کاربر
/// </summary>
public class LoginResponseDto
{
    /// <summary>توکن دسترسی JWT — با هر درخواست در سرآیند Authorization ارسال می‌شود</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>توکن تازه‌سازی — برای تمدید بدون ورود مجدد</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>عمر توکن دسترسی به ثانیه</summary>
    public int ExpiresIn { get; set; }

    /// <summary>نام کاربری</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>نام نمایشی کاربر</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>نقش‌های کاربر</summary>
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    /// <summary>دسترسی‌های کاربر</summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();

    /// <summary>آیا تغییر رمز عبور الزامی است</summary>
    public bool MustChangePassword { get; set; }
}
