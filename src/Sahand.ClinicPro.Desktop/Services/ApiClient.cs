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

    /// <summary>پرچم آخرین ورود — سرور تغییر رمز عبور را الزامی کرده است (چرخه چهاردهم، گزارش پنجم مالک)</summary>
    public bool LastLoginMustChangePassword { get; private set; }

    /// <summary>شناسه شعبه جاری — با هر درخواست در هدر X-Branch-Id ارسال می‌شود (گام U17)</summary>
    public long? BranchId { get; set; }

    /// <summary>نشانی دقیق سرور برای جداسازی بازیابی‌های محلی.</summary>
    public string PaymentRecoveryServer => _http.BaseAddress?.AbsoluteUri ?? throw new InvalidOperationException("نشانی سرور پرداخت مشخص نیست.");

    /// <summary>رویداد انقضای نشست</summary>
    public event EventHandler? SessionExpired;

    /// <summary>سازنده با تزریق کارخانه HttpClient و مدیریت توکن</summary>
    public ApiClient(HttpClient http, ITokenManager tokens, IConfiguration configuration)
    {
        _http = http;
        _tokens = tokens;
        // چرخه چهاردهم (گزارش پنجم مالک): آدرس پایه باید به اسلش پایانی ختم شود —
        // بدون آن، ترکیب مسیرهای نسبی طبق RFC 3986 قطعه «v1» را می‌اندازد و
        // همه درخواست‌ها به api/… (بدون v1) می‌رسیدند و 404 با بدنه خالی
        // می‌گرفتند؛ نرمال‌سازی در ApiHttp متمرکز شده و با تست گارد می‌شود.
        _http.BaseAddress = new Uri(ApiHttp.NormalizeBaseUrl(
            configuration["ApiSettings:BaseUrl"]));
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

    /// <summary>
    /// POST ناشناس بدون توکن — چرخه هجدهم (گزارش دوازدهم):
    /// فرمان‌های لایسنس (فعال‌سازی آنلاین/آفلاین) پیش از ورود هم کار کنند
    /// </summary>
    public async Task<bool> PostAnonymousAsync(string endpoint, object? body, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Post, endpoint, body, ct, attachToken: false);

    /// <summary>
    /// PUT ناشناس بدون توکن — چرخه هجدهم: ذخیرهٔ نشانی سرور لایسنس
    /// از پنجرهٔ فعال‌سازی، پیش از ورود کاربر
    /// </summary>
    public async Task<bool> PutAnonymousAsync(string endpoint, object? body, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Put, endpoint, body, ct, attachToken: false);

    /// <summary>PUT با بررسی پاکت استاندارد</summary>
    public async Task<bool> PutAsync(string endpoint, object? body, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Put, endpoint, body, ct);

    /// <summary>DELETE با بررسی پاکت استاندارد</summary>
    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
        => await SendCommandAsync(HttpMethod.Delete, endpoint, null, ct);

    /// <summary>
    /// آپلود فایل چندبخشی (multipart) با توکن — گام R4 برای فایل‌های DICOM
    /// </summary>
    /// <param name="endpoint">مسیر endpoint</param>
    /// <param name="fileName">نام نمایشی فایل</param>
    /// <param name="content">محتوای باینری</param>
    /// <param name="fields">فیلدهای همراه فرم (مثل orderId)</param>
    /// <param name="ct">توکن لغو</param>
    public async Task<T?> PostFileAsync<T>(string endpoint, string fileName, byte[] content,
        Dictionary<string, string>? fields = null, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            using var form = new MultipartFormDataContent();

            if (fields is not null)
                foreach (var (key, value) in fields)
                    form.Add(new StringContent(value), key);

            var fileContent = new ByteArrayContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileName);
            request.Content = form;

            if (!string.IsNullOrEmpty(_tokens.AccessToken))
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);

            using var response = await _http.SendAsync(request, ct);
            return await ReadResponseAsync<T>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            LastError = "ارتباط با سرور برقرار نشد — اتصال شبکه را بررسی کنید";
            System.Diagnostics.Debug.WriteLine($"خطای آپلود: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// دانلود بایت‌های خام فایل — گام R4 برای گشودن فایل DICOM در نمایشگر
    /// </summary>
    public async Task<byte[]?> DownloadFileAsync(string endpoint, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrEmpty(_tokens.AccessToken))
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                LastError = $"دریافت فایل ناموفق بود ({(int)response.StatusCode})";
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            LastError = "ارتباط با سرور برقرار نشد";
            System.Diagnostics.Debug.WriteLine($"خطای دانلود: {ex.Message}");
            return null;
        }
    }

    /// <summary>درخواست POST با بدنه JSON و دریافت بایت‌های خام — خروجی PDF گزارش‌ها (چرخه ۱۳، D6)</summary>
    public async Task<byte[]?> PostDownloadAsync(string endpoint, object? body, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (body is not null)
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(_tokens.AccessToken))
            {
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
            }

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                LastError = $"دریافت فایل ناموفق بود ({(int)response.StatusCode})";
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            LastError = "ارتباط با سرور برقرار نشد";
            System.Diagnostics.Debug.WriteLine($"خطای دانلود POST: {ex.Message}");
            return null;
        }
    }

    /// <summary>ارسال فرمان و بررسی موفقیت از پاکت ApiResponse</summary>
    private async Task<bool> SendCommandAsync(HttpMethod method, string endpoint,
        object? body, CancellationToken ct, bool attachToken = true)
    {
        LastError = null;
        try
        {
            using var request = new HttpRequestMessage(method, endpoint);
            if (body is not null)
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
            if (attachToken && !string.IsNullOrEmpty(_tokens.AccessToken))
            {
                if (BranchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
            }

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

    /// <summary>
    /// ورود به سامانه و ذخیره توکن‌ها — چرخه چهاردهم (گزارش پنجم مالک):
    /// پاسخ سرور در پاکت استاندارد ApiResponse (isSuccess/message/data) پیچیده
    /// می‌شود؛ پیش‌تر بدنه مستقیم به LoginResult نگاشت می‌شد و توکن‌ها همیشه
    /// null می‌شدند و ورود به‌ظاهر موفق، عملاً بی‌توکن بود. پرچم الزام تغییر
    /// رمز هم از همین پاسخ استخراج و در LastLoginMustChangePassword نگه می‌شود.
    /// </summary>
    public async Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        LastError = null;
        LastLoginMustChangePassword = false;

        try
        {
            using var response = await SendRawAsync("auth/login",
                new { usernameOrEmail, password }, attachToken: false, HttpMethod.Post, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = ApiHttp.ParseEnvelope<object>(json);
                LastError = err?.Message ?? $"خطای سرور — کد {(int)response.StatusCode}";
                return false;
            }

            var envelope = ApiHttp.ParseEnvelope<LoginResult>(json);
            if (envelope is not { IsSuccess: true } || envelope.Data is null)
            {
                LastError = envelope?.Message ?? "ورود ناموفق بود";
                return false;
            }

            LastLoginMustChangePassword = envelope.Data.MustChangePassword;
            // ═══ چرخهٔ سی‌وهشتم (سطح دسترسی و پروفایل کاربر) ═══
            // پاسخ لاگین سرور علاوه بر توکن‌ها «نام نمایشی + نقش‌ها + دسترسی‌ها»
            // را هم دارد؛ از این پس همراه توکن ذخیره می‌شود تا منوی پوسته بر
            // اساس دسترسی فیلتر شود و نام کامل کاربر نمایش یابد (پیش‌تر دور
            // ریخته می‌شد و فقط نام کاربری زنده می‌ماند).
            _tokens.SaveTokens(envelope.Data.AccessToken, envelope.Data.RefreshToken,
                envelope.Data.ExpiresIn, envelope.Data.Username,
                envelope.Data.DisplayName, envelope.Data.Roles, envelope.Data.Permissions);
            return true;
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
        var branchId = BranchId;
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
            {
                if (branchId is { } bid)
                    request.Headers.Add("X-Branch-Id", bid.ToString());
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
            }

            using var response = await _http.SendAsync(request, ct);

            // تمدید خودکار توکن در خطای ۴۰۱ — فقط یک‌بار
            if (response.StatusCode == HttpStatusCode.Unauthorized
                && attachToken && !string.IsNullOrEmpty(_tokens.RefreshToken))
            {
                var refreshed = await TryRefreshTokenAsync(ct);
                if (refreshed)
                {
                    // تکرار همان درخواست با توکن جدید
                    using var retry = await SendRawAsync(endpoint, body, attachToken: true, method ?? HttpMethod.Post, ct, branchId, useCapturedBranch: true);
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
        bool attachToken, HttpMethod method, CancellationToken ct, long? capturedBranch = null, bool useCapturedBranch = false)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        if (attachToken && !string.IsNullOrEmpty(_tokens.AccessToken))
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
        if ((useCapturedBranch ? capturedBranch : BranchId) is { } bid)
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

                var message = doc.RootElement.TryGetProperty("message", out var m)
                    && !string.IsNullOrWhiteSpace(m.GetString())
                        ? m.GetString()
                        : $"خطای سرور — کد {(int)response.StatusCode}";

                // گزارش هفتم مالک: آرایه errors پاکت استاندارد نادیده گرفته می‌شد و
                // کاربر فقط پیام عمومی («اطلاعات ارسالی نامعتبر است») می‌دید بدون
                // ریشهٔ واقعی (مثل «تکرار رمز عبور الزامی است»)؛ نخستین خطای
                // جزئی به پیام می‌چسبد تا دلیل ردشدن درخواست دیده شود
                if (doc.RootElement.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errors.EnumerateArray())
                    {
                        var detail = item.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                        {
                            message = $"{message} — {detail}";
                            break; // فقط نخستین خطا — از شلوغی پیام جلوگیری می‌شود
                        }
                    }
                }

                LastError = message;
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

            // چرخه چهاردهم: تمدید هم مثل ورود در پاکت ApiResponse پیچیده می‌شود —
            // توکن‌های تازه از فیلد data خوانده می‌شوند نه از ریشه بدنه
            var envelope = ApiHttp.ParseEnvelope<LoginResult>(json);
            if (envelope is not { IsSuccess: true } || envelope.Data is null) return false;

            _tokens.SaveTokens(envelope.Data.AccessToken, envelope.Data.RefreshToken,
                envelope.Data.ExpiresIn, _tokens.Username ?? string.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ساختار داده ورود — هم‌شکل فیلد data پاکت LoginResponseDto سرور.
    /// چرخهٔ سی‌وهشتم: displayName/roles/permissions هم نگاشت می‌شود (سطح
    /// دسترسی و پروفایل کاربر در پوسته).
    /// </summary>
    private sealed record LoginResult(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string Username,
        bool MustChangePassword,
        string? DisplayName = null,
        IReadOnlyList<string>? Roles = null,
        IReadOnlyList<string>? Permissions = null);
}
