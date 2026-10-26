using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Application.Features.Patients.Commands.CompletePatientIdentity;
using Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;
using Sahand.ClinicPro.Application.Features.Patients.Commands.CreateUnidentifiedPatient;
using Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;
using Sahand.ClinicPro.Application.Features.Patients.Commands.MergePatients;
using Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetAbnormalSummary;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientTimeline;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;
using Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر بیماران — مدیریت پرونده بیماران
/// مسیرها: /api/v1/patients
/// </summary>
[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>واحد کار — برای یادداشت سریع و برچسب‌ها (چرخهٔ ۶۹ / F-17+F-18)</summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کاربر جاری — ثبت‌کنندهٔ یادداشت سریع (چرخهٔ ۶۹)</summary>
    private readonly Application.Interfaces.ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق میانجی، واحد کار و کاربر جاری</summary>
    public PatientsController(IMediator mediator, IUnitOfWork unitOfWork,
        Application.Interfaces.ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>
    /// فهرست صفحه‌بندی‌شده بیماران
    /// </summary>
    /// <param name="page">شماره صفحه — پیش‌فرض ۱</param>
    /// <param name="pageSize">اندازه صفحه — حداکثر ۱۰۰</param>
    /// <param name="search">جستجو در نام و کد ملی و موبایل و پرونده</param>
    [HttpGet]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetAllPatientsQuery(
            new PagedRequest { PageNumber = page, PageSize = pageSize, Search = search }));
        return ToPagedResponse(result);
    }

    /// <summary>
    /// جزئیات کامل یک بیمار
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    [HttpGet("{id:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>
    /// جستجوی سریع بیماران — برای تکمیل خودکار
    /// </summary>
    /// <param name="term">عبارت جستجو</param>
    [HttpGet("search")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var result = await _mediator.Send(new SearchPatientsQuery(term));
        return ToResponse(result);
    }

    /// <summary>
    /// ثبت بیمار جدید — شماره پرونده خودکار
    /// </summary>
    /// <param name="dto">داده‌های بیمار جدید</param>
    [HttpPost]
    [HasPermission("patients.create")]
    [ProducesResponseType(typeof(ApiResponse<PatientListDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var result = await _mediator.Send(new CreatePatientCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value.Id },
                $"بیمار با شماره پرونده {result.Value.MedicalRecordNo} ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// ساخت پروندهٔ موقت بیمار ناشناس (اورژانسی) — بدون کد ملی، با کد
    /// موقت یکتای سیستمی؛ بیمار بلافاصله قابل پذیرش است و بعداً از پرونده
    /// «تکمیل هویت» می‌شود. کد ملی برای ثبت عادی همچنان الزامی است.
    /// </summary>
    /// <param name="dto">جنسیت و سن تقریبی اختیاری</param>
    [HttpPost("unidentified")]
    [HasPermission("patients.create")]
    [ProducesResponseType(typeof(ApiResponse<PatientListDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUnidentified([FromBody] CreateUnidentifiedPatientDto dto)
    {
        var result = await _mediator.Send(new CreateUnidentifiedPatientCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value.Id },
                $"پروندهٔ موقت با کد {result.Value.TemporaryCode} ساخته شد");
        return ToResponse(result);
    }

    /// <summary>
    /// تکمیل هویت بیمار ناشناس — اگر کد ملی از قبل پروندهٔ اصلی داشته
    /// باشد، نتیجه با وضعیت Duplicate برمی‌گردد تا کاربر ادغام را تأیید کند
    /// </summary>
    /// <param name="id">شناسهٔ پروندهٔ موقت</param>
    /// <param name="dto">مشخصات واقعی بیمار</param>
    [HttpPut("{id:long}/complete-identity")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> CompleteIdentity(long id, [FromBody] CompletePatientIdentityDto dto)
    {
        var result = await _mediator.Send(new CompletePatientIdentityCommand(id, dto));
        return ToResponse(result, "هویت بیمار تکمیل شد");
    }

    /// <summary>
    /// ادغام پروندهٔ موقت در پروندهٔ اصلی موجود — انتقال همهٔ سوابق
    /// بالینی و مالی و حذف نرم پروندهٔ موقت
    /// </summary>
    /// <param name="dto">شناسهٔ مبدأ (موقت) و مقصد (اصلی)</param>
    [HttpPost("merge")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> Merge([FromBody] MergePatientsDto dto)
    {
        var result = await _mediator.Send(new MergePatientsCommand(dto.SourcePatientId, dto.TargetPatientId));
        return ToResponse(result, "پرونده‌ها ادغام شدند و همهٔ سوابق منتقل شد");
    }

    /// <summary>
    /// ویرایش اطلاعات بیمار
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="dto">داده‌های ویرایش</param>
    [HttpPut("{id:long}")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePatientDto dto)
    {
        var result = await _mediator.Send(new UpdatePatientCommand(id, dto));
        return ToResponse(result, "اطلاعات بیمار به‌روزرسانی شد");
    }

    /// <summary>
    /// حذف نرم بیمار — رکورد فیزیکی حذف نمی‌شود
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    [HttpDelete("{id:long}")]
    [HasPermission("patients.delete")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _mediator.Send(new DeletePatientCommand(id));
        return ToResponse(result, "بیمار حذف شد");
    }

    /// <summary>
    /// خلاصه نتایج غیرعادی پاراکلینیک — نوار رنگی بالای پرونده (چرخه ۱۳، درخواست ۱-۴)
    /// آخرین نتیجه غیرطبیعی هر آزمایش + یافته‌های تصویری هشداردهنده
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    [HttpGet("{id:long}/abnormal-summary")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetAbnormalSummary(long id)
    {
        var result = await _mediator.Send(new GetAbnormalSummaryQuery(id));
        return ToResponse(result);
    }

    /// <summary>
    /// خط زمان رویدادهای بیمار — نمای زمان‌محور یکپارچه (چرخه ۱۳، درخواست ۱-۵)
    /// ویزیت/آزمایش/تصویر/نسخه/پذیرش/فاکتور/پرداخت با فیلتر نوع و سال شمسی
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="kinds">فیلتر نوع‌ها با کاما — visit,lab,imaging,prescription,reception,invoice,payment</param>
    /// <param name="yearShamsi">فیلتر سال شمسی</param>
    [HttpGet("{id:long}/timeline")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetTimeline(long id, [FromQuery] string? kinds = null,
        [FromQuery] int? yearShamsi = null)
    {
        var result = await _mediator.Send(new GetPatientTimelineQuery(id, kinds, yearShamsi));
        return ToResponse(result);
    }

    /* ═══ چرخهٔ ۶۹ (فاز ۳ / F-17+F-18 نقشه راه ai-review) — یادداشت سریع و برچسب بیمار ═══ */

    /// <summary>
    /// یادداشت‌های سریع بیمار — سابقه‌های از نوع Note (F-17 «یادداشت سریع»):
    /// ثبت خلاصهٔ مهم بدون ساخت ویزیت کامل؛ جدیدترین اول.
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="ct">توکن لغو</param>
    [HttpGet("{id:long}/notes")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetNotes(long id, CancellationToken ct)
    {
        var notes = await _unitOfWork.Repository<Domain.Entities.People.PatientHistory>()
            .QueryableNoTracking
            .Where(h => h.PatientId == id && !h.IsDeleted && h.HistoryType == Domain.Enums.HistoryType.Note)
            .OrderByDescending(h => h.RecordedDate)
            .Take(50)
            .Select(h => new
            {
                id = h.Id,
                text = h.Description,
                dateShamsi = h.RecordedDate.HasValue
                    ? Application.Common.Helpers.PersianDateHelper.ToShamsi(h.RecordedDate.Value)
                    : ""
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Success(notes, "یادداشت‌های بیمار دریافت شد"));
    }

    /// <summary>
    /// ثبت یادداشت سریع — یک سابقهٔ Note جدید با کاربر جاری (F-17)
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="body">متن یادداشت — { text: "..." }</param>
    /// <param name="ct">توکن لغو</param>
    [HttpPost("{id:long}/notes")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> AddNote(long id, [FromBody] QuickNoteDto body, CancellationToken ct)
    {
        var text = body.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(ApiResponse<object>.Failure("متن یادداشت خالی است"));

        var patient = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .QueryableTracking.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (patient is null)
            return NotFound(ApiResponse<object>.Failure("بیمار یافت نشد"));

        await _unitOfWork.Repository<Domain.Entities.People.PatientHistory>().AddAsync(
            new Domain.Entities.People.PatientHistory
            {
                PatientId = id,
                HistoryType = Domain.Enums.HistoryType.Note,
                Title = "یادداشت سریع",
                Description = text,
                RecordedDate = DateTime.UtcNow,
                RecordedBy = _currentUser.UserId
            });
        await _unitOfWork.SaveEntitiesAsync(ct);

        return Ok(ApiResponse<object>.Success(new { ok = true }, "یادداشت ثبت شد"));
    }

    /// <summary>
    /// ذخیرهٔ برچسب‌های بیمار — CSV مثل «VIP، دیابت» (F-18 «برچسب بیمار»)
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="body">برچسب‌ها — { tags: "VIP, دیابت" } یا تهی برای پاک‌کردن</param>
    /// <param name="ct">توکن لغو</param>
    [HttpPut("{id:long}/tags")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> SaveTags(long id, [FromBody] SaveTagsDto body, CancellationToken ct)
    {
        var patient = await _unitOfWork.Repository<Domain.Entities.People.Patient>()
            .QueryableTracking.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (patient is null)
            return NotFound(ApiResponse<object>.Failure("بیمار یافت نشد"));

        // پاک‌سازی و نرمال‌سازی: حذف جداکننده‌های تکراری + سقف طول
        var tags = string.Join("،", (body.Tags ?? string.Empty)
            .Split(new[] { ',', '،', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .Take(8));
        if (tags.Length > 200)
            return BadRequest(ApiResponse<object>.Failure("برچسب‌ها بیش از حد طولانی است (حداکثر ۸ برچسب)"));

        patient.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags;
        await _unitOfWork.SaveEntitiesAsync(ct);

        return Ok(ApiResponse<object>.Success(new { tags = patient.Tags }, "برچسب‌های بیمار ذخیره شد"));
    }

    /// <summary>بدنهٔ ثبت یادداشت سریع (F-17)</summary>
    public sealed record QuickNoteDto(string? Text);

    /// <summary>بدنهٔ ذخیرهٔ برچسب‌ها (F-18)</summary>
    public sealed record SaveTagsDto(string? Tags);
}
