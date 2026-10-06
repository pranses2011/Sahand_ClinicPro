using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// کمکی‌های HTTP مشترک کلاینت API — چرخه چهاردهم (گزارش پنجم مالک):
/// (۱) نرمال‌سازی آدرس پایه با اسلش انتهایی: ترکیب مسیر نسبی با BaseAddress
///     طبق RFC 3986 قطعه آخر مسیر پایه را می‌اندازد — «…/api/v1» + «auth/login»
///     به «…/api/auth/login» می‌رسید؛ همه مسیرهای API با api/v1 شروع می‌شوند
///     پس سرور 404 با بدنه خالی برمی‌گرداند و دسکتاپ «خطای سرور — کد 404»
///     نشان می‌داد. این باگ کل ارتباط دسکتاپ↔API را فلج کرده بود و چون
///     تست‌های ViewModel با FakeApiClient اجرا می‌شوند، تا اولین اجرای واقعی
///     پنهان مانده بود.
/// (۲) باز کردن پاکت استاندارد پاسخ API (isSuccess/message/data) — داده همه
///     endpointها در فیلد «data» است؛ لاگین و تمدید توکن پیش‌تر بدنه را مستقیم
///     به DTO داخلی نگاشت می‌کردند و توکن‌ها همیشه null می‌شدند.
/// </summary>
public static class ApiHttp
{
    /// <summary>آدرس پایه پیش‌فرض — هماهنگ با appsettings.json دسکتاپ</summary>
    public const string DefaultBaseUrl = "http://localhost:5000/api/v1";

    /// <summary>گزینه‌های سریالایز — نام‌گذاری camelCase مطابق API</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>پاکت استاندارد پاسخ API — هم‌شکل ApiResponse در سمت سرور</summary>
    public sealed record Envelope<T>(bool IsSuccess, string? Message, T? Data);

    /// <summary>
    /// نرمال‌سازی آدرس پایه — همیشه به اسلش پایانی ختم می‌شود تا مسیرهای نسبی
    /// بدون از دست رفتن قطعه آخر مسیر پایه (مثل «v1») ترکیب شوند.
    /// </summary>
    public static string NormalizeBaseUrl(string? baseUrl)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();
        return url.EndsWith('/') ? url : url + "/";
    }

    /// <summary>
    /// خواندن پاکت استاندارد پاسخ — در بدنه غیر JSON یا خراب null برمی‌گرداند
    /// تا فراخواننده پیام جایگزین (مثل «خطای سرور — کد 404») بسازد.
    /// </summary>
    public static Envelope<T>? ParseEnvelope<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Envelope<T>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
