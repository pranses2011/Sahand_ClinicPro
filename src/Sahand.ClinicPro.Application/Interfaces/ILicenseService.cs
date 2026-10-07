using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// قرارداد سرویس لایسنس سمت کلاینت — اعتبارسنجی و فعال‌سازی
/// </summary>
public interface ILicenseService
{
    /// <summary>وضعیت فعلی لایسنس درمانگاه</summary>
    Task<LicenseStatusReport> GetStatusAsync(CancellationToken ct = default);

    /// <summary>فعال‌سازی برخط — تماس با پنل لایسنس</summary>
    Task<LicenseStatusReport> ActivateOnlineAsync(string licenseKey, CancellationToken ct = default);

    /// <summary>فعال‌سازی برون‌خط — از فایل lic</summary>
    Task<LicenseStatusReport> ActivateOfflineAsync(string licenseFileContent, CancellationToken ct = default);

    /// <summary>تپ قلب روزانه به سرور لایسنس — با مهلت آفلاین قابل تنظیم</summary>
    Task<LicenseStatusReport> HeartbeatAsync(CancellationToken ct = default);

    /// <summary>آیا ماژول خاصی فعال است — توسط میدلور لایسنس استفاده می‌شود</summary>
    Task<bool> IsModuleEnabledAsync(string moduleCode, CancellationToken ct = default);

    /// <summary>
    /// اطلاعات پشتیبانی محصول — هنگام فعال‌سازی از سرور لایسنس دریافت و در تنظیمات محلی ذخیره می‌شود؛
    /// هیچ کلاینتی این مقادیر را هاردکد ندارد و همیشه از همین قرارداد می‌خواند
    /// </summary>
    Task<SupportInfoDto> GetSupportInfoAsync(CancellationToken ct = default);


    /// <summary>
    /// نشانی فعلی سرور لایسنس — چرخه هجدهم (گزارش دوازدهم):
    /// اولویت: تنظیم ذخیره‌شده توسط کاربر (SystemSettings) ← appsettings ←
    /// پیش‌فرض رسمی https://clinic-license.ea-fixer.ir/api.php
    /// </summary>
    Task<string> GetPortalUrlAsync(CancellationToken ct = default);

    /// <summary>
    /// ذخیرهٔ نشانی سرور لایسنس واردشدهٔ کاربر — نرمال‌سازی با LicensePortalHelper؛
    /// نشانی نامعتبر false برمی‌گرداند
    /// </summary>
    Task<bool> SetPortalUrlAsync(string portalUrl, CancellationToken ct = default);

    /// <summary>
    /// ساخت درخواست فعال‌سازی آفلاین (فایل .req) — شامل اثر انگشت سخت‌افزار
    /// و کلید اختیاری؛ فروشنده در پنل لایسنس همین محتوا را می‌پذیرد و فایل
    /// .lic امضاشده تحویل می‌دهد. (چرخه هجدهم — گزارش دوازدهم)
    /// </summary>
    Task<ActivationRequestDto> BuildActivationRequestAsync(string? licenseKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// تازه‌سازی اطلاعات پشتیبانی از سرور لایسنس (action=support عمومی) —
    /// best-effort؛ در صورت قطعی، مقادیر ذخیره‌شده/پیش‌فرض باقی می‌مانند
    /// </summary>
    Task<SupportInfoDto> RefreshSupportInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// پیام‌های پشتیبانی دریافتی از سرور لایسنس — با تپ‌قلب تحویل و در تنظیمات محلی ذخیره می‌شوند؛
    /// داشبورد درمانگاه آنها را در کارت «پیام‌های پشتیبانی» نمایش می‌دهد (چرخه دوازدهم)
    /// </summary>
    Task<List<SupportMessageDto>> GetSupportMessagesAsync(CancellationToken ct = default);
}

/// <summary>
/// یک پیام پشتیبانی ارسالی از پنل لایسنس — نمایش در داشبورد درمانگاه
/// </summary>
public sealed record SupportMessageDto(
    long Id,
    string Title,
    string Body,
    string Level,
    string Date)
{
    /// <summary>رنگ قاب بر اساس سطح — برای XAML</summary>
    public string LevelBrush => Level switch
    {
        "success" => "#2E7D32",
        "warning" => "#F9A825",
        "danger" => "#C62828",
        _ => "#1565C0",
    };

    /// <summary>آیکون بر اساس سطح</summary>
    public string LevelIcon => Level switch
    {
        "success" => "✅",
        "warning" => "⚠️",
        "danger" => "🚨",
        _ => "📢",
    };
}

/// <summary>
/// اطلاعات تماس با پشتیبان محصول — منبع یگانه برای سراسر برنامه (صفحه لایسنس، درباره، فوتر و راهنما)
/// </summary>
public sealed record SupportInfoDto(
    string Name,
    string Company,
    string Phone,
    string Mobile,
    string Email,
    string Website,
    string Source = "stored")
{
    /// <summary>نمونه خالی — پیش از فعال‌سازی لایسنس</summary>
    public static SupportInfoDto Empty { get; } = new("", "", "", "", "", "");

    /// <summary>آیا اطلاعاتی ذخیره شده است</summary>
    public bool HasData => !string.IsNullOrWhiteSpace(Company) || !string.IsNullOrWhiteSpace(Mobile)
                                                            || !string.IsNullOrWhiteSpace(Phone)
                                                            || !string.IsNullOrWhiteSpace(Email)
                                                            || !string.IsNullOrWhiteSpace(Website)
                                                            || !string.IsNullOrWhiteSpace(Name);
}

/// <summary>
/// درخواست فعال‌سازی آفلاین — محتوای فایل .req که برای فروشنده ارسال می‌شود؛
/// پنل PHP فیلدهای fingerprint/hardwareFingerprint و licenseKey را می‌خواند.
/// (چرخه هجدهم — گزارش دوازدهم مالک)
/// </summary>
public sealed record ActivationRequestDto(
    int FormatVersion,
    string RequestType,
    string Fingerprint,
    string? LicenseKey,
    string ClinicName,
    string AppVersion,
    string RequestedAt);

/// <summary>
/// گزارش وضعیت لایسنس
/// </summary>
public record LicenseStatusReport(
    bool IsValid,
    LicenseValidationResult Result,
    string? LicenseKey,
    DateOnly? ExpiryDate,
    int MaxUsers,
    int MaxDoctors,
    int MaxPatients,
    List<string> EnabledModules,
    string Message);
