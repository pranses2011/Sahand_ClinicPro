using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پیامک — ارسال فوری و مدیریت صف و قالب‌ها
/// مسیرها: /api/v1/sms
/// </summary>
[ApiController]
[Route("api/v1/sms")]
[Authorize]
public class SmsController : ApiBaseController
{
    private readonly ISmsService _sms;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق سرویس پیامک و واحد کار</summary>
    public SmsController(ISmsService sms, IUnitOfWork unitOfWork)
    {
        _sms = sms;
        _unitOfWork = unitOfWork;
    }

    /// <summary>ارسال پیامک فوری</summary>
    [HttpPost("send")]
    [HasPermission("sms.create")]
    public async Task<IActionResult> Send([FromBody] SendRequest request)
    {
        var result = await _sms.SendAsync(request.Recipient, request.Message);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage("پیامک ارسال شد"));
    }

    /// <summary>ارسال با قالب — کد قالب و متغیرها</summary>
    [HttpPost("send-template")]
    [HasPermission("sms.create")]
    public async Task<IActionResult> SendTemplate([FromBody] SendTemplateRequest request)
    {
        var result = await _sms.SendTemplateAsync(
            request.Recipient, request.TemplateCode, request.Variables);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Failure(result.Error ?? "ارسال ناموفق بود"));
        return Ok(ApiResponse<object>.SuccessMessage("پیامک قالب‌دار ارسال شد"));
    }

    /// <summary>فهرست قالب‌های فعال</summary>
    [HttpGet("templates")]
    [HasPermission("sms.view")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _unitOfWork.Repository<SmsTemplate>()
            .QueryableNoTracking
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Code, t.Content })
            .ToListAsync();
        return Ok(ApiResponse<object>.Success(templates));
    }

    /// <summary>درخواست ارسال پیامک</summary>
    public record SendRequest(string Recipient, string Message);

    /// <summary>درخواست ارسال قالبی</summary>
    public record SendTemplateRequest(string Recipient, string TemplateCode,
        Dictionary<string, string> Variables);
}
