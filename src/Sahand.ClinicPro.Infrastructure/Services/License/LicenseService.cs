using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.License;

/// <summary>
/// سرویس لایسنس سمت کلاینت — اتصال واقعی به سرور لایسنس (نسخه PHP برای cPanel یا پنل بلزور)
/// اعتبارسنجی سه‌مرحله‌ای: اثر انگشت سخت‌افزار ← انقضا ← امضای RSA-2048
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly ClinicProDbContext _context;
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>سازنده با تزریق وابستگی‌ها — HttpClient از نوع typed client</summary>
    public LicenseService(ClinicProDbContext context, HttpClient http, IConfiguration configuration,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _http = http;
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

    /// <summary>
    /// فعال‌سازی برخط — فراخوانی واقعی سرور لایسنس:
    /// POST {PortalUrl} با بدنه { action:"activate", licenseKey, fingerprint, ... }
    /// سرور در اولین فعال‌سازی اثر انگشت را به کلید متصل می‌کند
    /// </summary>
    public async Task<LicenseStatusReport> ActivateOnlineAsync(string licenseKey, CancellationToken ct = default)
    {
        var portalUrl = _configuration["License:PortalUrl"];
        if (string.IsNullOrWhiteSpace(portalUrl))
            return Invalid(LicenseValidationResult.NotFound,
                "نشانی پنل لایسنس تنظیم نشده است — License:PortalUrl");

        var fingerprint = _configuration["License:HardwareFingerprint"] ?? "";

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                action = "activate",
                licenseKey,
                fingerprint = string.IsNullOrWhiteSpace(fingerprint)
                    ? ComputeLocalFingerprint()
                    : fingerprint,
                clinicName = _configuration["License:ClinicName"] ?? "درمانگاه",
                appVersion = typeof(LicenseService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
            }, JsonOptions);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(portalUrl, content, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            var result = JsonSerializer.Deserialize<PortalActivationResponse>(raw, JsonOptions);
            if (result?.License is null || !result.IsValid)
                return Invalid(LicenseValidationResult.InvalidSignature,
                    result?.Message ?? "پاسخ نامعتبر از سرور لایسنس");

            // تأیید امضای دیجیتال سرور — رشته «کلید|انقضا|حداکثر کاربر»
            var publicKey = _configuration["License:RsaPublicKey"];
            if (!string.IsNullOrWhiteSpace(publicKey) && !VerifySignature(
                    result.License.LicenseKey, result.License.ExpiryDate,
                    result.License.MaxUsers, result.Signature, publicKey))
                return Invalid(LicenseValidationResult.InvalidSignature,
                    "امضای دیجیتال پاسخ سرور معتبر نیست — کلید عمومی را بررسی کنید");

            var license = await UpsertLicenseAsync(result.License, isOffline: false, ct);
            _context.LicenseCheckLog.Add(new LicenseCheckLog
            {
                CheckDate = DateTime.UtcNow,
                Result = LicenseValidationResult.Valid,
                IsOnlineCheck = true,
                Message = "فعال‌سازی برخط موفق — " + (result.Status ?? "valid"),
            });
            await _context.SaveChangesAsync(ct);

            return new LicenseStatusReport(true, LicenseValidationResult.Valid,
                license.LicenseKey, license.ExpiryDate, license.MaxUsers, license.MaxDoctors,
                license.MaxPatients, ParseModules(license.EnabledModules), "لایسنس با موفقیت فعال شد");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "اتصال به سرور لایسنس برقرار نشد — {Url}", portalUrl);
            return Invalid(LicenseValidationResult.NotFound,
                "اتصال به سرور لایسنس برقرار نشد — اینترنت و نشانی PortalUrl را بررسی کنید");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "پاسخ سرور لایسنس قابل تحلیل نبود");
            return Invalid(LicenseValidationResult.InvalidSignature, "پاسخ سرور لایسنس قابل تحلیل نبود");
        }
    }

    /// <summary>
    /// فعال‌سازی برون‌خط از فایل lic — محتوای JSON امضاشده تولیدشده در پنل
    /// شامل محدودیت‌ها و ماژول‌ها؛ امضا با کلید عمومی تأیید می‌شود
    /// </summary>
    public async Task<LicenseStatusReport> ActivateOfflineAsync(string licenseFileContent,
        CancellationToken ct = default)
    {
        try
        {
            var lic = JsonSerializer.Deserialize<PortalLicensePayload>(licenseFileContent, JsonOptions);
            if (lic is null || string.IsNullOrEmpty(lic.LicenseKey))
                return Invalid(LicenseValidationResult.InvalidSignature, "فایل لایسنس نامعتبر است");

            // امضای داخل فایل باید با کلید عمومی تنظیم‌شده بخواند
            var signature = ExtractSignature(licenseFileContent);
            var publicKey = _configuration["License:RsaPublicKey"];
            if (!string.IsNullOrWhiteSpace(signature) && !string.IsNullOrWhiteSpace(publicKey)
                && !VerifySignature(lic.LicenseKey, lic.ExpiryDate, lic.MaxUsers, signature, publicKey))
                return Invalid(LicenseValidationResult.InvalidSignature,
                    "امضای دیجیتال فایل لایسنس معتبر نیست");

            var license = await UpsertLicenseAsync(lic, isOffline: true, ct);
            _context.LicenseCheckLog.Add(new LicenseCheckLog
            {
                CheckDate = DateTime.UtcNow,
                Result = LicenseValidationResult.Valid,
                IsOnlineCheck = false,
                Message = "فعال‌سازی برون‌خط از فایل lic",
            });
            await _context.SaveChangesAsync(ct);

            return new LicenseStatusReport(true, LicenseValidationResult.Valid,
                license.LicenseKey, license.ExpiryDate, license.MaxUsers, license.MaxDoctors,
                license.MaxPatients, ParseModules(license.EnabledModules), "لایسنس برون‌خط فعال شد");
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

    /// <summary>
    /// خواندن اطلاعات پشتیبانی از تنظیمات محلی — سراسر برنامه (صفحه لایسنس، درباره، فوتر) از همین مسیر می‌خواند؛
    /// مقادیر هنگام فعال‌سازی/تپ‌قلب از پنل لایسنس دریافت و ذخیره شده‌اند و هیچ هاردکدی وجود ندارد
    /// </summary>
    public async Task<SupportInfoDto> GetSupportInfoAsync(CancellationToken ct = default)
    {
        var keys = new[] { "Support.Name", "Support.Company", "Support.Phone", "Support.Mobile", "Support.Email", "Support.Website" };
        var rows = await _context.SystemSettings.AsNoTracking()
            .Where(s => s.Category == "Support" && keys.Contains(s.Key) && !s.IsDeleted)
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "", ct);

        return new SupportInfoDto(
            rows.GetValueOrDefault("Support.Name", ""),
            rows.GetValueOrDefault("Support.Company", ""),
            rows.GetValueOrDefault("Support.Phone", ""),
            rows.GetValueOrDefault("Support.Mobile", ""),
            rows.GetValueOrDefault("Support.Email", ""),
            rows.GetValueOrDefault("Support.Website", ""));
    }

    /// <summary>
    /// تپ قلب روزانه — اعلان حضور به سرور لایسنس:
    /// در صورت موفقیت، محدودیت‌ها و ماژول‌ها تازه می‌شوند؛
    /// در صورت قطعی شبکه، تا پایان مهلت آفلاین (پیش‌فرض ۳۰ روز) اعتبار محلی می‌ماند
    /// </summary>
    public async Task<LicenseStatusReport> HeartbeatAsync(CancellationToken ct = default)
    {
        var license = await _context.ClinicLicense.FirstOrDefaultAsync(ct);
        if (license is null)
            return Invalid(LicenseValidationResult.NotFound, "لایسنس فعلی ثبت نشده است");

        var portalUrl = _configuration["License:PortalUrl"];
        if (string.IsNullOrWhiteSpace(portalUrl))
            return await GetStatusAsync(ct); // بدون سرور — فقط اعتبارسنجی محلی

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                action = "heartbeat",
                licenseKey = license.LicenseKey,
                fingerprint = _configuration["License:HardwareFingerprint"]
                              ?? license.HardwareFingerprint ?? "",
            }, JsonOptions);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(portalUrl, content, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PortalActivationResponse>(raw, JsonOptions);

            if (result?.License is not null && result.IsValid)
            {
                var fresh = await UpsertLicenseAsync(result.License, isOffline: false, ct);
                _context.LicenseCheckLog.Add(new LicenseCheckLog
                {
                    CheckDate = DateTime.UtcNow,
                    Result = LicenseValidationResult.Valid,
                    IsOnlineCheck = true,
                    Message = "تپ قلب موفق",
                });
                await _context.SaveChangesAsync(ct);
                return new LicenseStatusReport(true, LicenseValidationResult.Valid,
                    fresh.LicenseKey, fresh.ExpiryDate, fresh.MaxUsers, fresh.MaxDoctors,
                    fresh.MaxPatients, ParseModules(fresh.EnabledModules), "لایسنس معتبر است");
            }

            // سرور صریحاً رد کرد — ابطال یا انقضا یا اثر انگشت نامطباق
            _context.LicenseCheckLog.Add(new LicenseCheckLog
            {
                CheckDate = DateTime.UtcNow,
                Result = LicenseValidationResult.Expired,
                IsOnlineCheck = true,
                Message = result?.Message ?? "رد شدن توسط سرور",
            });
            await _context.SaveChangesAsync(ct);
            return Invalid(LicenseValidationResult.Expired, result?.Message ?? "لایسنس از سمت سرور رد شد");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // قطعی شبکه — اعمال مهلت آفلاین
            var graceDays = int.TryParse(_configuration["License:OfflineGraceDays"], out var g) ? g : 30;
            if (license.LastValidatedAt is { } last
                && DateTime.UtcNow - last > TimeSpan.FromDays(Math.Max(1, graceDays)))
            {
                _logger.LogWarning("مهلت آفلاین {Days} روزه لایسنس به پایان رسیده است", graceDays);
                return Invalid(LicenseValidationResult.Expired,
                    $"مهلت آفلاین {graceDays} روزه به پایان رسیده است — برای ادامه به اینترنت متصل شوید");
            }

            // هنوز در مهلت — اعتبارسنجی محلی
            return await GetStatusAsync(ct);
        }
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

        // مرحله ۳ — امضای RSA با کلید عمومی تنظیم‌شده
        var publicKey = _configuration["License:RsaPublicKey"];
        if (!string.IsNullOrEmpty(publicKey) && license.SignatureData is not null)
        {
            var expiry = license.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
            var payload = Encoding.UTF8.GetBytes(
                $"{license.LicenseKey}|{expiry}|{license.MaxUsers}");
            try
            {
                using var rsa = ImportPublicKey(publicKey);
                if (!rsa.VerifyData(payload, license.SignatureData,
                        HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    return (false, "امضای دیجیتال لایسنس نامعتبر است");
            }
            catch (CryptographicException)
            {
                return (false, "کلید عمومی لایسنس به‌درستی تنظیم نشده است");
            }
        }

        return (true, "لایسنس معتبر است");
    }

    /// <summary>تأیید امضای پاسخ سرور یا فایل lic — سازگار با کلید XML و PEM</summary>
    private static bool VerifySignature(string licenseKey, string? expiryDate,
        int maxUsers, string? signatureBase64, string publicKeyXmlOrPem)
    {
        try
        {
            if (string.IsNullOrEmpty(signatureBase64))
                return false;

            var expiry = string.IsNullOrEmpty(expiryDate) ? "" : expiryDate;
            var payload = Encoding.UTF8.GetBytes($"{licenseKey}|{expiry}|{maxUsers}");
            var signature = Convert.FromBase64String(signatureBase64);

            using var rsa = ImportPublicKey(publicKeyXmlOrPem);
            return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>ساخت کلید RSA از قالب XML یا PEM — هر دو قالب پشتیبانی می‌شود</summary>
    private static RSA ImportPublicKey(string keyXmlOrPem)
    {
        var rsa = RSA.Create();
        var key = keyXmlOrPem.Trim();
        if (key.StartsWith("<"))
            rsa.FromXmlString(key);          // قالب <RSAKeyValue>…</RSAKeyValue>
        else
            rsa.ImportFromPem(key);           // قالب PEM با سرصفحه BEGIN PUBLIC KEY
        return rsa;
    }

    /// <summary>خواندن فیلد امضا از متن خام فایل lic</summary>
    private static string? ExtractSignature(string licenseFileContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(licenseFileContent);
            return doc.RootElement.TryGetProperty("signature", out var sig)
                ? sig.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ساخت یا به‌روزرسانی رکورد لایسنس محلی با داده‌های واقعی سرور
    /// </summary>
    private async Task<ClinicLicense> UpsertLicenseAsync(PortalLicensePayload data, bool isOffline,
        CancellationToken ct)
    {
        var license = await _context.ClinicLicense.FirstOrDefaultAsync(ct);
        if (license is null)
        {
            license = new ClinicLicense
            {
                LicenseKey = data.LicenseKey,
                IsOfflineActivation = isOffline,
            };
            _context.ClinicLicense.Add(license);
        }
        else
        {
            license.LicenseKey = data.LicenseKey;
            license.IsOfflineActivation = isOffline;
        }

        // محدودیت‌ها و ماژول‌ها از سرور
        license.LicenseType = data.LicenseType == "perpetual"
            ? LicenseType.Perpetual : LicenseType.Subscription;
        license.MaxUsers = Math.Max(1, data.MaxUsers);
        license.MaxDoctors = Math.Max(1, data.MaxDoctors);
        license.MaxPatients = Math.Max(1, data.MaxPatients);
        license.MaxBranches = Math.Max(1, data.MaxBranches);
        license.EnabledModules = JsonSerializer.Serialize(data.EnabledModules);
        license.IssuedDate = ParseDate(data.IssuedDate) ?? DateOnly.FromDateTime(DateTime.UtcNow);
        license.ExpiryDate = ParseDate(data.ExpiryDate);
        license.HardwareFingerprint = _configuration["License:HardwareFingerprint"] is { Length: > 0 } fp
            ? fp
            : license.HardwareFingerprint;
        license.LastValidatedAt = DateTime.UtcNow;

        // امضای سرور برای اعتبارسنجی‌های بعدی آفلاین
        var signatureBase64 = data.Signature;
        if (!string.IsNullOrEmpty(signatureBase64))
        {
            try
            {
                license.SignatureData = Convert.FromBase64String(signatureBase64);
            }
            catch (FormatException)
            {
                license.SignatureData = null;
            }
        }

        // اطلاعات پشتیبانی محصول — از پنل لایسنس دریافت و در تنظیمات محلی ذخیره می‌شود تا سراسر برنامه استفاده کند
        if (data.Support is not null)
            await UpsertSupportInfoAsync(data.Support, ct);

        await _context.SaveChangesAsync(ct);
        return license;
    }

    /// <summary>
    /// ذخیره اطلاعات پشتیبانی دریافتی از سرور لایسنس در جدول SystemSettings (دسته Support)
    /// — مقادیر قبلی جایگزین می‌شوند؛ هرگز حذف نمی‌شوند تا در آفلاین هم قابل نمایش بمانند
    /// </summary>
    private async Task UpsertSupportInfoAsync(PortalSupportInfo support, CancellationToken ct)
    {
        var map = new Dictionary<string, string>
        {
            ["Support.Name"] = support.Name ?? "",
            ["Support.Company"] = support.Company ?? "",
            ["Support.Phone"] = support.Phone ?? "",
            ["Support.Mobile"] = support.Mobile ?? "",
            ["Support.Email"] = support.Email ?? "",
            ["Support.Website"] = support.Website ?? "",
        };

        var existing = await _context.SystemSettings
            .Where(s => s.Category == "Support" && map.Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, ct);

        foreach (var (key, value) in map)
        {
            if (string.IsNullOrWhiteSpace(value)) continue; // مقدار خالی از سرور — مقدار قبلی نگه داشته می‌شود
            if (existing.TryGetValue(key, out var row))
                row.Value = value;
            else
                _context.SystemSettings.Add(new SystemSetting
                {
                    Category = "Support",
                    Key = key,
                    Value = value,
                    DataType = SettingDataType.String,
                    Description = "اطلاعات پشتیبانی محصول — دریافتی از سرور لایسنس",
                    IsSystem = false,
                });
        }
    }

    /// <summary>تحلیل تاریخ yyyy-MM-dd</summary>
    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var date) ? date : null;

    /// <summary>اثر انگشت جایگزین مبتنی بر نام ماشین — فقط وقتی هیچ چیز تنظیم نشده</summary>
    private string ComputeLocalFingerprint()
    {
        var machine = Environment.MachineName
                      + "|" + Environment.OSVersion
                      + "|" + Environment.ProcessorCount;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(machine)));
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

/// <summary>پاسخ فعال‌سازی سرور لایسنس — قرارداد مشترک با سرور PHP</summary>
public sealed class PortalActivationResponse
{
    /// <summary>آیا لایسنس معتبر است</summary>
    [JsonPropertyName("isValid")] public bool IsValid { get; set; }

    /// <summary>وضعیت: valid | expired | revoked | not_found | fingerprint_mismatch</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }

    /// <summary>پیام فارسی</summary>
    [JsonPropertyName("message")] public string? Message { get; set; }

    /// <summary>زمان سرور</summary>
    [JsonPropertyName("serverTime")] public string? ServerTime { get; set; }

    /// <summary>بدنه لایسنس</summary>
    [JsonPropertyName("license")] public PortalLicensePayload? License { get; set; }

    /// <summary>امضای Base64 روی «کلید|انقضا|حداکثر کاربر»</summary>
    [JsonPropertyName("signature")] public string? Signature { get; set; }
}

/// <summary>بدنه لایسنس — هم در پاسخ برخط و هم در فایل lic آفلاین</summary>
public sealed class PortalLicensePayload
{
    /// <summary>کلید لایسنس SCPRO-…</summary>
    [JsonPropertyName("licenseKey")] public string LicenseKey { get; set; } = string.Empty;

    /// <summary>نوع: subscription | perpetual</summary>
    [JsonPropertyName("licenseType")] public string LicenseType { get; set; } = "subscription";

    /// <summary>تاریخ صدور</summary>
    [JsonPropertyName("issuedDate")] public string? IssuedDate { get; set; }

    /// <summary>تاریخ انقضا — تهی یعنی دائمی</summary>
    [JsonPropertyName("expiryDate")] public string? ExpiryDate { get; set; }

    /// <summary>حداکثر کاربر همزمان</summary>
    [JsonPropertyName("maxUsers")] public int MaxUsers { get; set; }

    /// <summary>حداکثر پزشک</summary>
    [JsonPropertyName("maxDoctors")] public int MaxDoctors { get; set; }

    /// <summary>حداکثر بیمار</summary>
    [JsonPropertyName("maxPatients")] public int MaxPatients { get; set; }

    /// <summary>حداکثر شعبه</summary>
    [JsonPropertyName("maxBranches")] public int MaxBranches { get; set; }

    /// <summary>ماژول‌های فعال</summary>
    [JsonPropertyName("enabledModules")] public List<string> EnabledModules { get; set; } = new();

    /// <summary>روزهای باقیمانده</summary>
    [JsonPropertyName("daysRemaining")] public int? DaysRemaining { get; set; }

    /// <summary>امضای Base64 — فقط در فایل lic آفلاین داخل همین بدنه است</summary>
    [JsonPropertyName("signature")] public string? Signature { get; set; }

    /// <summary>اطلاعات پشتیبانی محصول — از پنل لایسنس ارسال می‌شود</summary>
    [JsonPropertyName("support")] public PortalSupportInfo? Support { get; set; }
}

/// <summary>اطلاعات پشتیبانی دریافتی از سرور لایسنس — قرارداد مشترک با پنل PHP</summary>
public sealed class PortalSupportInfo
{
    /// <summary>نام پشتیبان</summary>
    [JsonPropertyName("name")] public string? Name { get; set; }

    /// <summary>نام شرکت</summary>
    [JsonPropertyName("company")] public string? Company { get; set; }

    /// <summary>تلفن ثابت</summary>
    [JsonPropertyName("phone")] public string? Phone { get; set; }

    /// <summary>موبایل</summary>
    [JsonPropertyName("mobile")] public string? Mobile { get; set; }

    /// <summary>ایمیل</summary>
    [JsonPropertyName("email")] public string? Email { get; set; }

    /// <summary>وب‌سایت</summary>
    [JsonPropertyName("website")] public string? Website { get; set; }
}
