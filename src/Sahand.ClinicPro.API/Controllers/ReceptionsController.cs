using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.UpdateReceptionStatus;
using Sahand.ClinicPro.Application.Features.Receptions.Queries.GetReceptionReceipt;
using Sahand.ClinicPro.Application.Features.Receptions.Queries.GetTodayReceptions;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Application.Features.Receptions.GetQueueStats;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پذیرش — ثبت و مشاهده پذیرش‌های روز
/// مسیرها: /api/v1/receptions
/// </summary>
[ApiController]
[Route("api/v1/receptions")]
[Authorize]
public class ReceptionsController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ReceptionsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>پذیرش‌های امروز</summary>
    /// <param name="doctorId">فیلتر پزشک</param>
    [HttpGet("today")]
    [HasPermission("receptions.view")]
    public async Task<IActionResult> GetToday([FromQuery] long? doctorId)
    {
        var result = await _mediator.Send(new GetTodayReceptionsQuery(doctorId));
        return ToResponse(result);
    }

    /// <summary>ثبت پذیرش بیمار</summary>
    /// <param name="dto">داده پذیرش</param>
    [HttpPost]
    [HasPermission("receptions.create")]
    public async Task<IActionResult> Create([FromBody] CreateReceptionDto dto)
    {
        var result = await _mediator.Send(new CreateReceptionCommand(dto, _currentUser.UserId));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetToday), new { }, "پذیرش با موفقیت ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تغییر وضعیت پذیرش — در صف، در ویزیت، ترخیص</summary>
    /// <param name="id">شناسه پذیرش</param>
    /// <param name="dto">وضعیت جدید</param>
    [HttpPut("{id:long}/status")]
    [HasPermission("receptions.edit")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusDto dto)
    {
        var reception = await _mediator.Send(new UpdateReceptionStatusCommand(id, dto.Status));
        return ToResponse(reception, "وضعیت پذیرش تغییر یافت");
    }

    /// <summary>مدل تغییر وضعیت</summary>
    public record UpdateStatusDto(ReceptionStatus Status);

    /// <summary>آمار روز پذیرش — انتظار/پذیرش‌شده/صف/درآمد</summary>
    [HttpGet("queue-stats")]
    [HasPermission("receptions.view")] // ممیزی چرخه ۱۲
public async Task<IActionResult> GetQueueStats()
    {
        var result = await _mediator.Send(new GetQueueStatsQuery());
        return ToResponse(result);
    }

    // ═════════════════════════════════════════════════════════════════
    // چرخهٔ ۷۸ (گزارش مالک): «برای صف و انجام خدمات درمانی یک صفحهٔ
    // جداگانه باشه» — سه اندپوینت صف مستقل خدمات درمانی:
    // مشاهدهٔ صف / فراخوان به اتاق / اعلام انجام. پذیرش‌های خدمتیِ بدون پزشک
    // از صف ویزیت (DoctorId != null) جدا شدند و اینجا مدیریت می‌شوند.
    // ═════════════════════════════════════════════════════════════════

    /// <summary>صف خدمات امروز — در انتظار / در حال انجام / انجام‌شده</summary>
    [HttpGet("service-queue")]
    [HasPermission("receptions.view")]
    public async Task<IActionResult> GetServiceQueue()
    {
        var result = await _mediator.Send(new Application.Features.Receptions.ServiceQueue.GetServiceQueueQuery());
        return ToResponse(result);
    }

    /// <summary>فراخوان بیمار خدمت به اتاق انجام</summary>
    /// <param name="id">شناسه پذیرش خدمتی</param>
    [HttpPost("service-call/{id:long}")]
    [HasPermission("receptions.edit")]
    public async Task<IActionResult> CallServicePatient(long id)
    {
        var result = await _mediator.Send(
            new Application.Features.Receptions.ServiceQueue.CallServicePatientCommand(id));
        return ToResponse(result, "بیمار به اتاق انجام خدمت فراخوانده شد");
    }

    /// <summary>اعلام انجام خدمت — پایان ردیف و انتقال به انجام‌شده‌ها</summary>
    /// <param name="id">شناسه پذیرش خدمتی</param>
    [HttpPost("service-complete/{id:long}")]
    [HasPermission("receptions.edit")]
    public async Task<IActionResult> CompleteService(long id)
    {
        var result = await _mediator.Send(
            new Application.Features.Receptions.ServiceQueue.CompleteServiceReceptionCommand(id));
        return ToResponse(result, "خدمت انجام شد و ثبت گردید");
    }
}
