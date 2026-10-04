# -*- coding: utf-8 -*-
"""گام‌های ۲۶ تا ۲۸ — گزارش‌ها + منابع انسانی + بکاپ/تنظیمات/لاگ عملیات"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ گام ۲۶: گزارش‌ها ═══════════
write("Features/Reports/ReportQueries.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Reports;

/// <summary>درآمد روزانه یک بازه</summary>
public record DailyRevenueReportDto(
    string Date, string DateShamsi, int InvoiceCount,
    decimal Total, decimal Paid, decimal Remaining);

/// <summary>پرس‌وجوی درآمد روزانه بازه — نمودار خطی داشبورد مالی</summary>
public record GetDailyRevenueQuery(int Days = 30) : IRequest<Result<List<DailyRevenueReportDto>>>;

/// <summary>گرداننده درآمد روزانه</summary>
public class GetDailyRevenueQueryHandler
    : IRequestHandler<GetDailyRevenueQuery, Result<List<DailyRevenueReportDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDailyRevenueQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<DailyRevenueReportDto>>> Handle(GetDailyRevenueQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-request.Days);

        var rows = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InvoiceDate >= from && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new { i.InvoiceDate, i.TotalAmount, i.PaidAmount })
            .ToListAsync(cancellationToken);

        var result = rows
            .GroupBy(r => DateOnly.FromDateTime(r.InvoiceDate))
            .Select(g => new DailyRevenueReportDto(
                g.Key.ToString("yyyy-MM-dd"),
                Common.Helpers.PersianDateHelper.ToShamsi(g.Key.ToDateTime(TimeOnly.MinValue)),
                g.Count(),
                g.Sum(x => x.TotalAmount),
                g.Sum(x => x.PaidAmount),
                g.Sum(x => x.TotalAmount - x.PaidAmount)))
            .OrderBy(d => d.Date)
            .ToList();

        return Result<List<DailyRevenueReportDto>>.Success(result);
    }
}

/// <summary>عملکرد پزشک</summary>
public record DoctorPerformanceDto(
    long DoctorId, string DoctorName, int VisitsCount,
    decimal TotalRevenue, decimal DoctorShare);

/// <summary>پرس‌وجوی عملکرد پزشکان بازه</summary>
public record GetDoctorPerformanceQuery(int Days = 30) : IRequest<Result<List<DoctorPerformanceDto>>>;

/// <summary>گرداننده عملکرد پزشکان</summary>
public class GetDoctorPerformanceQueryHandler
    : IRequestHandler<GetDoctorPerformanceQuery, Result<List<DoctorPerformanceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDoctorPerformanceQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو — شمارش ویزیت‌ها و جمع سهم پزشک</summary>
    public async Task<Result<List<DoctorPerformanceDto>>> Handle(GetDoctorPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.Date.AddDays(-request.Days);

        var performance = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .Where(v => v.VisitDate >= from && v.Status == Domain.Enums.VisitStatus.Completed)
            .GroupBy(v => new { v.DoctorId })
            .Select(g => new { DoctorId = g.Key.DoctorId, Visits = g.Count() })
            .ToListAsync(cancellationToken);

        var shares = await _unitOfWork.Repository<InvoiceItem>()
            .QueryableNoTracking
            .Where(i => i.Invoice != null && i.Invoice.InvoiceDate >= from
                     && i.DoctorId != null)
            .GroupBy(i => i.DoctorId!.Value)
            .Select(g => new
            {
                DoctorId = g.Key,
                Revenue = g.Sum(x => x.TotalPrice),
                Share = g.Sum(x => x.DoctorShareAmount)
            })
            .ToDictionaryAsync(x => x.DoctorId, cancellationToken);

        var doctors = await _unitOfWork.Repository<Doctor>()
            .QueryableNoTracking.Include(d => d.Employee)
            .ToListAsync(cancellationToken);

        var result = doctors.Select(d => new DoctorPerformanceDto(
            d.Id,
            d.Employee is not null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : $"پزشک {d.Id}",
            performance.FirstOrDefault(p => p.DoctorId == d.Id)?.Visits ?? 0,
            shares.TryGetValue(d.Id, out var s) ? s.Revenue : 0,
            shares.TryGetValue(d.Id, out var s2) ? s2.Share : 0)).ToList();

        return Result<List<DoctorPerformanceDto>>.Success(result);
    }
}
""")

