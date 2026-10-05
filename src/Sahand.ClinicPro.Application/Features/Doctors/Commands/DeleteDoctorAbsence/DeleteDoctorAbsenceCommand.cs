using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctorAbsence;

/// <summary>
/// فرمان حذف غیبت پزشک
/// </summary>
public record DeleteDoctorAbsenceCommand(long AbsenceId) : IRequest<Result>;

/// <summary>
/// گرداننده حذف غیبت
/// </summary>
public class DeleteDoctorAbsenceCommandHandler : IRequestHandler<DeleteDoctorAbsenceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeleteDoctorAbsenceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(DeleteDoctorAbsenceCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<DoctorAbsence>();
        var absence = await repo.QueryableTracking
            .FirstOrDefaultAsync(a => a.Id == request.AbsenceId, cancellationToken);
        if (absence is null)
            return Result.Failure(new Error("ABSENCE.NOTFOUND", "غیبت یافت نشد"));

        repo.Remove(absence);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
