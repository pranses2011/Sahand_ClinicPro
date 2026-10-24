using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// سرویس احراز هویت — ورود، تمدید توکن، تغییر رمز و کنترل دسترسی
/// </summary>
public interface IIdentityService
{
    /// <summary>احراز هویت با نام کاربری و رمز عبور — قفل ۳۰ دقیقه‌ای بعد از ۵ تلاش ناموفق</summary>
    Task<AuthResult> AuthenticateAsync(string usernameOrEmail, string password,
        string? ipAddress = null, string? deviceInfo = null, CancellationToken cancellationToken = default);

    /// <summary>تمدید توکن دسترسی با توکن تامه‌سازی معتبر — چرخش توکن قدیمی</summary>
    Task<RefreshResult> RefreshTokenAsync(string refreshToken,
        string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>خروج و ابطال توکن تازه‌سازی</summary>
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>تغییر رمز عبور با بررسی رمز فعلی و تاریخچه پنج رمز اخیر</summary>
    Task<Result> ChangePasswordAsync(long userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// بازیابی رمز عبور با ایمیل — چرخهٔ ۸۲ (گزارش مالک: «فراموشی رمز کار
    /// نمی‌کند؛ رمز فعلی به ایمیل کاربر ارسال شود»).
    /// </summary>
    /// <remarks>
    /// رمزها فقط به‌صورت هش BCrypt ذخیره می‌شوند و «رمز فعلی» قابل بازیابی نیست
    /// (خودِ همین ویژگی، امنیت حساب‌ها را حفظ می‌کند). بنابراین جریان درست و
    /// صنعتی این است: تولید «رمز موقت» تازه، ارسال آن به ایمیل ثبت‌شدهٔ کاربر،
    /// و اجبار به تغییر رمز در نخستین ورود بعدی (MustChangePassword).
    /// اگر ایمیل ارسال نشود، هیچ تغییری در پایگاه داده انجام نمی‌شود تا کاربر
    /// با رمز قبلی خود قفل نشود.
    /// </remarks>
    /// <param name="usernameOrEmail">نام کاربری یا ایمیل ثبت‌شده</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task<Result> SendPasswordRecoveryAsync(string usernameOrEmail,
        CancellationToken cancellationToken = default);

    /// <summary>گرفتن دسترسی‌های کاربر — از نقش‌های او</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>گرفتن نقش‌های کاربر</summary>
    Task<IReadOnlyList<string>> GetRolesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>آیا کاربر دسترسی مشخصی دارد</summary>
    Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken cancellationToken = default);
}
