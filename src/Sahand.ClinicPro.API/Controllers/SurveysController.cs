using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نظرسنجی — گام V1 چرخه هفتم (فرم ۳۴):
/// فهرست نظرسنجی‌ها + ایجاد + ویرایش + فعال‌سازی + ثبت پاسخ + نتایج و آمار
/// نتایج شامل میانگین امتیاز به تفکیک پزشک، توزیع ستاره‌ها و لیست نظرات است.
/// مسیرها: /api/v1/surveys
/// </summary>
[ApiController]
[Route("api/v1/surveys")]
[Authorize]
public class SurveysController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    /// <summary>سازنده با تزریق واحد کار و تنظیمات نشانی نظرسنجی</summary>
    public SurveysController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    /// <summary>فهرست نظرسنجی‌ها با شمارنده پاسخ و میانگین امتیاز</summary>
    [HttpGet]
    [HasPermission("surveys.view")]
    public async Task<IActionResult> GetSurveys()
    {
        var surveys = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.Id)
            .Select(s => new SurveyListItemDto(
                s.Id,
                s.Title,
                s.Description,
                s.SurveyType.ToString(),
                s.IsActive,
                s.Responses.Count,
                s.Responses.Where(r => r.Rating != null).Average(r => (double?)r.Rating!) ?? 0,
                // ═══ چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) ═══ «نظرسنجی‌ها طراحی
                // نمی‌شوند، فقط به لیست اضافه می‌شوند» — ریشه: فهرست، سوالات JSON
                // را برنمی‌گرداند و فرم ویرایش هر بار سوالات را از نو بازنویسی
                // می‌کرد؛ اکنون متن خام سوالات هم در فهرست می‌آید تا طراح،
                // سوالات موجود را بارگذاری و ویرایش کند
                s.Questions))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(surveys));
    }

    /// <summary>ثبت نظرسنجی جدید — سوالات به‌صورت JSON (سازنده پویا در کلاینت)</summary>
    [HttpPost]
    [HasPermission("surveys.manage")]
    public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyRequest request)
    {
        // اعتبارسنجی عنوان — خالی یا بیش از حد بلند نباشد
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
            return BadRequest(ApiResponse<object>.Failure("عنوان نظرسنجی الزامی است و حداکثر ۱۵۰ نویسه"));

        var survey = new Survey
        {
            Title = title,
            Description = request.Description?.Trim(),
            SurveyType = NormalizeSurveyType(request.SurveyType),
            Questions = request.Questions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Survey>().AddAsync(survey);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { surveyId = survey.Id }, "نظرسنجی ایجاد شد"));
    }

    /// <summary>ویرایش نظرسنجی — عنوان، توضیح، سوالات و وضعیت (گام V1-ب)</summary>
    [HttpPut("{id:long}")]
    [HasPermission("surveys.manage")]
    public async Task<IActionResult> UpdateSurvey(long id, [FromBody] CreateSurveyRequest request)
    {
        var survey = await _unitOfWork.Repository<Survey>().QueryableTracking
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (survey is null)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی یافت نشد"));

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
            return BadRequest(ApiResponse<object>.Failure("عنوان نظرسنجی الزامی است و حداکثر ۱۵۰ نویسه"));

        // پاسخ‌های ثبت‌شده حفظ می‌شوند؛ ویرایش سوالات روی پاسخ‌های قبلی اثری ندارد
        survey.Title = title;
        survey.Description = request.Description?.Trim();
        survey.Questions = request.Questions;
        survey.SurveyType = NormalizeSurveyType(request.SurveyType);
        if (request.IsActive.HasValue)
            survey.IsActive = request.IsActive.Value;
        survey.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("نظرسنجی به‌روزرسانی شد"));
    }

    /// <summary>
    /// حذف نرم نظرسنجی — چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک): «دکمه‌های عملیاتی
    /// لیست نظرسنجی ناقص است» — حذف با تأییدیهٔ فارسی خود برنامه در کلاینت؛
    /// پاسخ‌های ثبت‌شدهٔ نظرسنجی حفظ می‌شوند (حذف نرم ISoftDeletable).
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPermission("surveys.manage")]
    public async Task<IActionResult> DeleteSurvey(long id)
    {
        var survey = await _unitOfWork.Repository<Survey>().QueryableTracking
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (survey is null)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی یافت نشد"));

        // حذف نرم — رکورد و پاسخ‌ها در دیتابیس می‌مانند ولی از فهرست‌ها کنار می‌روند
        survey.IsDeleted = true;
        survey.IsActive = false;
        survey.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage($"نظرسنجی «{survey.Title}» حذف شد"));
    }

    /// <summary>فعال/غیرفعال‌سازی نظرسنجی</summary>
    [HttpPost("{id:long}/toggle-active")]
    [HasPermission("surveys.manage")]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var survey = await _unitOfWork.Repository<Survey>().QueryableTracking
            .FirstOrDefaultAsync(s => s.Id == id);
        if (survey is null)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی یافت نشد"));

        survey.IsActive = !survey.IsActive;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage(
            survey.IsActive ? "نظرسنجی فعال شد" : "نظرسنجی غیرفعال شد"));
    }

    /// <summary>ثبت پاسخ نظرسنجی — امتیاز ۱ تا ۵ + نظر آزاد</summary>
    [HttpPost("{id:long}/responses")]
    [Authorize]
    [HasPermission("surveys.view")] // ممیزی چرخه ۱۲ — ثبت پاسخ توسط پرسنل/کیوسک
