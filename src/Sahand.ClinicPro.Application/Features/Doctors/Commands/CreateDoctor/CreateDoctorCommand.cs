using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctor;

/// <summary>
/// فرمان ثبت پزشک جدید
/// </summary>
/// <param name="Dto">داده‌های پزشک</param>
public record CreateDoctorCommand(SaveDoctorDto Dto) : IRequest<Result<long>>;

/// <summary>
/// اعتبارسنج ثبت پزشک — با پیام فارسی
/// </summary>
public class CreateDoctorCommandValidator : AbstractValidator<CreateDoctorCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateDoctorCommandValidator()
    {
        RuleFor(x => x.Dto.ConsultationFee)
            .GreaterThanOrEqualTo(0).WithMessage("حق ویزیت نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.VisitDuration)
            .InclusiveBetween(5, 120).WithMessage("مدت ویزیت باید بین ۵ تا ۱۲۰ دقیقه باشد");
        RuleFor(x => x.Dto.DoctorPercentage)
            .InclusiveBetween(0, 100).WithMessage("درصد سهم پزشک باید بین صفر تا صد باشد");
    }
}

/// <summary>
/// گرداننده ثبت پزشک
/// </summary>
public class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateDoctorCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای فرمان ثبت</summary>
    public async Task<Result<long>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var doctor = new Doctor
        {
            EmployeeId = dto.EmployeeId,
            MedicalLicenseNo = dto.MedicalLicenseNo,
            NezamPezeshkiCode = dto.NezamPezeshkiCode,
            SpecialtyId = dto.SpecialtyId,
            SubSpecialtyId = dto.SubSpecialtyId,
            AcademicRank = dto.AcademicRank,
            Biography = dto.Biography,
            ConsultationFee = dto.ConsultationFee,
            VisitDuration = dto.VisitDuration,
            MaxPatientsPerShift = dto.MaxPatientsPerShift,
            DoctorPercentage = dto.DoctorPercentage,
            OnlineBookingEnabled = dto.OnlineBookingEnabled,
            AcceptsNewPatients = dto.AcceptsNewPatients,
            SignatureImage = Convert.FromBase64String(dto.SignatureImageBase64 ?? string.Empty) is { Length: > 0 } sig ? sig : null,
            StampImage = Convert.FromBase64String(dto.StampImageBase64 ?? string.Empty) is { Length: > 0 } stamp ? stamp : null,
            IsActive = dto.IsActive
        };

        await _unitOfWork.Repository<Doctor>().AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(doctor.Id);
    }
}
