using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        var source = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.Id)
            .Select(s => new
            {
                s.Id, s.Title, s.Description, s.SurveyType, s.IsActive, s.Questions, s.RowVersion,
                ResponseCount = s.Responses.Count,
                AverageRating = s.Responses.Where(r => r.Rating != null).Average(r => (double?)r.Rating!) ?? 0
            }).ToListAsync();
        var surveys = source.Select(s => new SurveyListItemDto(s.Id, s.Title, s.Description,
            s.SurveyType.ToString(), s.IsActive, s.ResponseCount, s.AverageRating, s.Questions,
            SurveyEditVersion.Create(s.Id, s.Title, s.Description, s.SurveyType, s.IsActive, s.Questions, s.RowVersion))).ToList();
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

        if (!ValidQuestions(request.Questions))
            return BadRequest(ApiResponse<object>.Failure("سؤال‌های نظرسنجی نامعتبرند؛ نوع، متن و گزینه‌ها را بررسی کنید (حداکثر ۱۰۰ سؤال)"));
        if (!ValidSurveyType(request.SurveyType))
            return BadRequest(ApiResponse<object>.Failure("نوع نظرسنجی پشتیبانی نمی‌شود"));

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

        if (!ValidSurveyType(request.SurveyType))
            return BadRequest(ApiResponse<object>.Failure("نوع نظرسنجی پشتیبانی نمی‌شود"));

        // دادهٔ قدیمی ناسازگار را می‌توان بدون تغییر نگه داشت؛ ساخت یا تغییر
        // سؤال ناسازگار مجاز نیست تا فرم غیرقابل پاسخ ساخته نشود.
        if (!string.Equals(survey.Questions, request.Questions, StringComparison.Ordinal) && !ValidQuestions(request.Questions))
            return BadRequest(ApiResponse<object>.Failure("سؤال‌های نظرسنجی نامعتبرند؛ نوع، متن و گزینه‌ها را بررسی کنید (حداکثر ۱۰۰ سؤال)"));

        // نسخهٔ بازشده باید با دادهٔ فعلی سرور برابر باشد؛ کلاینت قدیمی نیز
        // نمی‌تواند بدون آگاهی آخرین تغییر را پنهان کند.
        if (string.IsNullOrWhiteSpace(request.EditVersion) || request.EditVersion != SurveyEditVersion.Create(survey))
            return Conflict(ApiResponse<object>.Failure("نظرسنجی پس از باز شدن فرم تغییر کرده یا نسخهٔ برنامه قدیمی است؛ فهرست را تازه‌سازی کنید."));

        // پاسخ‌های ثبت‌شده حفظ می‌شوند؛ ویرایش سوالات روی پاسخ‌های قبلی اثری ندارد
        survey.Title = title;
        survey.Description = request.Description?.Trim();
        survey.Questions = request.Questions;
        if (request.SurveyType.HasValue) survey.SurveyType = request.SurveyType.Value;
        if (request.IsActive.HasValue)
            survey.IsActive = request.IsActive.Value;
        survey.UpdatedAt = DateTime.UtcNow;

        try { await _unitOfWork.SaveEntitiesAsync(); }
        catch (DbUpdateConcurrencyException)
        { return Conflict(ApiResponse<object>.Failure("نظرسنجی همزمان تغییر کرد؛ فهرست را تازه‌سازی کنید.")); }
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
        if (request.Rating is not null && request.Rating is not (>= 1 and <= 5))
            return BadRequest(ApiResponse<object>.Failure("امتیاز باید بین ۱ تا ۵ باشد"));
        if (request.Comments?.Trim().Length > 2000)
            return BadRequest(ApiResponse<object>.Failure("نظر آزاد حداکثر ۲۰۰۰ نویسه است"));
        if (request.Rating is null && string.IsNullOrWhiteSpace(request.Comments) && (request.Answers is null || request.Answers.Count == 0))
            return BadRequest(ApiResponse<object>.Failure("حداقل یک پاسخ، امتیاز یا نظر وارد کنید"));
        var survey = await _unitOfWork.Repository<Survey>().QueryableNoTracking
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive && !s.IsDeleted);
        if (survey is null)
            return BadRequest(ApiResponse<object>.Failure("نظرسنجی فعال یافت نشد"));
        // اطلاعات اختیاری‌اند، اما در صورت ارسال باید معتبر و در کنار هم
        // سازگار باشند؛ وگرنه آمار پزشک یا هویت پاسخ به اشتباه نسبت داده می‌شود.
        if (request.PatientId.HasValue && !await _unitOfWork.Repository<Patient>().QueryableNoTracking
                .AnyAsync(p => p.Id == request.PatientId.Value && !p.IsDeleted))
            return BadRequest(ApiResponse<object>.Failure("بیمار انتخاب‌شده یافت نشد"));
        if (request.DoctorId.HasValue && !await _unitOfWork.Repository<Doctor>().QueryableNoTracking
                .AnyAsync(d => d.Id == request.DoctorId.Value && !d.IsDeleted))
            return BadRequest(ApiResponse<object>.Failure("پزشک انتخاب‌شده یافت نشد"));
        if (request.VisitId.HasValue)
        {
            var visit = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
                .Where(v => v.Id == request.VisitId.Value && !v.IsDeleted)
                .Select(v => new { v.PatientId, v.DoctorId }).FirstOrDefaultAsync();
            if (visit is null || (request.PatientId.HasValue && visit.PatientId != request.PatientId.Value) ||
                (request.DoctorId.HasValue && visit.DoctorId != request.DoctorId.Value))
                return BadRequest(ApiResponse<object>.Failure("ویزیت یافت نشد یا با بیمار و پزشک انتخاب‌شده مطابقت ندارد"));
        }

        string? answerSnapshot = null;
        if (request.Answers is not null)
        {
            if (!string.Equals(survey.Questions, request.QuestionsSnapshot, StringComparison.Ordinal))
                return Conflict(ApiResponse<object>.Failure("سؤال‌های نظرسنجی تغییر کرده‌اند؛ فهرست را تازه‌سازی و پاسخ را دوباره وارد کنید"));
            if (!TryBuildAnswerSnapshot(survey.Questions, request.Answers, out answerSnapshot))
                return BadRequest(ApiResponse<object>.Failure("پاسخ‌ها با سؤال‌های نظرسنجی سازگار نیستند یا کامل وارد نشده‌اند"));
        }

        var response = new SurveyResponse
        {
            SurveyId = id,
            PatientId = request.PatientId,
            VisitId = request.VisitId,
            DoctorId = request.DoctorId,
            Rating = request.Rating is >= 1 and <= 5 ? request.Rating : null,
            Comments = request.Comments?.Trim(),
            Answers = answerSnapshot,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<SurveyResponse>().AddAsync(response);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("پاسخ شما ثبت شد — سپاس از همراهی"));
    }

    // با قالب خوانندهٔ دسکتاپ و اعتبار پاسخ‌ها سازگار است. نظرسنجی‌های
    // قدیمی بدون سؤال ساختاریافته همچنان برای امتیاز و نظر قابل استفاده‌اند.
    internal static bool ValidQuestions(string? questions)
    {
        if (string.IsNullOrWhiteSpace(questions)) return true;
        if (questions.Length > 200_000) return false;
        try
        {
            using var doc = JsonDocument.Parse(questions);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() > 100) return false;
            foreach (var q in doc.RootElement.EnumerateArray())
            {
                if (q.ValueKind != JsonValueKind.Object) return false;
                var properties = q.EnumerateObject().ToList();
                if (properties.Any(p => p.Name is not ("text" or "q" or "type" or "options")) ||
                    properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Count ||
                    (q.TryGetProperty("text", out _) && q.TryGetProperty("q", out _))) return false;
                if (!q.TryGetProperty("text", out var text) && !q.TryGetProperty("q", out text)) return false;
                if (text.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(text.GetString()) || text.GetString()!.Length > 2000) return false;
                var type = "rating";
                if (q.TryGetProperty("type", out var ty))
                {
                    if (ty.ValueKind != JsonValueKind.String) return false;
                    type = ty.GetString() ?? "";
                }
                if (type is not ("rating" or "text" or "choice")) return false;
                if (!q.TryGetProperty("options", out var options) || options.ValueKind == JsonValueKind.Null)
                {
                    if (type == "choice") return false;
                    continue;
                }
                if (type != "choice") return false;
                string[] choices;
                if (options.ValueKind == JsonValueKind.Array)
                {
                    var elements = options.EnumerateArray().ToList();
                    if (elements.Any(o => o.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(o.GetString()) || o.GetString()!.Contains('|'))) return false;
                    choices = elements.Select(o => o.GetString()!.Trim()).ToArray();
                }
                else if (options.ValueKind == JsonValueKind.String)
                    choices = (options.GetString() ?? "").Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                else return false;
                if (choices.Length < 2 || choices.Length > 100 || choices.Any(c => c.Length > 2000)) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    // عکس سؤال و پاسخ همراه هم ذخیره می‌شود؛ ویرایش بعدی فرم معنای
    // پاسخ تاریخی را عوض نمی‌کند. درخواست‌های قدیمی بدون Answers سازگارند.
    internal static bool TryBuildAnswerSnapshot(string? questions, List<string?> answers, out string? snapshot)
    {
        snapshot = null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(string.IsNullOrWhiteSpace(questions) ? "[]" : questions);
            var root = doc.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Array || root.GetArrayLength() != answers.Count || answers.Count > 100) return false;
            var saved = new List<object>();
            var index = 0;
            foreach (var q in root.EnumerateArray())
            {
                if (q.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
                if (!q.TryGetProperty("text", out var text) && !q.TryGetProperty("q", out text)) return false;
                if (text.ValueKind != System.Text.Json.JsonValueKind.String || string.IsNullOrWhiteSpace(text.GetString())) return false;
                var type = q.TryGetProperty("type", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String ? t.GetString() : "rating";
                var answer = answers[index++]?.Trim();
                if (string.IsNullOrEmpty(answer) || answer.Length > 2000) return false;
                if (type == "rating" && answer is not ("1" or "2" or "3" or "4" or "5")) return false;
                if (type == "choice")
                {
                    if (!q.TryGetProperty("options", out var options)) return false;
                    var valid = options.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.Array => options.EnumerateArray().Any(x => x.ValueKind == System.Text.Json.JsonValueKind.String && x.GetString()?.Trim() == answer),
                        System.Text.Json.JsonValueKind.String => (options.GetString() ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(answer),
                        _ => false
                    };
                    if (!valid) return false;
                }
                else if (type is not ("rating" or "text")) return false;
                saved.Add(new { text = text.GetString(), type, answer });
            }
            snapshot = System.Text.Json.JsonSerializer.Serialize(saved);
            return true;
        }
        catch (System.Text.Json.JsonException) { return false; }
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
                    ? PersianDateHelper.ToShamsi(IranDisplayTime(r.SubmittedAt.Value)) + " — " + IranDisplayTime(r.SubmittedAt.Value).ToString("HH:mm")
                    : PersianDateHelper.ToShamsi(IranDisplayTime(r.CreatedAt))))
            .ToList();

        var recentAnswers = responses.Where(r => !string.IsNullOrWhiteSpace(r.Answers))
            .OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt).Take(20)
            .Select(r => new SurveyAnswerDto(r.Id, PersianDateHelper.ToShamsi(IranDisplayTime(r.SubmittedAt ?? r.CreatedAt)), r.Answers!)).ToList();
        var result = new SurveyResultsDto(summary, distribution, perDoctor.ToList(), comments, recentAnswers);
        return Ok(ApiResponse<object>.Success(result));
    }

    // زمان پاسخ در UTC ذخیره می‌شود؛ SQL Server معمولاً Kind را حفظ نمی‌کند.
    private static DateTime IranDisplayTime(DateTime timestamp) =>
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), "Iran Standard Time");

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
        var builtIn = _configuration.GetValue<bool>("Surveys:EnableBuiltInPublic");
        // در سرویس داخلی توکن فقط به نشانی پیکربندی‌شده توسط مدیر نصب می‌رود.
        var configuredUrl = builtIn || string.IsNullOrWhiteSpace(request?.BaseUrl)
            ? _configuration["Surveys:PublicBaseUrl"] : request!.BaseUrl;
        if (!Uri.TryCreate(configuredUrl?.Trim(), UriKind.Absolute, out var publicUri) ||
            publicUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(publicUri.UserInfo) ||
            !string.IsNullOrEmpty(publicUri.Query) || !string.IsNullOrEmpty(publicUri.Fragment))
            return BadRequest(ApiResponse<object>.Failure("نشانی HTTPS سرویس عمومی پاسخ‌گویی نظرسنجی تنظیم نشده یا نامعتبر است؛ پیش از ارسال پیامک آن را تنظیم و بررسی کنید"));
        var link = $"{publicUri.AbsoluteUri.TrimEnd('/')}/s/{id}";

        // مرزهای روز ایران بر اساس منطقه زمانی، به UTC برای ستون ویزیت.
        var (todayStart, tomorrowStart) = PersianDateHelper.IranDayUtcBounds(DateTime.UtcNow);

        // ویزیت‌های امروز با وضعیت معنادار (شروع‌شده/در جریان/تکمیل‌شده) و بیمار دارای موبایل
        var visits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.VisitDate >= todayStart && v.VisitDate < tomorrowStart
                     && (v.Status == VisitStatus.Started || v.Status == VisitStatus.InProgress || v.Status == VisitStatus.Completed)
                     && v.Patient != null && v.Patient.Mobile != null && v.Patient.Mobile.Trim() != "")
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
        var issuedAt = DateTime.UtcNow;
        foreach (var v in visits)
        {
            var patientLink = link;
            if (builtIn)
            {
                var token = SurveyPublicToken.Create();
                // توکن در fragment است تا در مسیر درخواست و لاگ عادی وب ثبت نشود.
                patientLink = $"{publicUri.AbsoluteUri.TrimEnd('/')}/s/#{token}";
                await _unitOfWork.Repository<SurveyInvitation>().AddAsync(new SurveyInvitation
                {
                    SurveyId = id, TokenHash = SurveyPublicToken.Hash(token)!, ExpiresAt = issuedAt.AddDays(7)
                });
            }
            var message =
                $"{v.FirstName} گرامی، از اعتماد شما به درمانگاه سهند سپاسگزاریم. " +
                $"لطفاً با ثبت نظرتان کیفیت خدمات امروز را به ما کمک کنید: {patientLink}";
            await queueRepo.AddAsync(new SmsQueueItem
            {
                Recipient = v.Mobile.Trim(),
                Message = message,
                ScheduledAt = DateTime.UtcNow,
                Status = SmsStatus.Queued,
                Priority = 5,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _unitOfWork.SaveEntitiesAsync();

        return Ok(ApiResponse<object>.Success(
            new { queuedCount = visits.Count, link = builtIn ? null : link },
            $"{visits.Count} پیامک لینک نظرسنجی در صف ارسال قرار گرفت"));
    }

    /* ─── مدل‌ها ─── */

    /// <summary>نرمال‌سازی نوع نظرسنجی — مقدار نامعتبر به «رضایت» برمی‌گردد</summary>
    private static bool ValidSurveyType(SurveyType? value) => value is null or SurveyType.Satisfaction or SurveyType.Service or SurveyType.Doctor;
    private static SurveyType NormalizeSurveyType(SurveyType? value) =>
        value is SurveyType.Satisfaction or SurveyType.Service or SurveyType.Doctor
            ? value.Value
            : SurveyType.Satisfaction;
    /// <summary>ردیف نظرسنجی</summary>
    public record SurveyListItemDto(long Id, string Title, string? Description,
        string Type, bool IsActive, int ResponseCount, double AverageRating,
        string? Questions, string EditVersion);

    /// <summary>درخواست ایجاد/ویرایش نظرسنجی — سوالات JSON با سازنده پویای کلاینت</summary>
    public record CreateSurveyRequest(string Title, string? Description, string? Questions,
        SurveyType? SurveyType, bool? IsActive, string? EditVersion = null);

    /// <summary>درخواست ثبت پاسخ</summary>
    public record SubmitResponseRequest(long? PatientId, long? VisitId, long? DoctorId,
        byte? Rating, string? Comments, List<string?>? Answers = null, string? QuestionsSnapshot = null);

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

    /// <summary>پاسخ‌های سؤال‌به‌سؤال، همراه عکس متن پرسش در زمان ثبت.</summary>
    public record SurveyAnswerDto(long Id, string SubmittedShamsi, string Answers);

    /// <summary>بسته کامل نتایج</summary>
    public record SurveyResultsDto(SurveyResultsSummaryDto Summary,
        List<StarDistributionDto> StarDistribution,
        List<DoctorRatingDto> DoctorRatings,
        List<SurveyCommentDto> RecentComments,
        List<SurveyAnswerDto> RecentAnswers);
}
