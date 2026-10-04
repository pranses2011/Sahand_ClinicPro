using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// گرداننده ویرایش بیمار — بارگذاری و اعمال تغییرات
/// </summary>
public class UpdatePatientCommandHandler : IRequestHandler<UpdatePatientCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePatientCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdatePatientCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdatePatientCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ویرایش</summary>
    public async Task<Result> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Patient>();
        var patient = await repo.GetByIdAsync(request.Id, cancellationToken);

        if (patient is null)
            return Result.Failure(Error.NotFound("بیمار", request.Id));

        var dto = request.Dto;
        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.FatherName = dto.FatherName;
        patient.Mobile = dto.Mobile;
        patient.Phone = dto.Phone;
        patient.Email = dto.Email;
        patient.Address = dto.Address;
        patient.City = dto.City;
        patient.Province = dto.Province;
        patient.InsuranceCompanyId = dto.InsuranceCompanyId;
        patient.InsuranceNumber = dto.InsuranceNumber;
        patient.SupplementaryInsuranceId = dto.SupplementaryInsuranceId;

        repo.Update(patient);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("اطلاعات بیمار {PatientId} ویرایش شد", request.Id);
        return Result.Success();
    }
}
