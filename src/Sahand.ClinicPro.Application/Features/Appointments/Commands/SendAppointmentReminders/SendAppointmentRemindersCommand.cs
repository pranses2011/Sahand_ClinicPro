using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.SendAppointmentReminders;

/// <summary>
/// فرمان ارسال پیامک یادآوری نوبت — پیش‌فرض نوبت‌های فردا (فرم ۹)
/// </summary>
public record SendAppointmentRemindersCommand(string? DateShamsi = null)
    : IRequest<Result<BatchOperationResultDto>>;

/// <summary>
/// گرداننده یادآوری پیامکی
/// </summary>
public class SendAppointmentRemindersCommandHandler
    : IRequestHandler<SendAppointmentRemindersCommand, Result<BatchOperationResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmsService _sms;

    /// <summary>سازنده</summary>
    public SendAppointmentRemindersCommandHandler(IUnitOfWork unitOfWork, ISmsService sms)
    {
        _unitOfWork = unitOfWork;
        _sms = sms;
    }

    /// <summary>اجرا — ارسال صف‌شده برای همه نوبت‌های فعال روز هدف</summary>
    public async Task<Result<BatchOperationResultDto>> Handle(SendAppointmentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var target = PersianDateHelper.ToGregorian(request.DateShamsi) ??
                     DateTime.Today.AddDays(1); // پیش‌فرض: فردا
        var date = DateOnly.FromDateTime(target);

        var appointments = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow &&
                        a.Status != AppointmentStatus.Completed &&
                        !a.ReminderSent)
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Employee)
            .ToListAsync(cancellationToken);

        var succeeded = 0;
        var repo = _unitOfWork.Repository<Appointment>();
        foreach (var a in appointments)
        {
            var patientName = a.Patient.FirstName;
            var doctorName = a.Doctor?.Employee is null ? "" :
                $"{a.Doctor.Employee.FirstName} {a.Doctor.Employee.LastName}";
            var message = $"{patientName} عزیز، نوبت شما برای {request.DateShamsi ?? PersianDateHelper.ToShamsi(target)} " +
                          $"ساعت {a.AppointmentTime:HH:mm} با {doctorName} ثبت است. لطفاً ۱۰ دقیقه زودتر حاضر شوید.";
            await _sms.EnqueueAsync(a.Patient.Mobile, "APPOINTMENT_REMINDER", new Dictionary<string, string>
            {
                ["name"] = patientName,
                ["date"] = a.AppointmentDateShamsi ?? string.Empty,
                ["time"] = a.AppointmentTime.ToString("HH:mm"),
                ["doctor"] = doctorName
            }, null, cancellationToken);

            var tracked = await repo.GetByIdAsync(a.Id, cancellationToken);
            if (tracked is not null)
            {
                tracked.ReminderSent = true;
                repo.Update(tracked);
            }
            succeeded++;
        }

        if (succeeded > 0)
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<BatchOperationResultDto>.Success(
            new BatchOperationResultDto(appointments.Count, succeeded, appointments.Count - succeeded));
    }
}
