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

    /// <summary>شناسه شعبه جاری — با هر درخواست در هدر X-Branch-Id ارسال می‌شود (گام U17)</summary>
    public long? BranchId { get; set; }

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

    /// <summary>POST با بررسی پاکت استاندارد — برای فرمان‌ها</summary>
    public async Task<bool> PostAsync(string endpoint, object? body, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Post, endpoint, body, ct);

    /// <summary>PUT با بررسی پاکت استاندارد</summary>
    public async Task<bool> PutAsync(string endpoint, object? body, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Put, endpoint, body, ct);

    /// <summary>DELETE با بررسی پاکت استاندارد</summary>
    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Delete, endpoint, null, ct);

    /// <summary>ارسال فرمان و بررسی موفقیت از پاکت ApiResponse</summary>
    private async Task<bool> SendCommandAsync(HttpMethod method, string endpoint,
        object? body, CancellationToken ct)
    {
        LastError = null;
        try
        {
            using var request = new HttpRequestMessage(method, endpoint);
            if (body is not null)
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(_tokens.AccessToken))
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);

            using var response = await _http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
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
                return false;
            }

            // پاسخ موفق — بررسی پاکت استاندارد در صورت وجود
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("isSuccess", out var isSuccess))
                    return isSuccess.GetBoolean();
                return true;
            }
            catch
            {
                return true;
            }
        }
        catch (HttpRequestException)
        {
            LastError = "ارتباط با سرور برقرار نشد — اتصال شبکه را بررسی کنید";
            return false;
        }
        catch (TaskCanceledException)
        {
            LastError = "مهلت درخواست به پایان رسید";
            return false;
        }
    }

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
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
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
        if (BranchId is { } bid)
            request.Headers.Add("X-Branch-Id", bid.ToString());
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
