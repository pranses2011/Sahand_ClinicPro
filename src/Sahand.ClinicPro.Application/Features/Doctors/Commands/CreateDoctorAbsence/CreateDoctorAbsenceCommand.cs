using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctorAbsence;

/// <summary>
/// نتیجه ثبت غیبت — اگر نوبت رزروشده باشد کلید هشدار پر می‌شود
/// </summary>
public record AbsenceConflictInfo(long AbsenceId, int BookedAppointments);

/// <summary>
/// فرمان ثبت غیبت پزشک
/// </summary>
public record CreateDoctorAbsenceCommand(long DoctorId, CreateAbsenceRequest Request)
    : IRequest<Result<AbsenceConflictInfo>>;

/// <summary>
/// گرداننده ثبت غیبت — شمارش نوبت‌های درگیر برای تأیید لغو توسط کلاینت
/// </summary>
public class CreateDoctorAbsenceCommandHandler : IRequestHandler<CreateDoctorAbsenceCommand, Result<AbsenceConflictInfo>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateDoctorAbsenceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<AbsenceConflictInfo>> Handle(CreateDoctorAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        // تبدیل تاریخ شمسی به میلادی — نمونه: ۱۴۰۴/۰۴/۰۳
        var gregorian = PersianDateHelper.ToGregorian(dto.DateShamsi);
        if (gregorian is null)
            return Result<AbsenceConflictInfo>.Failure(
                new Error("ABSENCE.DATE", "تاریخ شمسی نامعتبر است — نمونه: ۱۴۰۴/۰۴/۰۳"));
        var date = DateOnly.FromDateTime(gregorian.Value);

        TimeOnly? from = null, to = null;
        if (!string.IsNullOrWhiteSpace(dto.FromTime) && !TimeOnly.TryParse(dto.FromTime, out var f))
            return Result<AbsenceConflictInfo>.Failure(new Error("ABSENCE.TIME", "قالب ساعت شروع نامعتبر است"));
        if (!string.IsNullOrWhiteSpace(dto.ToTime) && !TimeOnly.TryParse(dto.ToTime, out var t))
            return Result<AbsenceConflictInfo>.Failure(new Error("ABSENCE.TIME", "قالب ساعت پایان نامعتبر است"));
        if (dto.FromTime is not null) from = TimeOnly.Parse(dto.FromTime);
        if (dto.ToTime is not null) to = TimeOnly.Parse(dto.ToTime);

        // شمارش نوبت‌های رزروشده درگیر — طبق فرم ۷: «۵ نوبت رزرو شده وجود دارد. لغو شوند؟»
        var conflictQuery = _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(x => x.DoctorId == request.DoctorId &&
                        x.AppointmentDate == date &&
                        x.Status != AppointmentStatus.Cancelled &&
                        x.Status != AppointmentStatus.NoShow);
        if (from.HasValue && to.HasValue)
            conflictQuery = conflictQuery.Where(x => x.AppointmentTime >= from.Value && x.AppointmentTime <= to.Value);
        var booked = await conflictQuery.CountAsync(cancellationToken);

        var absence = new DoctorAbsence
        {
            DoctorId = request.DoctorId,
            AbsenceDate = date,
            FromTime = from,
            ToTime = to,
            AbsenceType = dto.AbsenceType,
            Reason = dto.Reason
        };
        await _unitOfWork.Repository<DoctorAbsence>().AddAsync(absence, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<AbsenceConflictInfo>.Success(new AbsenceConflictInfo(absence.Id, booked));
    }
}
