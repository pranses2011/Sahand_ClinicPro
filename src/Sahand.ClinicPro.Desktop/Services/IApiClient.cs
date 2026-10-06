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

    /// <summary>درخواست POST با نتیجه موفقیت/شکست — برای فرمان‌ها بدون داده خروجی</summary>
    Task<bool> PostAsync(string endpoint, object? body, CancellationToken ct = default);

    /// <summary>درخواست PUT با نتیجه موفقیت/شکست</summary>
    Task<bool> PutAsync(string endpoint, object? body, CancellationToken ct = default);

    /// <summary>درخواست DELETE با نتیجه موفقیت/شکست</summary>
    Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// آپلود فایل چندبخشی (multipart) با توکن — گام R4 برای فایل‌های DICOM
    /// </summary>
    Task<T?> PostFileAsync<T>(string endpoint, string fileName, byte[] content,
        Dictionary<string, string>? fields = null, CancellationToken ct = default);

    /// <summary>
    /// دانلود بایت‌های خام فایل — گام R4 برای گشودن در نمایشگر DICOM
    /// </summary>
    Task<byte[]?> DownloadFileAsync(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// درخواست POST با بدنه JSON و دریافت بایت‌های خام — خروجی PDF گزارش‌ها (چرخه ۱۳، D6)
    /// </summary>
    Task<byte[]?> PostDownloadAsync(string endpoint, object? body, CancellationToken ct = default);

    /// <summary>ورود و دریافت توکن‌ها</summary>
    Task<bool> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default);

    /// <summary>خروج و ابطال توکن</summary>
    Task LogoutAsync();

    /// <summary>آخرین پیام خطای فارسی</summary>
    string? LastError { get; }

    /// <summary>پرچم آخرین ورود — سرور تغییر رمز عبور را الزامی کرده است (چرخه چهاردهم)</summary>
    bool LastLoginMustChangePassword { get; }

    /// <summary>رویداد نیاز به ورود مجدد — ناوبری به صفحه ورود</summary>
    event EventHandler? SessionExpired;
}
