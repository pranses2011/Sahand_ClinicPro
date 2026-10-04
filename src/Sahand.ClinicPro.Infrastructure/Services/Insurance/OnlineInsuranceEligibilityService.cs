using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Integration;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Infrastructure.Services.Insurance;

/// <summary>
/// سرویس استعلام برخط اعتبار بیمه — متصل به ApiEndpoint شرکت بیمه
/// </summary>
/// <remarks>
/// الگوی پایداری:
/// ۱. کش درون‌حافظه‌ای ۳۰ دقیقه‌ای برای جلوگیری از استعلام تکراری
/// ۲. سه تلاش با وقفه نمایی هنگام خطای شبکه
/// ۳. سقوط به حالت برون‌خط در قطع ارتباط — بررسی رابطه بیمار/بیمه محلی
/// ۴. ثبت کامل هر تماس در ApiCallLog برای پایش
/// </remarks>
public class OnlineInsuranceEligibilityService : IInsuranceEligibilityService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    /// <summary>کش نتیجه استعلام — کلید ترکیبی بیمار/بیمه</summary>
    private static readonly ConcurrentDictionary<string, (InsuranceEligibilityResult Result, DateTime ExpiresAt)> Cache = new();

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>سازنده با کارخانه HttpClient و واحد کار و پیکربندی</summary>
    public OnlineInsuranceEligibilityService(IHttpClientFactory httpClientFactory,
        IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    /// <summary>استعلام اعتبار — کش → برخط → برون‌خط</summary>
    public async Task<InsuranceEligibilityResult> CheckAsync(InsuranceEligibilityRequest request,
        CancellationToken ct = default)
    {
        var cacheKey = $"eligibility:{request.PatientId}:{request.InsuranceCompanyId}";
        if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Result with { Message = cached.Result.Message + " (کش)" };

        var company = await _unitOfWork.Repository<Domain.Entities.Billing.InsuranceCompany>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(c => c.Id == request.InsuranceCompanyId && !c.IsDeleted, ct);

        if (company is null)
            return new InsuranceEligibilityResult(false, false, "شرکت بیمه یافت نشد");

        // بدون نشانی API — مستقیم حالت برون‌خط
        if (string.IsNullOrWhiteSpace(company.ApiEndpoint))
            return await OfflineFallbackAsync(request, "بیمه فاقد نشانی برخط است");

        var result = await CheckOnlineAsync(company.ApiEndpoint, request, ct);

        if (result.WasOnline)
        {
            Cache[cacheKey] = (result, DateTime.UtcNow.AddMinutes(30));
            return result;
        }

        // سقوط به برون‌خط — پاسخ برخط در دسترس نبود
        var fallback = await OfflineFallbackAsync(request,
            $"برخط ناموفق بود ({result.Message}) — استعلام برون‌خط انجام شد");
        return fallback;
    }

    /// <summary>تماس برخط با سه تلاش و ثبت ApiCallLog</summary>
    private async Task<InsuranceEligibilityResult> CheckOnlineAsync(string endpoint,
        InsuranceEligibilityRequest request, CancellationToken ct)
    {
        var retryCount = _configuration.GetValue("Insurance:RetryCount", 3);
        var timeoutSeconds = _configuration.GetValue("Insurance:TimeoutSeconds", 15);
        var url = $"{endpoint.TrimEnd('/')}/eligibility";
        var payload = JsonSerializer.Serialize(new
        {
            nationalCode = request.NationalCode,
            insuranceNumber = request.InsuranceNumber
        }, Json);

        using var client = _httpClientFactory.CreateClient("insurance-eligibility");
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            int? statusCode = null;
            string? responseBody = null;
            try
            {
                using var response = await client.PostAsync(url,
                    new StringContent(payload, System.Text.Encoding.UTF8, "application/json"), ct);
                statusCode = (int)response.StatusCode;
                responseBody = await response.Content.ReadAsStringAsync(ct);
                stopwatch.Stop();

                await LogCallAsync(url, payload, responseBody, statusCode,
                    stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode, ct);

                if (!response.IsSuccessStatusCode)
                    return new InsuranceEligibilityResult(false, true,
                        $"سامانه بیمه کد {(int)response.StatusCode} برگرداند");

                var doc = JsonSerializer.Deserialize<EligibilityResponse>(responseBody, Json);
                if (doc is null)
                    return new InsuranceEligibilityResult(false, true, "پاسخ بیمه قابل تفسیر نبود");

                return new InsuranceEligibilityResult(
                    doc.IsEligible, true,
                    doc.IsEligible ? "بیمه معتبر است" : "پوشش بیمه معتبر نیست",
                    doc.ApprovalCode,
                    DateOnly.TryParse(doc.ValidUntil, out var validUntil) ? validUntil : null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                stopwatch.Stop();
                statusCode ??= 0;
                await LogCallAsync(url, payload, ex.Message, statusCode,
                    stopwatch.ElapsedMilliseconds, false, ct);

                if (attempt >= retryCount)
                    return new InsuranceEligibilityResult(false, false,
                        $"اتصال به سامانه بیمه پس از {retryCount} تلاش برقرار نشد");

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), ct);
            }
        }

        return new InsuranceEligibilityResult(false, false, "استعلام برخط انجام نشد");
    }

    /// <summary>حالت برون‌خط — بررسی رابطه بیمار/بیمه از داده محلی</summary>
    private async Task<InsuranceEligibilityResult> OfflineFallbackAsync(
        InsuranceEligibilityRequest request, string reason)
    {
        if (!_configuration.GetValue("Insurance:OfflineFallback", true))
            return new InsuranceEligibilityResult(false, false, reason);

        var linked = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .QueryableNoTracking
            .AnyAsync(p => p.Id == request.PatientId
                && p.InsuranceCompanyId == request.InsuranceCompanyId, CancellationToken.None);

        return linked
            ? new InsuranceEligibilityResult(true, false, $"پوشش بیمه در قرارداد محلی ثبت است — {reason}")
            : new InsuranceEligibilityResult(false, false, $"بیمار با این بیمه قرارداد محلی ندارد — {reason}");
    }

    /// <summary>ثبت تماس خارجی در ApiCallLog</summary>
    private async Task LogCallAsync(string url, string request, string? response,
        int? statusCode, long duration, bool isSuccess, CancellationToken ct)
    {
        try
        {
            await _unitOfWork.Repository<ApiCallLog>().AddAsync(new ApiCallLog
            {
                ServiceName = "InsuranceEligibility",
                Endpoint = url,
                Method = "POST",
                RequestBody = request,
                ResponseBody = response is { Length: > 2000 } ? response[..2000] : response,
                StatusCode = statusCode,
                Duration = (int)duration,
                CalledAt = DateTime.UtcNow,
                IsSuccess = isSuccess
            }, ct);
            await _unitOfWork.SaveEntitiesAsync(ct);
        }
        catch
        {
            // خطای لاگ هرگز استعلام را نمی‌شکند
        }
    }

    /// <summary>قرارداد پاسخ سامانه بیمه</summary>
    /// <remarks>فیلدهای استاندارد مورد انتظار پنل بیمه — در مستندات استقرار توضیح داده شده</remarks>
    private sealed class EligibilityResponse
    {
        public bool IsEligible { get; set; }
        public string? ApprovalCode { get; set; }
        public string? ValidUntil { get; set; }
    }
}