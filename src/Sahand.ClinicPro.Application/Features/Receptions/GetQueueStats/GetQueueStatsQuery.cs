using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.GetQueueStats;

/// <summary>آمار روز صفحه پذیرش — چهار کارت بالای فرم ۱۰</summary>
public record GetQueueStatsQuery : IRequest<Result<QueueStatsDto>>;

/// <summary>مدل آمار صف پذیرش</summary>
public record QueueStatsDto(
    int WaitingCount,
    int ReceivedCount,
    int InQueue,
    decimal TodayRevenue);

/// <summary>گرداننده آمار صف</summary>
public class GetQueueStatsQueryHandler : IRequestHandler<GetQueueStatsQuery, Result<QueueStatsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetQueueStatsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — پذیرش‌های امروز و پرداخت‌های امروز</summary>
    public async Task<Result<QueueStatsDto>> Handle(GetQueueStatsQuery request,
        CancellationToken cancellationToken)
    {
        // ═══ چرخهٔ ۷۴ — مرز روز بر پایهٔ UTC همانند صف ویزیت (GetVisitQueue) ═══
        // پیش‌تر DateTime.Today (زمان محلی سرور) بود و در سرورهای با منطقهٔ
        // زمانی متفاوت، پذیرش تازه در مرز روز در آمار گم می‌شد؛ ثبت پذیرش با
        // DateTime.UtcNow انجام می‌شود پس مرز آمار هم همان است.
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var receptions = _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow);

        // ═══ چرخهٔ ۷۴ — «در صف» = پذیرش‌شده + درصف واقعی ═══
        // پذیرش تازه با وضعیت Admitted ثبت می‌شود و هیچ مسیری InQueue نمی‌گذاشت؛
        // آمار «منتظر» همیشه صفر می‌ماند در حالی که بیمار در صف ویزیت دیده
        // می‌شد (نمایش صف Status != Cancelled است). هم‌تراز‌سازی با همان منطق:
        var waiting = await receptions.CountAsync(
            r => r.Status == ReceptionStatus.Admitted || r.Status == ReceptionStatus.InQueue,
            cancellationToken);
        var received = await receptions.CountAsync(r => r.Status != ReceptionStatus.Cancelled, cancellationToken);
        var inQueue = await receptions.CountAsync(
            r => r.Status == ReceptionStatus.InQueue
                 || r.Status == ReceptionStatus.Admitted
                 || r.Status == ReceptionStatus.InVisit, cancellationToken);

        var todayRevenue = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return Result<QueueStatsDto>.Success(new QueueStatsDto(waiting, received, inQueue, todayRevenue));
    }
}
