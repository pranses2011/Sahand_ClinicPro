# -*- coding: utf-8 -*-
"""گام ۱۰ — راه‌اندازی WPF: سرویس‌های کلاینت + App.xaml کامل + MainWindow با ناوبری"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
DESK = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Desktop"))


def write(rel, content):
    full = os.path.join(DESK, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ تنظیمات ═══════════
write("appsettings.json", """
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000/api/v1",
    "TimeoutSeconds": 30
  }
}
""")

# ═══════════ مدیریت توکن ═══════════
write("Services/ITokenManager.cs", """
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
""")

write("Services/TokenManager.cs", """
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
""")

# ═══════════ کلاینت API ═══════════
write("Services/IApiClient.cs", """
namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد کلاینت API — فراخوانی REST با مدیریت خودکار توکن
/// </summary>
public interface IApiClient
{
    /// <summary>درخواست GET عمومی</summary>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);

    /// <summary>درخواست POST عمومی</summary>
    Task<T?> PostAsync<T>(string endpoint, object? body, CancellationToken ct = default);

    /// <summary>درخواست PUT عمومی</summary>
    Task<T?> PutAsync<T>(string endpoint, object? body, CancellationToken ct = default);

    /// <summary>درخواست DELETE عمومی</summary>
    Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default);

    /// <summary>درخواست GET بدون احراز هویت</summary>
    Task<T?> GetAnonymousAsync<T>(string endpoint, CancellationToken ct = default);

    /// <summary>ورود و دریافت توکن‌ها</summary>
    Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default);

    /// <summary>خروج و ابطال توکن</summary>
    Task LogoutAsync();

    /// <summary>آخرین پیام خطای فارسی</summary>
    string? LastError { get; }

    /// <summary>رویداد نیاز به ورود مجدد — ناوبری به صفحه ورود</summary>
    event EventHandler? SessionExpired;
}
""")

write("Services/ApiClient.cs", """
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// کلاینت REST سامانه — پیوست خودکار توکن و تمدید خودکار در پاسخ ۴۰۱
/// الگو: در خطای ۴۰۱ یک‌بار توکن تمدید و درخواست تکرار می‌شود؛
/// در صورت شکست دوباره SessionExpired انتشار می‌یابد.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenManager _tokens;

    /// <summary>گزینه‌های سریالایز — نام‌گذاری camelCase مطابق API</summary>
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>آخرین پیام خطای فارسی</summary>
    public string? LastError { get; private set; }

    /// <summary>رویداد انقضای نشست</summary>
    public event EventHandler? SessionExpired;

    /// <summary>سازنده با تزریق کارخانه HttpClient و مدیریت توکن</summary>
    public ApiClient(HttpClient http, ITokenManager tokens, IConfiguration configuration)
    {
        _http = http;
        _tokens = tokens;
        _http.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]
            ?? "http://localhost:5000/api/v1");
        _http.Timeout = TimeSpan.FromSeconds(
            configuration.GetValue("ApiSettings:TimeoutSeconds", 30));
    }

    /// <summary>GET با توکن</summary>
    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, endpoint, null, attachToken: true, ct);

    /// <summary>POST با توکن</summary>
    public async Task<T?> PostAsync<T>(string endpoint, object? body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Post, endpoint, body, attachToken: true, ct);

    /// <summary>PUT با توکن</summary>
    public async Task<T?> PutAsync<T>(string endpoint, object? body, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Put, endpoint, body, attachToken: true, ct);

    /// <summary>DELETE با توکن</summary>
    public async Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Delete, endpoint, null, attachToken: true, ct);

    /// <summary>GET بدون احراز هویت</summary>
    public async Task<T?> GetAnonymousAsync<T>(string endpoint, CancellationToken ct = default)
        => await SendAsync<T>(HttpMethod.Get, endpoint, null, attachToken: false, ct);

    /// <summary>ورود به سامانه و ذخیره توکن‌ها</summary>
    public async Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        LastError = null;
        var payload = new { usernameOrEmail, password };
        var response = await SendInternalAsync<LoginResult>("auth/login", payload, attachToken: false, ct);

        if (response is null)
            return false;

        _tokens.SaveTokens(response.AccessToken, response.RefreshToken,
            response.ExpiresIn, response.Username);
        return true;
    }

    /// <summary>خروج و پاک‌کردن توکن‌ها</summary>
    public async Task LogoutAsync()
    {
        try
        {
            var refresh = _tokens.RefreshToken;
            if (!string.IsNullOrEmpty(refresh))
                await SendInternalAsync<object>("auth/logout",
                    new { refreshToken = refresh }, attachToken: true, default);
        }
        catch
        {
            // خطای شبکه هنگام خروج نادیده گرفته می‌شود
        }
        finally
        {
            _tokens.Clear();
        }
    }

    /// <summary>ارسال با مدیریت تمدید خودکار توکن روی ۴۰۱</summary>
    private async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object? body,
        bool attachToken, CancellationToken ct, bool retried = false)
    {
        var response = await SendInternalAsync<T>(endpoint, body, attachToken, ct, method);

        if (response is ApiErrorResponse errorResponse)
        {
            LastError = errorResponse.Message;
        }
        return response;
    }

    /// <summary>ارسال خام درخواست و ساخت آبجکت پاسخ</summary>
    private async Task<T?> SendInternalAsync<T>(string endpoint, object? body,
        bool attachToken, CancellationToken ct, HttpMethod? method = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method ?? HttpMethod.Post, endpoint);

            if (body is not null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, Json),
                    Encoding.UTF8, "application/json");
            }

            if (attachToken && !string.IsNullOrEmpty(_tokens.AccessToken))
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);

            using var response = await _http.SendAsync(request, ct);

            // تمدید خودکار توکن در خطای ۴۰۱ — فقط یک‌بار
            if (response.StatusCode == HttpStatusCode.Unauthorized
                && attachToken && !string.IsNullOrEmpty(_tokens.RefreshToken))
            {
                var refreshed = await TryRefreshTokenAsync(ct);
                if (refreshed)
                {
                    // تکرار همان درخواست با توکن جدید
                    using var retry = await SendRawAsync(endpoint, body, attachToken: true, method ?? HttpMethod.Post, ct);
                    return await ReadResponseAsync<T>(retry, ct);
                }

                _tokens.Clear();
                SessionExpired?.Invoke(this, EventArgs.Empty);
            }

            return await ReadResponseAsync<T>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            LastError = "ارتباط با سرور برقرار نشد — اتصال شبکه را بررسی کنید";
            System.Diagnostics.Debug.WriteLine($"خطای API: {ex.Message}");
            return default;
        }
        catch (TaskCanceledException)
        {
            LastError = "مهلت درخواست به پایان رسید";
            return default;
        }
    }

    /// <summary>ارسال خام بدون تفسیر پاسخ</summary>
    private async Task<HttpResponseMessage> SendRawAsync(string endpoint, object? body,
        bool attachToken, HttpMethod method, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        if (attachToken && !string.IsNullOrEmpty(_tokens.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
        return await _http.SendAsync(request, ct);
    }

    /// <summary>خواندن و نگاشت پاسخ به نوع هدف</summary>
    private async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // استخراج پیام فارسی از پاسخ استاندارد خطا
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    LastError = message.GetString();
            }
            catch
            {
                LastError = $"خطای سرور — کد {(int)response.StatusCode}";
            }
            return default;
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, Json);
        }
        catch (JsonException)
        {
            LastError = "پاسخ سرور قابل تفسیر نبود";
            return default;
        }
    }

    /// <summary>تلاش برای تمدید توکن با توکن تازه‌سازی</summary>
    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        try
        {
            using var response = await _http.PostAsync("auth/refresh",
                new StringContent(
                    JsonSerializer.Serialize(new { refreshToken = _tokens.RefreshToken }),
                    Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<LoginResult>(json, Json);
            if (result is null) return false;

            _tokens.SaveTokens(result.AccessToken, result.RefreshToken,
                result.ExpiresIn, _tokens.Username ?? string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>ساختار پاسخ ورود</summary>
    private sealed record LoginResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string Username);
}
""")

write("Services/ApiErrorResponse.cs", """
namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// پاسخ خطای استاندارد API — برای تشخیص پیام فارسی سرور
/// </summary>
public record ApiErrorResponse(bool IsSuccess, string Message, int StatusCode);
""")

# ═══════════ سرویس ناوبری ═══════════
write("Services/INavigationService.cs", """
namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد ناوبری بین صفحات داخل پنجره اصلی
/// </summary>
public interface INavigationService
{
    /// <summary>رفتن به صفحه مقصد با پارامتر اختیاری</summary>
    void NavigateTo(string pageKey, object? parameter = null);

    /// <summary>رویداد درخواست ناوبری</summary>
    event EventHandler<NavigationEventArgs> NavigationRequested;
}

/// <summary>
/// آرگومان رویداد ناوبری
/// </summary>
public record NavigationEventArgs(string PageKey, object? Parameter);
""")

write("Services/NavigationService.cs", """
namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس ناوبری — انتشار رویداد برای پوسته اصلی
/// </summary>
public class NavigationService : INavigationService
{
    /// <summary>رویداد درخواست ناوبری</summary>
    public event EventHandler<NavigationEventArgs>? NavigationRequested;

    /// <summary>درخواست رفتن به صفحه</summary>
    public void NavigateTo(string pageKey, object? parameter = null)
    {
        NavigationRequested?.Invoke(this, new NavigationEventArgs(pageKey, parameter));
    }
}
""")

# ═══════════ سرویس تم ═══════════
write("Services/IThemeService.cs", """
namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// قرارداد سرویس تم — تغییر تم روشن و تیره
/// </summary>
public interface IThemeService
{
    /// <summary>تم فعلی تیره است؟</summary>
    bool IsDarkTheme { get; }

    /// <summary>تغییر تم</summary>
    void ToggleTheme();
}
""")

write("Services/ThemeService.cs", """
using MaterialDesignThemes.Wpf;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// سرویس تم — تغییر پویا بین روشن و تیره با PaletteHelper
/// </summary>
public class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new();
    private bool _isDark;

    /// <summary>تم فعلی تیره است؟</summary>
    public bool IsDarkTheme => _isDark;

    /// <summary>تغییر تم روشن و تیره</summary>
    public void ToggleTheme()
    {
        _isDark = !_isDark;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(_isDark ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }
}
""")

print("✅ گام ۱۰ — بخش ۱ (سرویس‌ها) کامل شد")
