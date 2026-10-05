using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Attendance;

// ═══════════════════════════════════════════════════════════════
// تابلوی امروز
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف تابلوی امروز</summary>
public record TodayBoardRow(
    long EmployeeId,
    string FullName,
    string? PersonnelCode,
    string? DepartmentName,
    string? ShiftName,
    string? ShiftColor,
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    int LateMinutes,
    int EarlyLeaveMinutes,
    AttendanceStatus Status,
    bool IsManuallyEdited);

/// <summary>داده کامل تابلوی امروز</summary>
public record TodayBoardResult(
    DateOnly Date,
    string ShamsiDate,
    int TotalStaff,
    int PresentCount,
    int LateCount,
    int AbsentCount,
    int LeaveCount,
    int MissionCount,
    int NotArrivedCount,
    List<TodayBoardRow> Rows);

/// <summary>پرس‌وجوی تابلوی امروز — وضعیت زنده همه پرسنل</summary>
public record GetTodayBoardQuery(DateOnly? Date) : IRequest<Result<TodayBoardResult>>;

/// <summary>گرداننده تابلوی امروز</summary>
public class GetTodayBoardQueryHandler : IRequestHandler<GetTodayBoardQuery, Result<TodayBoardResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetTodayBoardQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — تجمیع حضور روز با شیفت مؤثر هر پرسنل</summary>
    public async Task<Result<TodayBoardResult>> Handle(GetTodayBoardQuery request, CancellationToken ct)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var employees = await _unitOfWork.Repository<Domain.Entities.People.Employee>().QueryableNoTracking
            .Where(e => !e.IsDeleted && e.IsActive)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => new
            {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.PersonnelCode,
                DepartmentName = e.Department != null ? e.Department.Name : null,
            })
            .ToListAsync(ct);

        var attendances = await _unitOfWork.Repository<EmployeeAttendance>().QueryableNoTracking
            .Where(a => a.AttendanceDate == date && !a.IsDeleted)
            .ToDictionaryAsync(a => a.EmployeeId, ct);

        var assignments = await _unitOfWork.Repository<EmployeeShiftAssignment>().QueryableNoTracking
            .Where(a => a.IsActive && !a.IsDeleted
                     && a.FromDate <= date && (a.ToDate == null || a.ToDate >= date))
            .OrderByDescending(a => a.Priority)
            .ToListAsync(ct);

        var shifts = await _unitOfWork.Repository<Shift>().QueryableNoTracking
            .Where(s => s.IsActive && !s.IsDeleted)
            .ToDictionaryAsync(s => s.Id, ct);

        var rows = new List<TodayBoardRow>(employees.Count);
        foreach (var emp in employees)
        {
            attendances.TryGetValue(emp.Id, out var att);
            var assignment = assignments.FirstOrDefault(a =>
                a.EmployeeId == emp.Id && a.IsEffectiveOn(date));
            shifts.TryGetValue(assignment?.ShiftId ?? 0, out var shift);

            rows.Add(new TodayBoardRow(
                emp.Id,
                emp.FullName,
                emp.PersonnelCode,
                emp.DepartmentName,
                shift?.ShiftName,
                shift?.Color,
                att?.CheckIn,
                att?.CheckOut,
                att?.LateMinutes ?? 0,
                att?.EarlyLeaveMinutes ?? 0,
                att?.Status ?? AttendanceStatus.Absent,
                att?.IsManuallyEdited ?? false));
        }

        var result = new TodayBoardResult(
            date,
            PersianDateHelper.ToShamsi(date.ToDateTime(TimeOnly.MinValue)),
            rows.Count,
            rows.Count(r => r.Status == AttendanceStatus.Present),
            rows.Count(r => r.Status == AttendanceStatus.Late),
            rows.Count(r => r.Status == AttendanceStatus.Absent),
            rows.Count(r => r.Status == AttendanceStatus.Leave),
            rows.Count(r => r.Status == AttendanceStatus.Mission),
            rows.Count(r => r.CheckIn is null && r.Status is AttendanceStatus.Absent or AttendanceStatus.Leave or AttendanceStatus.Mission),
            rows);

        return Result<TodayBoardResult>.Success(result);
    }
}

