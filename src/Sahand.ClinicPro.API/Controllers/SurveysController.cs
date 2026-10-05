using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نظرسنجی — گام U19 (فرم ۳۴):
/// فهرست نظرسنجی‌ها با میانگین امتیاز + ثبت نظرسنجی جدید + ثبت پاسخ
/// مسیرها: /api/v1/surveys
/// </summary>
[ApiController]
[Route("api/v1/surveys")]
[Authorize]
public class SurveysController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SurveysController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>فهرست نظرسنجی‌ها با شمارنده پاسخ و میانگین امتیاز</summary>
    [HttpGet]
    [HasPermission("reports.view")]
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
                s.Responses.Where(r => r.Rating != null).Average(r => (double?)r.Rating!) ?? 0))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(surveys));
    }

    /// <summary>ثبت نظرسنجی جدید — سوالات به‌صورت JSON</summary>
    [HttpPost]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyRequest request)
    {
        var survey = new Survey
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            SurveyType = Domain.Enums.SurveyType.Satisfaction,
            Questions = request.Questions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<Survey>().AddAsync(survey);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(new { surveyId = survey.Id }, "نظرسنجی ایجاد شد"));
    }

    /// <summary>فعال/غیرفعال‌سازی نظرسنجی</summary>
    [HttpPost("{id:long}/toggle-active")]
    [HasPermission("settings.edit")]
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

    /// <summary>ثبت پاسخ نظرسنجی — امتیاز ۱ تا ۵ + نظر</summary>
    [HttpPost("{id:long}/responses")]
    [Authorize]
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
            Comments = request.Comments,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<SurveyResponse>().AddAsync(response);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("پاسخ شما ثبت شد — سپاس از همراهی"));
    }

    /* ─── مدل‌ها ─── */

    /// <summary>ردیف نظرسنجی</summary>
    public record SurveyListItemDto(long Id, string Title, string? Description,
        string Type, bool IsActive, int ResponseCount, double AverageRating);

    /// <summary>درخواست ثبت نظرسنجی</summary>
    public record CreateSurveyRequest(string Title, string? Description, string? Questions);

    /// <summary>درخواست ثبت پاسخ</summary>
    public record SubmitResponseRequest(long? PatientId, long? VisitId, long? DoctorId,
        byte? Rating, string? Comments);
}
