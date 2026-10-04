using System.IO;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// مدیریت توکن — ذخیره در فایل JSON پوشه AppData درمانگاه
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

    /// <summary>ساختار ذخیره توکن</summary>
    private sealed class TokenStore
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>توکن دسترسی فعلی</summary>
    public string? AccessToken => _store?.AccessToken;

    /// <summary>توکن تازه‌سازی فعلی</summary>
    public string? RefreshToken => _store?.RefreshToken;

    /// <summary>زمان انقضای توکن دسترسی</summary>
    public DateTime? ExpiresAt => _store?.ExpiresAt;

    /// <summary>نام کاربری</summary>
    public string? Username => _store?.Username;

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

    /// <summary>ذخیره توکن‌های جدید</summary>
    public void SaveTokens(string accessToken, string refreshToken, int expiresIn, string username)
    {
        _store = new TokenStore
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
            Username = username
        };
        Persist();
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
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