// ═══════════════════════════════════════════════════════════════
// گزارش ماهانه
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف گزارش ماهانه هر پرسنل</summary>
public record MonthlyReportRow(
    long EmployeeId,
    string FullName,
    string? PersonnelCode,
    string? DepartmentName,
    int PresentDays,
    int AbsentDays,
    int LeaveDays,
    int MissionDays,
    int LateCount,
    int TotalLateMinutes,
    int TotalEarlyLeaveMinutes,
    decimal TotalWorkedHours,
    decimal TotalOvertimeHours);

/// <summary>پرس‌وجوی گزارش ماهانه — سال/ماه شمسی</summary>
public record GetMonthlyAttendanceReportQuery(int Year, int Month, long? DepartmentId)
    : IRequest<Result<List<MonthlyReportRow>>>;

/// <summary>گرداننده گزارش ماهانه</summary>
public class GetMonthlyAttendanceReportQueryHandler
    : IRequestHandler<GetMonthlyAttendanceReportQuery, Result<List<MonthlyReportRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetMonthlyAttendanceReportQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — بازه ماه شمسی به میلادی و تجمیع</summary>
    public async Task<Result<List<MonthlyReportRow>>> Handle(GetMonthlyAttendanceReportQuery request, CancellationToken ct)
    {
        var (rangeFrom, rangeTo) = MonthRange(request.Year, request.Month);

        var employees = await _unitOfWork.Repository<Domain.Entities.People.Employee>().QueryableNoTracking
            .Where(e => !e.IsDeleted && e.IsActive
                     && (request.DepartmentId == null || e.DepartmentId == request.DepartmentId))
            .OrderBy(e => e.LastName)
            .Select(e => new
            {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.PersonnelCode,
                DepartmentName = e.Department != null ? e.Department.Name : null,
            })
            .ToListAsync(ct);

        var records = await _unitOfWork.Repository<EmployeeAttendance>().QueryableNoTracking
            .Where(a => !a.IsDeleted
                     && a.AttendanceDate >= rangeFrom
                     && a.AttendanceDate < rangeTo)
            .ToListAsync(ct);

        var rows = employees
            .Select(emp =>
            {
                var mine = records.Where(r => r.EmployeeId == emp.Id).ToList();
                return new MonthlyReportRow(
                    emp.Id,
                    emp.FullName,
                    emp.PersonnelCode,
                    emp.DepartmentName,
                    mine.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late),
                    mine.Count(r => r.Status == AttendanceStatus.Absent),
                    mine.Count(r => r.Status == AttendanceStatus.Leave),
                    mine.Count(r => r.Status == AttendanceStatus.Mission),
                    mine.Count(r => r.Status == AttendanceStatus.Late),
                    mine.Sum(r => r.LateMinutes),
                    mine.Sum(r => r.EarlyLeaveMinutes),
                    mine.Sum(r => r.WorkedHours),
                    mine.Sum(r => r.OvertimeHours));
            })
            .ToList();

        return Result<List<MonthlyReportRow>>.Success(rows);
    }

    /// <summary>بازه میلادی ماه شمسی — از روز نخست تا روز نخست ماه بعد</summary>
    private static (DateOnly From, DateOnly To) MonthRange(int year, int month)
    {
        var start = PersianDateHelper.ToGregorian(year, month, 1);
        var (ny, nm) = month == 12 ? (year + 1, 1) : (year, month + 1);
        var end = PersianDateHelper.ToGregorian(ny, nm, 1);
        return (DateOnly.FromDateTime(start), DateOnly.FromDateTime(end));
    }
}

// ═══════════════════════════════════════════════════════════════
// دستگاه‌ها و نگاشت‌ها
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف دستگاه</summary>
public record DeviceListRow(
    long Id,
    string Name,
    DeviceBrand Brand,
    string? Model,
    string IpAddress,
    int Port,
    string? Location,
    DeviceStatus Status,
    bool IsActive,
    int SyncIntervalMinutes,
    DateTime? LastSyncAt,
    int? LastSyncLogCount,
    string? LastError,
    int MappedUsersCount);

