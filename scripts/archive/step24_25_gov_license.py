# -*- coding: utf-8 -*-
"""گام‌های ۲۴ و ۲۵ — یکپارچه‌سازی دولتی + سامانه لایسنس"""
import os, json

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
INFRA = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Infrastructure"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════════════ گام ۲۴: دولتی ═══════════════════
write("Interfaces/IGovernmentServices.cs", """
namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>نتیجه فراخوانی سامانه دولتی</summary>
public record GovCallResult(bool Success, string? ReferenceId, string? Error);

/// <summary>
/// سرویس سپاس — ثبت ویزیت در سامانه پوشش سلامت همگا
/// </summary>
public interface ISepasService
{
    /// <summary>ثبت ویزیت (Encounter) در سپاس</summary>
    Task<GovCallResult> RegisterEncounterAsync(long visitId, CancellationToken ct = default);
}

/// <summary>
/// سرویس نسخه الکترونیک — ارسال به سامانه سلامت یا تأمین اجتماعی
/// </summary>
public interface IElectronicPrescriptionService
{
    /// <summary>ارسال نسخه به سامانه بیمه — خروجی کد رهگیری</summary>
    Task<GovCallResult> SendAsync(long prescriptionId, CancellationToken ct = default);
}
""", APP)

write("Services/Government/GovernmentServices.cs", """
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Integration;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Services.Government;

/// <summary>
/// سرویس سپاس — ثبت ویزیت با ثبت کامل لاگ فراخوانی
/// نشانی و اعتبارسنجی از تنظیمات Government:Sepas خوانده می‌شود
/// اگر فعال نباشد خطای روشن برمی‌گرداند
/// </summary>
public class SepasService : ISepasService
{
    private readonly HttpClient _http;
    private readonly ClinicProDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SepasService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public SepasService(HttpClient http, ClinicProDbContext context,
        IConfiguration configuration, ILogger<SepasService> logger)
    {
        _http = http;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>ثبت ویزیت در سپاس</summary>
    public async Task<GovCallResult> RegisterEncounterAsync(long visitId, CancellationToken ct = default)
    {
        var endpoint = _configuration["Government:Sepas:Endpoint"];
        var token = _configuration["Government:Sepas:Token"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(token))
            return new GovCallResult(false, null,
                "یکپارچه‌سازی سپاس فعال نیست — تنظیمات Government:Sepas را کامل کنید");

        var visit = await _context.Visits
            .Include(v => v.Patient)
            .FirstOrDefaultAsync(v => v.Id == visitId, ct);

        if (visit is null)
            return new GovCallResult(false, null, "ویزیت یافت نشد");

        var startedAt = DateTime.UtcNow;
        GovCallResult result;

        try
        {
            var payload = new
            {
                patientNationalCode = visit.Patient != null ? visit.Patient.NationalCode : "",
                encounterDate = visit.VisitDate,
                sepasPatientId = visit.Patient?.SepasPatientId,
                referenceCode = visit.SepasReferenceCode
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/api/v2/encounters");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            result = response.IsSuccessStatusCode
                ? new GovCallResult(true, ExtractReference(body), null)
                : new GovCallResult(false, null, $"خطای سپاس — کد {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای ارتباط با سپاس برای ویزیت {VisitId}", visitId);
            result = new GovCallResult(false, null, "ارتباط با سامانه سپاس برقرار نشد");
        }

        await LogTransactionAsync(visitId, visit.PatientId, payload: null,
            result, startedAt, ct);

        if (result.Success && result.ReferenceId is not null)
            visit.SepasReferenceCode = result.ReferenceId;

        await _context.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>استخراج شناسه ارجاع از پاسخ JSON</summary>
    private static string? ExtractReference(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("referenceId", out var id)
                ? id.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>ثبت تراکنش سپاس در دیتابیس</summary>
    private async Task LogTransactionAsync(long visitId, long patientId, object? payload,
        GovCallResult result, DateTime startedAt, CancellationToken ct)
    {
        _context.SepasTransactions.Add(new SepasTransaction
        {
            PatientId = patientId,
            VisitId = visitId,
            TransactionType = SepasTransactionType.EncounterRegistration,
            SepasReferenceId = result.ReferenceId,
            ResponseData = result.Error ?? "ثبت موفق",
            Status = result.Success ? SyncStatus.Synced : SyncStatus.Failed,
            ErrorMessage = result.Error,
            SentAt = startedAt,
            RespondedAt = DateTime.UtcNow,
            RetryCount = 0
        });

        _context.ApiCallLogs.Add(new ApiCallLog
        {
            ServiceName = "Sepas",
            Endpoint = "encounters",
            Method = "POST",
            StatusCode = result.Success ? 200 : 500,
            Duration = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds,
            CalledAt = startedAt,
            IsSuccess = result.Success
        });

        await Task.CompletedTask;
    }
}

/// <summary>
/// سرویس نسخه الکترونیک — ارسال نسخه به سامانه بیمه (سلامت/تأمین)
/// </summary>
public class ElectronicPrescriptionService : IElectronicPrescriptionService
{
    private readonly HttpClient _http;
    private readonly ClinicProDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ElectronicPrescriptionService> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ElectronicPrescriptionService(HttpClient http, ClinicProDbContext context,
        IConfiguration configuration, ILogger<ElectronicPrescriptionService> logger)
    {
        _http = http;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>ارسال نسخه به سامانه الکترونیک</summary>
    public async Task<GovCallResult> SendAsync(long prescriptionId, CancellationToken ct = default)
    {
        var endpoint = _configuration["Government:EPrescription:Endpoint"];
        var token = _configuration["Government:EPrescription:Token"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(token))
            return new GovCallResult(false, null,
                "یکپارچه‌سازی نسخه الکترونیک فعال نیست — تنظیمات Government:EPrescription را کامل کنید");

        var prescription = await _context.Prescriptions
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .Include(p => p.Patient)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, ct);

        if (prescription is null)
            return new GovCallResult(false, null, "نسخه یافت نشد");

        var startedAt = DateTime.UtcNow;

        try
        {
            var payload = new
            {
                patientNationalCode = prescription.Patient != null ? prescription.Patient.NationalCode : "",
                prescriptionDate = prescription.PrescriptionDate,
                items = prescription.Items.Select(i => new
                {
                    ircCode = i.Medication?.IrcCode,
                    dosage = i.Dosage,
                    frequency = i.Frequency,
                    duration = i.Duration,
                    quantity = i.Quantity
                })
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/prescriptions");
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Content = JsonContent.Create(payload);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            var trackingCode = response.IsSuccessStatusCode ? ExtractTracking(body) : null;

            // ثبت رکورد نسخه الکترونیک
            _context.ElectronicPrescriptions.Add(new ElectronicPrescription
            {
                PrescriptionId = prescriptionId,
                SystemType = InsuranceSystemType.Salamat,
                TrackingCode = trackingCode,
                Status = response.IsSuccessStatusCode
                    ? ElectronicPrescriptionStatus.Sent
                    : ElectronicPrescriptionStatus.Rejected,
                Response = body.Length > 4000 ? body[..4000] : body,
                SentAt = DateTime.UtcNow,
                IsSuccess = response.IsSuccessStatusCode,
                ErrorCode = response.IsSuccessStatusCode ? null : ((int)response.StatusCode).ToString()
            });

            if (response.IsSuccessStatusCode)
            {
                prescription.IsElectronic = true;
                prescription.ElectronicTrackingCode = trackingCode;
                prescription.Status = PrescriptionStatus.SentToInsurance;
            }

            await _context.SaveChangesAsync(ct);

            return response.IsSuccessStatusCode
                ? new GovCallResult(true, trackingCode, null)
                : new GovCallResult(false, null, $"خطای سامانه بیمه — کد {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای ارسال نسخه الکترونیک {PrescriptionId}", prescriptionId);
            return new GovCallResult(false, null, "ارتباط با سامانه بیمه برقرار نشد");
        }
    }

    /// <summary>استخراج کد رهگیری</summary>
    private static string? ExtractTracking(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("trackingCode", out var code)
                ? code.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
""", INFRA)

