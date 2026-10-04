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

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DashboardController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
}