public async Task<IActionResult> SubmitResponse(long id, [FromBody] SubmitResponseRequest request)
    {
        var exists = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .AnyAsync(s => s.Id == id && s.IsActive);
        if (!exists)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی فعال یافت نشد"));

        var response = new SurveyResponse
        {
            SurveyId = id,
            PatientId = request.PatientId,
            VisitId = request.VisitId,
            DoctorId = request.DoctorId,
            Rating = request.Rating is >= 1 and <= 5 ? request.Rating : null,
            Comments = request.Comments?.Trim(),
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<SurveyResponse>().AddAsync(response);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("پاسخ شما ثبت شد — سپاس از همراهی"));
    }

    /// <summary>
    /// نتایج و آمار نظرسنجی (گام V1-الف):
    /// کارت‌های خلاصه (تعداد پاسخ + میانگین کل + پاسخ ۳۰ روز اخیر)
    /// + توزیع ستاره‌های ۱ تا ۵ + میانگین امتیاز هر پزشک + ۲۰ نظر آزاد اخیر با تاریخ شمسی
    /// </summary>
    [HttpGet("{id:long}/results")]
    [HasPermission("surveys.view")]
    public async Task<IActionResult> GetResults(long id)
    {
        var surveyExists = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .AnyAsync(s => s.Id == id && !s.IsDeleted);
        if (!surveyExists)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی یافت نشد"));

        var responses = await _unitOfWork.Repository<SurveyResponse>().QueryableNoTracking
            .Where(r => r.SurveyId == id && !r.IsDeleted)
            .ToListAsync();

        var cutoff = DateTime.UtcNow.AddDays(-30);

        // ─── کارت‌های خلاصه ───
        var rated = responses.Where(r => r.Rating is >= 1 and <= 5).ToList();
        var summary = new SurveyResultsSummaryDto(
            responses.Count,
            rated.Count == 0 ? 0 : Math.Round(rated.Average(r => r.Rating!.Value), 2),
            responses.Count(r => r.SubmittedAt >= cutoff));

        // ─── توزیع ستاره‌ها (۵ تا ۱) ───
        var distribution = Enumerable.Range(1, 5)
            .OrderByDescending(star => star)
            .Select(star => new StarDistributionDto(star,
                rated.Count(r => r.Rating == star)))
            .ToList();

        // ─── میانگین امتیاز به تفکیک پزشک — نام از پرسنل مرتبط ───
        var doctors = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .Select(d => new { d.Id, FirstName = d.Employee != null ? d.Employee.FirstName : null,
                LastName = d.Employee != null ? d.Employee.LastName : null })
            .ToListAsync();
        var doctorNames = doctors.ToDictionary(
            d => d.Id,
            d => d.FirstName != null ? $"{d.FirstName} {d.LastName}" : null);

        var perDoctor = responses
            .Where(r => r.DoctorId.HasValue && r.Rating is >= 1 and <= 5)
            .GroupBy(r => r.DoctorId!.Value)
            .Select(g => new DoctorRatingDto(
                g.Key,
                doctorNames.GetValueOrDefault(g.Key) ?? $"پزشک #{g.Key}",
                g.Count(),
                Math.Round(g.Average(r => r.Rating!.Value), 2)))
            .OrderByDescending(d => d.Average);

        // ─── نظرات آزاد اخیر (۲۰ مورد) با نام بیمار و تاریخ شمسی ───
        var patientIds = responses.Where(r => r.PatientId.HasValue).Select(r => r.PatientId!.Value).Distinct().ToList();
        var patients = await _unitOfWork.Repository<Patient>().QueryableNoTracking
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName })
            .ToListAsync();
        var patientNames = patients.ToDictionary(p => p.Id, p => $"{p.FirstName} {p.LastName}");

        var comments = responses
            .Where(r => !string.IsNullOrWhiteSpace(r.Comments))
            .OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
            .Take(20)
            .Select(r => new SurveyCommentDto(
                r.Id,
                r.PatientId.HasValue ? patientNames.GetValueOrDefault(r.PatientId.Value) ?? "بیمار حذف‌شده" : "بیننده ناشناس",
                r.DoctorId.HasValue ? doctorNames.GetValueOrDefault(r.DoctorId.Value) ?? "—" : "—",
                r.Rating,
                r.Comments!,
                r.SubmittedAt.HasValue
                    ? PersianDateHelper.ToShamsi(r.SubmittedAt.Value) + " — " + r.SubmittedAt.Value.ToString("HH:mm")
                    : PersianDateHelper.ToShamsi(r.CreatedAt)))
            .ToList();

        var result = new SurveyResultsDto(summary, distribution, perDoctor.ToList(), comments);
        return Ok(ApiResponse<object>.Success(result));
    }

    /// <summary>
    /// ارسال لینک پیامکی نظرسنجی به بیماران ویزیت‌شده امروز (گام V3-الف):
    /// ویزیت‌های امروزِ تکمیل‌شده/در جریان با بیمار دارای موبایل → درج در صف پیامک (Status=Queued)
    /// که توسط پردازشگر صف (Hangfire) ارسال می‌شود — بدون ارسال مستقیم و بدون بلاک‌شدن درخواست.
    /// </summary>
    [HttpPost("{id:long}/send-links")]
    [HasPermission("surveys.manage")]
    public async Task<IActionResult> SendSurveyLinks(long id, [FromBody] SendLinksRequest? request)
    {
        // نظرسنجی باید فعال باشد
        var surveyActive = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .AnyAsync(s => s.Id == id && s.IsActive && !s.IsDeleted);
        if (!surveyActive)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی فعال یافت نشد"));

        // سرویس عمومی پاسخ‌گویی در این برنامه تعبیه نشده است. ارسال به دامنهٔ
        // حدسی می‌تواند پیامک حاوی لینک نامعتبر به بیماران بفرستد؛ فقط نشانی
        // صریح سرویس بیرونی که مدیر نصب پیکربندی کرده قابل استفاده است.
        var configuredUrl = string.IsNullOrWhiteSpace(request?.BaseUrl)
            ? _configuration["Surveys:PublicBaseUrl"] : request!.BaseUrl;
        if (!Uri.TryCreate(configuredUrl?.Trim(), UriKind.Absolute, out var publicUri) ||
            publicUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(publicUri.UserInfo) ||
            !string.IsNullOrEmpty(publicUri.Query) || !string.IsNullOrEmpty(publicUri.Fragment))
            return BadRequest(ApiResponse<object>.Failure("نشانی HTTPS سرویس عمومی پاسخ‌گویی نظرسنجی تنظیم نشده یا نامعتبر است؛ پیش از ارسال پیامک آن را تنظیم و بررسی کنید"));
        var link = $"{publicUri.AbsoluteUri.TrimEnd('/')}/s/{id}";

        // مرز امروز به وقت محلی ایران (۱۱۰۴۰۰+ ثانیه از UTC، بدون DST)
        var iranNow = DateTime.UtcNow.AddHours(3.5);
        var todayStart = iranNow.Date.AddHours(-3.5);

        // ویزیت‌های امروز با وضعیت معنادار (شروع‌شده/در جریان/تکمیل‌شده) و بیمار دارای موبایل
        var visits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.VisitDate >= todayStart && v.VisitDate < todayStart.AddDays(1)
                     && v.Status != Domain.Enums.VisitStatus.Draft
                     && v.Patient != null && v.Patient.Mobile != "")
            .Select(v => new
            {
                v.PatientId,
                v.Patient!.FirstName,
                v.Patient.Mobile
            })
            .Distinct()
            .ToListAsync();

        if (visits.Count == 0)
            return Ok(ApiResponse<object>.Success(
                new { queuedCount = 0 }, "امروز ویزیت فعالی برای ارسال لینک یافت نشد"));

        // درج در صف پیامک — پردازشگر صف به‌صورت پس‌زمینه ارسال می‌کند
        var queueRepo = _unitOfWork.Repository<SmsQueueItem>();
        foreach (var v in visits)
        {
            var message =
                $"{v.FirstName} گرامی، از اعتماد شما به درمانگاه سهند سپاسگزاریم. " +
                $"لطفاً با ثبت نظرتان کیفیت خدمات امروز را به ما کمک کنید: {link}";
            await queueRepo.AddAsync(new SmsQueueItem
            {
                Recipient = v.Mobile,
                Message = message,
                ScheduledAt = DateTime.UtcNow,
                Status = SmsStatus.Queued,
                Priority = 5,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.Success(
            new { queuedCount = visits.Count, link },
            $"{visits.Count} پیامک لینک نظرسنجی در صف ارسال قرار گرفت"));
    }

    /* ─── مدل‌ها ─── */

    /// <summary>نرمال‌سازی نوع نظرسنجی — مقدار نامعتبر به «رضایت» برمی‌گردد</summary>
    private static SurveyType NormalizeSurveyType(SurveyType? value) =>
        value is SurveyType.Satisfaction or SurveyType.Service or SurveyType.Doctor
            ? value.Value
            : SurveyType.Satisfaction;
    /// <summary>ردیف نظرسنجی</summary>
    public record SurveyListItemDto(long Id, string Title, string? Description,
        string Type, bool IsActive, int ResponseCount, double AverageRating,
        string? Questions);

    /// <summary>درخواست ایجاد/ویرایش نظرسنجی — سوالات JSON با سازنده پویای کلاینت</summary>
    public record CreateSurveyRequest(string Title, string? Description, string? Questions,
        SurveyType? SurveyType, bool? IsActive);

    /// <summary>درخواست ثبت پاسخ</summary>
    public record SubmitResponseRequest(long? PatientId, long? VisitId, long? DoctorId,
        byte? Rating, string? Comments);

    /// <summary>درخواست ارسال لینک — نشانی پایه اختیاری لینک نظرسنجی (پیش‌فرض دامنه محصول)</summary>
    public record SendLinksRequest(string? BaseUrl);

    /// <summary>کارت‌های خلاصه نتایج</summary>
    public record SurveyResultsSummaryDto(int TotalResponses, double AverageRating, int ResponsesLast30Days);

    /// <summary>توزیع یک ستاره</summary>
    public record StarDistributionDto(int Star, int Count);

    /// <summary>میانگین امتیاز یک پزشک</summary>
    public record DoctorRatingDto(long DoctorId, string DoctorName, int ResponseCount, double Average);

    /// <summary>نظر آزاد بیمار</summary>
    public record SurveyCommentDto(long Id, string PatientName, string DoctorName,
        byte? Rating, string Comment, string SubmittedShamsi);

    /// <summary>بسته کامل نتایج</summary>
    public record SurveyResultsDto(SurveyResultsSummaryDto Summary,
        List<StarDistributionDto> StarDistribution,
        List<DoctorRatingDto> DoctorRatings,
        List<SurveyCommentDto> RecentComments);
}