/// <summary>پرس‌وجوی فهرست دستگاه‌ها</summary>
public record GetDevicesQuery() : IRequest<Result<List<DeviceListRow>>>;

/// <summary>گرداننده فهرست دستگاه‌ها</summary>
public class GetDevicesQueryHandler : IRequestHandler<GetDevicesQuery, Result<List<DeviceListRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDevicesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<DeviceListRow>>> Handle(GetDevicesQuery request, CancellationToken ct)
    {
        var devices = await _unitOfWork.Repository<AttendanceDevice>().QueryableNoTracking
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new DeviceListRow(
                d.Id, d.Name, d.Brand, d.Model, d.IpAddress, d.Port, d.Location,
                d.Status, d.IsActive, d.SyncIntervalMinutes, d.LastSyncAt,
                d.LastSyncLogCount, d.LastError,
                d.UserMappings.Count(m => m.IsActive && !m.IsDeleted)))
            .ToListAsync(ct);

        return Result<List<DeviceListRow>>.Success(devices);
    }
}

/// <summary>ردیف نگاشت کاربر دستگاه</summary>
public record DeviceMappingRow(
    long Id,
    string EnrollNumber,
    long EmployeeId,
    string EmployeeName,
    string? PersonnelCode,
    bool IsActive);

/// <summary>پرس‌وجوی نگاشت‌های یک دستگاه</summary>
public record GetDeviceMappingsQuery(long DeviceId) : IRequest<Result<List<DeviceMappingRow>>>;

/// <summary>گرداننده نگاشت‌های دستگاه</summary>
public class GetDeviceMappingsQueryHandler : IRequestHandler<GetDeviceMappingsQuery, Result<List<DeviceMappingRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDeviceMappingsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<DeviceMappingRow>>> Handle(GetDeviceMappingsQuery request, CancellationToken ct)
    {
        var rows = await _unitOfWork.Repository<DeviceUserMapping>().QueryableNoTracking
            .Where(m => !m.IsDeleted && m.DeviceId == request.DeviceId)
            .OrderBy(m => m.EnrollNumber)
            .Select(m => new DeviceMappingRow(
                m.Id, m.EnrollNumber, m.EmployeeId,
                m.Employee!.FirstName + " " + m.Employee.LastName,
                m.Employee.PersonnelCode, m.IsActive))
            .ToListAsync(ct);

        return Result<List<DeviceMappingRow>>.Success(rows);
    }
}

// ═══════════════════════════════════════════════════════════════
// لاگ خام
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف لاگ خام</summary>
public record RawLogRow(
    long Id,
    string DeviceName,
    string EnrollNumber,
    string? EmployeeName,
    DateTime PunchTime,
    string PunchStateFa,
    string StatusFa,
    string? ErrorMessage);

/// <summary>پرس‌وجوی لاگ‌های خام با فیلتر</summary>
public record GetRawLogsQuery(
    long? DeviceId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    RawLogStatus? Status,
    int Take = 300) : IRequest<Result<List<RawLogRow>>>;

