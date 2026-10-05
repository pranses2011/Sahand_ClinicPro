using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.SaveDoctorSchedule;

/// <summary>
/// فرمان ذخیره برنامه هفتگی پزشک — جایگزینی کامل ردیف‌های فعلی
/// </summary>
public record SaveDoctorScheduleCommand(long DoctorId, SaveScheduleRequest Request) : IRequest<Result>;

/// <summary>
/// اعتبارسنج برنامه هفتگی — حداقل یک ردیف فعال برای ذخیره
/// </summary>
public class SaveDoctorScheduleCommandValidator : AbstractValidator<SaveDoctorScheduleCommand>
{
    /// <summary>قواعد</summary>
    public SaveDoctorScheduleCommandValidator()
    {
        RuleFor(x => x.DoctorId).GreaterThan(0).WithMessage("شناسه پزشک نامعتبر است");
        RuleFor(x => x.Request.Rows).NotNull().WithMessage("ردیف‌های برنامه الزامی است");
    }
}

/// <summary>
/// گرداننده ذخیره برنامه هفتگی
/// </summary>
public class SaveDoctorScheduleCommandHandler : IRequestHandler<SaveDoctorScheduleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SaveDoctorScheduleCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — حذف نرم ردیف‌های قبلی و افزودن ردیف‌های فعال</summary>
    public async Task<Result> Handle(SaveDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<DoctorSchedule>();
        var existing = await repo.QueryableTracking
            .Where(s => s.DoctorId == request.DoctorId)
            .ToListAsync(cancellationToken);
        foreach (var row in existing)
            repo.Remove(row);

        foreach (var row in request.Request.Rows.Where(r => r.IsEnabled))
        {
            if (!TimeOnly.TryParse(row.StartTime, out var start) ||
                !TimeOnly.TryParse(row.EndTime, out var end))
                return Result.Failure(new Error("SCHEDULE.TIME", "قالب ساعت نامعتبر است — نمونه صحیح: 08:30"));

            if (end <= start)
                return Result.Failure(new Error("SCHEDULE.RANGE", "ساعت پایان باید بعد از ساعت شروع باشد"));

            TimeOnly? breakStart = null, breakEnd = null;
            if (!string.IsNullOrWhiteSpace(row.BreakStart) && !string.IsNullOrWhiteSpace(row.BreakEnd))
            {
                if (!TimeOnly.TryParse(row.BreakStart, out var bs) || !TimeOnly.TryParse(row.BreakEnd, out var be))
                    return Result.Failure(new Error("SCHEDULE.BREAK", "قالب ساعت استراحت نامعتبر است"));
                breakStart = bs;
                breakEnd = be;
            }

            await repo.AddAsync(new DoctorSchedule
            {
                DoctorId = request.DoctorId,
                DayOfWeek = row.DayOfWeek,
                StartTime = start,
                EndTime = end,
                BreakStart = breakStart,
                BreakEnd = breakEnd,
                RoomId = row.RoomId,
                MaxPatients = row.MaxPatients,
                SlotDuration = 15,
                IsActive = true
            }, cancellationToken);
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
