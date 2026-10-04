using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;

/// <summary>
/// فرمان تغییر وضعیت نوبت — تأیید، لغو، حضور، تکمیل
/// </summary>
/// <param name="Id">شناسه نوبت</param>
/// <param name="NewStatus">وضعیت جدید</param>
/// <param name="Reason">دلیل لغو — الزامی هنگام لغو</param>
public record ChangeAppointmentStatusCommand(long Id, AppointmentStatus NewStatus, string? Reason = null)
    : IRequest<Result>;

/// <summary>اعتبارسنج تغییر وضعیت</summary>
public class ChangeAppointmentStatusCommandValidator : AbstractValidator<ChangeAppointmentStatusCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public ChangeAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("شناسه نوبت نامعتبر است");
        RuleFor(x => x.Reason).NotEmpty()
            .When(x => x.NewStatus == AppointmentStatus.Cancelled)
            .WithMessage("دلیل لغو نوبت الزامی است");
    }
}

/// <summary>گرداننده تغییر وضعیت نوبت</summary>
public class ChangeAppointmentStatusCommandHandler : IRequestHandler<ChangeAppointmentStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ChangeAppointmentStatusCommandHandler> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ChangeAppointmentStatusCommandHandler(IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<ChangeAppointmentStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>اجرای تغییر وضعیت</summary>
    public async Task<Result> Handle(ChangeAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Repository<Appointment>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (appointment is null)
            return Result.Failure(Error.NotFound("نوبت", request.Id));

        if (appointment.Status == AppointmentStatus.Completed)
            return Result.Failure(new Error("ALREADY_COMPLETED", "نوبت تکمیل‌شده قابل تغییر نیست"));

        var patient = await _unitOfWork.Repository<Patient>()
            .GetByIdAsync(appointment.PatientId, cancellationToken);

        if (request.NewStatus == AppointmentStatus.Cancelled)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Reason;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancelledBy = _currentUser.UserId;

            if (patient is not null)
                appointment.AddDomainEvent(new AppointmentCancelledEvent(
                    appointment.Id, patient.Id, request.Reason ?? string.Empty));
        }
        else
        {
            appointment.Status = request.NewStatus;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("وضعیت نوبت {Id} به {Status} تغییر یافت",
            request.Id, request.NewStatus);
        return Result.Success();
    }
}

