using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;

/// <summary>
/// فرمان پذیرش بیمار — از نوبت یا مراجعه حضوری بدون نوبت
/// </summary>
/// <param name="Dto">داده پذیرش</param>
/// <param name="ReceptionistId">شناسه منشی پذیرش‌کننده — از کاربر فعلی پر می‌شود</param>
public record CreateReceptionCommand(CreateReceptionDto Dto, long? ReceptionistId = null)
    : IRequest<Result<long>>;

/// <summary>اعتبارسنج پذیرش</summary>
public class CreateReceptionCommandValidator : AbstractValidator<CreateReceptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateReceptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
    }
}

/// <summary>گرداننده ثبت پذیرش — ورود بیمار به جریان ویزیت روز</summary>
public class CreateReceptionCommandHandler : IRequestHandler<CreateReceptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReceptionCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateReceptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateReceptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان پذیرش</summary>
    public async Task<Result<long>> Handle(CreateReceptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<long>.Failure(Error.NotFound("پزشک", dto.DoctorId));

        var now = DateTime.UtcNow;
        var reception = new Reception
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            InsuranceCompanyId = dto.InsuranceCompanyId ?? patient.InsuranceCompanyId,
            ReferralCode = dto.ReferralCode,
            ReceptionDate = now,
            ReceptionDateShamsi = PersianDateHelper.ToShamsi(now),
            Status = ReceptionStatus.Admitted,
            ReceptionistId = request.ReceptionistId,
            EnterTime = now,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Reception>().AddAsync(reception, cancellationToken);

        // اگر از نوبت است — وضعیت نوبت به حاضر تغییر می‌کند
        if (dto.AppointmentId.HasValue)
        {
            var appointment = await _unitOfWork.Repository<Appointment>()
                .GetByIdAsync(dto.AppointmentId.Value, cancellationToken);
            if (appointment is not null)
                appointment.Status = AppointmentStatus.Arrived;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("پذیرش {Number} برای بیمار {PatientId} ثبت شد",
            reception.ReceptionNumber, dto.PatientId);

        return Result<long>.Success(reception.Id);
    }
}
