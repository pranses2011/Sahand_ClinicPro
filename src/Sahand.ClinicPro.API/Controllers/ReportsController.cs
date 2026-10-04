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