/// <summary>گرداننده لاگ‌های خام</summary>
public class GetRawLogsQueryHandler : IRequestHandler<GetRawLogsQuery, Result<List<RawLogRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetRawLogsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — با نام پرسنل نگاشت‌شده و برچسب‌های فارسی</summary>
    public async Task<Result<List<RawLogRow>>> Handle(GetRawLogsQuery request, CancellationToken ct)
    {
        var q = _unitOfWork.Repository<DeviceRawLog>().QueryableNoTracking
            .Where(r => r.Device != null && !r.Device.IsDeleted);

        if (request.DeviceId is { } deviceId)
            q = q.Where(r => r.DeviceId == deviceId);
        if (request.FromUtc is { } from)
            q = q.Where(r => r.PunchTime >= from);
        if (request.ToUtc is { } to)
            q = q.Where(r => r.PunchTime <= to);
        if (request.Status is { } status)
            q = q.Where(r => r.ProcessedStatus == status);

        var mappings = await _unitOfWork.Repository<DeviceUserMapping>().QueryableNoTracking
            .Where(m => m.IsActive && !m.IsDeleted)
            .Select(m => new { m.DeviceId, m.EnrollNumber, Name = m.Employee!.FirstName + " " + m.Employee.LastName })
            .ToListAsync(ct);
        var mapIndex = mappings
            .GroupBy(m => (m.DeviceId, m.EnrollNumber))
            .ToDictionary(g => g.Key, g => g.First().Name);

        var raw = await q
            .OrderByDescending(r => r.PunchTime)
            .Take(request.Take)
            .Select(r => new
            {
                r.Id,
                DeviceName = r.Device!.Name,
                r.DeviceId,
                r.EnrollNumber,
                r.PunchTime,
                r.PunchState,
                r.ProcessedStatus,
                r.ErrorMessage,
            })
            .ToListAsync(ct);

        var rows = raw
            .Select(r => new RawLogRow(
                r.Id,
                r.DeviceName,
                r.EnrollNumber,
                mapIndex.TryGetValue((r.DeviceId, r.EnrollNumber), out var name) ? name : null,
                r.PunchTime,
                PunchStateFa(r.PunchState),
                ProcessedFa(r.ProcessedStatus),
                r.ErrorMessage))
            .ToList();

        return Result<List<RawLogRow>>.Success(rows);
    }

    private static string PunchStateFa(PunchState s) => s switch
    {
        PunchState.CheckIn => "ورود",
        PunchState.CheckOut => "خروج",
        PunchState.BreakOut => "خروج استراحت",
        PunchState.BreakIn => "بازگشت استراحت",
        PunchState.OvertimeIn => "ورود اضافه‌کاری",
        PunchState.OvertimeOut => "خروج اضافه‌کاری",
        _ => "نامشخص",
    };

    private static string ProcessedFa(RawLogStatus s) => s switch
    {
        RawLogStatus.Pending => "در انتظار",
        RawLogStatus.Processed => "پردازش‌شده",
        RawLogStatus.Ignored => "نادیده",
        RawLogStatus.Error => "خطا",
        _ => "؟",
    };
}

// ═══════════════════════════════════════════════════════════════
// شیفت‌ها و تخصیص‌ها
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف شیفت</summary>
public record ShiftListRow(
    long Id,
    string ShiftName,
    string? ShiftCode,
    TimeOnly StartTime,
    TimeOnly EndTime,
    ShiftType ShiftType,
    bool IsNightShift,
    int LateGraceMinutes,
    int EarlyLeaveGraceMinutes,
    int BreakMinutes,
    int OvertimeStartAfterMinutes,
    string? Color,
    bool IsActive,
    int AssignedCount);

/// <summary>پرس‌وجوی فهرست شیفت‌ها</summary>
public record GetShiftsQuery() : IRequest<Result<List<ShiftListRow>>>;

/// <summary>گرداننده فهرست شیفت‌ها</summary>
public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, Result<List<ShiftListRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetShiftsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<ShiftListRow>>> Handle(GetShiftsQuery request, CancellationToken ct)
    {
        var rows = await _unitOfWork.Repository<Shift>().QueryableNoTracking
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftListRow(
                s.Id, s.ShiftName, s.ShiftCode, s.StartTime, s.EndTime, s.ShiftType,
                s.IsNightShift, s.LateGraceMinutes, s.EarlyLeaveGraceMinutes,
                s.BreakMinutes, s.OvertimeStartAfterMinutes, s.Color, s.IsActive,
                s.Assignments.Count(a => a.IsActive && !a.IsDeleted)))
            .ToListAsync(ct);

        return Result<List<ShiftListRow>>.Success(rows);
    }
}

