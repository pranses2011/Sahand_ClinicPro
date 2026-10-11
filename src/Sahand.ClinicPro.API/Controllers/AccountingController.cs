using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Accounting;
using Sahand.ClinicPro.Application.Features.Accounting;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر حسابداری دوبل — درخت سرفصل و اسناد
/// مسیرها: /api/v1/accounting
/// </summary>
[ApiController]
[Route("api/v1/accounting")]
[Authorize]
public class AccountingController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AccountingController(IMediator mediator) => _mediator = mediator;

    /// <summary>درخت سرفصل‌های چهارسطحی — گروه و کل و معین و تفصیلی</summary>
    [HttpGet("accounts")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetChart()
    {
        var result = await _mediator.Send(new GetChartOfAccountsQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست اسناد حسابداری</summary>
    [HttpGet("entries")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetJournalEntriesQuery(page, pageSize));
        return ToResponse(result);
    }

    /// <summary>ثبت سند دستی — توازن بدهکار و بستانکار الزامی</summary>
    [HttpPost("entries")]
    [HasPermission("accounting.create")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateJournalEntryDto dto)
    {
        var result = await _mediator.Send(new CreateJournalEntryCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetEntries", new { }, "سند حسابداری ثبت شد");
        return ToResponse(result);
    }

    // ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) ═══
    // «در صفحه حسابداری و تب دفتر روزنامه، با دابل کلیک بر روی یک سند،
    //  جزئیات کاملش رو بهمون نشون بده» — اندپوینت جزئیات کامل سند با
    // همهٔ سطرها و عنوان حساب هر سطر؛ دسکتاپ در دابل‌کلیک ردیف دفتر
    // روزنامه دیالوگ فارسی جزئیات را با همین پاسخ پر می‌کند.
    /// <summary>جزئیات کامل یک سند — سربرگ + همهٔ سطرها با عنوان حساب</summary>
    [HttpGet("entries/{entryId:long}")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetEntryDetail(long entryId)
    {
        var result = await _mediator.Send(new GetJournalEntryDetailQuery(entryId));
        return ToResponse(result);
    }

    /// <summary>دفتر کل یک حساب — گردش و مانده تراکمی</summary>
    [HttpGet("ledger")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetLedger([FromQuery] long accountId,
        [FromQuery] string? from = null, [FromQuery] string? to = null)
    {
        var result = await _mediator.Send(new GetGeneralLedgerQuery(accountId, from, to));
        return ToResponse(result);
    }

    /// <summary>تراز آزمایشی — همه حساب‌های تفصیلی</summary>
    [HttpGet("trial-balance")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var result = await _mediator.Send(new GetTrialBalanceQuery(from, to));
        return ToResponse(result);
    }

    /// <summary>صورت سود و زیان ساختاریافته</summary>
    [HttpGet("income-statement")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetIncomeStatement([FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var result = await _mediator.Send(new GetIncomeStatementQuery(from, to));
        return ToResponse(result);
    }

    /// <summary>ثبت حساب جدید — درخت چهارسطحی</summary>
    [HttpPost("accounts")]
    [HasPermission("accounting.create")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _mediator.Send(new CreateAccountCommand(request));
        return ToResponse(result, "حساب ثبت شد");
    }
}
