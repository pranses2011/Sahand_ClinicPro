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

            var previousDayIndex = (dayIndex + 6) % 7;
            scheduleByDay.TryGetValue((byte)dayIndex, out var schedule);
            scheduleByDay.TryGetValue((byte)previousDayIndex, out var previousSchedule);
            if (previousSchedule is not null && previousSchedule.EndTime >= previousSchedule.StartTime)
                previousSchedule = null; // فقط بخشِ پس از نیمه‌شبِ شیفت شبِ روز قبل

            if (schedule is null && previousSchedule is null)
            {
                days.Add(new CalendarDayDto(shamsi, true, 0, false, 0, 0, true));
                continue;
            }

            if (absenceSet.Contains(date))
            {
                days.Add(new CalendarDayDto(shamsi, true, 0, true, 0, 0, true));
                continue;
            }

            var total = 0;
            if (schedule is not null)
            {
                var shiftMinutes = GetShiftDurationMinutes(schedule);
                // شیفتِ با مدت صفر (ساعت شروع و پایان یکسان ثبت شده) یعنی
                // مدت مشخص نیست — ظرفیت با یک شیفت کاری معمول برآورد می‌شود
                // تا روز «بدون جزئیات و تکمیل» نماند (گزارش مالک)
                if (shiftMinutes <= 0) shiftMinutes = 480;
                var slotMinutes = Math.Max(10, schedule.SlotDuration);
                // سقف بیمارِ صفر یعنی «ثبت نشده» — مثل خالی با مدت شیفت حساب
                // می‌شود؛ صفرِ خام ظرفیت روز را صفر و همهٔ روزها را «تکمیل و
                // غیرقابل‌کلیک» می‌کرد (گزارش مالک)
                total += schedule.MaxPatients is int cap && cap > 0
                    ? cap
                    : (slotMinutes > 0 ? shiftMinutes / slotMinutes : 0);
            }
            if (previousSchedule is not null)
            {
                var tailMinutes = (int)previousSchedule.EndTime.ToTimeSpan().TotalMinutes;
                var fullMinutes = GetShiftDurationMinutes(previousSchedule);
                var slotMinutes = Math.Max(10, previousSchedule.SlotDuration);
                var tailCapacity = previousSchedule.MaxPatients is int maxPatients && maxPatients > 0 && fullMinutes > 0
                    ? (int)Math.Ceiling(maxPatients * (double)tailMinutes / fullMinutes)
                    : tailMinutes / slotMinutes;
                total += tailCapacity;
            }

            var booked = bookedMap.GetValueOrDefault(date);
            var free = Math.Max(0, total - booked);
            days.Add(new CalendarDayDto(shamsi, free == 0, free, true, total, booked, false));
        }

        return Result<CalendarSummaryDto>.Success(new CalendarSummaryDto(days));
    }

    private static int GetShiftDurationMinutes(DoctorSchedule schedule) =>
        DoctorScheduleTime.DurationMinutes(schedule.StartTime, schedule.EndTime);
}
