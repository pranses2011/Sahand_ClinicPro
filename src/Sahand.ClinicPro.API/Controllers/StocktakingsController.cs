using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Pharmacy;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر انبارگردانی داروخانه — چرخه شمارش و اعمال مغایرت — گام P2 (چرخه ششم)
/// مسیرها: /api/v1/stocktakings
/// چرخه: شروع (عکس فوری) ← ثبت شمارش اقلام ← تکمیل ← تأیید و اعمال
/// </summary>
[ApiController]
[Route("api/v1/stocktakings")]
[Authorize]
public class StocktakingsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public StocktakingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست جلسات انبارگردانی — جدیدترین ابتدا</summary>
    [HttpGet]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetList()
    {
        var result = await _mediator.Send(new GetStocktakingsQuery());
        return ToResponse(result);
    }

    /// <summary>جزئیات جلسه با تمام اقلام و مغایرت‌ها</summary>
    /// <param name="id">شناسه جلسه</param>
    [HttpGet("{id:long}")]
    [HasPermission("pharmacy.view")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _mediator.Send(new GetStocktakingDetailQuery(id));
        return ToResponse(result);
    }

    /// <summary>شروع جلسه جدید — عکس فوری از کل موجودی + قفل تک‌جلسه‌ای</summary>
    [HttpPost("start")]
    [HasPermission("pharmacy.stocktake")]
    public async Task<IActionResult> Start([FromBody] StartStocktakingRequest request)
    {
        var result = await _mediator.Send(new StartStocktakingCommand(request.Notes));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetDetail", new { id = result.Value }, "جلسه انبارگردانی با عکس فوری موجودی آغاز شد");
        return ToResponse(result);
    }

    /// <summary>ثبت مقدار شمارش یک قلم</summary>
    [HttpPost("{stockTakingId:long}/items/{itemId:long}/count")]
    [HasPermission("pharmacy.stocktake")]
    public async Task<IActionResult> RecordCount(long stockTakingId, long itemId, [FromBody] RecordCountRequest request)
    {
        var result = await _mediator.Send(new RecordCountCommand(stockTakingId, itemId, request.CountedQuantity, request.Notes));
        return ToResponse(result, "شمارش قلم ثبت شد");
    }

    /// <summary>تکمیل جلسه — قفل شمارش‌ها و انتقال به وضعیت در انتظار تأیید</summary>
    /// <param name="stockTakingId">شناسه جلسه</param>
    [HttpPost("{stockTakingId:long}/finalize")]
    [HasPermission("pharmacy.stocktake")]
    public async Task<IActionResult> Finalize(long stockTakingId)
    {
        var result = await _mediator.Send(new FinalizeStocktakingCommand(stockTakingId));
        return ToResponse(result, "جلسه تکمیل شد — آماده تأیید نهایی و اعمال مغایرت");
    }

    /// <summary>تأیید نهایی — اعمال مغایرت به موجودی + تراکنش اصلاح برای هر قلم</summary>
    /// <param name="stockTakingId">شناسه جلسه</param>
    [HttpPost("{stockTakingId:long}/approve")]
    [HasPermission("pharmacy.stocktake")]
    public async Task<IActionResult> Approve(long stockTakingId)
    {
        var result = await _mediator.Send(new ApproveStocktakingCommand(stockTakingId));
        if (result.IsSuccess)
            return Ok(ApiResponse<object>.Success(new
            {
                message = $"مغایرت {result.Value.AdjustedItems} قلم اعمال شد — کسری: {result.Value.TotalShortage}، زیادندی: {result.Value.TotalSurplus}",
                adjustedItems = result.Value.AdjustedItems,
                totalShortage = result.Value.TotalShortage,
                totalSurplus = result.Value.TotalSurplus
            }));
        return ToResponse(result);
    }

    /// <summary>لغو جلسه در حال شمارش — حذف نرم بدون اثر بر موجودی</summary>
    /// <param name="stockTakingId">شناسه جلسه</param>
    [HttpPost("{stockTakingId:long}/cancel")]
    [HasPermission("pharmacy.stocktake")]
    public async Task<IActionResult> Cancel(long stockTakingId)
    {
        var result = await _mediator.Send(new CancelStocktakingCommand(stockTakingId));
        return ToResponse(result, "جلسه انبارگردانی لغو شد");
    }
}

/// <summary>بدنه درخواست شروع انبارگردانی</summary>
public record StartStocktakingRequest(string? Notes);

/// <summary>بدنه درخواست ثبت شمارش قلم</summary>
public record RecordCountRequest(decimal CountedQuantity, string? Notes);
