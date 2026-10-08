using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Application.Features.Prescriptions.Commands.CreatePrescription;
using Sahand.ClinicPro.Application.Features.Prescriptions.Queries;
using Sahand.ClinicPro.Application.Features.Prescriptions.CheckInteractions;
using Sahand.ClinicPro.Application.Features.Prescriptions.PrescriptionTemplates;
using Sahand.ClinicPro.Application.Features.Prescriptions.SearchMedications;
using Sahand.ClinicPro.Application.Features.Prescriptions.SubmitElectronic;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نسخه — صدور و مشاهده نسخه‌ها با بررسی تداخل دارویی
/// مسیرها: /api/v1/prescriptions
/// </summary>
[ApiController]
[Route("api/v1/prescriptions")]
[Authorize]
public class PrescriptionsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PrescriptionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست صفحه‌بندی‌شده نسخه‌ها — با فیلتر بیمار و وضعیت</summary>
    /// <param name="page">شماره صفحه</param>
    /// <param name="pageSize">اندازه صفحه</param>
    /// <param name="patientId">فیلتر بیمار</param>
    /// <param name="status">فیلتر وضعیت</param>
    /// <param name="search">جستجوی کد نسخه یا نام بیمار</param>
    [HttpGet]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] long? patientId = null, [FromQuery] Domain.Enums.PrescriptionStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new Application.Features.Prescriptions.Queries.GetPrescriptionsQuery(
            new Application.Common.Models.PagedRequest { PageNumber = page, PageSize = pageSize, Search = search },
            patientId, status));
        return ToPagedResponse(result);
    }

    /// <summary>نسخه‌های یک ویزیت</summary>
    /// <param name="visitId">شناسه ویزیت</param>
    [HttpGet("visit/{visitId:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetByVisit(long visitId)
    {
        var result = await _mediator.Send(new GetVisitPrescriptionsQuery(visitId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل نسخه با اقلام دارو</summary>
    /// <param name="id">شناسه نسخه</param>
    [HttpGet("{id:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetPrescriptionByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>صدور نسخه جدید — با بررسی تداخل ممنوع مصرف</summary>
    /// <param name="dto">داده نسخه و اقلام</param>
    [HttpPost]
    [HasPermission("prescriptions.create")]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var result = await _mediator.Send(new CreatePrescriptionCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "نسخه صادر شد");
        return ToResponse(result);
    }

    /// <summary>جستجوی دارو برای نسخه‌نویسی — نام ژنریک/تجاری/بارکد/IRC</summary>
    [HttpGet("medications/search")]
    // عمدتاً عمومی برای همه کاربران واردشده (ممیزی چرخه ۱۲): جستجوی مرجع دارو — فقط-خواندنی برای همه فرم‌ها
public async Task<IActionResult> SearchMedications([FromQuery] string? q, [FromQuery] int limit = 15)
    {
        var result = await _mediator.Send(new SearchMedicationsQuery(q, limit));
        return ToResponse(result);
    }

    /// <summary>بررسی تداخل دارویی — ۴ سطح + تداخل با آلرژی بیمار</summary>
    [HttpPost("check-interactions")]
    [HasPermission("prescriptions.view")] // ممیزی چرخه ۱۲ — لازمِ نسخه‌نویس
public async Task<IActionResult> CheckInteractions([FromBody] CheckInteractionsRequest request)
    {
        var result = await _mediator.Send(new CheckInteractionsCommand(request));
        return ToResponse(result);
    }

    /// <summary>ارسال نسخه به سامانه نسخه الکترونیک — کد رهگیری</summary>
    [HttpPost("{id:long}/submit-electronic")]
    [HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲ — ارسال دولتی
public async Task<IActionResult> SubmitElectronic(long id)
    {
        var result = await _mediator.Send(new SubmitElectronicCommand(id));
        return ToResponse(result, "نسخه به سامانه بیمه ارسال شد");
    }

    /// <summary>تمپلیت‌های نسخه</summary>
    [HttpGet("templates")]
    [HasPermission("prescriptions.view")] // ممیزی چرخه ۱۲
public async Task<IActionResult> GetTemplates()
    {
        var result = await _mediator.Send(new GetPrescriptionTemplatesQuery());
        return ToResponse(result);
    }

    /// <summary>ذخیره تمپلیت جدید</summary>
    [HttpPost("templates")]
    [HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲
public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest request)
    {
        // چرخه بیست‌وسوم: پزشک صاحب تمپلیت از خود درخواست خوانده می‌شود
        // (کلاینت آن را می‌فرستد) — اختیاری است و null یعنی تمپلیت عمومی
        var result = await _mediator.Send(new SavePrescriptionTemplateCommand(request, request.DoctorId));
        return ToResponse(result, "تمپلیت ذخیره شد");
    }

    /// <summary>حذف تمپلیت</summary>
    [HttpDelete("templates/{id:long}")]
    [HasPermission("prescriptions.edit")] // ممیزی چرخه ۱۲
    public async Task<IActionResult> DeleteTemplate(long id)
    {
        var result = await _mediator.Send(new DeletePrescriptionTemplateCommand(id));
        return ToResponse(result, "تمپلیت حذف شد");
    }

    // ═══════════ چرخه بیستم (گزارش چهاردهم مالک) ═══════════
    // «تمامی تداخل‌ها داخل برنامه باشد و دکمهٔ بروزرسانی داشته باشد» +
    // «دکمه‌ای برای بروزرسانی لیست داروها تا داروهای جدید اضافه شوند»

    /// <summary>
    /// دانلود کامل کاتالوگ تداخل‌های دارویی — برای کش محلی کلاینت دسکتاپ.
    /// کلاینت این فهرست را در %AppData% ذخیره می‌کند تا بررسی تداخل حتی در
    /// قطعی شبکه از پایگاه محلی قابل انجام باشد؛ دکمهٔ «بروزرسانی تداخل‌ها»
    /// همین endpoint را فرامی‌خواند و کش را نو می‌کند.
    /// </summary>
    [HttpGet("interactions")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetInteractions(CancellationToken ct)
    {
        var rows = await _mediator.Send(new GetDrugInteractionsQuery(), ct);
        return ToResponse(rows);
    }

    /// <summary>
    /// بروزرسانی فهرست داروها — داروها و جفت‌های تداخلی تازهٔ کاتالوگ بذر را
    /// که هنوز در دیتابیس نیستند اضافه می‌کند (upsert تلنگری) و شمار
    /// افزوده‌ها را برمی‌گرداند.
    /// </summary>
    [HttpPost("medications/refresh-catalog")]
    [HasPermission("prescriptions.edit")]
    public async Task<IActionResult> RefreshMedicationCatalog(CancellationToken ct)
    {
        var seeder = new Sahand.ClinicPro.Infrastructure.Persistence.Seeders.ReferenceDataSeeder(
            HttpContext.RequestServices
                .GetRequiredService<Sahand.ClinicPro.Infrastructure.Persistence.ClinicProDbContext>());
        var (meds, interactions) = await seeder.RefreshCatalogAsync(ct);
        return Ok(ApiResponse<object>.Success(new
        {
            medicationsAdded = meds,
            interactionsAdded = interactions,
        }, meds == 0 && interactions == 0
            ? "فهرست داروها از قبل به‌روز بود"
            : $"فهرست داروها به‌روز شد — {meds} دارو و {interactions} تداخل جدید افزوده شد"));
    }
}
