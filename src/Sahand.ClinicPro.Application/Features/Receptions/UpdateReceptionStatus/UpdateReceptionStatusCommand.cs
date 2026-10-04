using MediatR;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateReceptionStatusCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
            reception.ExitTime = DateTime.UtcNow;
            if (reception.EnterTime.HasValue)
            {
                reception.WaitingTime = (int)(reception.ExitTime.Value - reception.EnterTime.Value).TotalMinutes;
            }
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
