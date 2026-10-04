namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد مدیریت توکن — ذخیره و خواندن توکن‌های دسترسی و تازه‌سازی
/// </summary>
public interface ITokenManager
{
    /// <summary>توکن دسترسی فعلی — تهی یعنی ورود نشده</summary>
    string? AccessToken { get; }

    /// <summary>توکن تازه‌سازی فعلی</summary>
    string? RefreshToken { get; }

    /// <summary>زمان انقضای توکن دسترسی</summary>
    DateTime? ExpiresAt { get; }

    /// <summary>نام کاربری ذخیره‌شده</summary>
    string? Username { get; }

    /// <summary>آیا کاربر قبلاً وارد شده و توکن معتبر است</summary>
    bool HasValidToken { get; }

    /// <summary>ذخیره توکن‌های دریافتی از ورود</summary>
    void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username);

    /// <summary>پاک‌کردن توکن‌ها هنگام خروج</summary>
    void Clear();

    /// <summary>رویداد تغییر وضعیت ورود</summary>
    event EventHandler? AuthStateChanged;
}
