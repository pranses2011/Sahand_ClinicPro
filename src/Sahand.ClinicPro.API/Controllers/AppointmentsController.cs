using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.CreateAppointment;
using Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.BatchCancelAppointments;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.SendAppointmentReminders;
using Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAvailableSlots;
using Sahand.ClinicPro.Application.Features.Appointments.Queries.SearchAppointments;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نوبت‌دهی — رزرو و مدیریت نوبت‌ها
/// مسیرها: /api/v1/appointments
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public class AppointmentsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>نوبت‌های یک تاریخ — با فیلتر پزشک و وضعیت</summary>
    /// <param name="date">تاریخ شمسی — تهی یعنی امروز</param>
    /// <param name="doctorId">فیلتر پزشک</param>
    /// <param name="status">فیلتر وضعیت</param>
    [HttpGet]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetByDate([FromQuery] string? date,
        [FromQuery] long? doctorId, [FromQuery] AppointmentStatus? status)
    {
        var result = await _mediator.Send(new GetAppointmentsByDateQuery(date, doctorId, status));
        return ToResponse(result);
    }

    /// <summary>رزرو نوبت جدید — با کنترل ظرفیت و تداخل</summary>
    /// <param name="dto">داده نوبت</param>
    [HttpPost]
    [HasPermission("appointments.create")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetByDate), new { }, "نوبت با موفقیت ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تغییر وضعیت نوبت — تأیید یا لغو یا حضور یا تکمیل</summary>
    /// <param name="id">شناسه نوبت</param>
    /// <param name="dto">وضعیت جدید و دلیل</param>
    [HttpPut("{id:long}/status")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto)
    {
        var result = await _mediator.Send(new ChangeAppointmentStatusCommand(id, dto.Status, dto.Reason));
        return ToResponse(result, "وضعیت نوبت تغییر یافت");
    }

    /// <summary>مدل درخواست تغییر وضعیت</summary>
    public record ChangeStatusDto(AppointmentStatus Status, string? Reason);

    /// <summary>اسلات‌های زمانی آزاد پزشک در یک تاریخ — برای رزرو نوبت</summary>
    /// <param name="id">شناسه پزشک</param>
    /// <param name="date">تاریخ شمسی</param>
    [HttpGet("doctor/{id:long}/available-slots")]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetAvailableSlots(long id, [FromQuery] string date)
    {
        var result = await _mediator.Send(new GetAvailableSlotsQuery(id, date));
        return ToResponse(result);
    }

    /// <summary>جستجوی پیشرفته نوبت‌ها با همه فیلترها و صفحه‌بندی</summary>
    [HttpPost("search")]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> Search([FromBody] AppointmentSearchFilter filter)
    {
        var result = await _mediator.Send(new SearchAppointmentsQuery(filter));
        return ToResponse(result);
    }

    /// <summary>لغو دسته‌ای نوبت‌های انتخاب‌شده</summary>
    [HttpPost("batch-cancel")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> BatchCancel([FromBody] BatchCancelRequest request)
    {
        var result = await _mediator.Send(new BatchCancelAppointmentsCommand(request));
        return ToResponse(result, "لغو دسته‌ای انجام شد");
    }

    /// <summary>ارسال پیامک یادآوری نوبت‌ها — پیش‌فرض نوبت‌های فردا</summary>
    [HttpPost("send-reminders")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> SendReminders([FromQuery] string? date = null)
    {
        var result = await _mediator.Send(new SendAppointmentRemindersCommand(date));
        return ToResponse(result, "پیامک‌های یادآوری در صف ارسال قرار گرفتند");
    }
}
