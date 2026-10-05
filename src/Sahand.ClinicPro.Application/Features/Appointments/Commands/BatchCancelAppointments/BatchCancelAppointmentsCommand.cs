using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.BatchCancelAppointments;

/// <summary>
/// فرمان لغو دسته‌ای نوبت‌ها — فرم ۹
/// </summary>
public record BatchCancelAppointmentsCommand(BatchCancelRequest Request)
    : IRequest<Result<BatchOperationResultDto>>;

/// <summary>
/// گرداننده لغو دسته‌ای
/// </summary>
public class BatchCancelAppointmentsCommandHandler
    : IRequestHandler<BatchCancelAppointmentsCommand, Result<BatchOperationResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public BatchCancelAppointmentsCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<BatchOperationResultDto>> Handle(BatchCancelAppointmentsCommand request,
        CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<Appointment>();
        var appointments = await repo.QueryableTracking
            .Where(a => request.Request.Ids.Contains(a.Id) &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.Completed)
            .ToListAsync(cancellationToken);

        var succeeded = 0;
        foreach (var appointment in appointments)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Request.Reason ?? "لغو دسته‌ای";
            appointment.CancelledAt = DateTime.UtcNow;
            repo.Update(appointment);
            succeeded++;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<BatchOperationResultDto>.Success(
            new BatchOperationResultDto(request.Request.Ids.Count, succeeded, request.Request.Ids.Count - succeeded));
    }
}
