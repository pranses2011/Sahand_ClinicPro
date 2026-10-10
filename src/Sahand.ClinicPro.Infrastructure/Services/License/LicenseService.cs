using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
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
public partial class LicenseService : ILicenseService
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

    /// <summary>
    /// گیت هم‌زمانی جای‌گذاری لایسنس — چرخهٔ سی‌وهشتم (رفع DbUpdateConcurrencyException).
    /// ایستا است تا «همهٔ» نمونه‌های سرویس (هر درخواست HTTP نمونهٔ خودش را
    /// می‌سازد) روی یک صف مشترک بیفتند؛ تپ‌قلب/فعال‌سازی‌های هم‌زمان در
    /// این پردازه پشت هم اجرا می‌شوند و رکورد تک‌مصرف لایسنس با RowVersion
    /// (نشانهٔ همزمانی خوش‌بینانه) دیگر تصادم ۰-سطری نمی‌گیرد.
    /// </summary>
    private static readonly SemaphoreSlim UpsertGate = new(1, 1);

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
        // چرخه هجدهم (گزارش دوازدهم): تنظیم خالی دیگر خطا نمی‌دهد —
        // اولویت: SystemSettings ← appsettings ← پیش‌فرض رسمی محصول
        var portalUrl = await ResolvePortalUrlAsync(ct);
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

            // چرخه دوازدهم — پیام‌های مستقیم پشتیبانی به داشبورد درمانگاه
            await SaveServerMessagesAsync(result.Messages, ct);

            // ═══ چرخه سی‌ویکم (گزارش مالک) — «در تنظیمات و اطلاعات درمانگاه،
            // اطلاعات رو از لایسنس نمیاره و نشون نمیده» ═══
            // ریشه: همگام‌سازی اطلاعات درمانگاه فقط در تپ قلبِ شبانهٔ ۳ بامداد
            // اجرا می‌شد؛ پس از فعال‌سازی، صفحهٔ تنظیمات تا اولین ۳ صبح خالی
            // می‌ماند. اکنون همان لحظهٔ فعال‌سازی موفقِ برخط، فیلدهای پنل
            // لایسنس (نام/مدیر/تلفن/ایمیل/شهر/نشانی/کدها) روی دستهٔ ClinicInfo
            // تنظیمات محلی می‌نشیند تا صفحهٔ تنظیمات بی‌درنگ نمایششان دهد.
            await SyncClinicInfoFromLicenseAsync(result.License, ct);

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

            // چرخه سی‌ویکم — همان همگام‌سازیِ فعال‌سازی برخط، برای فایل lic برون‌خط:
            // فایل تولیدی پنل لایسنس همان فیلدهای اطلاعات درمانگاه را دارد و پس از
            // فعال‌سازی برون‌خط هم باید بی‌درنگ در صفحهٔ تنظیمات ظاهر شود
            await SyncClinicInfoFromLicenseAsync(lic, ct);

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

        // ═══ چرخهٔ سی‌امین — الگوی امن به‌جای ToDictionaryAsync مستقیم ═══
        // جدول SystemSettings یکتایی دیتابیسی روی (Category, Key) ندارد؛
        // ردیف تکراری (حذف‌نرم‌شدهٔ بازمانده از پشتیبان قدیمی و…) نباید
        // خواندن اطلاعات پشتیبانی را با استثنای «کلید تکراری» بکشد —
        // همان ردهٔ خطای بذرافشان مرجع. اولین ردیفِ هر کلید ملاک است.
        var supportRows = await _context.SystemSettings.AsNoTracking()
            .Where(s => s.Category == "Support" && keys.Contains(s.Key) && !s.IsDeleted)
            .Select(s => new { s.Key, s.Value })
            .ToListAsync(ct);

        var rows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in supportRows)
            if (!string.IsNullOrEmpty(row.Key) && !rows.ContainsKey(row.Key))
                rows[row.Key] = row.Value ?? "";

        var stored = new SupportInfoDto(
            rows.GetValueOrDefault("Support.Name", ""),
            rows.GetValueOrDefault("Support.Company", ""),
            rows.GetValueOrDefault("Support.Phone", ""),
            rows.GetValueOrDefault("Support.Mobile", ""),
            rows.GetValueOrDefault("Support.Email", ""),
            rows.GetValueOrDefault("Support.Website", ""));
        return stored.HasData ? stored : DefaultSupportInfo();
    }

    /// <summary>
    /// نشانی سرور لایسنس — چرخه هجدهم (گزارش دوازدهم مالک):
    /// اولویت: تنظیم ذخیره‌شدهٔ کاربر (SystemSettings: License.PortalUrl) ←
    /// appsettings (License:PortalUrl) ← پیش‌فرض رسمی محصول.
    /// خطای «نشانی پنل لایسنس تنظیم نشده است» با همین زنجیره رفع شد.
    /// </summary>
    public async Task<string> GetPortalUrlAsync(CancellationToken ct = default)
        => await ResolvePortalUrlAsync(ct);

    /// <summary>
    /// ذخیرهٔ نشانی سرور لایسنس کاربر — نرمال‌سازی (ریشه ← api.php) و
    /// اعتبارسنجی http/https؛ سپس ذخیره در SystemSettings (دستهٔ License).
    /// </summary>
    public async Task<bool> SetPortalUrlAsync(string portalUrl, CancellationToken ct = default)
    {
        var raw = (portalUrl ?? string.Empty).Trim();
        if (raw.Length > 0 && !LicensePortalHelper.IsValidHttpUrl(
                raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? raw
                    : "https://" + raw))
            return false;

        var normalized = LicensePortalHelper.NormalizeApiUrl(raw);
        var key = "License.PortalUrl";
        var existing = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Category == "License" && s.Key == key, ct);
        if (existing is not null)
            existing.Value = normalized;
        else
            _context.SystemSettings.Add(new SystemSetting
            {
                Category = "License",
                Key = key,
                Value = normalized,
                DataType = SettingDataType.String,
                Description = "نشانی سرور لایسنس — تنظیم‌شده از صفحهٔ فعال‌سازی دسکتاپ",
                IsSystem = false,
            });
        await _context.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// ساخت درخواست فعال‌سازی آفلاین (فایل .req) — چرخه هجدهم (گزارش دوازدهم).
    /// کلید اختیاری است؛ اگر داده شود پنل از ناسازگاری کلید/لایسنس جلوگیری می‌کند.
    /// </summary>
    public async Task<ActivationRequestDto> BuildActivationRequestAsync(string? licenseKey = null,
        CancellationToken ct = default)
    {
        var fingerprint = _configuration["License:HardwareFingerprint"];
        if (string.IsNullOrWhiteSpace(fingerprint))
            fingerprint = ComputeLocalFingerprint();
        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length < 16)
            fingerprint = fingerprint.PadLeft(16, '0');

        // کلید داده نشده؟ آخرین کلید ثبت‌شدهٔ محلی (در ادامهٔ فعال‌سازی قبلی) به‌کار می‌رود
        string? key = null;
        if (string.IsNullOrWhiteSpace(licenseKey))
            key = (await _context.ClinicLicense.AsNoTracking().FirstOrDefaultAsync(ct))?.LicenseKey;
        else
            key = licenseKey.Trim().ToUpperInvariant();

        return new ActivationRequestDto(
            FormatVersion: 1,
            RequestType: "activationRequest",
            Fingerprint: fingerprint,
            LicenseKey: string.IsNullOrWhiteSpace(key) ? null : key,
            ClinicName: _configuration["License:ClinicName"] ?? "درمانگاه سهند",
            AppVersion: typeof(LicenseService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
            RequestedAt: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    /// <summary>
    /// تازه‌سازی اطلاعات پشتیبانی از سرور لایسنس — چرخه هجدهم (گزارش دوازدهم):
    /// action=support عمومی پنل PHP خوانده می‌شود و در SystemSettings ذخیره
    /// می‌شود؛ «از دفعات بعد» همین مقادیر تازه نمایش داده می‌شوند.
    /// در صورت خطا، مقادیر ذخیره‌شده/پیش‌فرض برگردانده می‌شوند.
    /// </summary>
    public async Task<SupportInfoDto> RefreshSupportInfoAsync(CancellationToken ct = default)
    {
        var portalUrl = await ResolvePortalUrlAsync(ct);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                portalUrl + (portalUrl.Contains('?') ? "&" : "?") + "action=support");
            using var response = await _http.SendAsync(request, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PortalSupportResponse>(raw, JsonOptions);
            if (result?.Support is { } sup)
            {
                await UpsertSupportInfoAsync(sup, ct);
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogDebug(ex, "تازه‌سازی اطلاعات پشتیبانی از سرور لایسنس ناموفق بود — مقادیر موجود می‌مانند");
        }
        return await GetSupportInfoAsync(ct);
    }

    /// <summary>
    /// خواندن نشانی سرور لایسنس با اولویت: تنظیم کاربر ← appsettings ← پیش‌فرض رسمی.
    /// نرمال‌سازی تضمین می‌کند همیشه endpoint کامل (api.php) باشد.
    /// </summary>
    private async Task<string> ResolvePortalUrlAsync(CancellationToken ct)
    {
        var stored = await _context.SystemSettings.AsNoTracking()
            .Where(s => s.Category == "License" && s.Key == "License.PortalUrl" && !s.IsDeleted)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(stored))
            return stored;
        var configured = _configuration["License:PortalUrl"];
        return LicensePortalHelper.NormalizeApiUrl(configured);
    }

    /// <summary>
    /// اطلاعات پشتیبانی پیش‌فرض — چرخه هجدهم (گزارش دوازدهم مالک):
    /// «بصورت پیش‌فرض اطلاعات پشتیبانی موجود باشه»؛ تا نخستین تماس موفق با
    /// سرور لایسنس، این مقادیر در صفحهٔ فعال‌سازی نمایش داده می‌شوند و پس از
    /// دریافت مقادیر واقعی از پنل، همان‌ها جایگزین و در دیتابیس ذخیره می‌شوند.
    /// </summary>
    private static SupportInfoDto DefaultSupportInfo() => new(
        "سعید نجفی",
        "سهند سرویس",
        "041-34436141",
        "09144220430",
        "info@ea-fixer.ir",
        "https://ea-fixer.ir",
        Source: "default");

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

        // چرخه هجدهم: نشانی سرور از Resolver مشترک — پیش‌فرض رسمی همیشه موجود است
        var portalUrl = await ResolvePortalUrlAsync(ct);

        try
        {
            // ═══ چرخهٔ سی‌ودوم (گزارش مالک) — ریشهٔ «اطلاعات درمانگاه و ماژول‌ها
            // از لایسنس به‌روز نمی‌شوند» ═══
            // پیش‌تر: `config ?? license.HardwareFingerprint ?? ""` — مقدار config
            // در appsettings پیش‌فرض «رشتهٔ خالی» است (نه null!) → عملگر ?? هرگز
            // به fallback نمی‌رسید → تپ قلب همیشه با اثر انگشت خالی ارسال می‌شد
            // → سرور لایسنس (پنل PHP) آن را با fingerprint_mismatch رد می‌کرد →
            // تپ قلب هرگز موفق نمی‌شد؛ یعنی همگام‌سازی اطلاعات درمانگاه، تازه‌سازی
            // ماژول‌ها و تحویل پیام‌های پشتیبانی عملاً از کار افتاده بود.
            // حل سه‌مرحله‌ای (همان ترتیب فعال‌سازی): تنظیم صریح config ←
            // اثر انگشت ثبت‌شده روی رکورد لایسنس ← اثر انگشت محلی محاسبه‌شده
            // (MachineName|OSVersion|ProcessorCount که در فعال‌سازی برخط به کلید
            // بسته شده و قطعاً همان است).
            var configFingerprint = _configuration["License:HardwareFingerprint"];
            var fingerprint = !string.IsNullOrWhiteSpace(configFingerprint)
                ? configFingerprint
                : !string.IsNullOrWhiteSpace(license.HardwareFingerprint)
                    ? license.HardwareFingerprint
                    : ComputeLocalFingerprint();

            var body = JsonSerializer.Serialize(new
            {
                action = "heartbeat",
                licenseKey = license.LicenseKey,
                fingerprint,
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

                // چرخه دوازدهم — پیام‌های مستقیم پشتیبانی به داشبورد درمانگاه
                await SaveServerMessagesAsync(result.Messages, ct);

                // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) ═══ «در تنظیمات، اطلاعات
                // درمانگاه را از لایسنس بخوان و در اولین چک کردن لایسنس در تنظیمات
                // قرار بده؛ فقط از سرور لایسنس قابل تغییر باشد» — با هر اعتبارسنجی
                // موفق آنلاین، اطلاعات درمانگاهِ سرور لایسنس روی تنظیمات محلی
                // (دستهٔ ClinicInfo) همگام می‌شود؛ صفحهٔ تنظیمات این بخش را فقط-
                // خواندنی نمایش می‌دهد و تغییر تنها از پنل مدیریت لایسنس است.
                await SyncClinicInfoFromLicenseAsync(result.License, ct);

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
    /// جای‌گذاری/به‌روزرسانی رکورد لایسنس از دادهٔ سرور — چرخهٔ سی‌وهشتم
    /// (گزارش مالک: خطای DbUpdateConcurrencyException در لاگ سرور API).
    ///
    /// ═══ ریشهٔ خطا ═══
    /// Column RowVersion روی همهٔ جدول‌ها «نشانهٔ همزمانی» است
    /// (ClinicProDbContext آن را با IsRowVersion ثبت کرده)؛ یعنی هر UPDATE
    /// با شرط «RowVersion همان‌طور که خوانده شد» اجرا می‌شود. رکورد لایسنس
    /// «تک» است و چند مسیر هم‌زمان می‌توانند بخواهند او را بنویسند:
    /// تازه‌سازی دستی لایسنس (پنجرهٔ فعال‌سازی، اندپوینت refresh) از چند
    /// ایستگاه هم‌زمان + فعال‌سازی مجدد + تپ‌قلب. دو فراخوانی هم‌زمان، هر دو
    /// نسخهٔ یکسان RowVersion را می‌خوانند؛ نخستین ذخیره نسخه را جلو می‌برد
    /// و ذخیرهٔ دومی «۰ سطر» می‌یابد ⇒ DbUpdateConcurrencyException و شکست
    /// کامل تپ‌قلب (500 به کلاینت).
    ///
    /// ═══ رفع (دو لایه) ═══
    /// ۱) گیت فرایندِ ایستا: همهٔ جای‌گذاری‌های لایسنس این پردازه پشت یک
    ///    سمافور صف می‌شوند تا اصلاً هم‌پوشانی رخ ندهد (کاهش تصادم در مبدأ).
    /// ۲) تلاش مجدد خوش‌بینانه: اگر با این حال تصادمی از پردازه/ایستگاه
    ///    دیگر رسید، خطای همزمانی گرفته می‌شود، «فقط» موجودیت‌های همین
    ///    عملیات (لایسنس + تنظیمات) از ردیاب جدا می‌شوند و چرخه با دادهٔ
    ///    تازهٔ دیتابیس دوباره اجرا می‌شود — تا ۴ تلاش با وقفهٔ پلکانی.
    ///    منطق درست این‌جای کار «دادهٔ سرور مقدم است» (last-write-wins) است؛
    ///    پاسخ پورتال مرجع است نه نسخهٔ خوانده‌شدهٔ محلی.
    /// </summary>
    private async Task<ClinicLicense> UpsertLicenseAsync(PortalLicensePayload data, bool isOffline, CancellationToken ct)
    {
        // گیت ایستا — همهٔ نمونه‌های سرویس (هر درخواست، نمونهٔ خودش را دارد)
        // روی همین سمافور مشترک‌اند؛ صف‌شدن در سطح پردازه
        await UpsertGate.WaitAsync(ct);
        try
        {
            const int maxAttempts = 4;
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await UpsertLicenseCoreAsync(data, isOffline, ct);
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    // موجودیت‌های این عملیات را از ردیاب جدا کن — تنها این‌ها
                    // دوباره ساخته می‌شوند؛ رکوردهای ردیابی‌شدهٔ میدلورهای
                    // دیگر (مثل ممیزی) دست‌نخورده می‌مانند
                    foreach (var entry in _context.ChangeTracker.Entries()
                                 .Where(e => e.Entity is ClinicLicense or SystemSetting)
                                 .ToList())
                        entry.State = EntityState.Detached;

                    _logger.LogInformation(
                        "تصادم همزمانی در ذخیرهٔ لایسنس (تلاش {Attempt} از {Max}) — دادهٔ تازه بازیابی و دوباره ذخیره می‌شود",
                        attempt, maxAttempts);
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), ct);
                }
            }
        }
        finally
        {
            UpsertGate.Release();
        }
    }

    /// <summary>
    /// هستهٔ جای‌گذاری لایسنس — بدون منطق تلاش مجدد (پوشش آن در UpsertLicenseAsync).
    /// موجودیت تازه از دیتابیس خوانده می‌شود، مقادیر پورتال روی آن می‌نشیند
    /// و همراه تنظیمات پشتیبانی در یک تراکنش ذخیره می‌شود.
    /// </summary>
    private async Task<ClinicLicense> UpsertLicenseCoreAsync(PortalLicensePayload data, bool isOffline, CancellationToken ct)
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
        // ═══ چرخهٔ سی‌ودوم — ماندگاری اثر انگشت روی رکورد لایسنس ═══
        // پیش‌تر وقتی config خالی بود (حالت رایج)، مقدار فعلی (در اولین فعال‌سازی:
        // تهی) دست‌نخورده می‌ماند؛ اکنون اگر config تنظیم نشده و مقدار فعلی هم خالی
        // است، اثر انگشت محلیِ محاسبه‌شده (همانی که فعال‌سازی برخط برای سرور لایسنس
        // فرستاده و به کلید بسته شده) ذخیره می‌شود تا تپ‌قلب‌های بعدی مستقل از
        // config هم مقدار صحیح را داشته باشند. مقدار موجود هرگز بازنویسی نمی‌شود.
        license.HardwareFingerprint = _configuration["License:HardwareFingerprint"] is { Length: > 0 } fp
            ? fp
            : string.IsNullOrWhiteSpace(license.HardwareFingerprint)
                ? ComputeLocalFingerprint()
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

        // ═══ چرخهٔ سی‌امین — الگوی امن + فیلتر حذف‌نرم‌شده‌ها ═══
        // پیش از این پرس‌وجو نه فیلتر IsDeleted داشت و نه مصونیت از کلید
        // تکراری: اگر برای یک کلید هم ردیف حذف‌شده باشد هم ردیف فعال،
        // ToDictionaryAsync با «An item with the same key…» می‌شکست و تپ‌قلب/
        // فعال‌سازی لایسنس (فراخوانندهٔ همین متد) ناقص می‌ماند؛ ضمن آنکه
        // به‌روزرسانی ردیفِ حذف‌شده بی‌اثر بود (مسیرهای خواندن IsDeleted را
        // فیلتر می‌کنند و مقدار هرگز دیده نمی‌شد). اکنون: فقط ردیف‌های فعال
        // خوانده می‌شوند و ساخت دیکشنری در حافظه با «اولین ردیف ملاک» است.
        var existingRows = await _context.SystemSettings
            .Where(s => s.Category == "Support" && map.Keys.Contains(s.Key) && !s.IsDeleted)
            .ToListAsync(ct);

        var existing = new Dictionary<string, SystemSetting>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in existingRows)
            if (!string.IsNullOrEmpty(row.Key) && !existing.ContainsKey(row.Key))
                existing[row.Key] = row;

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

    /* ═══════════════════════════════════════════════════════
       چرخه دوازدهم — پیام‌های مستقیم پشتیبانی به داشبورد درمانگاه:
       با هر تپ‌قلب/فعال‌سازی موفق، آرایه messages از سرور دریافت و در
       SystemSettings (دسته ServerMessages) ذخیره می‌شود؛ داشبورد می‌خواند.
       شکست ذخیره هرگز جریان اصلی تپ‌قلب را نمی‌شکند.
       ═══════════════════════════════════════════════════════ */

    /// <summary>
    /// ذخیره پیام‌های دریافتی سرور — idempotent با کلید ServerMessages.{id}.
    /// چرخهٔ سی‌وهشتم: ذخیره پشت «تلاش مجدد خوش‌بینانه» رفت — ردیف‌های
    /// SystemSettings هم RowVersion (نشانهٔ همزمانی) دارند و دو تازه‌سازی
    /// هم‌زمان می‌توانند تصادم بزنند (همان ریشهٔ خطای لایسنس).
    /// </summary>
    private async Task SaveServerMessagesAsync(List<PortalMessage>? messages, CancellationToken ct)
    {
        if (messages is null || messages.Count == 0)
            return;
        try
        {
            await SaveSettingsWithRetryAsync(
                () => SaveServerMessagesCoreAsync(messages, ct), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ذخیره پیام‌های پشتیبانی ناموفق بود — تپ‌قلب ادامه می‌یابد");
        }
    }

    /// <summary>هستهٔ ذخیرهٔ پیام‌ها — بدون تلاش مجدد (پوشش آن در متد بیرونی)</summary>
    private async Task SaveServerMessagesCoreAsync(List<PortalMessage> messages, CancellationToken ct)
    {
            var fresh = new List<SystemSetting>();
            foreach (var m in messages.Take(20))
            {
                if (string.IsNullOrWhiteSpace(m.Title))
                    continue;
                var key = $"ServerMessages.{m.Id}";
                // ═══ چرخهٔ سی‌وهفتم (گزارش مالک — «رنگ کادر با انتخاب پنل فرق داشت») ═══
                // پیش از این چرخه پیامِ یک‌بار ذخیره‌شده برای همیشه «منجمد» می‌شد:
                // شرط exists ردیف را رد می‌کرد و حتی اگر فروشنده در پنل رنگ/متن/تاریخِ
                // پیام را ویرایش می‌کرد، نسخهٔ کهنه در دیتابیس می‌ماند — پیام‌هایِ
                // ذخیره‌شدهٔ پیش از چرخهٔ ۳۵ اصلاً فیلد color نداشتند و همیشه با
                // رنگِ سطح (نه رنگِ انتخابی) رندر می‌شدند. اکنون با مفهوم «upsert»:
                // ردیف موجود خوانده می‌شود و اگر نسخهٔ سرور (رنگ/عنوان/متن/سطح/تاریخ)
                // با نسخهٔ ذخیره‌شده فرق داشته باشد، مقدارش بازنویسی می‌شود؛ تنها
                // وقتی ردیف نبود یا یکسان بود که دست نمی‌زنیم.
                var existing = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Category == "ServerMessages" && s.Key == key && !s.IsDeleted, ct);
                if (existing is not null)
                {
                    var newValue = JsonSerializer.Serialize(
                        new { m.Id, m.Title, m.Body, m.Level, m.Date, m.Color }, JsonOptions);
                    if (!string.Equals(existing.Value, newValue, StringComparison.Ordinal))
                    {
                        existing.Value = newValue;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("پیام پشتیبانی {Key} از سرور لایسنس به‌روزرسانی شد", key);
                    }
                    continue;
                }
                fresh.Add(new SystemSetting
                {
                    Category = "ServerMessages",
                    Key = key,
                    // ═══ چرخهٔ سی‌وپنجم (گزارش مالک) — رنگ انتخابی پنل لایسنس ═══
                    // «رنگ کادر پیام با رنگ انتخابی در سرور لایسنس فرق دارد» —
                    // فیلد color که فروشنده در پنل PHP برای هر پیام برمی‌گزیند از
                    // این پس در زنجیره ذخیره/تحویل می‌ماند (مقدار خالی مجاز است).
                    Value = JsonSerializer.Serialize(new { m.Id, m.Title, m.Body, m.Level, m.Date, m.Color }, JsonOptions),
                    DataType = SettingDataType.String,
                    Description = "پیام پشتیبانی — دریافتی از سرور لایسنس",
                    IsSystem = false,
                });
            }
            if (fresh.Count > 0)
            {
                _context.SystemSettings.AddRange(fresh);
                await _context.SaveChangesAsync(ct);
                _logger.LogInformation("{Count} پیام پشتیبانی جدید از سرور لایسنس دریافت شد", fresh.Count);
            }
            else if (_context.ChangeTracker.Entries<SystemSetting>().Any(e => e.State == EntityState.Modified))
            {
                // ذخیرهٔ به‌روزرسانیِ ردیف‌های موجود (مسیر upsert)
                await _context.SaveChangesAsync(ct);
            }
    }

    /// <summary>
    /// پیام‌های پشتیبانی ذخیره‌شده — جدیدترین اول؛ داشبورد در کارت
    /// «پیام‌های پشتیبانی» نمایش می‌دهد
    /// </summary>
    public async Task<List<SupportMessageDto>> GetSupportMessagesAsync(CancellationToken ct = default)
    {
        try
        {
            var rows = await _context.SystemSettings.AsNoTracking()
                .Where(s => s.Category == "ServerMessages" && !s.IsDeleted)
                .OrderByDescending(s => s.Key)
                .Take(30)
                .ToListAsync(ct);
            var result = new List<SupportMessageDto>();
            foreach (var row in rows)
            {
                try
                {
                    var m = JsonSerializer.Deserialize<PortalMessage>(row.Value ?? "", JsonOptions);
                    if (m is null || string.IsNullOrWhiteSpace(m.Title))
                        continue;
                    // چرخهٔ سی‌وپنجم: رنگ انتخابی سرور هم تحویل می‌شود (خالی = کلاینت
                    // به رنگ‌بندی سطح اهمیت برمی‌گردد)
                    result.Add(new SupportMessageDto(m.Id, m.Title, m.Body ?? "", m.Level ?? "info", m.Date ?? "", m.Color));
                }
                catch (JsonException)
                {
                    // سطر خراب — نادیده گرفته می‌شود
                }
            }
            // مرتب‌سازی نزولی بر اساس شناسه پیام سرور
            return result.OrderByDescending(m => m.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "خواندن پیام‌های پشتیبانی ناموفق بود");
            return new List<SupportMessageDto>();
        }
    }

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
// ═══ چرخه ۲۴ — همگام‌سازی اطلاعات درمانگاه از سرور لایسنس ═══
// «اطلاعات درمانگاه فقط از سرور لایسنس خوانده شود» — با هر تپ قلب/اعتبارسنجی
// موفق آنلاین، مقادیر پنل لایسنس روی دستهٔ ClinicInfo تنظیمات محلی می‌نشیند.
public sealed partial class LicenseService
{
    /// <summary>
    /// اجرای یک عملیات «بارگذاری + تغییر + ذخیرهٔ تنظیمات» با تلاش مجدد خوش‌بینانه —
    /// چرخهٔ سی‌وهشتم (قانون هم‌کلاسی‌ها برای ریشهٔ DbUpdateConcurrencyException):
    /// ردیف‌های SystemSettings/ClinicLicense همگی RowVersion (نشانهٔ همزمانی
    /// خوش‌بینانه) دارند؛ دو تپ‌قلب/تازه‌سازی هم‌زمان می‌توانند نسخهٔ یکسان بخوانند و
    /// دومی هنگام ذخیره «۰ سطر» بیابد. این پوشش چرخه را با دادهٔ تازه دوباره اجرا
    /// می‌کند (تا ۴ تلاش با وقفهٔ پلکانی) و فقط موجودیت‌های همین عملیات را از
    /// ردیاب جدا می‌کند تا رکوردهای میدلورهای دیگر (مثل ممیزی) دست‌نخورده بمانند.
    /// </summary>
    /// <param name="operation">عملیات بارگذاری+تغییر+ذخیره — پس از هر تلاش ناموفق از نو اجرا می‌شود</param>
    /// <param name="ct">توکن لغو</param>
    private async Task SaveSettingsWithRetryAsync(Func<Task> operation, CancellationToken ct)
    {
        const int maxAttempts = 4;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                // جداکردن فقط موجودیت‌های این عملیات — نه کل ردیاب
                foreach (var entry in _context.ChangeTracker.Entries()
                             .Where(e => e.Entity is ClinicLicense or SystemSetting)
                             .ToList())
                    entry.State = EntityState.Detached;

                _logger.LogInformation(
                    "تصادم همزمانی در ذخیرهٔ تنظیمات لایسنس (تلاش {Attempt} از {Max}) — دوباره با دادهٔ تازه",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), ct);
            }
        }
    }

    /// <summary>
    /// جای‌گذاری اطلاعات درمانگاه دریافتی از سرور لایسنس در تنظیمات برنامه —
    /// فقط فیلدهای دارای مقدار بازنویسی می‌شوند (پنل می‌تواند فیلدی را خالی بگذارد).
    /// چرخهٔ سی‌وهشتم: ذخیره پشت تلاش مجدد خوش‌بینانه رفت (همان الگوی ریشهٔ
    /// خطای همزمانی لایسنس — این متد هم روی SystemSettings می‌نویسد).
    /// </summary>
    /// <param name="license">بدنهٔ لایسنس دریافتی از پورتال</param>
    /// <param name="ct">توکن لغو</param>
    private async Task SyncClinicInfoFromLicenseAsync(PortalLicensePayload license, CancellationToken ct)
    {
        try
        {
            await SaveSettingsWithRetryAsync(
                () => SyncClinicInfoCoreAsync(license, ct), ct);
        }
        catch
        {
            // همگام‌سازی اطلاعات درمانگاه هرگز نباید جریان اعتبارسنجی را متوقف کند
        }
    }

    /// <summary>هستهٔ همگام‌سازی اطلاعات درمانگاه — بدون تلاش مجدد (پوشش آن در متد بیرونی)</summary>
    private async Task SyncClinicInfoCoreAsync(PortalLicensePayload license, CancellationToken ct)
    {
        // نگاشت فیلدهای پورتال → کلیدهای تنظیمات ClinicInfo (همان که صفحهٔ تنظیمات می‌خواند)
            var map = new (string Key, string? Value)[]
            {
                ("ClinicName", license.ClinicName),
                ("ManagerName", license.ClinicManager),
                ("Phone", license.ClinicPhone),
                ("Email", license.ClinicEmail),
                ("City", license.ClinicCity),
                ("Address", license.ClinicAddress),
                // چرخهٔ بیست‌وهفتم — کد اقتصادی و شناسه ملی از پنل لایسنس
                ("EconomicCode", license.ClinicEconomicCode),
                ("NationalId", license.ClinicNationalId),
                // شماره لایسنس = همان کلیدی که درمانگاه با آن فعال شده
                ("LicenseNumber", license.LicenseKey),
            };

            foreach (var (key, value) in map)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue; // پنل مقدار نداده — مقدار فعلی درمانگاه دست‌نخورده می‌ماند

                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => !s.IsDeleted && s.Category == "ClinicInfo" && s.Key == key, ct);
                if (setting is null)
                {
                    _context.SystemSettings.Add(new SystemSetting
                    {
                        Category = "ClinicInfo",
                        Key = key,
                        Value = value.Trim(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    setting.Value = value.Trim();
                    setting.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync(ct);
    }
}

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

    /// <summary>پیام‌های مستقیم پشتیبانی به داشبورد — چرخه دوازدهم</summary>
    [JsonPropertyName("messages")] public List<PortalMessage>? Messages { get; set; }
}

/// <summary>یک پیام پشتیبانی دریافتی از سرور لایسنس</summary>
public sealed class PortalMessage
{
    /// <summary>شناسه پیام در سرور — برای تحویل تک‌بار</summary>
    [JsonPropertyName("id")] public long Id { get; set; }

    /// <summary>عنوان پیام</summary>
    [JsonPropertyName("title")] public string? Title { get; set; }

    /// <summary>متن پیام</summary>
    [JsonPropertyName("body")] public string? Body { get; set; }

    /// <summary>سطح اهمیت: info | success | warning | danger</summary>
    [JsonPropertyName("level")] public string? Level { get; set; }

    /// <summary>تاریخ ارسال (yyyy-MM-dd)</summary>
    [JsonPropertyName("date")] public string? Date { get; set; }

    /// <summary>
    /// ═══ چرخهٔ سی‌وپنجم (گزارش مالک) ═══
    /// «رنگ کادرش با رنگ انتخابی در سرور لایسنس فرق دارد» — فروشنده در پنل
    /// PHP برای هر پیام رنگ دلخواه (مثل #E53935) برمی‌گزیند؛ تهی/موجودنبودن
    /// مجاز است → کلاینت به رنگ‌بندی سطح اهمیت برمی‌گردد.
    /// </summary>
    [JsonPropertyName("color")] public string? Color { get; set; }
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

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — اطلاعات درمانگاه از سرور لایسنس ═══
    // «در تنظیمات، اطلاعات درمانگاه را از لایسنس بخوان و در اولین چک کردن لایسنس
    // در تنظیمات قرار بده؛ فقط از سرور لایسنس قابل تغییر باشد»
    /// <summary>نام درمانگاه</summary>
    [JsonPropertyName("clinicName")] public string? ClinicName { get; set; }
    /// <summary>نام مدیر درمانگاه</summary>
    [JsonPropertyName("clinicManager")] public string? ClinicManager { get; set; }
    /// <summary>تلفن درمانگاه</summary>
    [JsonPropertyName("clinicPhone")] public string? ClinicPhone { get; set; }
    /// <summary>ایمیل درمانگاه</summary>
    [JsonPropertyName("clinicEmail")] public string? ClinicEmail { get; set; }
    /// <summary>شهر</summary>
    [JsonPropertyName("clinicCity")] public string? ClinicCity { get; set; }
    /// <summary>نشانی کامل</summary>
    [JsonPropertyName("clinicAddress")] public string? ClinicAddress { get; set; }
    // ═══ چرخهٔ بیست‌وهفتم — تکمیل فیلدهای سرور لایسنس ═══
    // «کد اقتصادی» و «شناسه ملی» درمانگاه — ستون/فیلد پنل PHP در همین چرخه
    // تکمیل شد؛ بخش اطلاعات درمانگاه برنامه این دو را هم از لایسنس می‌گیرد.
    /// <summary>کد اقتصادی درمانگاه</summary>
    [JsonPropertyName("clinicEconomicCode")] public string? ClinicEconomicCode { get; set; }
    /// <summary>شناسه ملی درمانگاه</summary>
    [JsonPropertyName("clinicNationalId")] public string? ClinicNationalId { get; set; }

    /// <summary>روزهای باقیمانده</summary>
    [JsonPropertyName("daysRemaining")] public int? DaysRemaining { get; set; }

    /// <summary>امضای Base64 — فقط در فایل lic آفلاین داخل همین بدنه است</summary>
    [JsonPropertyName("signature")] public string? Signature { get; set; }

    /// <summary>اطلاعات پشتیبانی محصول — از پنل لایسنس ارسال می‌شود</summary>
    [JsonPropertyName("support")] public PortalSupportInfo? Support { get; set; }
}

/// <summary>پاسخ action=support پنل PHP — فقط بستهٔ اطلاعات پشتیبانی</summary>
public sealed class PortalSupportResponse
{
    /// <summary>بدنهٔ اطلاعات پشتیبانی</summary>
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
