using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Attendance;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر ماژول حضور و غیاب — تابلو، دستگاه‌ها، شیفت‌ها، اصلاحیه‌ها و گزارش‌ها
/// مسیرها: /api/v1/attendance
/// </summary>
[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AttendanceController(IMediator mediator) => _mediator = mediator;

    // ═══════════════ تابلوی امروز ═══════════════

    /// <summary>تابلوی زنده امروز — کارت‌های آماری و ردیف همه پرسنل</summary>
    [HttpGet("today-board")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetTodayBoard([FromQuery] DateOnly? date)
        => ToResponse(await _mediator.Send(new GetTodayBoardQuery(date)));

    /// <summary>گزارش ماهانه — سال/ماه شمسی با فیلتر بخش</summary>
    [HttpGet("monthly-report")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month, [FromQuery] long? departmentId)
        => ToResponse(await _mediator.Send(new GetMonthlyAttendanceReportQuery(year, month, departmentId)));

    /// <summary>تایم‌لاین ضربه‌های یک پرسنل در یک روز</summary>
    [HttpGet("timeline/{employeeId:long}")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetTimeline(long employeeId, [FromQuery] DateOnly date)
        => ToResponse(await _mediator.Send(new GetEmployeeTimelineQuery(employeeId, date)));

    // ═══════════════ دستگاه‌ها ═══════════════

    /// <summary>فهرست دستگاه‌ها با وضعیت اتصال</summary>
    [HttpGet("devices")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetDevices()
        => ToResponse(await _mediator.Send(new GetDevicesQuery()));

    /// <summary>ثبت/ویرایش دستگاه</summary>
    [HttpPost("devices")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> SaveDevice([FromBody] SaveDeviceDto dto)
        => ToResponse(await _mediator.Send(new SaveDeviceCommand(dto)));

    /// <summary>حذف نرم دستگاه</summary>
    [HttpDelete("devices/{deviceId:long}")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> DeleteDevice(long deviceId)
        => ToResponse(await _mediator.Send(new DeleteDeviceCommand(deviceId)));

    /// <summary>آزمون اتصال دستگاه</summary>
    [HttpPost("devices/{deviceId:long}/test")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> TestDevice(long deviceId)
        => ToResponse(await _mediator.Send(new TestDeviceConnectionCommand(deviceId)));

    /// <summary>همگام‌سازی فوری دستگاه</summary>
    [HttpPost("devices/{deviceId:long}/sync")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> SyncDevice(long deviceId)
        => ToResponse(await _mediator.Send(new SyncDeviceCommand(deviceId)));

    /// <summary>همگام‌سازی همه دستگاه‌های فعال</summary>
    [HttpPost("devices/sync-all")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> SyncAllDevices()
        => ToResponse(await _mediator.Send(new SyncAllDevicesCommand()));

    /// <summary>نگاشت‌های کاربران یک دستگاه</summary>
    [HttpGet("devices/{deviceId:long}/mappings")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetMappings(long deviceId)
        => ToResponse(await _mediator.Send(new GetDeviceMappingsQuery(deviceId)));

    /// <summary>ثبت نگاشت شناسه دستگاه به پرسنل</summary>
    [HttpPost("devices/mappings")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> MapDeviceUser([FromBody] MapDeviceUserCommand command)
        => ToResponse(await _mediator.Send(command));

    /// <summary>حذف نگاشت</summary>
    [HttpDelete("mappings/{mappingId:long}")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> UnmapDeviceUser(long mappingId)
        => ToResponse(await _mediator.Send(new UnmapDeviceUserCommand(mappingId)));

    /// <summary>لاگ‌های خام با فیلتر</summary>
    [HttpGet("raw-logs")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetRawLogs(
        [FromQuery] long? deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] RawLogStatus? status,
        [FromQuery] int take = 300)
        => ToResponse(await _mediator.Send(new GetRawLogsQuery(deviceId, from, to, status, take)));

    // ═══════════════ شیفت‌ها و تخصیص ═══════════════

    /// <summary>فهرست شیفت‌ها</summary>
    [HttpGet("shifts")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetShifts()
        => ToResponse(await _mediator.Send(new GetShiftsQuery()));

    /// <summary>ثبت/ویرایش شیفت</summary>
    [HttpPost("shifts")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> SaveShift([FromBody] SaveShiftDto dto)
        => ToResponse(await _mediator.Send(new SaveShiftCommand(dto)));

    /// <summary>حذف شیفت</summary>
    [HttpDelete("shifts/{shiftId:long}")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> DeleteShift(long shiftId)
        => ToResponse(await _mediator.Send(new DeleteShiftCommand(shiftId)));

    /// <summary>تخصیص‌های شیفت — همه یا یک پرسنل</summary>
    [HttpGet("shift-assignments")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetAssignments([FromQuery] long? employeeId)
        => ToResponse(await _mediator.Send(new GetShiftAssignmentsQuery(employeeId)));

    /// <summary>تخصیص شیفت به پرسنل</summary>
    [HttpPost("shift-assignments")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> AssignShift([FromBody] AssignShiftCommand command)
        => ToResponse(await _mediator.Send(command));

    /// <summary>پایان تخصیص شیفت</summary>
    [HttpPost("shift-assignments/{assignmentId:long}/end")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> EndAssignment(long assignmentId, [FromQuery] DateOnly? endDate)
        => ToResponse(await _mediator.Send(new EndShiftAssignmentCommand(assignmentId, endDate)));

    // ═══════════════ تعطیلات ═══════════════

    /// <summary>تعطیلات یک سال شمسی</summary>
    [HttpGet("holidays")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetHolidays([FromQuery] int year)
        => ToResponse(await _mediator.Send(new GetHolidaysQuery(year)));

    /// <summary>ثبت تعطیلی</summary>
    [HttpPost("holidays")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> CreateHoliday([FromBody] CreateHolidayCommand command)
        => ToResponse(await _mediator.Send(command));

    /// <summary>حذف تعطیلی</summary>
    [HttpDelete("holidays/{holidayId:long}")]
    [HasPermission("attendance.shifts")]
    public async Task<IActionResult> DeleteHoliday(long holidayId)
        => ToResponse(await _mediator.Send(new DeleteHolidayCommand(holidayId)));

    // ═══════════════ اصلاحیه‌ها و ثبت دستی ═══════════════

    /// <summary>اصلاحیه‌های کارت — در انتظار یا همه</summary>
    [HttpGet("corrections")]
    [HasPermission("attendance.view")]
    public async Task<IActionResult> GetCorrections([FromQuery] bool onlyPending = true)
        => ToResponse(await _mediator.Send(new GetCorrectionsQuery(onlyPending)));

    /// <summary>ثبت درخواست اصلاح کارت</summary>
    [HttpPost("corrections")]
    [HasPermission("attendance.create")]
    public async Task<IActionResult> SubmitCorrection([FromBody] SubmitCorrectionCommand command)
        => ToResponse(await _mediator.Send(command));

    /// <summary>تأیید/رد اصلاحیه — با اعمال خودکار روی رکورد حضور</summary>
    [HttpPost("corrections/{correctionId:long}/decide")]
    [HasPermission("attendance.approve")]
    public async Task<IActionResult> DecideCorrection(long correctionId, [FromBody] DecideRequest request)
        => ToResponse(await _mediator.Send(new DecideCorrectionCommand(correctionId, request.Approve, request.Note)));

    /// <summary>بدنه تصمیم اصلاحیه</summary>
    public record DecideRequest(bool Approve, string? Note);

    /// <summary>ثبت دستی ورود/خروج</summary>
    [HttpPost("manual-punch")]
    [HasPermission("attendance.create")]
    public async Task<IActionResult> ManualPunch([FromBody] RegisterManualPunchCommand command)
        => ToResponse(await _mediator.Send(command));

    /// <summary>محاسبه مجدد یک روز — پردازش لاگ‌های مانده</summary>
    [HttpPost("recalculate")]
    [HasPermission("attendance.devices")]
    public async Task<IActionResult> Recalculate([FromBody] RecalculateRequest request)
        => ToResponse(await _mediator.Send(new RecalculateAttendanceCommand(request.Date, request.EmployeeId)));

    /// <summary>بدنه درخواست محاسبه مجدد</summary>
    public record RecalculateRequest(DateOnly? Date, long? EmployeeId);
}
