using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.UpdateReceptionStatus;

/// <summary>
/// فرمان تغییر وضعیت پذیرش — در صف، در ویزیت، ویزیت‌شده، ترخیص، لغو
/// </summary>
/// <param name="Id">شناسه پذیرش</param>
/// <param name="Status">وضعیت جدید</param>
public record UpdateReceptionStatusCommand(long Id, ReceptionStatus Status) : IRequest<Result>;

/// <summary>گرداننده تغییر وضعیت پذیرش — همراه محاسبه زمان انتظار هنگام ترخیص</summary>
public class UpdateReceptionStatusCommandHandler
    : IRequestHandler<UpdateReceptionStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRealtimeNotifier _realtime;

    /// <summary>سازنده با تزریق واحد کار و اتصال بلادرنگ صف (گام R1)</summary>
    public UpdateReceptionStatusCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier realtime)
    {
        _unitOfWork = unitOfWork;
        _realtime = realtime;
    }

    /// <summary>اجرای تغییر وضعیت</summary>
    public async Task<Result> Handle(UpdateReceptionStatusCommand request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Reception>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (reception is null)
            return Result.Failure(Error.NotFound("پذیرش", request.Id));

        reception.Status = request.Status;

        // محاسبه زمان انتظار و ساعت خروج هنگام ترخیص
        if (request.Status == ReceptionStatus.Discharged)
        {
            reception.ExitTime = PersianDateHelper.IranNow();
            if (reception.EnterTime.HasValue)
            {
                reception.WaitingTime = (int)(reception.ExitTime.Value - reception.EnterTime.Value).TotalMinutes;
            }
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // پخش بلادرنگ — تغییر وضعیت (ترخیص/لغو و...) برای صف زنده شعبه (گام R1)
        await _realtime.QueueChangedAsync(reason: "status", cancellationToken: cancellationToken);

        return Result.Success();
    }
}
