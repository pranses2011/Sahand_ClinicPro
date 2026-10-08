using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داشبورد — آمار روزانه برای صفحه آغازین کلاینت دسکتاپ
/// مسیرها: /api/v1/dashboard
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ApiBaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Application.Interfaces.ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق واحد کار و کاربر جاری</summary>
    public DashboardController(IUnitOfWork unitOfWork,
        Application.Interfaces.ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>
    /// آمار خلاصه امروز — بیماران ثبت‌شده، نوبت‌ها، ویزیت‌ها و درآمد
    /// </summary>
    [HttpGet("stats")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetTodayStats(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newPatients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .CountAsync(p => p.RegistrationDate != null
                          && DateOnly.FromDateTime(p.RegistrationDate.Value) == today, ct);

        var todayAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today, ct);

        var activeAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today
                          && (a.Status == AppointmentStatus.Booked
                              || a.Status == AppointmentStatus.Confirmed
                              || a.Status == AppointmentStatus.Arrived), ct);

        var todayRevenue = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => DateOnly.FromDateTime(i.InvoiceDate) == today
                     && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.PaidAmount, ct) ?? 0m;

        var todayVisits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .CountAsync(v => DateOnly.FromDateTime(v.VisitDate) == today, ct);

        var stats = new
        {
            newPatients,
            todayAppointments,
            activeAppointments,
            todayVisits,
            todayRevenue,
            todayDateShamsi = Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.UtcNow),
            serverTime = DateTime.UtcNow
        };

        return Ok(Application.Common.Models.ApiResponse<object>.Success(stats,
            "آمار امروز دریافت شد"));
    }

    /// <summary>
    /// نمودار درآمد روزانه — مشخصات پرامپت UI بخش ۱ (فرم ۲)
    /// خروجی: برچسب شمسی + مبلغ پرداخت‌شده برای N روز اخیر
    /// </summary>
    [HttpGet("revenue-chart")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-(days - 1));

        // واکشی خام + گروه‌بندی سمت کاربر — از ترجمه SQL توابع تاریخ پرهیز می‌شود
        var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
        var toDateTime = today.ToDateTime(TimeOnly.MaxValue);

        var rows = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InvoiceDate >= fromDateTime
                     && i.InvoiceDate <= toDateTime
                     && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new { i.InvoiceDate, i.PaidAmount })
            .ToListAsync(ct);

        var chart = Enumerable.Range(0, days)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                var dayStart = day.ToDateTime(TimeOnly.MinValue);
                var dayEnd = day.ToDateTime(TimeOnly.MaxValue);
                return new
                {
                    label = Application.Common.Helpers.PersianDateHelper.ToShamsi(dayStart),
                    amount = rows
                        .Where(r => r.InvoiceDate >= dayStart && r.InvoiceDate <= dayEnd)
                        .Sum(r => r.PaidAmount),
                };
            })
            .ToList();

        return Ok(Application.Common.Models.ApiResponse<object>.Success(chart,
            "داده نمودار درآمد دریافت شد"));
    }

    /// <summary>
    /// نوبت‌های امروز — مشخصات پرامپت UI بخش ۱ (فرم ۲: نوبت‌های پیش‌رو)
    /// </summary>
    [HttpGet("appointments-today")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetAppointmentsToday([FromQuery] int limit = 8,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var list = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .Where(a => a.AppointmentDate == today
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.AppointmentTime)
            .Take(limit)
            .Select(a => new
            {
                id = a.Id,
                queueNumber = a.QueueNumber,
                time = a.AppointmentTime.ToString("HH:mm"),
                patientName = a.Patient != null
                    ? a.Patient.FirstName + " " + a.Patient.LastName
                    : "ناشناس",
                doctorName = a.Doctor != null && a.Doctor.Employee != null
                    ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName
                    : "پزشک",
                status = a.Status.ToString(),
                statusText = a.Status == AppointmentStatus.Completed ? "تمام‌شده"
                    : a.Status == AppointmentStatus.InProgress ? "در حال ویزیت"
                    : a.Status == AppointmentStatus.Arrived ? "حاضر"
                    : a.Status == AppointmentStatus.Confirmed ? "تاییدشده"
                    : "رزرو شده",
            })
            .ToListAsync(ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(list,
            "نوبت‌های امروز دریافت شد"));
    }

    /// <summary>
    /// فعالیت‌های اخیر — مشخصات پرامپت UI بخش ۱ (فرم ۲: جدول فعالیت‌ها از AuditLog)
    /// </summary>
    [HttpGet("recent-activities")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var list = await _unitOfWork.Repository<Domain.Entities.Infrastructure.AuditLog>()
            .QueryableNoTracking
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .Select(l => new
            {
                time = l.Timestamp,
                timeShamsi = Application.Common.Helpers.PersianDateHelper
                    .ToShamsiWithTime(l.Timestamp),
                user = l.Username ?? "سیستم",
                action = l.ActionType.ToString(),
                entity = l.EntityName ?? string.Empty,
                entityId = l.EntityId,
                module = l.Module ?? string.Empty,
            })
            .ToListAsync(ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(list,
            "فعالیت‌های اخیر دریافت شد"));
    }

    /// <summary>
    /// هشدارهای داشبورد — موجودی کم، نزدیک انقضا، آزمایش در انتظار
    /// </summary>
    [HttpGet("alerts")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthAhead = today.AddDays(30);

        var lowStock = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .CountAsync(i => i.IsActive
                           && i.MinStock > 0
                           && i.Quantity <= i.MinStock, ct);

        var expiringSoon = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .CountAsync(i => i.IsActive
                           && i.ExpiryDate != null
                           && i.ExpiryDate >= today
                           && i.ExpiryDate <= monthAhead, ct);

        var pendingLabOrders = await _unitOfWork.Repository<Domain.Entities.Paraclinical.LabOrder>()
            .QueryableNoTracking
            .CountAsync(o => o.Status == Domain.Enums.LabOrderStatus.Ordered, ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            lowStock,
            expiringSoon,
            pendingLabOrders,
        }, "هشدارهای داشبورد دریافت شد"));
    }

    /* ═══════════ داشبورد شخصی‌سازی‌شده — چرخه ۱۳ (درخواست ۴-۲) ═══════════ */

    /// <summary>کلید تنظیم چیدمان داشبورد هر کاربر</summary>
    private string LayoutKey => $"dashboard-layout:{_currentUser.UserId ?? 0}";

    /// <summary>
    /// چیدمان شخصی داشبورد کاربر جاری — JSON ترتیب کارت‌ها و نمایش بخش‌ها
    /// </summary>
    [HttpGet("layout")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetLayout(CancellationToken ct)
    {
        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Key == LayoutKey && !x.IsDeleted, ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            layout = setting?.Value
        }));
    }

    /// <summary>
    /// ذخیره چیدمان شخصی داشبورد — هر کاربر چیدمان مستقل خودش را دارد
    /// </summary>
    /// <param name="request">JSON چیدمان — ترتیب کارت‌ها و نمایش بخش‌ها</param>
    /// <param name="ct">توکن لغو هم‌زمان (چرخه بیست‌وسوم رفع CS1573)</param>
    [HttpPut("layout")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Layout) || request.Layout.Length > 4000)
            return BadRequest(Application.Common.Models.ApiResponse<object>.Failure(
                "چیدمان نامعتبر است (حداکثر ۴۰۰۰ نویسه)"));

        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableTracking
            .FirstOrDefaultAsync(x => x.Key == LayoutKey && !x.IsDeleted, ct);

        if (setting is null)
        {
            setting = new Domain.Entities.Infrastructure.SystemSetting
            {
                Category = "DashboardLayout",
                Key = LayoutKey,
                Value = request.Layout,
                DataType = Domain.Enums.SettingDataType.Json,
                Description = $"چیدمان داشبورد شخصی کاربر {_currentUser.Username}"
            };
            await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
                .AddAsync(setting, ct);
        }
        else
        {
            setting.Value = request.Layout;
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return Ok(Application.Common.Models.ApiResponse<object>.SuccessMessage("چیدمان داشبورد ذخیره شد"));
    }
}

/// <summary>درخواست ذخیره چیدمان داشبورد</summary>
public record SaveLayoutRequest(string Layout);
