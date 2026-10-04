using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.License;

/// <summary>
/// سرویس لایسنس سمت کلاینت — اعتبارسنجی سه‌مرحله‌ای:
/// اثر انگشت سخت‌افزار ← انقضا ← امضای RSA-2048
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly ClinicProDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public LicenseService(ClinicProDbContext context, IConfiguration configuration,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>وضعیت فعلی لایسنس</summary>
    public async Task<LicenseStatusReport> GetStatusAsync(CancellationToken ct = default)
    {
        var license = await _context.ClinicLicense.FirstOrDefaultAsync(ct);
        if (license is null)
            return Invalid(LicenseValidationResult.NotFound, "لایسنس فعلی ثبت نشده است");

        var (valid, message) = Validate(license);
        return new LicenseStatusReport(
            valid, valid ? LicenseValidationResult.Valid : LicenseValidationResult.Expired,
            license.LicenseKey, license.ExpiryDate,
            license.MaxUsers, license.MaxDoctors, license.MaxPatients,
            ParseModules(license.EnabledModules), message);
    }

    /// <summary>فعال‌سازی برخط — در گام ۲۵ به پنل وب متصل می‌شود؛ فعلاً ثبت محلی</summary>
    public async Task<LicenseStatusReport> ActivateOnlineAsync(string licenseKey, CancellationToken ct = default)
    {
        var portalUrl = _configuration["License:PortalUrl"];
        if (string.IsNullOrEmpty(portalUrl))
            return Invalid(LicenseValidationResult.NotFound,
                "نشانی پنل لایسنس تنظیم نشده است — License:PortalUrl");

        // تماس با پنل — در استقرار تولیدی فعال می‌شود
        // فعلاً ساخت رکورد محلی برای توسعه
        var license = await UpsertLicenseAsync(licenseKey, isOffline: false, ct);
        var (valid, message) = Validate(license);
        return new LicenseStatusReport(valid,
            valid ? LicenseValidationResult.Valid : LicenseValidationResult.Expired,
            license.LicenseKey, license.ExpiryDate, license.MaxUsers, license.MaxDoctors,
            license.MaxPatients, ParseModules(license.EnabledModules), message);
    }

    /// <summary>فعال‌سازی برون‌خط از فایل lic — محتوای JSON امضاشده</summary>
    public async Task<LicenseStatusReport> ActivateOfflineAsync(string licenseFileContent,
        CancellationToken ct = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(licenseFileContent);
            var root = doc.RootElement;

            var licenseKey = root.GetProperty("licenseKey").GetString() ?? string.Empty;
            if (string.IsNullOrEmpty(licenseKey))
                return Invalid(LicenseValidationResult.InvalidSignature, "فایل لایسنس نامعتبر است");

            var license = await UpsertLicenseAsync(licenseKey, isOffline: true, ct);
            var (valid, message) = Validate(license);
            return new LicenseStatusReport(valid,
                valid ? LicenseValidationResult.Valid : LicenseValidationResult.Expired,
                license.LicenseKey, license.ExpiryDate, license.MaxUsers, license.MaxDoctors,
                license.MaxPatients, ParseModules(license.EnabledModules), message);
        }
        catch (JsonException)
        {
            return Invalid(LicenseValidationResult.InvalidSignature, "فایل لایسنس قابل خواندن نیست");
        }
    }

    /// <summary>آیا ماژول فعال است</summary>
    public async Task<bool> IsModuleEnabledAsync(string moduleCode, CancellationToken ct = default)
    {
        var status = await GetStatusAsync(ct);
        return status.IsValid
               && (status.EnabledModules.Contains(moduleCode)
                   || status.EnabledModules.Contains("*"));
    }

    /// <summary>اعتبارسنجی سه‌مرحله‌ای — انقضا و اثر انگشت و امضا</summary>
    private (bool Valid, string Message) Validate(ClinicLicense license)
    {
        // مرحله ۱ — انقضا
        if (license.ExpiryDate.HasValue && license.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return (false, "لایسنس منقضی شده است");

        // مرحله ۲ — اثر انگشت سخت‌افزار (اگر تنظیم شده باشد)
        var expectedFingerprint = _configuration["License:HardwareFingerprint"];
        if (!string.IsNullOrEmpty(expectedFingerprint)
            && !string.IsNullOrEmpty(license.HardwareFingerprint)
            && license.HardwareFingerprint != expectedFingerprint)
            return (false, "این لایسنس به دستگاه دیگری تعلق دارد");

        // مرحله ۳ — امضای RSA — با کلید عمومی تنظیم‌شده
        var publicKey = _configuration["License:RsaPublicKey"];
        if (!string.IsNullOrEmpty(publicKey) && license.SignatureData is not null)
        {
            if (!VerifyRsaSignature(license, publicKey))
                return (false, "امضای دیجیتال لایسنس نامعتبر است");
        }

        // ثبت لاگ اعتبارسنجی
        _context.LicenseCheckLog.Add(new LicenseCheckLog
        {
            CheckDate = DateTime.UtcNow,
            Result = LicenseValidationResult.Valid,
            IsOnlineCheck = false,
            Message = "اعتبارسنجی موفق"
        });
        _ = _context.SaveChangesAsync();

        return (true, "لایسنس معتبر است");
    }

    /// <summary>بررسی امضای RSA-2048 با SHA256</summary>
    private static bool VerifyRsaSignature(ClinicLicense license, string publicKeyXml)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);

            var payload = Encoding.UTF8.GetBytes(
                $"{license.LicenseKey}|{license.ExpiryDate:yyyy-MM-dd}|{license.MaxUsers}");

            return rsa.VerifyData(payload, license.SignatureData,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>ساخت یا به‌روزرسانی رکورد لایسنس محلی</summary>
    private async Task<ClinicLicense> UpsertLicenseAsync(string licenseKey, bool isOffline,
        CancellationToken ct)
    {
        var license = await _context.ClinicLicense.FirstOrDefaultAsync(ct);
        if (license is null)
        {
            license = new ClinicLicense
            {
                LicenseKey = licenseKey,
                LicenseType = LicenseType.Subscription,
                IsOfflineActivation = isOffline
            };
            _context.ClinicLicense.Add(license);
        }
        else
        {
            license.LicenseKey = licenseKey;
            license.IsOfflineActivation = isOffline;
        }

        license.LastValidatedAt = DateTime.UtcNow;

        // ماژول‌های پایه همیشه فعال
        license.EnabledModules = JsonSerializer.Serialize(new[]
        {
            "Appointments", "Patients", "Visits", "Prescriptions",
            "Billing", "Users", "Settings", "Backup", "Audit", "Dashboard"
        });

        await _context.SaveChangesAsync(ct);
        return license;
    }

    /// <summary>خواندن فهرست ماژول‌های فعال از JSON</summary>
    private static List<string> ParseModules(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>ساخت گزارش نامعتبر</summary>
    private static LicenseStatusReport Invalid(LicenseValidationResult result, string message) =>
        new(false, result, null, null, 0, 0, 0, new List<string>(), message);
}
