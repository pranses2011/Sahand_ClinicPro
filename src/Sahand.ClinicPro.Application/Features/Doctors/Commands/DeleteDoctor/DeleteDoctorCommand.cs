using MediatR;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctor;

/// <summary>
/// فرمان حذف نرم پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
public record DeleteDoctorCommand(long Id) : IRequest<Result>;

/// <summary>
/// گرداننده حذف نرم پزشک
/// </summary>
public class DeleteDoctorCommandHandler : IRequestHandler<DeleteDoctorCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeleteDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان حذف نرم</summary>
    public async Task<Result> Handle(DeleteDoctorCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Doctor>();
        var doctor = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (doctor is null)
            return Result.Failure(Error.NotFound("پزشک", request.Id));

        repo.Remove(doctor);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