/// <summary>ردیف تخصیص شیفت</summary>
public record ShiftAssignmentRow(
    long Id,
    long EmployeeId,
    string EmployeeName,
    string? PersonnelCode,
    long ShiftId,
    string ShiftName,
    string? ShiftColor,
    DateOnly FromDate,
    DateOnly? ToDate,
    ShiftAssignmentRecurrence Recurrence,
    int WeekDaysMask,
    int Priority,
    string? Notes);

/// <summary>پرس‌وجوی تخصیص‌ها — همه یا یک پرسنل</summary>
public record GetShiftAssignmentsQuery(long? EmployeeId) : IRequest<Result<List<ShiftAssignmentRow>>>;

/// <summary>گرداننده تخصیص‌ها</summary>
public class GetShiftAssignmentsQueryHandler : IRequestHandler<GetShiftAssignmentsQuery, Result<List<ShiftAssignmentRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetShiftAssignmentsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<ShiftAssignmentRow>>> Handle(GetShiftAssignmentsQuery request, CancellationToken ct)
    {
        var q = _unitOfWork.Repository<EmployeeShiftAssignment>().QueryableNoTracking
            .Where(a => !a.IsDeleted && a.IsActive);

        if (request.EmployeeId is { } empId)
            q = q.Where(a => a.EmployeeId == empId);

        var rows = await q
            .OrderByDescending(a => a.FromDate)
            .Select(a => new ShiftAssignmentRow(
                a.Id, a.EmployeeId,
                a.Employee!.FirstName + " " + a.Employee.LastName,
                a.Employee.PersonnelCode,
                a.ShiftId, a.Shift!.ShiftName, a.Shift.Color,
                a.FromDate, a.ToDate, a.Recurrence, a.WeekDaysMask, a.Priority, a.Notes))
            .ToListAsync(ct);

        return Result<List<ShiftAssignmentRow>>.Success(rows);
    }
}

// ═══════════════════════════════════════════════════════════════
// تعطیلات و اصلاحیه‌ها
// ═══════════════════════════════════════════════════════════════

/// <summary>ردیف تعطیلی</summary>
public record HolidayRow(long Id, DateOnly Date, string ShamsiDate, string Title, HolidayType Type, bool IsRecurring);

/// <summary>پرس‌وجوی تعطیلات یک سال شمسی</summary>
public record GetHolidaysQuery(int Year) : IRequest<Result<List<HolidayRow>>>;

/// <summary>گرداننده تعطیلات</summary>
public class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, Result<List<HolidayRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetHolidaysQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — فیلتر تقویمی سال شمسی</summary>
    public async Task<Result<List<HolidayRow>>> Handle(GetHolidaysQuery request, CancellationToken ct)
    {
        var from = PersianDateHelper.ToGregorian(request.Year, 1, 1);
        var to = PersianDateHelper.ToGregorian(request.Year + 1, 1, 1);

        var rows = await _unitOfWork.Repository<Holiday>().QueryableNoTracking
            .Where(h => !h.IsDeleted && h.Date >= DateOnly.FromDateTime(from) && h.Date < DateOnly.FromDateTime(to))
            .OrderBy(h => h.Date)
            .Select(h => new HolidayRow(
                h.Id, h.Date, "", h.Title, h.HolidayType, h.IsRecurring))
            .ToListAsync(ct);

        var result = rows
            .Select(r => r with { ShamsiDate = PersianDateHelper.ToShamsi(r.Date.ToDateTime(TimeOnly.MinValue)) })
            .ToList();

        return Result<List<HolidayRow>>.Success(result);
    }
}

/// <summary>ردیف اصلاحیه</summary>
public record CorrectionRow(
    long Id,
    long EmployeeId,
    string EmployeeName,
    string? PersonnelCode,
    DateOnly AttendanceDate,
    string ShamsiDate,
    string FieldName,
    string? OldValue,
    string NewValue,
    string Reason,
    CorrectionStatus Status,
    string? ApproverNote);

/// <summary>پرس‌وجوی اصلاحیه‌ها — در انتظار یا همه</summary>
public record GetCorrectionsQuery(bool OnlyPending) : IRequest<Result<List<CorrectionRow>>>;

