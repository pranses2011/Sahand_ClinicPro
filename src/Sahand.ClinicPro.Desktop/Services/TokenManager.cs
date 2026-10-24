using System.IO;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// مدیریت توکن — ذخیره در فایل JSON پوشه AppData درمانگاه
/// ═══ چرخهٔ سی‌وهشتم ═══ نزد پاسخ ورود، «نام نمایشی + نقش‌ها + دسترسی‌ها»
/// هم ذخیره می‌شود (پیش‌تر دور ریخته می‌شد) تا فیلتر منو بر اساس سطح
/// دسترسی و نمایش نام کامل/تصویر کاربر ممکن شود؛ «تمدید توکن» پروفایل
/// قبلی را حفظ می‌کند و users/me می‌تواند پروفایل را زنده به‌روز کند.
/// </summary>
/// <remarks>
/// در محیط تولید توصیه می‌شود از DPAPI ویندوز برای رمزنگاری فایل استفاده شود —
/// بهبود در گام ۲۹ (امنیت‌پردازی) اعمال می‌شود.
/// </remarks>
public class TokenManager : ITokenManager
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SahandClinicPro");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "tokens.json");

    private TokenStore? _store;

    /// <summary>ساختار ذخیره توکن و پروفایل</summary>
    private sealed class TokenStore
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;

        // ═══ چرخهٔ سی‌وهشتم — پروفایل کاربر کنار توکن ═══
        /// <summary>نام نمایشی (نام کامل پرسنل) — تهی = بدون پرسنل متصل</summary>
        public string? DisplayName { get; set; }

        /// <summary>نقش‌ها — «Admin» همهٔ دسترسی‌ها را می‌گیرد</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>دسترسی‌های اتمی مثل patients.view</summary>
        public List<string> Permissions { get; set; } = new();
    }

    /// <summary>توکن دسترسی فعلی</summary>
    public string? AccessToken => _store?.AccessToken;

    /// <summary>توکن تازه‌سازی فعلی</summary>
    public string? RefreshToken => _store?.RefreshToken;

    /// <summary>زمان انقضای توکن دسترسی</summary>
    public DateTime? ExpiresAt => _store?.ExpiresAt;

    /// <summary>نام کاربری</summary>
    public string? Username => _store?.Username;

    /// <summary>نام نمایشی کاربر — تهی یعنی پرسنل متصل ندارد (نمایش = نام کاربری)</summary>
    public string? DisplayName => _store?.DisplayName;

    /// <summary>نقش‌های کاربر</summary>
    public IReadOnlyList<string> Roles => _store?.Roles ?? (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>دسترسی‌های اتمی کاربر — همان کلیدهای HasPermission سرور</summary>
    public IReadOnlyList<string> Permissions => _store?.Permissions ?? (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>آیا نقش مدیر کل را دارد — فیلتر منو برای او بی‌معناست (همه‌چیز مجاز)</summary>
    public bool IsAdmin => _store?.Roles.Contains(Sahand.ClinicPro.Domain.Entities.Security.SystemRoles.Admin, StringComparer.OrdinalIgnoreCase) == true;

    /// <summary>آیا توکن معتبر موجود است</summary>
    public bool HasValidToken =>
        _store is not null
        && !string.IsNullOrEmpty(_store.AccessToken)
        && _store.ExpiresAt > DateTime.UtcNow.AddMinutes(1);

    /// <summary>رویداد تغییر وضعیت ورود</summary>
    public event EventHandler? AuthStateChanged;

    /// <summary>سازنده — بارگذاری توکن ذخیره‌شده</summary>
    public TokenManager()
    {
        Load();
    }

    /// <summary>
    /// ذخیرهٔ توکن‌های «ورود» همراه نقش‌ها/دسترسی‌ها/نام نمایشی — چرخهٔ ۳۸:
    /// پاسخ لاگین سرور این داده‌ها را دارد و از این پس زنده نگه داشته می‌شود.
    /// </summary>
    public void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username,
        string? displayName, IReadOnlyList<string>? roles, IReadOnlyList<string>? permissions)
    {
        _store = new TokenStore
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            Roles = roles?.ToList() ?? new List<string>(),
            Permissions = permissions?.ToList() ?? new List<string>(),
        };
        Persist();
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// ذخیرهٔ توکن‌های «تمدید» — پاسخ تمدید نقش/دسترسی/نام ندارد؛ مشخصات
    /// پروفایلِ ذخیره‌شده از ورود قبلی حفظ می‌شود (جابه‌جایی ناخواستهٔ
    /// نام کاربر در پوسته پس از هر تمدیدِ خودکار نداریم).
    /// </summary>
    public void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username)
    {
        var previous = _store;
        _store = new TokenStore
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
            Username = username,
            // حفظ پروفایل قبلی — توکن تازه هم‌ارز همان نشست است
            DisplayName = previous?.DisplayName,
            Roles = previous?.Roles ?? new List<string>(),
            Permissions = previous?.Permissions ?? new List<string>(),
        };
        Persist();
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// به‌روزرسانی پروفایل بدون تغییر توکن — پس از پاسخ زندهٔ users/me
    /// (چرخهٔ ۳۸): اگر نقش‌های کاربر در میانهٔ نشست عوض شده باشد، منو
    /// بی‌درنگ با دسترسی‌های واقعی هم‌قدم می‌شود.
    /// </summary>
    public void UpdateProfile(string? displayName, IReadOnlyList<string>? roles, IReadOnlyList<string>? permissions)
    {
        if (_store is null) return; // نشستی نیست — داده روی چه چیزی بنشیند؟
        if (displayName is not null)
            _store.DisplayName = displayName.Trim();
        if (roles is not null)
            _store.Roles = roles.ToList();
        if (permissions is not null)
            _store.Permissions = permissions.ToList();
        Persist();
    }

    /// <summary>پاک‌کردن توکن‌ها</summary>
    public void Clear()
    {
        _store = null;
        try
        {
            if (File.Exists(SettingsFile)) File.Delete(SettingsFile);
        }
        catch
        {
            // نادیده گرفتن خطای حذف — بارگذاری بعدی خواهد بود
        }
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>بارگذاری از فایل</summary>
    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return;
            var json = File.ReadAllText(SettingsFile);
            _store = JsonSerializer.Deserialize<TokenStore>(json);
        }
        catch
        {
            _store = null;
        }
    }

    /// <summary>نوشتن در فایل</summary>
    private void Persist()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(_store,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // نبود دسترسی نوشتن نباید برنامه را متوقف کند
        }
    }
}
