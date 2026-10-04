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
