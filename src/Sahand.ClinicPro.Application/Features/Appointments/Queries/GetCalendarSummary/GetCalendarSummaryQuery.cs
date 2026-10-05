using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Queries.GetCalendarSummary;

/// <summary>
/// خلاصه تقویم ماهانه پزشک — تعداد نوبت آزاد هر روز + روزهای تعطیل
/// </summary>
public record GetCalendarSummaryQuery(long DoctorId, int Year, int Month)
    : IRequest<Result<CalendarSummaryDto>>;

/// <summary>
/// گرداننده خلاصه تقویم
/// </summary>
public class GetCalendarSummaryQueryHandler : IRequestHandler<GetCalendarSummaryQuery, Result<CalendarSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetCalendarSummaryQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — برای هر روز ماه شمسی</summary>
    public async Task<Result<CalendarSummaryDto>> Handle(GetCalendarSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<CalendarSummaryDto>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var schedules = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive)
            .ToListAsync(cancellationToken);
        var scheduleByDay = schedules
            .GroupBy(s => s.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.Id).First());

        var daysInMonth = DateTime.DaysInMonth;
        // طول ماه شمسی — از تبدیل برعکس: روز اول ماه بعد منهای روز اول همین ماه
        var firstOfThis = PersianDateHelper.ToGregorian($"{request.Year}/{request.Month:00}/01");
        if (firstOfThis is null)
            return Result<CalendarSummaryDto>.Failure(new Error("DATE.INVALID", "سال/ماه نامعتبر است"));
        var nextMonth = request.Month == 12 ? $"{request.Year + 1}/01/01" : $"{request.Year}/{request.Month + 1:00}/01";
        var firstOfNext = PersianDateHelper.ToGregorian(nextMonth)!;
        var totalDays = (int)(firstOfNext.Value.Date - firstOfThis.Value.Date).TotalDays;

        var start = DateOnly.FromDateTime(firstOfThis.Value);
        var end = start.AddDays(totalDays - 1);

        var absences = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate >= start && a.AbsenceDate <= end)
            .Select(a => a.AbsenceDate)
            .ToListAsync(cancellationToken);
        var absenceSet = absences.ToHashSet();

        var bookedCounts = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AppointmentDate >= start && a.AppointmentDate <= end &&
                        a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .GroupBy(a => a.AppointmentDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var bookedMap = bookedCounts.ToDictionary(b => b.Date, b => b.Count);

        var days = new List<CalendarDayDto>();
        for (var i = 0; i < totalDays; i++)
        {
            var date = start.AddDays(i);
            var shamsi = PersianDateHelper.ToShamsi(date.ToDateTime(TimeOnly.MinValue));
            var dayIndex = date.DayOfWeek switch
            {
                DayOfWeek.Saturday => 0,
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                _ => 6
            };

            if (!scheduleByDay.TryGetValue((byte)dayIndex, out var schedule) || !schedule.IsActive)
            {
                days.Add(new CalendarDayDto(shamsi, true, 0, false));
                continue;
            }

            if (absenceSet.Contains(date))
            {
                days.Add(new CalendarDayDto(shamsi, true, 0, true));
                continue;
            }

            var total = schedule.MaxPatients ??
                        (int)((schedule.EndTime - schedule.StartTime).TotalMinutes / Math.Max(10, schedule.SlotDuration));
            var booked = bookedMap.GetValueOrDefault(date);
            var free = Math.Max(0, total - booked);
            days.Add(new CalendarDayDto(shamsi, free == 0, free, true));
        }

        return Result<CalendarSummaryDto>.Success(new CalendarSummaryDto(days));
    }
}
