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
/// ═══════════════════════════════════════════════════════════════════
/// چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) — «ساعت‌های صفحهٔ نوبت‌دهی همه
/// فعال می‌آیند و ساعات پر مشخص نمی‌شوند». دو شکاف واقعی رفع شد:
/// ۱) تطابق «تساوی دقیقِ ساعت» فقط نوبت‌هایی را پر نشان می‌داد که دقیقاً
///    روی مرز اسلات‌ها ثبت شده‌اند؛ نوبت ثبت‌شده از فرم نوبت (ساعت آزاد
///    مثل ۰۹:۳۵) یا با گام متفاوتِ برنامهٔ کاری هرگز هیچ اسلاتی را پر
///    نمی‌کرد. اکنون «تداخل بازه‌ای» بررسی می‌شود: بازهٔ نوبت
///    [ساعت، ساعت + مدت) با بازهٔ اسلات [شروع، شروع + گام) تصادم دارد؟
/// ۲) پذیرش‌های حضوری همان روز برای همان پزشک (ثبت از صفحهٔ پذیرش)
///    اصلاً در این پرس‌وجو دیده نمی‌شدند — بیماری که پذیرش شده ولی نوبت
///    ندارد، اسلاتی را اشغال نمی‌کرد. اکنون پذیرش‌های فعال روز (مرتبط
///    با پزشک) هم بازهٔ اشغالی می‌سازند.
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

        // روز هفته ایرانی — شنبه=۰
        var dayIndex = date.DayOfWeek switch
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

        // برنامه همان روز و شیفت شبِ روز قبل را با هم می‌خوانیم؛ بخش بعد از
        // نیمه‌شبِ شیفت روز قبل باید در تقویم روز جاری هم قابل نوبت‌دهی باشد.
        var previousDayIndex = (dayIndex + 6) % 7;
        var schedules = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive &&
                        (s.DayOfWeek == dayIndex || s.DayOfWeek == previousDayIndex))
            .OrderByDescending(s => s.Id)
            .ToListAsync(cancellationToken);

        var todaySchedule = schedules.FirstOrDefault(s => s.DayOfWeek == dayIndex);
        var previousNightSchedule = schedules.FirstOrDefault(s =>
            s.DayOfWeek == previousDayIndex && s.EndTime < s.StartTime);
        var windows = new List<ScheduleWindow>();

        if (todaySchedule is not null)
        {
            var startMinute = ToMinute(todaySchedule.StartTime);
            var endMinute = ToMinute(todaySchedule.EndTime);
            if (endMinute > startMinute)
                windows.Add(new ScheduleWindow(todaySchedule, startMinute, endMinute, startMinute));
            else if (endMinute < startMinute)
                windows.Add(new ScheduleWindow(todaySchedule, startMinute, 24 * 60, startMinute));
        }

        if (previousNightSchedule is not null)
        {
            var endMinute = ToMinute(previousNightSchedule.EndTime);
            if (endMinute > 0)
            {
                var previousStartMinute = ToMinute(previousNightSchedule.StartTime) - 24 * 60;
                windows.Add(new ScheduleWindow(previousNightSchedule, 0, endMinute, previousStartMinute));
            }
        }

        if (windows.Count == 0)
            return Result<List<SlotDto>>.Success(new List<SlotDto>());

        var roomIds = windows.Select(w => w.Schedule.RoomId).Where(id => id.HasValue)
            .Select(id => id!.Value).Distinct().ToList();
        var rooms = roomIds.Count == 0
            ? new Dictionary<long, ExaminationRoom>()
            : await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
                .Where(r => roomIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, cancellationToken);

        // نوبت‌های همان روز — تداخل بازه‌ای با مدت واقعی نوبت
        var booked = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow)
            .Select(a => new
            {
                a.AppointmentTime,
                a.Duration,
                PatientName = a.Patient!.FirstName + " " + a.Patient.LastName,
                Gender = a.Patient!.Gender.ToString(),
                a.Patient.BirthDate,
                // نوع بیمه یعنی نام بیمه‌گر (شرکت بیمه)؛ فیلد InsuranceType بیمار
                // «نسبت بیمه‌ای» است (اصلی/تبعی) و نباید اینجا بیاید (گزارش مالک)
                Insurance = a.Patient.InsuranceCompany != null ? a.Patient.InsuranceCompany.Name : null,
                a.Patient.NationalCode,
                Phone = a.Patient.Mobile
            })
            .ToListAsync(cancellationToken);

        // پذیرش حضوری همان روز هم بازهٔ پزشک را اشغال می‌کند.
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var receptions = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.DoctorId == request.DoctorId && !r.IsDeleted &&
                        r.ReceptionDate >= dayStart && r.ReceptionDate < dayEnd)
            .Select(r => new
            {
                Time = r.EnterTime != null
                    ? TimeOnly.FromDateTime(r.EnterTime.Value)
                    : TimeOnly.FromDateTime(r.ReceptionDate),
                PatientName = r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "",
                Gender = r.Patient != null ? r.Patient.Gender.ToString() : null,
                BirthDate = r.Patient != null ? r.Patient.BirthDate : null,
                Insurance = r.Patient != null ? (r.Patient.InsuranceCompany != null ? r.Patient.InsuranceCompany.Name : null) : null,
                NationalCode = r.Patient != null ? r.Patient.NationalCode : null,
                Phone = r.Patient != null ? r.Patient.Mobile : null
            })
            .ToListAsync(cancellationToken);

        var hourAbsence = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate == date &&
                        a.FromTime != null && a.ToTime != null)
            .Select(a => new { a.FromTime, a.ToTime })
            .ToListAsync(cancellationToken);

        var slots = new List<(int Minute, SlotDto Slot)>();
        var addedSlotMinutes = new HashSet<int>();

        foreach (var window in windows.OrderBy(w => w.StartMinute))
        {
            var windowSlotDuration = doctor.VisitDuration > 0
                ? doctor.VisitDuration
                : window.Schedule.SlotDuration;
            if (windowSlotDuration <= 0) continue;

            var breakRange = GetBreakRange(window);
            for (var minute = window.StartMinute; minute < window.EndMinute; minute += windowSlotDuration)
            {
                // اگر دو شیفت روی یک ساعت افتادند، یک گزینه نمایش داده شود.
                if (!addedSlotMinutes.Add(minute)) continue;

                var slotEnd = minute + windowSlotDuration;
                var isBreak = breakRange is { } br && RangesOverlap(minute, slotEnd, br.Start, br.End);
                var inAbsence = hourAbsence.Any(a =>
                    TimeRangeOverlaps(minute, slotEnd, a.FromTime, a.ToTime));

                var bookedHit = booked.FirstOrDefault(b =>
                {
                    var bookedStart = ToMinute(b.AppointmentTime);
                    var bookedDuration = b.Duration > 0 ? b.Duration : windowSlotDuration;
                    return bookedStart < slotEnd && minute < bookedStart + bookedDuration;
                });
                var receptionHit = receptions.FirstOrDefault(r =>
                {
                    var receptionStart = ToMinute(r.Time);
                    return receptionStart < slotEnd && minute < receptionStart + windowSlotDuration;
                });

                var patient = bookedHit?.PatientName ?? receptionHit?.PatientName;
                var isBooked = bookedHit is not null || receptionHit is not null;
                // جزئیات بیمار برای نوار جزئیات اسلات (جنسیت/سن/بیمه/کد ملی/تلفن)
                var pGender = bookedHit?.Gender ?? receptionHit?.Gender;
                var pBirth = bookedHit?.BirthDate ?? receptionHit?.BirthDate;
                int? pAge = pBirth is { } bd
                    ? date.Year - bd.Year - (date.Month < bd.Month || (date.Month == bd.Month && date.Day < bd.Day) ? 1 : 0)
                    : null;
                var pInsurance = bookedHit?.Insurance ?? receptionHit?.Insurance;
                var pNational = bookedHit?.NationalCode ?? receptionHit?.NationalCode;
                var pPhone = bookedHit?.Phone ?? receptionHit?.Phone;
                var displayMinute = minute % (24 * 60);
                var timeText = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(displayMinute)).ToString("HH:mm");
                var roomName = window.Schedule.RoomId is long roomId && rooms.TryGetValue(roomId, out var room)
                    ? (string.IsNullOrWhiteSpace(room.RoomName)
                        ? $"اتاق {room.RoomNumber}"
                        : room.RoomName)
                    : null;

                slots.Add((minute, new SlotDto(
                    timeText,
                    !isBreak && !isBooked && !inAbsence,
                    patient,
                    isBreak || inAbsence,
                    roomName,
                    doctor.ConsultationFee,
                    pGender, pAge, pInsurance, pNational, pPhone)));
            }
        }

        return Result<List<SlotDto>>.Success(slots.OrderBy(s => s.Minute).Select(s => s.Slot).ToList());
    }

    private sealed record ScheduleWindow(
        DoctorSchedule Schedule,
        int StartMinute,
        int EndMinute,
        int ShiftStartMinute);

    private static int ToMinute(TimeOnly time) => time.Hour * 60 + time.Minute;

    private static (int Start, int End)? GetBreakRange(ScheduleWindow window)
    {
        var start = window.Schedule.BreakStart;
        var end = window.Schedule.BreakEnd;
        if (start is null || end is null || start == end) return null;

        var shiftStartClock = ToMinute(window.Schedule.StartTime);
        var breakStart = window.ShiftStartMinute + Modulo(ToMinute(start.Value) - shiftStartClock, 24 * 60);
        var breakEnd = window.ShiftStartMinute + Modulo(ToMinute(end.Value) - shiftStartClock, 24 * 60);
        if (breakEnd <= breakStart) breakEnd += 24 * 60;
        return (breakStart, breakEnd);
    }

    private static bool TimeRangeOverlaps(int slotStart, int slotEnd, TimeOnly? from, TimeOnly? to)
    {
        if (from is null || to is null || from == to) return false;
        var rangeStart = ToMinute(from.Value);
        var rangeEnd = ToMinute(to.Value);
        if (rangeEnd < rangeStart)
        {
            rangeEnd += 24 * 60;
            return RangesOverlap(slotStart, slotEnd, rangeStart, rangeEnd) ||
                   RangesOverlap(slotStart, slotEnd, rangeStart - 24 * 60, rangeEnd - 24 * 60);
        }
        return RangesOverlap(slotStart, slotEnd, rangeStart, rangeEnd);
    }

    private static bool RangesOverlap(int firstStart, int firstEnd, int secondStart, int secondEnd) =>
        firstStart < secondEnd && secondStart < firstEnd;

    private static int Modulo(int value, int modulus) => (value % modulus + modulus) % modulus;

}
