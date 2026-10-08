using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Settings;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر اداری — تنظیمات و لاگ عملیات
/// مسیرها: /api/v1/settings و /api/v1/auditlogs
/// چرخه بیستم (گزارش چهاردهم مالک): پیش‌تر قالب مسیر اکشن‌ها "api/v1/settings"
/// بود که در ASP.NET Core به مسیر پایهٔ کنترلر (api/v1/[controller]) «الحاق»
/// می‌شد و مسیر واقعی api/v1/Admin/api/v1/settings می‌شد — نتیجه: 404 برای
/// ذخیرهٔ تنظیمات (خطای تم/پشتیبان در کلاینت). پیشوند ~/ مسیر مطلق می‌سازد و
/// مسیر پایه را بازنویسی می‌کند.
/// </summary>
[ApiController]
[Authorize]
public class AdminController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست تنظیمات سیستم — مسیر مطلق با ~/ برای بازنویسی مسیر پایهٔ کنترلر</summary>
    [HttpGet("~/api/v1/settings")]
    [HasPermission("settings.view")]
    public async Task<IActionResult> GetSettings([FromQuery] string? category)
    {
        var result = await _mediator.Send(new GetSettingsQuery(category));
        return ToResponse(result);
    }

    /// <summary>ذخیره تنظیم — ایجاد یا ویرایش — مسیر مطلق با ~/</summary>
    [HttpPut("~/api/v1/settings")]
    [HasPermission("settings.edit")]
    public async Task<IActionResult> SaveSetting([FromBody] SaveSettingCommand command)
    {
        var result = await _mediator.Send(command);
        return ToResponse(result, "تنظیم ذخیره شد");
    }

    /// <summary>لاگ عملیات با صفحه‌بندی و فیلتر — مسیر مطلق با ~/</summary>
    [HttpGet("~/api/v1/auditlogs")]
    [HasPermission("auditlogs.view")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, [FromQuery] long? userId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(page, pageSize, userId, entityName, action));
        return ToResponse(result);
    }
}
