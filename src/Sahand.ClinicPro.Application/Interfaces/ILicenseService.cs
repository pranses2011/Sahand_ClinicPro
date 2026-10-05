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
    string Website)
{
    /// <summary>نمونه خالی — پیش از فعال‌سازی لایسنس</summary>
    public static SupportInfoDto Empty { get; } = new("", "", "", "", "", "");

    /// <summary>آیا اطلاعاتی ذخیره شده است</summary>
    public bool HasData => !string.IsNullOrWhiteSpace(Company) || !string.IsNullOrWhiteSpace(Mobile);
}

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
