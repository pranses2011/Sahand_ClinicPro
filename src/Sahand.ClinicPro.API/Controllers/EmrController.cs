using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پرونده بالینی پیشرفته — گام R2 (چرخه چهارم)
/// سه ماژول: اندازه‌گیری رشد (نمودار WHO) + سابقه خانوادگی + طرح مراقبت مزمن
/// مسیرها: /api/v1/emr/growth و /api/v1/emr/family-history و /api/v1/emr/care-plans
/// </summary>
[ApiController]
[Route("api/v1/emr")]
[Authorize]
public class EmrController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public EmrController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    // ═══════════════ بخش ۱: اندازه‌گیری رشد ═══════════════

    /// <summary>فهرست اندازه‌گیری‌های رشد بیمار — مرتب از کهن‌ترین برای ترسیم نمودار</summary>
    [HttpGet("growth/{patientId:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetGrowth(long patientId)
    {
        var items = await _unitOfWork.Repository<GrowthMeasurement>().QueryableNoTracking
            .Where(g => g.PatientId == patientId)
            .OrderBy(g => g.MeasuredAt)
            .Select(g => new GrowthRowDto(g.Id, g.MeasuredAt, g.AgeMonths,
                g.WeightKg, g.HeightCm, g.HeadCircumferenceCm, g.Notes))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>ثبت اندازه‌گیری رشد جدید — سن ماهانه از تاریخ تولد محاسبه می‌شود</summary>
    [HttpPost("growth")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> AddGrowth([FromBody] AddGrowthDto dto)
    {
        var patient = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .GetByIdAsync(dto.PatientId);
        if (patient is null)
            return BadRequest(ApiResponse<object>.Failure("بیمار یافت نشد"));
        if (dto.WeightKg is null && dto.HeightCm is null && dto.HeadCircumferenceCm is null)
            return BadRequest(ApiResponse<object>.Failure("حداقل یکی از مقادیر وزن/قد/محیط سر الزامی است"));

        // سن ماهانه از تاریخ تولد — تقویم میلادی پایه محاسبه
        var ageMonths = ComputeAgeMonths(patient.BirthDate, dto.MeasuredAt);

        // یک رکورد در هر تاریخ — ویرایش رکورد موجود به‌جای خطا
        var existing = await _unitOfWork.Repository<GrowthMeasurement>().QueryableTracking
            .FirstOrDefaultAsync(g => g.PatientId == dto.PatientId && g.MeasuredAt == dto.MeasuredAt);

        if (existing is not null)
        {
            existing.WeightKg = dto.WeightKg ?? existing.WeightKg;
            existing.HeightCm = dto.HeightCm ?? existing.HeightCm;
            existing.HeadCircumferenceCm = dto.HeadCircumferenceCm ?? existing.HeadCircumferenceCm;
            existing.Notes = dto.Notes ?? existing.Notes;
            if (ageMonths is { } months) existing.AgeMonths = months;
        }
        else
        {
            await _unitOfWork.Repository<GrowthMeasurement>().AddAsync(new GrowthMeasurement
            {
                PatientId = dto.PatientId,
                MeasuredAt = dto.MeasuredAt,
                AgeMonths = ageMonths ?? 0,
                WeightKg = dto.WeightKg,
                HeightCm = dto.HeightCm,
                HeadCircumferenceCm = dto.HeadCircumferenceCm,
                Notes = dto.Notes
            });
        }

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("اندازه‌گیری رشد ثبت شد"));
    }

    /// <summary>حذف نرم اندازه‌گیری رشد</summary>
    [HttpDelete("growth/{id:long}")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> DeleteGrowth(long id)
    {
        var measurement = await _unitOfWork.Repository<GrowthMeasurement>()
            .QueryableTracking.FirstOrDefaultAsync(g => g.Id == id);
        if (measurement is null)
            return BadRequest(ApiResponse<object>.Failure("اندازه‌گیری یافت نشد"));

        _unitOfWork.Repository<GrowthMeasurement>().Remove(measurement);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("اندازه‌گیری حذف شد"));
    }

    // ═══════════════ بخش ۲: سابقه خانوادگی ═══════════════

    /// <summary>فهرست سابقه خانوادگی بیمار</summary>
    [HttpGet("family-history/{patientId:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetFamilyHistory(long patientId)
    {
        var items = await _unitOfWork.Repository<FamilyHistory>().QueryableNoTracking
            .Where(f => f.PatientId == patientId)
            .OrderByDescending(f => f.Id)
            .Select(f => new FamilyHistoryRowDto(f.Id, f.PatientId, (int)f.Relative,
                f.Disease, f.Icd10Code, f.OnsetAge, f.IsGenetic, f.IsDeceased, f.DeathAge, f.Notes))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>ثبت سابقه خانوادگی جدید</summary>
    [HttpPost("family-history")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> AddFamilyHistory([FromBody] AddFamilyHistoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Disease))
            return BadRequest(ApiResponse<object>.Failure("عنوان بیماری الزامی است"));

        await _unitOfWork.Repository<FamilyHistory>().AddAsync(new FamilyHistory
        {
            PatientId = dto.PatientId,
            Relative = (FamilyRelative)dto.Relative,
            Disease = dto.Disease.Trim(),
            Icd10Code = dto.Icd10Code,
            OnsetAge = dto.OnsetAge,
            IsGenetic = dto.IsGenetic,
            IsDeceased = dto.IsDeceased,
            DeathAge = dto.DeathAge,
            Notes = dto.Notes
        });
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("سابقه خانوادگی ثبت شد"));
    }

    /// <summary>حذف نرم سابقه خانوادگی</summary>
    [HttpDelete("family-history/{id:long}")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> DeleteFamilyHistory(long id)
    {
        var row = await _unitOfWork.Repository<FamilyHistory>()
            .QueryableTracking.FirstOrDefaultAsync(f => f.Id == id);
        if (row is null)
            return BadRequest(ApiResponse<object>.Failure("رکورد یافت نشد"));

        _unitOfWork.Repository<FamilyHistory>().Remove(row);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("سابقه خانوادگی حذف شد"));
    }

    // ═══════════════ بخش ۳: طرح مراقبت مزمن ═══════════════

    /// <summary>فهرست طرح‌های مراقبت بیمار — طرح‌های نیازمند بازبینی اول فهرست</summary>
    [HttpGet("care-plans/{patientId:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetCarePlans(long patientId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await _unitOfWork.Repository<ChronicCarePlan>().QueryableNoTracking
            .Where(c => c.PatientId == patientId)
            .OrderBy(c => c.Status == CarePlanStatus.ReviewDue ? 0 : 1)
            .ThenByDescending(c => c.Id)
            .Select(c => new CarePlanRowDto(c.Id, c.PatientId, c.DiseaseTitle, c.Icd10Code,
                (int)c.Status, c.StartDate, c.NextReviewDate, c.Goals, c.Actions, c.CareTeam, c.Notes,
                // پرچم موعد بازبینی — تاریخ گذشته و طرح هنوز فعال است
                c.NextReviewDate != null && c.NextReviewDate < today &&
                    (c.Status == CarePlanStatus.Active || c.Status == CarePlanStatus.ReviewDue)))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>طرح‌های نیازمند بازبینی در کل درمانگاه — برای داشبورد پرستار</summary>
    [HttpGet("care-plans/due")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetDueCarePlans([FromQuery] int days = 7)
    {
        var horizon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
        var items = await _unitOfWork.Repository<ChronicCarePlan>().QueryableNoTracking
            .Where(c => (c.Status == CarePlanStatus.Active || c.Status == CarePlanStatus.ReviewDue) &&
                        c.NextReviewDate != null && c.NextReviewDate <= horizon)
            .OrderBy(c => c.NextReviewDate)
            .Take(100)
            .Select(c => new DuePlanDto(c.Id, c.PatientId,
                c.Patient != null ? c.Patient.FirstName + " " + c.Patient.LastName : "",
                c.DiseaseTitle, c.NextReviewDate!.Value))
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(items));
    }

    /// <summary>ثبت طرح مراقبت جدید</summary>
    [HttpPost("care-plans")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> AddCarePlan([FromBody] AddCarePlanDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DiseaseTitle))
            return BadRequest(ApiResponse<object>.Failure("عنوان بیماری مزمن الزامی است"));

        await _unitOfWork.Repository<ChronicCarePlan>().AddAsync(new ChronicCarePlan
        {
            PatientId = dto.PatientId,
            DiseaseTitle = dto.DiseaseTitle.Trim(),
            Icd10Code = dto.Icd10Code,
            Status = CarePlanStatus.Active,
            StartDate = dto.StartDate,
            NextReviewDate = dto.NextReviewDate,
            Goals = dto.Goals,
            Actions = dto.Actions,
            CareTeam = dto.CareTeam,
            Notes = dto.Notes
        });
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("طرح مراقبت ثبت شد"));
    }

    /// <summary>تغییر وضعیت طرح مراقبت (فعال/بازبینی/تکمیل/توقف)</summary>
    [HttpPut("care-plans/{id:long}/status")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> UpdateCarePlanStatus(long id, [FromQuery] string status)
    {
        if (!Enum.TryParse<CarePlanStatus>(status, true, out var parsed))
            return BadRequest(ApiResponse<object>.Failure("وضعیت نامعتبر است (Active/ReviewDue/Completed/Discontinued)"));

        var plan = await _unitOfWork.Repository<ChronicCarePlan>()
            .QueryableTracking.FirstOrDefaultAsync(c => c.Id == id);
        if (plan is null)
            return BadRequest(ApiResponse<object>.Failure("طرح مراقبت یافت نشد"));

        plan.Status = parsed;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("وضعیت طرح مراقبت به‌روزرسانی شد"));
    }

    /// <summary>حذف نرم طرح مراقبت</summary>
    [HttpDelete("care-plans/{id:long}")]
    [HasPermission("emr.advanced")]
    public async Task<IActionResult> DeleteCarePlan(long id)
    {
        var plan = await _unitOfWork.Repository<ChronicCarePlan>()
            .QueryableTracking.FirstOrDefaultAsync(c => c.Id == id);
        if (plan is null)
            return BadRequest(ApiResponse<object>.Failure("طرح مراقبت یافت نشد"));

        _unitOfWork.Repository<ChronicCarePlan>().Remove(plan);
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("طرح مراقبت حذف شد"));
    }

    // ═══════════════ ابزارهای مشترک ═══════════════

    /// <summary>
    /// سن ماهانه از تاریخ تولد — تقویم میلادی با تقریب ۳۰/۴۴ روز در ماه
    /// </summary>
    private static int? ComputeAgeMonths(DateOnly? birthDate, DateOnly measuredAt)
    {
        if (birthDate is null) return null;
        if (measuredAt < birthDate.Value) return 0;

        var months = (measuredAt.Year - birthDate.Value.Year) * 12 +
                     measuredAt.Month - birthDate.Value.Month;
        if (measuredAt.Day < birthDate.Value.Day) months--;
        return Math.Max(0, months);
    }

    // ── DTOهای ردیف ──

    /// <summary>ردیف اندازه‌گیری رشد</summary>
    public record GrowthRowDto(long Id, DateOnly MeasuredAt, int AgeMonths,
        decimal? WeightKg, decimal? HeightCm, decimal? HeadCircumferenceCm, string? Notes);

    /// <summary>ورودی ثبت رشد</summary>
    public record AddGrowthDto(long PatientId, DateOnly MeasuredAt,
        decimal? WeightKg, decimal? HeightCm, decimal? HeadCircumferenceCm, string? Notes);

    /// <summary>ردیف سابقه خانوادگی</summary>
    public record FamilyHistoryRowDto(long Id, long PatientId, int Relative, string Disease,
        string? Icd10Code, int? OnsetAge, bool IsGenetic, bool IsDeceased, int? DeathAge, string? Notes);

    /// <summary>ورودی سابقه خانوادگی</summary>
    public record AddFamilyHistoryDto(long PatientId, int Relative, string Disease,
        string? Icd10Code, int? OnsetAge, bool IsGenetic, bool IsDeceased, int? DeathAge, string? Notes);

    /// <summary>ردیف طرح مراقبت</summary>
    public record CarePlanRowDto(long Id, long PatientId, string DiseaseTitle, string? Icd10Code,
        int Status, DateOnly StartDate, DateOnly? NextReviewDate, string? Goals,
        string? Actions, string? CareTeam, string? Notes, bool IsReviewOverdue);

    /// <summary>ورودی طرح مراقبت</summary>
    public record AddCarePlanDto(long PatientId, string DiseaseTitle, string? Icd10Code,
        DateOnly StartDate, DateOnly? NextReviewDate, string? Goals, string? Actions,
        string? CareTeam, string? Notes);

    /// <summary>ردیف طرح موعد‌رسیده</summary>
    public record DuePlanDto(long Id, long PatientId, string PatientName,
        string DiseaseTitle, DateOnly NextReviewDate);
}
