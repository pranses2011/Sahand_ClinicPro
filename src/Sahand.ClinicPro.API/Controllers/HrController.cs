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

    /// <summary>
    /// جزئیات یک پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// بارگذاری فرم ویرایش با تمام فیلدهای ثبت
    /// </summary>
    [HttpGet("employees/{id:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetEmployeeById(long id)
    {
        var result = await _mediator.Send(new GetEmployeeByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>
    /// ویرایش کامل اطلاعات پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «در لیست پرسنل امکان ویرایش پرسنل وجود ندارد»
    /// </summary>
    [HttpPut("employees/{id:long}")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> UpdateEmployee(long id, [FromBody] CreateEmployeeDto dto)
    {
        var result = await _mediator.Send(new UpdateEmployeeCommand(id, dto));
        return ToResponse(result);
    }

    /// <summary>
    /// حذف نرم پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «کلیدهای عملیاتی پرسنل ناقص‌اند» — رکورد پنهان می‌شود و سابقه‌ها سالم می‌مانند
    /// </summary>
    [HttpDelete("employees/{id:long}")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> DeleteEmployee(long id)
    {
        var result = await _mediator.Send(new DeleteEmployeeCommand(id));
        return ToResponse(result);
    }

    /// <summary>
    /// ویرایش اطلاعات حقوقی پرسنل — حقوق پایه + درصد حقوق از سود خدمات انجامی
    /// (چرخه نهم؛ درصد فقط از سود خدمات خود پرسنل اعمال می‌شود نه کل دریافتی)
    /// </summary>
    [HttpPut("employees/{id:long}/salary")]
    [HasPermission("patients.create")]
    public async Task<IActionResult> UpdateEmployeeSalary(long id,
        [FromBody] UpdateEmployeeSalaryBody body)
    {
        var result = await _mediator.Send(new UpdateEmployeeSalaryCommand(
            id, body.BaseSalary, body.CommissionPercent));
        return ToResponse(result);
    }

    /// <summary>بدنه ویرایش اطلاعات حقوقی پرسنل</summary>
    public record UpdateEmployeeSalaryBody(decimal BaseSalary, decimal CommissionPercent);

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
