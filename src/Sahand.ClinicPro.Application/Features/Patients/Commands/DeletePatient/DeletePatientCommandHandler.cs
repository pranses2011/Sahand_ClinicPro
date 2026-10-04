using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;

/// <summary>
/// گرداننده حذف نرم بیمار — توسط SoftDeleteInterceptor به ویرایش تبدیل می‌شود
/// </summary>
public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeletePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<DeletePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان حذف نرم</summary>
    public async Task<Result> Handle(DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Patient>();
        var patient = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure(Error.NotFound("بیمار", request.Id));

        repo.Remove(patient); // حذف فیزیکی → اینترسپتور → IsDeleted=true
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("بیمار {PatientId} حذف نرم شد", request.Id);
        return Result.Success();
    }
}