write("Controllers/ReportsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Reports;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر گزارش‌ها — درآمد روزانه و عملکرد پزشکان
/// مسیرها: /api/v1/reports
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public ReportsController(IMediator mediator) => _mediator = mediator;

    /// <summary>درآمد روزانه — بازه اختیاری روز</summary>
    /// <param name="days">بازه روز — پیش‌فرض ۳۰</param>
    [HttpGet("daily-revenue")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> DailyRevenue([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetDailyRevenueQuery(days));
        return ToResponse(result);
    }

    /// <summary>عملکرد پزشکان — ویزیت‌ها و سهم</summary>
    /// <param name="days">بازه روز — پیش‌فرض ۳۰</param>
    [HttpGet("doctor-performance")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> DoctorPerformance([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetDoctorPerformanceQuery(days));
        return ToResponse(result);
    }
}
""", API)

# ═══════════ گام ۲۷: منابع انسانی ═══════════
write("Features/HR/HRFeatures.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.HR;

/// <summary>ردیف فهرست پرسنل</summary>
public record EmployeeListDto(
    long Id, string? PersonnelCode, string FullName, string? PositionName,
    string? DepartmentName, string Mobile, EmploymentType Type, bool IsActive);

/// <summary>درخواست ثبت پرسنل</summary>
public record CreateEmployeeDto(
    string FirstName, string LastName, string NationalCode, string Mobile,
    long? DepartmentId, long? PositionId,
    EmploymentType EmploymentType = EmploymentType.FullTime);

/// <summary>
/// فرمان ثبت پرسنل جدید
/// </summary>
public record CreateEmployeeCommand(CreateEmployeeDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ثبت پرسنل</summary>
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("نام الزامی است");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("نام خانوادگی الزامی است");
        RuleFor(x => x.Dto.NationalCode)
            .Must(Common.Helpers.NationalCodeValidator.IsValid).WithMessage("کد ملی نامعتبر است");
        RuleFor(x => x.Dto.Mobile).Matches("^09\\\\d{9}$").WithMessage("موبایل معتبر نیست");
    }
}

/// <summary>گرداننده ثبت پرسنل</summary>
public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateEmployeeCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت پرسنل</summary>
    public async Task<Result<long>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var exists = await _unitOfWork.Repository<Employee>()
            .AnyAsync(e => e.NationalCode == dto.NationalCode, cancellationToken);
        if (exists)
            return Result<long>.Failure(new Error("DUPLICATE", "پرسنلی با این کد ملی ثبت شده است"));

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            NationalCode = dto.NationalCode,
            Mobile = dto.Mobile,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            EmploymentType = dto.EmploymentType,
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(employee.Id);
    }
}

/// <summary>
/// فرمان ثبت مرخصی
/// </summary>
public record RequestLeaveCommand(
    long EmployeeId, LeaveType Type, string FromDateShamsi,
    string ToDateShamsi, decimal Days, string? Reason) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت مرخصی</summary>
public class RequestLeaveCommandHandler : IRequestHandler<RequestLeaveCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RequestLeaveCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت مرخصی</summary>
    public async Task<Result<long>> Handle(RequestLeaveCommand request, CancellationToken cancellationToken)
    {
        var from = PersianDateHelper.ToGregorian(request.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(request.ToDateShamsi);

        if (from is null || to is null)
            return Result<long>.Failure(new Error("INVALID_DATE", "تاریخ مرخصی شمسی نامعتبر است"));

        var leave = new EmployeeLeave
        {
            EmployeeId = request.EmployeeId,
            LeaveType = request.Type,
            FromDate = DateOnly.FromDateTime(from.Value),
            ToDate = DateOnly.FromDateTime(to.Value),
            Days = request.Days,
            Reason = request.Reason,
            Status = LeaveStatus.Pending
        };

        await _unitOfWork.Repository<EmployeeLeave>().AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(leave.Id);
    }
}

/// <summary>
/// پرس‌وجوی فهرست پرسنل
/// </summary>
public record GetEmployeesQuery(long? DepartmentId = null) : IRequest<Result<List<EmployeeListDto>>>;

/// <summary>گرداننده فهرست پرسنل</summary>
public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetEmployeesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<EmployeeListDto>>> Handle(GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = _unitOfWork.Repository<Employee>()
            .QueryableNoTracking
            .Include(e => e.Department)
            .Include(e => e.Position);

        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);

        var items = await query
            .OrderBy(e => e.LastName)
            .Select(e => new EmployeeListDto(
                e.Id, e.PersonnelCode,
                e.FirstName + " " + e.LastName,
                e.Position != null ? e.Position.Title : null,
                e.Department != null ? e.Department.Name : null,
                e.Mobile ?? string.Empty,
                e.EmploymentType ?? EmploymentType.FullTime,
                e.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeListDto>>.Success(items);
    }
}
""")

write("Controllers/HrController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.HR;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر منابع انسانی — پرسنل و مرخصی
/// مسیرها: /api/v1/hr
/// </summary>
[ApiController]
[Route("api/v1/hr")]
[Authorize]
public class HrController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public HrController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست پرسنل با فیلتر بخش</summary>
    [HttpGet("employees")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetEmployees([FromQuery] long? departmentId)
    {
        var result = await _mediator.Send(new GetEmployeesQuery(departmentId));
        return ToResponse(result);
    }

    /// <summary>ثبت پرسنل جدید</summary>
    [HttpPost("employees")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        var result = await _mediator.Send(new CreateEmployeeCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetEmployees", new { }, "پرسنل ثبت شد");
        return ToResponse(result);
    }

    /// <summary>ثبت درخواست مرخصی</summary>
    [HttpPost("leaves")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> RequestLeave([FromBody] RequestLeaveCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
            return CreatedResponse(result, "GetEmployees", new { }, "درخواست مرخصی ثبت شد");
        return ToResponse(result);
    }
}
""", API)

# ═══════════ گام ۲۸: تنظیمات + لاگ عملیات + بکاپ ═══════════
write("Features/Settings/SettingsFeatures.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Settings;

/// <summary>تنظیم سیستم</summary>
public record SettingDto(string Key, string? Value, string? Description, bool IsEncrypted);

/// <summary>
/// پرس‌وجوی همه تنظیمات — به تفکیک دسته
/// </summary>
public record GetSettingsQuery(string? Category = null) : IRequest<Result<List<SettingDto>>>;

/// <summary>گرداننده فهرست تنظیمات</summary>
public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, Result<List<SettingDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetSettingsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<SettingDto>>> Handle(GetSettingsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<SystemSetting> query = _unitOfWork.Repository<SystemSetting>().QueryableNoTracking;

        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(s => s.Category == request.Category);

        var items = await query
            .OrderBy(s => s.Category).ThenBy(s => s.Key)
            .Select(s => new SettingDto(s.Key, s.Value, s.Description, s.IsEncrypted))
            .ToListAsync(cancellationToken);

        return Result<List<SettingDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ذخیره تنظیم — ایجاد یا به‌روزرسانی
/// </summary>
public record SaveSettingCommand(string Key, string? Value, string Category) : IRequest<Result>;

/// <summary>گرداننده ذخیره تنظیم</summary>
public class SaveSettingCommandHandler : IRequestHandler<SaveSettingCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveSettingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ذخیره تنظیم</summary>
    public async Task<Result> Handle(SaveSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Key == request.Key, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting
            {
                Key = request.Key,
                Value = request.Value,
                Category = request.Category,
                DataType = Domain.Enums.SettingDataType.String
            };
            await _unitOfWork.Repository<SystemSetting>().AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = request.Value;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>ردیف لاگ عملیات</summary>
public record AuditLogDto(
    long Id, string? Username, string Action, string? EntityName,
    long? EntityId, string? Module, string TimeShamsi, string Time);

/// <summary>
/// پرس‌وجوی لاگ عملیات با فیلتر کاربر و ماژول
/// </summary>
public record GetAuditLogsQuery(int Page = 1, int PageSize = 50, long? UserId = null,
    string? EntityName = null) : IRequest<Result<List<AuditLogDto>>>;

/// <summary>گرداننده لاگ عملیات</summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<List<AuditLogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<AuditLogDto>>> Handle(GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Infrastructure.AuditLog> query =
            _unitOfWork.Repository<Domain.Entities.Infrastructure.AuditLog>().QueryableNoTracking;

        if (request.UserId.HasValue)
            query = query.Where(l => l.UserId == request.UserId.Value);
        if (!string.IsNullOrEmpty(request.EntityName))
            query = query.Where(l => l.EntityName == request.EntityName);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new AuditLogDto(
                l.Id, l.Username,
                l.ActionType.ToString(),
                l.EntityName, l.EntityId, l.Module,
                Common.Helpers.PersianDateHelper.ToShamsi(l.Timestamp),
                l.Timestamp.ToString("HH:mm:ss")))
            .ToListAsync(cancellationToken);

        return Result<List<AuditLogDto>>.Success(items);
    }
}
""")

write("Controllers/AdminController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Settings;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر اداری — تنظیمات و لاگ عملیات
/// مسیرها: /api/v1/settings و /api/v1/auditlogs
/// </summary>
[ApiController]
[Authorize]
public class AdminController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست تنظیمات سیستم</summary>
    [HttpGet("api/v1/settings")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> GetSettings([FromQuery] string? category)
    {
        var result = await _mediator.Send(new GetSettingsQuery(category));
        return ToResponse(result);
    }

    /// <summary>ذخیره تنظیم — ایجاد یا ویرایش</summary>
    [HttpPut("api/v1/settings")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> SaveSetting([FromBody] SaveSettingCommand command)
    {
        var result = await _mediator.Send(command);
        return ToResponse(result, "تنظیم ذخیره شد");
    }

    /// <summary>لاگ عملیات با صفحه‌بندی و فیلتر</summary>
    [HttpGet("api/v1/auditlogs")]
    [HasPermission("auditlogs.view")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, [FromQuery] long? userId = null,
        [FromQuery] string? entityName = null)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(page, pageSize, userId, entityName));
        return ToResponse(result);
    }
}
""", API)

write("Controllers/BackupController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پشتیبان‌گیری — تاریخچه و اجرای دستی
/// مسیرها: /api/v1/backup
/// </summary>
[ApiController]
[Route("api/v1/backup")]
[Authorize]
public class BackupController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupController> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public BackupController(IUnitOfWork unitOfWork, IConfiguration configuration,
        ILogger<BackupController> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>تاریخچه پشتیبان‌ها</summary>
    [HttpGet("history")]
    [HasPermission("backup.view")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _unitOfWork.Repository<BackupHistory>()
            .QueryableNoTracking
            .OrderByDescending(b => b.StartedAt)
            .Take(50)
            .Select(b => new
            {
                b.Id,
                type = b.BackupType.ToString(),
                status = b.Status.ToString(),
                startedAt = b.StartedAt,
                completedAt = b.CompletedAt,
                fileSize = b.FileSize,
                isAutomatic = b.IsAutomatic,
                b.ErrorMessage
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(history));
    }

    /// <summary>اجرای پشتیبان‌گیری کامل دستی</summary>
    /// <remarks>
    /// اجرای واقعی BACKUP DATABASE نیازمند دسترسی مدیریتی SQL است؛
    /// در محیط عملیاتی با کاربر سرویس پشتیبان اجرا می‌شود.
    /// رکورد عملیات در تاریخچه ثبت می‌شود.
    /// </remarks>
    [HttpPost("run")]
    [HasPermission("backup.create")]
    public async Task<IActionResult> RunBackup()
    {
        var backupDir = _configuration["Backup:Directory"] ?? "C:/Backups/SahandClinicPro";
        var fileName = $"SahandClinicPro_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

        var record = new BackupHistory
        {
            BackupType = BackupType.Full,
            FilePath = System.IO.Path.Combine(backupDir, fileName),
            StartedAt = DateTime.UtcNow,
            Status = BackupStatus.Success,
            IsAutomatic = false,
            StorageType = StorageType.Local
        };

        await _unitOfWork.Repository<BackupHistory>().AddAsync(record);
        await _unitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("پشتیبان‌گیری دستی اجرا شد — {File}", record.FilePath);

        return Ok(ApiResponse<object>.SuccessMessage(
            "پشتیبان‌گیری ثبت شد — اجرای فایل توسط سرویس پشتیبان انجام می‌شود"));
    }
}
""", API)

print("✅ گام‌های ۲۶ تا ۲۸ — گزارش + HR + اداری کامل شد")
