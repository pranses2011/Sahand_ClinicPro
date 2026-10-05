using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Payroll.GenerateSalaries;
using Sahand.ClinicPro.Application.Features.Payroll.PayrollQueries;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر حقوق و فیش حقوقی — گام R3 (چرخه چهارم)
/// تولید دوره، فهرست، پرداخت و داده فیش چاپی
/// مسیرها: /api/v1/salaries
/// </summary>
[ApiController]
[Route("api/v1/salaries")]
[Authorize]
public class SalariesController : ApiBaseController
{
    private readonly MediatR.IMediator _mediator;

    /// <summary>سازنده با فرستنده CQRS</summary>
    public SalariesController(MediatR.IMediator mediator) => _mediator = mediator;

    /// <summary>تولید حقوق دوره — همه کارکنان فعال یا یک کارمند (idempotent)</summary>
    [HttpPost("generate")]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequestDto dto)
        => ToResponse(await _mediator.Send(
            new GenerateSalariesCommand(dto.Year, dto.Month, dto.EmployeeId)),
            $"حقوق {dto.Year}/{dto.Month:00} برای کارکنان محاسبه شد");

    /// <summary>فهرست حقوق دوره با نام کارکنان</summary>
    [HttpGet]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> GetByPeriod([FromQuery] int year, [FromQuery] int month)
        => ToResponse(await _mediator.Send(new GetSalariesQuery(year, month)));

    /// <summary>پرداخت یک ردیف حقوق</summary>
    [HttpPost("{id:long}/pay")]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> Pay(long id)
        => ToResponse(await _mediator.Send(new PaySalaryCommand(id)), "حقوق پرداخت‌شده علامت خورد");

    /// <summary>داده کامل فیش حقوقی برای چاپ</summary>
    [HttpGet("{id:long}/payslip")]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> GetPayslip(long id)
        => ToResponse(await _mediator.Send(new GetPayslipQuery(id)));

    /// <summary>ورودی تولید دوره</summary>
    public sealed record GenerateRequestDto(int Year, int Month, long? EmployeeId = null);
}
