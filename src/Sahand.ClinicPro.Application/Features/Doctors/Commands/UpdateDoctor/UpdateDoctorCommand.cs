using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.UpdateDoctor;

/// <summary>
/// فرمان ویرایش پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
/// <param name="Dto">داده‌های ویرایش</param>
public record UpdateDoctorCommand(long Id, SaveDoctorDto Dto) : IRequest<Result>;

/// <summary>
/// اعتبارسنج ویرایش پزشک
/// </summary>
public class UpdateDoctorCommandValidator : AbstractValidator<UpdateDoctorCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public UpdateDoctorCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("شناسه پزشک نامعتبر است");
        RuleFor(x => x.Dto.VisitDuration)
            .InclusiveBetween(5, 120).WithMessage("مدت ویزیت باید بین ۵ تا ۱۲۰ دقیقه باشد");
    }
}

/// <summary>
/// گرداننده ویرایش پزشک
/// </summary>
public class UpdateDoctorCommandHandler : IRequestHandler<UpdateDoctorCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان ویرایش</summary>
    public async Task<Result> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Doctor>();
        var doctor = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (doctor is null)
            return Result.Failure(Error.NotFound("پزشک", request.Id));

        var dto = request.Dto;
        doctor.EmployeeId = dto.EmployeeId;
        doctor.MedicalLicenseNo = dto.MedicalLicenseNo;
        doctor.NezamPezeshkiCode = dto.NezamPezeshkiCode;
        doctor.SpecialtyId = dto.SpecialtyId;
        doctor.SubSpecialtyId = dto.SubSpecialtyId;
        doctor.AcademicRank = dto.AcademicRank;
        doctor.Biography = dto.Biography;
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.VisitDuration = dto.VisitDuration;
        doctor.MaxPatientsPerShift = dto.MaxPatientsPerShift;
        doctor.DoctorPercentage = dto.DoctorPercentage;
        doctor.OnlineBookingEnabled = dto.OnlineBookingEnabled;
        doctor.AcceptsNewPatients = dto.AcceptsNewPatients;

        repo.Update(doctor);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
