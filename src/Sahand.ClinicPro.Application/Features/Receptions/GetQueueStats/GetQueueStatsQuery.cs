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
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var receptions = _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.ReceptionDate >= today && r.ReceptionDate < tomorrow);

        var waiting = await receptions.CountAsync(r => r.Status == ReceptionStatus.InQueue, cancellationToken);
        var received = await receptions.CountAsync(r => r.Status != ReceptionStatus.Cancelled, cancellationToken);
        var inQueue = await receptions.CountAsync(
            r => r.Status == ReceptionStatus.InQueue || r.Status == ReceptionStatus.InVisit, cancellationToken);

        var todayRevenue = await _unitOfWork.Repository<Payment>().QueryableNoTracking
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return Result<QueueStatsDto>.Success(new QueueStatsDto(waiting, received, inQueue, todayRevenue));
    }
}
