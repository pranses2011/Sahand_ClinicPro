using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Common.Models;

/// <summary>
/// نتیجه احراز هویت — توکن‌های دسترسی و تازه‌سازی همراه اطلاعات کاربر
/// </summary>
public sealed class AuthResult
{
    /// <summary>آیا احراز هویت موفق بود</summary>
    public bool Succeeded { get; init; }

    /// <summary>پیام خطا در صورت شکست</summary>
    public string? Error { get; init; }

    /// <summary>توکن دسترسی JWT</summary>
    public string? AccessToken { get; init; }

    /// <summary>توکن تازه‌سازی</summary>
    public string? RefreshToken { get; init; }

    /// <summary>زمان انقضای توکن دسترسی به ثانیه</summary>
    public int ExpiresIn { get; init; }

    /// <summary>نام کاربری</summary>
    public string? Username { get; init; }

    /// <summary>نام نمایشی کاربر</summary>
    public string? DisplayName { get; init; }

    /// <summary>نقش‌های کاربر</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>دسترسی‌های کاربر</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

    /// <summary>آیا تغییر رمز عبور الزامی است</summary>
    public bool MustChangePassword { get; init; }

    /// <summary>ساخت نتیجه موفق</summary>
    public static AuthResult Success(string accessToken, string refreshToken, int expiresIn,
        string username, string displayName, IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions, bool mustChangePassword) => new()
    {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = expiresIn,
        Username = username,
        DisplayName = displayName,
        Roles = roles,
        Permissions = permissions,
        MustChangePassword = mustChangePassword
    };

    /// <summary>ساخت نتیجه ناموفق</summary>
    public static AuthResult Failure(string error) => new() { Succeeded = false, Error = error };
}