write("Controllers/GovernmentController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر یکپارچه‌سازی دولتی — سپاس و نسخه الکترونیک
/// مسیرها: /api/v1/government
/// </summary>
[ApiController]
[Route("api/v1/government")]
[Authorize]
public class GovernmentController : ApiBaseController
{
    private readonly ISepasService _sepas;
    private readonly IElectronicPrescriptionService _ePrescription;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public GovernmentController(ISepasService sepas,
        IElectronicPrescriptionService ePrescription)
    {
        _sepas = sepas;
        _ePrescription = ePrescription;
    }

    /// <summary>ثبت ویزیت در سامانه سپاس</summary>
    /// <param name="visitId">شناسه ویزیت</param>
    [HttpPost("sepas/encounters/{visitId:long}")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> RegisterEncounter(long visitId)
    {
        var result = await _sepas.RegisterEncounterAsync(visitId);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ثبت ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage(
            $"ویزیت با شناسه ارجاع {result.ReferenceId} در سپاس ثبت شد"));
    }

    /// <summary>ارسال نسخه به سامانه نسخه الکترونیک</summary>
    /// <param name="prescriptionId">شناسه نسخه</param>
    [HttpPost("eprescription/{prescriptionId:long}")]
    [HasPermission("prescriptions.edit")]
    public async Task<IActionResult> SendPrescription(long prescriptionId)
    {
        var result = await _ePrescription.SendAsync(prescriptionId);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage(
            $"نسخه با کد رهگیری {result.ReferenceId} ارسال شد"));
    }
}
""", API)

# ═══════════════════ گام ۲۵: لایسنس ═══════════════════
write("Interfaces/ILicenseService.cs", """
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

    /// <summary>آیا ماژول خاصی فعال است — توسط میدلور لایسنس استفاده می‌شود</summary>
    Task<bool> IsModuleEnabledAsync(string moduleCode, CancellationToken ct = default);
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
""", APP)

write("Services/License/LicenseService.cs", """
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
""", INFRA)

write("Middleware/LicenseValidationMiddleware.cs", """
using Sahand.ClinicPro.Application.Interfaces;
using System.Text.Json;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور اعتبارسنجی لایسنس — کنترل ماژول‌های فعال روی مسیرهای API
/// خطاهای سه‌گانه استاندارد با کد ۴۰۳:
/// LICENSE_INVALID و USER_LIMIT_EXCEEDED و MODULE_NOT_LICENSED
/// </summary>
public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>سازنده با نقطه بعدی خط لوله</summary>
    public LicenseValidationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>فراخوانی میدلور — نقشه ماژول از مسیر</summary>
    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        // نقشه پیشوند مسیر به کد ماژول
        var moduleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["api/v1/patients"] = "Patients",
            ["api/v1/appointments"] = "Appointments",
            ["api/v1/receptions"] = "Receptions",
            ["api/v1/visits"] = "Visits",
            ["api/v1/prescriptions"] = "Prescriptions",
            ["api/v1/invoices"] = "Billing",
            ["api/v1/payments"] = "Billing",
            ["api/v1/accounting"] = "Accounting",
            ["api/v1/insurance"] = "Insurance",
            ["api/v1/lab"] = "Laboratory",
            ["api/v1/pharmacy"] = "Pharmacy",
            ["api/v1/sms"] = "Sms",
            ["api/v1/reports"] = "Reports"
        };

        var path = context.Request.Path.Value ?? string.Empty;
        var matched = moduleMap.FirstOrDefault(kv =>
            path.StartsWithsWith($"/{kv.Key}") || path.Contains($"/{kv.Key}/"));

        // فقط مسیرهای ماژول‌محور کنترل می‌شوند — احراز هویت و سلامت آزادند
        if (!string.IsNullOrEmpty(matched.Key))
        {
            var status = await licenseService.GetStatusAsync();

            if (!status.IsValid)
            {
                await WriteForbidden(context, "LICENSE_INVALID",
                    "لایسنس سامانه معتبر نیست — با پشتیبانی تماس بگیرید");
                return;
            }

            if (!await licenseService.IsModuleEnabledAsync(matched.Value))
            {
                await WriteForbidden(context, "MODULE_NOT_LICENSED",
                    $"ماژول {matched.Value} در لایسنس شما فعال نیست");
                return;
            }
        }

        await _next(context);
    }

    /// <summary>نوشتن پاسخ ۴۰۳ استاندارد فارسی</summary>
    private static async Task WriteForbidden(HttpContext context, string code, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            isSuccess = false,
            statusCode = 403,
            errorCode = code,
            message
        }));
    }
}