/// <summary>گرداننده اصلاحیه‌ها</summary>
public class GetCorrectionsQueryHandler : IRequestHandler<GetCorrectionsQuery, Result<List<CorrectionRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetCorrectionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<CorrectionRow>>> Handle(GetCorrectionsQuery request, CancellationToken ct)
    {
        var q = _unitOfWork.Repository<AttendanceCorrection>().QueryableNoTracking
            .Where(c => !c.IsDeleted);

        if (request.OnlyPending)
            q = q.Where(c => c.Status == CorrectionStatus.Pending);

        var rows = await q
            .OrderByDescending(c => c.AttendanceDate)
            .Select(c => new CorrectionRow(
                c.Id, c.EmployeeId,
                c.Employee!.FirstName + " " + c.Employee.LastName,
                c.Employee.PersonnelCode,
                c.AttendanceDate, "", c.FieldName, c.OldValue, c.NewValue,
                c.Reason, c.Status, c.ApproverNote))
            .ToListAsync(ct);

        var result = rows
            .Select(r => r with
            {
                ShamsiDate = PersianDateHelper.ToShamsi(r.AttendanceDate.ToDateTime(TimeOnly.MinValue)),
                FieldName = r.FieldName == "CheckIn" ? "ورود" : "خروج",
            })
            .ToList();

        return Result<List<CorrectionRow>>.Success(result);
    }
}

/// <summary>ردیف تایم‌لاین روز یک پرسنل — همه ضربه‌های خام به ترتیب</summary>
public record TimelineEntry(DateTime PunchTime, string DeviceName, string State, string VerifyMode);

/// <summary>پرس‌وجوی تایم‌لاین روز</summary>
public record GetEmployeeTimelineQuery(long EmployeeId, DateOnly Date)
    : IRequest<Result<List<TimelineEntry>>>;

/// <summary>گرداننده تایم‌لاین — بازسازی مسیر روز از لاگ خام</summary>
public class GetEmployeeTimelineQueryHandler : IRequestHandler<GetEmployeeTimelineQuery, Result<List<TimelineEntry>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetEmployeeTimelineQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — از میان دستگاه‌های دارای نگاشت به این پرسنل</summary>
    public async Task<Result<List<TimelineEntry>>> Handle(GetEmployeeTimelineQuery request, CancellationToken ct)
    {
        var enrollNumbers = await _unitOfWork.Repository<DeviceUserMapping>().QueryableNoTracking
            .Where(m => m.EmployeeId == request.EmployeeId && m.IsActive && !m.IsDeleted)
            .Select(m => m.EnrollNumber)
            .Distinct()
            .ToListAsync(ct);

        if (enrollNumbers.Count == 0)
            return Result<List<TimelineEntry>>.Success([]);

        var dayStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1).AddHours(12);

        var logs = await _unitOfWork.Repository<DeviceRawLog>().QueryableNoTracking
            .Where(r => enrollNumbers.Contains(r.EnrollNumber)
                     && r.PunchTime >= dayStart.ToUniversalTime()
                     && r.PunchTime <= dayEnd.ToUniversalTime())
            .OrderBy(r => r.PunchTime)
            .Select(r => new TimelineEntry(
                r.PunchTime,
                r.Device!.Name,
                r.PunchState == PunchState.CheckIn ? "ورود"
                    : r.PunchState == PunchState.CheckOut ? "خروج"
                    : r.PunchState == PunchState.BreakOut ? "خروج استراحت"
                    : r.PunchState == PunchState.BreakIn ? "بازگشت استراحت"
                    : r.PunchState == PunchState.OvertimeIn ? "ورود اضافه‌کاری"
                    : r.PunchState == PunchState.OvertimeOut ? "خروج اضافه‌کاری"
                    : "نامشخص",
                r.VerifyMode == 1 ? "اثر انگشت" : r.VerifyMode == 2 ? "کارت" : r.VerifyMode == 15 ? "چهره" : "سایر"))
            .ToListAsync(ct);

        return Result<List<TimelineEntry>>.Success(logs);
    }
}
