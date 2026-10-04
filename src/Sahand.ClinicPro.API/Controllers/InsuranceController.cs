using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Insurance;
using Sahand.ClinicPro.Application.Features.Insurance;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر بیمه — مطالبات و استعلامات
/// مسیرها: /api/v1/insurance
/// </summary>
[ApiController]
[Route("api/v1/insurance")]
[Authorize]
public class InsuranceController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public InsuranceController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست مطالبات بیمه</summary>
    /// <param name="companyId">فیلتر شرکت بیمه</param>
    [HttpGet("claims")]
    [HasPermission("insurance.view")]
    public async Task<IActionResult> GetClaims([FromQuery] long? companyId)
    {
        var result = await _mediator.Send(new GetClaimsQuery(companyId));
        return ToResponse(result);
    }

    /// <summary>ثبت مطالبه دوره‌ای — جمع خودکار اقلام بیمه‌ای دوره</summary>
    [HttpPost("claims")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimDto dto)
    {
        var result = await _mediator.Send(new SubmitClaimCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetClaims", new { }, "مطالبه ثبت و ارسال شد");
        return ToResponse(result);
    }

    /// <summary>ثبت استعلام بیمه — اتصال برخط در گام ۲۴</summary>
    [HttpPost("inquiries")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> RecordInquiry([FromBody] InsuranceInquiryDto dto)
    {
        var result = await _mediator.Send(new RecordInquiryCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetClaims", new { }, "استعلام ثبت شد");
        return ToResponse(result);
    }
}