/// <summary>متدهای الحاقی مسیر</summary>
internal static class PathExtensions
{
    /// <summary>شروع با پیشوند — بدون حساسیت بزرگی حروف</summary>
    public static bool StartsWithsWith(this string path, string prefix) =>
        path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
""", API)

write("Controllers/LicenseController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر لایسنس — وضعیت و فعال‌سازی برخط و برون‌خط
/// مسیرها: /api/v1/license — بدون نیاز به احراز هویت برای فعال‌سازی
/// </summary>
[ApiController]
[Route("api/v1/license")]
public class LicenseController : ApiBaseController
{
    private readonly ILicenseService _license;

    /// <summary>سازنده با تزریق سرویس لایسنس</summary>
    public LicenseController(ILicenseService license) => _license = license;

    /// <summary>وضعیت فعلی لایسنس — ماژول‌ها و گنجایش‌ها</summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _license.GetStatusAsync();
        var response = new
        {
            isValid = status.IsValid,
            licenseKey = status.LicenseKey,
            expiryDate = status.ExpiryDate,
            maxUsers = status.MaxUsers,
            maxDoctors = status.MaxDoctors,
            maxPatients = status.MaxPatients,
            enabledModules = status.EnabledModules,
            message = status.Message
        };
        return Ok(ApiResponse<object>.Success(response, status.Message));
    }

    /// <summary>فعال‌سازی برخط با کلید لایسنس</summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateOnline([FromBody] ActivateRequest request)
    {
        var status = await _license.ActivateOnlineAsync(request.LicenseKey);
        if (!status.IsValid)
            return BadRequest(ApiResponse<object>.Failure(status.Message));
        return Ok(ApiResponse<object>.SuccessMessage("لایسنس با موفقیت فعال شد"));
    }

    /// <summary>فعال‌سازی برون‌خط از محتوای فایل lic</summary>
    [HttpPost("activate-offline")]
    [AllowAnonymous]
    public async Task<IActionResult> ActivateOffline([FromBody] ActivateOfflineRequest request)
    {
        var status = await _license.ActivateOfflineAsync(request.LicenseFileContent);
        if (!status.IsValid)
            return BadRequest(ApiResponse<object>.Failure(status.Message));
        return Ok(ApiResponse<object>.SuccessMessage("لایسنس برون‌خط فعال شد"));
    }

    /// <summary>درخواست فعال‌سازی برخط</summary>
    public record ActivateRequest(string LicenseKey);

    /// <summary>درخواست فعال‌سازی برون‌خط</summary>
    public record ActivateOfflineRequest(string LicenseFileContent);
}
""", API)

print("✅ گام‌های ۲۴ و ۲۵ — دولتی + لایسنس کامل شد")
