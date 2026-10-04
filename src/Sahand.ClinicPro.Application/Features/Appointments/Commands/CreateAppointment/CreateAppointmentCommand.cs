using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.CreateAppointment;

/// <summary>
/// فرمان رزرو نوبت — کنترل ظرفیت پزشک و بررسی غیبت برنامه‌ریزی‌شده
/// </summary>
/// <param name="Dto">داده نوبت جدید</param>
public record CreateAppointmentCommand(CreateAppointmentDto Dto) : IRequest<Result<AppointmentDetailDto>>;

/// <summary>اعتبارسنج رزرو نوبت — با پیام فارسی</summary>
public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
        RuleFor(x => x.Dto.DateShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi).WithMessage("تاریخ نوبت شمسی نامعتبر است");
        RuleFor(x => x.Dto.Time)
            .Matches("^([01]?\\d|2[0-3]):[0-5]\\d$").WithMessage("ساعت نوبت نامعتبر است — الگوی 09:30");
    }
}

/// <summary>گرداننده رزرو نوبت — ظرفیت و غیبت پزشک و تداخل زمان بیمار بررسی می‌شود</summary>
public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateAppointmentCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان رزرو</summary>
    public async Task<Result<AppointmentDetailDto>> Handle(CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<AppointmentDetailDto>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive)
            return Result<AppointmentDetailDto>.Failure(Error.NotFound("پزشک", dto.DoctorId));

        var gregorianDate = PersianDateHelper.ToGregorian(dto.DateShamsi);
        if (gregorianDate is null || gregorianDate.Value.Date < DateTime.UtcNow.Date)
            return Result<AppointmentDetailDto>.Failure(
                new Error("INVALID_DATE", "تاریخ نوبت نمی‌تواند در گذشته باشد"));

        var date = DateOnly.FromDateTime(gregorianDate.Value);
        if (!TimeOnly.TryParse(dto.Time, out var time))
            return Result<AppointmentDetailDto>.Failure(
                new Error("INVALID_TIME", "ساعت نوبت نامعتبر است"));

        // بررسی غیبت برنامه‌ریزی‌شده پزشک در آن روز
        var doctorAbsent = await _unitOfWork.Repository<DoctorAbsence>()
            .AnyAsync(a => a.DoctorId == dto.DoctorId && a.AbsenceDate == date, cancellationToken);
        if (doctorAbsent)
            return Result<AppointmentDetailDto>.Failure(
                new Error("DOCTOR_ABSENT", "پزشک در این تاریخ مرخصی دارد"));

        // کنترل ظرفیت — سقف بیمار در هر شیفت
        var appointmentRepo = (IAppointmentRepository)_unitOfWork.Repository<Appointment>();
        var activeCount = await appointmentRepo.CountActiveByDoctorAndDateAsync(
            dto.DoctorId, date, cancellationToken);
        if (doctor.MaxPatientsPerShift.HasValue && activeCount >= doctor.MaxPatientsPerShift.Value)
            return Result<AppointmentDetailDto>.Failure(
                new Error("CAPACITY_FULL", "ظرفیت نوبت این پزشک در این تاریخ تکمیل است"));

        // تداخل زمانی بیمار — نوبت دیگر در همان ساعت
        var patientBusy = await _unitOfWork.Repository<Appointment>().AnyAsync(
            a => a.PatientId == dto.PatientId
                 && a.AppointmentDate == date
                 && a.AppointmentTime == time
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.NoShow,
            cancellationToken);
        if (patientBusy)
            return Result<AppointmentDetailDto>.Failure(
                new Error("PATIENT_CONFLICT", "بیمار در این ساعت نوبت دیگری دارد"));

        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            SpecialtyId = dto.SpecialtyId ?? doctor.SpecialtyId,
            AppointmentDate = date,
            AppointmentDateShamsi = dto.DateShamsi,
            AppointmentTime = time,
            Duration = doctor.VisitDuration,
            Status = AppointmentStatus.Booked,
            AppointmentType = dto.Type,
            Source = dto.Source,
            IsFirstVisit = dto.IsFirstVisit,
            Priority = Priority.Normal,
            EstimatedFee = doctor.ConsultationFee,
            QueueNumber = activeCount + 1,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Appointment>().AddAsync(appointment, cancellationToken);

        // رویداد دامنه — پیامک تأیید نوبت
        appointment.AddDomainEvent(new AppointmentBookedEvent(
            appointment.Id, dto.PatientId, patient.Mobile));

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("نوبت {Code} برای بیمار {PatientId} با پزشک {DoctorId} ثبت شد",
            appointment.AppointmentCode, dto.PatientId, dto.DoctorId);

        return Result<AppointmentDetailDto>.Success(new AppointmentDetailDto(
            appointment.Id, appointment.AppointmentCode, dto.PatientId,
            $"{patient.FirstName} {patient.LastName}", dto.DoctorId,
            "پزشک", null, dto.DateShamsi, dto.Time, appointment.Duration,
            appointment.Status, appointment.AppointmentType, appointment.QueueNumber,
            appointment.EstimatedFee, appointment.IsFirstVisit, appointment.Notes));
    }
}
