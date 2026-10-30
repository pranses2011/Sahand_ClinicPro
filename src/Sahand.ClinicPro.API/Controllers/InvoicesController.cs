using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Application.Features.Billing.Commands.CreateInvoice;
using Sahand.ClinicPro.Application.Features.Billing.Queries;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر فاکتور — صدور صورت‌حساب با سند حسابداری خودکار
/// مسیرها: /api/v1/invoices
/// </summary>
[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست فاکتورها با فیلتر بیمار و وضعیت پرداخت</summary>
    [HttpGet]
    [HasPermission("invoices.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] long? patientId = null, [FromQuery] PaymentStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(
            new PagedRequest { PageNumber = page, PageSize = pageSize, Search = search },
            patientId, status));
        return ToPagedResponse(result);
    }

    /// <summary>جزئیات کامل فاکتور با اقلام و سهم‌ها</summary>
    /// <param name="id">شناسه فاکتور</param>
    [HttpGet("{id:long}")]
    [HasPermission("invoices.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>پیش‌نمایش فقط‌خواندنی با نسخه قابل تطبیق هنگام صدور.</summary>
    [HttpPost("preview")]
    [HasPermission("invoices.create")]
    public async Task<IActionResult> Preview([FromBody] CreateInvoiceDto dto)
        => ToResponse(await _mediator.Send(new PreviewInvoiceQuery(dto)));

    /// <summary>صدور یا بازپخش همان درخواست با شناسه الزامی.</summary>
    [HttpPost("idempotent")]
    [HasPermission("invoices.create")]
    public async Task<IActionResult> CreateIdempotent([FromBody] CreateInvoiceDto dto)
    {
        if (!dto.RequestId.HasValue || dto.RequestId == Guid.Empty)
            return BadRequest(ApiResponse<object>.Failure("شناسه پایدار درخواست فاکتور الزامی است.", 400));
        return await Create(dto);
    }

    /// <summary>صدور فاکتور جدید — سهم بیمه محاسبه و سند حسابداری تولید می‌شود</summary>
    [HttpPost]
    [HasPermission("invoices.create")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        try
        {
            var result = await _mediator.Send(new CreateInvoiceCommand(dto));
            if (result.IsFailure && result.Error.Code is "BILLING.QUOTE_CHANGED" or "BILLING.REQUEST_CONFLICT")
                return Conflict(ApiResponse<object>.Failure(result.Error.Message, 409));
            if (result.IsSuccess)
                return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "فاکتور صادر شد");
            return ToResponse(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(ApiResponse<object>.Failure("پذیرش همزمان تغییر کرد؛ پیش از تلاش مجدد فاکتورهای آن را بررسی کنید.", 409));
        }
        catch (DbUpdateException ex) when (RequestIdConflict.IsRequestIdViolation(ex, RequestIdConflict.InvoiceIndex))
        {
            return Conflict(ApiResponse<object>.Failure("فاکتوری با همین شناسه هم‌زمان ثبت شده است؛ وضعیت را بررسی و فقط با همان شناسه دوباره تلاش کنید.", 409));
        }
    }
}
