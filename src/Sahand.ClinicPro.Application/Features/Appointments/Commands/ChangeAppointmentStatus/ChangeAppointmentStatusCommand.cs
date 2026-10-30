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
    private readonly IWaitlistNotifier _waitlistNotifier;
    private readonly IDateTimeService _clock;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ChangeAppointmentStatusCommandHandler(IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<ChangeAppointmentStatusCommandHandler> logger,
        IWaitlistNotifier waitlistNotifier,
        IDateTimeService clock)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
        _waitlistNotifier = waitlistNotifier;
        _clock = clock;
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

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            // بازپخش لغو نباید جای خالی یکسان را دوباره پیشنهاد کند یا
            // دلیل اصلی را رونویسی کند. بازگرداندن مستقیم نوبت لغوشده نیز
            // می‌تواند با جای خالی پیشنهادشده تداخل کند؛ رزرو تازه لازم است.
            return request.NewStatus == AppointmentStatus.Cancelled
                ? Result.Success()
                : Result.Failure(new Error("ALREADY_CANCELLED", "نوبت لغوشده قابل بازگردانی نیست؛ نوبت جدید ثبت کنید"));
        }

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

            // ═══ چرخه ۱۳ (درخواست ۳-۴): پیشنهاد خودکار جای خالی به لیست انتظار ═══
            // تاریخ و ساعت نوبت به وقت محلی درمانگاه‌اند؛ مقایسهٔ صرف تاریخ
            // با UTC می‌توانست نوبت گذشتهٔ امروز را همچنان پیشنهاد دهد.
            if (appointment.DoctorId > 0 && appointment.AppointmentDate.ToDateTime(appointment.AppointmentTime) > _clock.NowIran)
            {
                // شکست صف نباید بلعیده شود: پیشنهاد و لغو در تراکنش مشترک‌اند
                // و در خطا هر دو باید عقب برگردند، نه اینکه لاگ موفقیت بدهیم.
                await _waitlistNotifier.NotifyFreedSlotAsync(
                    appointment.Id, appointment.DoctorId,
                    appointment.AppointmentDate, appointment.AppointmentTime,
                    cancellationToken);
            }
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

