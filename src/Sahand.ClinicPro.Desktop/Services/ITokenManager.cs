namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد مدیریت توکن — ذخیره و خواندن توکن‌های دسترسی و تازه‌سازی
/// ═══ چرخهٔ سی‌وهشتم (گزارش مالک — سطح دسترسی و پروفایل کاربر) ═══
/// «فقط منوها و فرم‌هایی که کاربر دسترسی دارد نمایش داده شود» +
/// «نام کامل در صورت وجود وگرنه نام کاربری» + «تصویر کاربر»:
/// پاسخ ورود از سرور علاوه بر توکن‌ها، نام نمایشی و نقش‌ها و دسترسی‌ها را
/// هم دارد؛ از این پس همین قرارداد آن‌ها را کنار توکن نگه می‌دارد تا
/// پوستهٔ برنامه بتواند منو را فیلتر کند و نام کاربر را درست بنویسد —
/// پیش‌تر این داده‌ها در لحظهٔ ورود دور ریخته می‌شد و فقط نام کاربری
/// زنده می‌ماند.
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

    /// <summary>
    /// نام نمایشی کاربر (نام و نام خانوادگی پرسنل متصل) — تهی یعنی کاربر
    /// پرسنل متصل ندارد؛ مصرف‌کننده باید در این حالت به نام کاربری برگردد
    /// (قانون مالک چرخهٔ ۳۸: «نام کامل در صورت وجود، وگرنه نام کاربری»).
    /// </summary>
    string? DisplayName { get; }

    /// <summary>نقش‌های کاربر — نقش «Admin» همهٔ دسترسی‌ها را می‌گیرد</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// دسترسی‌های اتمی کاربر (مثل patients.view) — همان کلیدهایی که سرور
    /// خودش با HasPermission اجرا می‌کند؛ منوی دسکتاپ با همین فهرست فیلتر می‌شود.
    /// </summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>آیا کاربر نقش مدیر کل را دارد — دور زدن فیلتر منو</summary>
    bool IsAdmin { get; }

    /// <summary>آیا کاربر قبلاً وارد شده و توکن معتبر است</summary>
    bool HasValidToken { get; }

    /// <summary>
    /// ذخیرهٔ توکن‌های دریافتی از «ورود» — همراه نقش‌ها و دسترسی‌ها و نام نمایشی.
    /// </summary>
    void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username,
        string? displayName, IReadOnlyList<string>? roles, IReadOnlyList<string>? permissions);

    /// <summary>
    /// ذخیرهٔ توکن‌های تازه‌شده از «تمدید» — پاسخ تمدید نقش/دسترسی/نام ندارد؛
    /// مشخصات پروفایلِ ذخیره‌شدهٔ ورود قبلی دست‌نخورده می‌ماند.
    /// </summary>
    void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username);

    /// <summary>
    /// به‌روزرسانی پروفایل کاربر (نام نمایشی/نقش‌ها/دسترسی‌ها) بدون تغییر توکن —
    /// پس از دریافت پاسخ زندهٔ users/me توسط پوسته؛ توکن دسترسی هم‌ارز این
    /// داده‌هاست پس همگام نگه‌داشتنشان بی‌خطر و درست است.
    /// </summary>
    void UpdateProfile(string? displayName, IReadOnlyList<string>? roles, IReadOnlyList<string>? permissions);

    /// <summary>پاک‌کردن توکن‌ها هنگام خروج</summary>
    void Clear();

    /// <summary>رویداد تغییر وضعیت ورود</summary>
    event EventHandler? AuthStateChanged;
}
