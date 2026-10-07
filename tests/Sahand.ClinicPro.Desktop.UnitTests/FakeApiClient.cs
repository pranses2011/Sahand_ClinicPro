using System.Text.Json;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// کلاینت API ساختگی برای تست ViewModelها — چرخه دوازدهم (C1):
/// فراخوانی‌ها ضبط می‌شوند و پاسخ‌های آماده برگردانده می‌شوند؛
/// بدون شبکه، بدون سرور، بدون توکن.
/// </summary>
public sealed class FakeApiClient : IApiClient
{
    /// <summary>متریک فراخوانی‌های ثبت‌شده: (متد، endpoint)</summary>
    public List<(string Method, string Endpoint)> Calls { get; } = new();

    /// <summary>بدنه آخرین فراخوانی POST/PUT</summary>
    public object? LastBody { get; private set; }

    /// <summary>پاسخ‌های آماده — کلید: endpoint، مقدار: شیء سریال‌شونده</summary>
    public Dictionary<string, object> CannedResponses { get; } = new();

    /// <summary>پاسخ بولی آماده برای فراخوانی‌های بدون داده خروجی</summary>
    public bool NextBoolResult { get; set; } = true;

    /// <summary>خطای تنظیم‌شده برای LastError</summary>
    public string? FakeError { get; set; }

    /// <summary>پاسخ آماده LoginAsync — پیش‌فرض موفق</summary>
    public bool FakeLoginSucceeds { get; set; } = true;

    /// <summary>پرچم آماده — آیا ورود نیازمند تغییر رمز است (الزام سرور)</summary>
    public bool FakeMustChangePassword { get; set; }

    /// <summary>
    /// آخرین جفت (نام کاربری، رمز) ارسالی به LoginAsync — چرخه هفدهم (گزارش یازدهم):
    /// برای گارد تستیِ پاک‌سازی نویسه‌های نامرئی جهت پیش از ارسال به سرور
    /// </summary>
    public (string Username, string Password)? LastLoginCall { get; private set; }

    /// <summary>آخرین پیام خطای فارسی</summary>
    public string? LastError => FakeError;

    /// <summary>پرچم آخرین ورود — در نسخه ساختگی با تنظیم دستی</summary>
    public bool LastLoginMustChangePassword => FakeMustChangePassword;

    /// <summary>رویداد نیاز به ورود مجدد</summary>
    public event EventHandler? SessionExpired;

    /// <summary>شلیک دستی رویداد انقضای نشست (برای تست‌ها)</summary>
    public void RaiseSessionExpired() => SessionExpired?.Invoke(this, EventArgs.Empty);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private T? Resolve<T>(string endpoint)
    {
        if (CannedResponses.TryGetValue(endpoint, out var value))
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        return default;
    }

    /// <summary>GET عمومی</summary>
    public Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        Calls.Add(("GET", endpoint));
        return Task.FromResult(Resolve<T>(endpoint));
    }

    /// <summary>POST عمومی</summary>
    public Task<T?> PostAsync<T>(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("POST", endpoint));
        LastBody = body;
        return Task.FromResult(Resolve<T>(endpoint));
    }

    /// <summary>PUT عمومی</summary>
    public Task<T?> PutAsync<T>(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("PUT", endpoint));
        LastBody = body;
        return Task.FromResult(Resolve<T>(endpoint));
    }

    /// <summary>DELETE عمومی</summary>
    public Task<T?> DeleteAsync<T>(string endpoint, CancellationToken ct = default)
    {
        Calls.Add(("DELETE", endpoint));
        return Task.FromResult(Resolve<T>(endpoint));
    }

    /// <summary>GET بدون احراز هویت</summary>
    public Task<T?> GetAnonymousAsync<T>(string endpoint, CancellationToken ct = default)
    {
        Calls.Add(("GET-ANON", endpoint));
        return Task.FromResult(Resolve<T>(endpoint));
    }

    /// <summary>POST با نتیجه بولی</summary>
    public Task<bool> PostAsync(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("POST-bool", endpoint));
        LastBody = body;
        return Task.FromResult(NextBoolResult);
    }

    /// <summary>POST ناشناس با نتیجه بولی — چرخه هجدهم (فعال‌سازی لایسنس پیش از ورود)</summary>
    public Task<bool> PostAnonymousAsync(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("POST-ANON-bool", endpoint));
        LastBody = body;
        return Task.FromResult(NextBoolResult);
    }

    /// <summary>PUT با نتیجه بولی</summary>
    public Task<bool> PutAsync(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("PUT-bool", endpoint));
        LastBody = body;
        return Task.FromResult(NextBoolResult);
    }

    /// <summary>PUT ناشناس با نتیجه بولی — چرخه هجدهم (تنظیمات سرور لایسنس پیش از ورود)</summary>
    public Task<bool> PutAnonymousAsync(string endpoint, object? body, CancellationToken ct = default)
    {
        Calls.Add(("PUT-ANON-bool", endpoint));
        LastBody = body;
        return Task.FromResult(NextBoolResult);
    }

    /// <summary>DELETE با نتیجه بولی</summary>
    public Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        Calls.Add(("DELETE-bool", endpoint));
        return Task.FromResult(NextBoolResult);
    }

    /// <summary>آپلود فایل — در تست استفاده نمی‌شود</summary>
    public Task<T?> PostFileAsync<T>(string endpoint, string fileName, byte[] content,
        Dictionary<string, string>? fields = null, CancellationToken ct = default)
        => throw new NotSupportedException("آپلود در تست ViewModel پشتیبانی نمی‌شود");

    /// <summary>دانلود فایل — در تست استفاده نمی‌شود</summary>
    public Task<byte[]?> DownloadFileAsync(string endpoint, CancellationToken ct = default)
        => throw new NotSupportedException("دانلود در تست ViewModel پشتیبانی نمی‌شود");

    /// <summary>دانلود POST — خروجی PDF گزارش‌ها (چرخه ۱۳، D6) — در تست استفاده نمی‌شود</summary>
    public Task<byte[]?> PostDownloadAsync(string endpoint, object? body, CancellationToken ct = default)
        => throw new NotSupportedException("دانلود POST در تست ViewModel پشتیبانی نمی‌شود");

    /// <summary>ورود</summary>
    public Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        LastLoginCall = (usernameOrEmail, password);
        return Task.FromResult(FakeLoginSucceeds);
    }

    /// <summary>خروج</summary>
    public Task LogoutAsync() => Task.CompletedTask;
}
