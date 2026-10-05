using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAvailableSlots;

/// <summary>
/// پرس‌وجوی اسلات‌های زمانی یک پزشک در یک تاریخ — گام U4 طبق فرم ۸
/// تولید اسلات از برنامه هفتگی + حذف رزروشده‌ها + استراحت + غیبت
/// </summary>
public record GetAvailableSlotsQuery(long DoctorId, string DateShamsi)
    : IRequest<Result<List<SlotDto>>>;

/// <summary>
/// گرداننده تولید اسلات
/// </summary>
public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, Result<List<SlotDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetAvailableSlotsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — الگوریتم تولید اسلات</summary>
    public async Task<Result<List<SlotDto>>> Handle(GetAvailableSlotsQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<List<SlotDto>>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var gregorian = PersianDateHelper.ToGregorian(request.DateShamsi);
        if (gregorian is null)
            return Result<List<SlotDto>>.Failure(new Error("DATE.INVALID", "تاریخ شمسی نامعتبر است — نمونه: ۱۴۰۴/۰۴/۰۳"));
        var date = DateOnly.FromDateTime(gregorian.Value);

        // غیبت کل‌روز → همه اسلات‌ها غیرفعال
        var dayAbsence = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .AnyAsync(a => a.DoctorId == request.DoctorId && a.AbsenceDate == date, cancellationToken);
        if (dayAbsence)
            return Result<List<SlotDto>>.Success(new List<SlotDto>());

        // روز هفته ایرانی: شنبه=۰
        var persianDay = ((int)date.DayOfWeek + 1) % 7; // شنبه=6→0 با +1
        var persianDayIndex = ((int)date.DayOfWeek + 2) % 7; // اصلاح: شنبه DotNet=6
        // شنبه دات‌نت=Saturday=6 → index 0
        var dayIndex = ((int)date.DayOfWeek + 2) % 7 == 0 ? 6 : ((int)date.DayOfWeek + 1) % 7;
        dayIndex = date.DayOfWeek switch
        {
            DayOfWeek.Saturday => 0,
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            _ => 0
        };

        var schedule = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive && s.DayOfWeek == dayIndex)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (schedule is null)
            return Result<List<SlotDto>>.Success(new List<SlotDto>());

        var room = schedule.RoomId is null ? null :
            await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.Id == schedule.RoomId, cancellationToken);

        // نوبت‌های همان روز
        var booked = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow)
            .Select(a => new { a.AppointmentTime, PatientName = a.Patient!.FirstName + " " + a.Patient.LastName })
            .ToListAsync(cancellationToken);
        var bookedMap = booked.ToDictionary(b => b.AppointmentTime, b => b.PatientName);

        // غیبت ساعتی
        var hourAbsence = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate == date && a.FromTime != null && a.ToTime != null)
            .Select(a => new { a.FromTime, a.ToTime })
            .ToListAsync(cancellationToken);

        var slotDuration = doctor.VisitDuration > 0 ? doctor.VisitDuration : schedule.SlotDuration;
        var slots = new List<SlotDto>();

        for (var time = schedule.StartTime; time < schedule.EndTime; time = time.AddMinutes(slotDuration))
        {
            var isBreak = schedule.BreakStart is not null && schedule.BreakEnd is not null &&
                          time >= schedule.BreakStart && time < schedule.BreakEnd;
            var inAbsence = hourAbsence.Any(a => time >= a.FromTime && time < a.ToTime);
            var isBooked = bookedMap.ContainsKey(time);
            var patient = isBooked ? bookedMap[time] : null;

            slots.Add(new SlotDto(
                time.ToString("HH:mm"),
                !isBreak && !isBooked && !inAbsence,
                patient,
                isBreak || inAbsence,
                room is null ? null :
                    (string.IsNullOrWhiteSpace(room.RoomName) ? $"اتاق {room.RoomNumber}" : room.RoomName),
                doctor.ConsultationFee));
        }

        return Result<List<SlotDto>>.Success(slots);
    }
}
