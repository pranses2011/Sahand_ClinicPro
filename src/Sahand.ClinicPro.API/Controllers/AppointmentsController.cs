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
using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;

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
    private readonly IDateTimeService _clock;

    /// <summary>سازنده با تزریق میانجی و ساعت درمانگاه</summary>
    public AppointmentsController(IMediator mediator, IDateTimeService clock)
    {
        _mediator = mediator;
        _clock = clock;
    }

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
        return ToPagedResponse(result);
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

    /* ═══════════ لیست انتظار — چرخه ۱۳ (درخواست ۳-۴) ═══════════ */

    /// <summary>فهرست لیست انتظار — با فیلتر وضعیت اختیاری</summary>
    /// <param name="status">فیلتر وضعیت — تهی یعنی همه</param>
    [HttpGet("waitlist")]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetWaitlist([FromQuery] WaitlistStatus? status = null)
    {
        var result = await _mediator.Send(new GetWaitlistQuery(status));
        return ToResponse(result);
    }

    /// <summary>ثبت بیمار در لیست انتظار</summary>
    [HttpPost("waitlist")]
    [HasPermission("appointments.create")]
    public async Task<IActionResult> AddToWaitlist([FromBody] AddWaitlistDto dto)
    {
        var result = await _mediator.Send(new AddToWaitlistCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetWaitlist", new { }, "بیمار در لیست انتظار ثبت شد");
        return ToResponse(result);
    }

    /// <summary>لغو ردیف لیست انتظار</summary>
    /// <param name="id">شناسه ردیف</param>
    [HttpPost("waitlist/{id:long}/cancel")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> CancelWaitlist(long id)
    {
        var result = await _mediator.Send(new CancelWaitlistEntryCommand(id));
        return ToResponse(result, "ردیف لیست انتظار لغو شد");
    }

    /// <summary>
    /// پیشنهاد دستی جای خالی — متناظرترین منتظر همان پزشک پیامک پیشنهاد می‌گیرد
    /// </summary>
    /// <param name="doctorId">پزشک نوبت آزادشده</param>
    /// <param name="date">تاریخ شمسی نوبت — تهی یعنی امروز</param>
    /// <param name="time">ساعت نوبت — مثل 10:30</param>
    [HttpPost("waitlist/offer")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> OfferFreedSlot([FromQuery] long doctorId,
        [FromQuery] string? date = null, [FromQuery] string time = "10:00")
    {
        if (doctorId <= 0)
            return ToResponse(Result<long>.Failure(new Error("WAITLIST.DOCTOR", "شناسه پزشک نامعتبر است")));
        var g = PersianDateHelper.ToGregorian(date);
        if (!string.IsNullOrWhiteSpace(date) && g is null)
            return ToResponse(Result<long>.Failure(new Error("WAITLIST.DATE", "تاریخ شمسی نوبت نامعتبر است")));
        if (string.IsNullOrWhiteSpace(time) || !TimeOnly.TryParse(PersianDateHelper.ToLatinDigits(time), out var timeOnly))
            return ToResponse(Result<long>.Failure(new Error("WAITLIST.TIME", "ساعت نوبت نامعتبر است")));
        var dateOnly = g is { } gdate ? DateOnly.FromDateTime(gdate) : _clock.TodayIran;
        var result = await _mediator.Send(new NotifyWaitlistForFreedSlotCommand(0, doctorId, dateOnly, timeOnly));
        return ToResponse(result, result.IsSuccess && result.Value > 0
            ? "پیشنهاد نوبت برای بیمار در صف ارسال پیامک ثبت شد"
            : "بیمار منتظرِ واجد شرایطی برای این پزشک/تاریخ نیست");
    }
}
