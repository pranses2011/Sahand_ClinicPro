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

        var schedule = await _unitOfWork.Repository<DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive && s.DayOfWeek == dayIndex)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (schedule is null)
            return Result<List<SlotDto>>.Success(new List<SlotDto>());

        var room = schedule.RoomId is null ? null :
            await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.Id == schedule.RoomId, cancellationToken);

        var slotDuration = doctor.VisitDuration > 0 ? doctor.VisitDuration : schedule.SlotDuration;

        // ═══ نوبت‌های همان روز — با «مدت» برای تداخل بازه‌ای (چرخه ۲۸) ═══
        var booked = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AppointmentDate == date &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow)
            .Select(a => new
            {
                a.AppointmentTime,
                // مدت واقعی نوبت؛ صفر = همان گام اسلات (حداقل یک اسلات اشغال)
                Duration = a.Duration > 0 ? a.Duration : slotDuration,
                PatientName = a.Patient!.FirstName + " " + a.Patient.LastName
            })
            .ToListAsync(cancellationToken);

        // ═══ پذیرش‌های حضوری همان روز برای همین پزشک — چرخه ۲۸ ═══
        // بیمارِ پذیرش‌شده از صفحهٔ پذیرش هم وقت پزشک را می‌گیرد؛ ساعتِ مراجعه
        // از خود ReceptionDate (شامل ساعت ثبت) و در نبود آن EnterTime خوانده
        // می‌شود؛ پذیرش‌های لغو/ترخیص‌شده غالباً جای پزشک را آزاد کرده‌اند اما
        // برای اطمینانِ ظرفیت، همهٔ پذیرش‌های فعال روز لحاظ می‌شوند
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var receptions = await _unitOfWork.Repository<Reception>().QueryableNoTracking
            .Where(r => r.DoctorId == request.DoctorId &&
                        !r.IsDeleted &&
                        r.ReceptionDate >= dayStart && r.ReceptionDate < dayEnd)
            .Select(r => new
            {
                Time = r.EnterTime != null
                    ? TimeOnly.FromDateTime(r.EnterTime.Value)
                    : TimeOnly.FromDateTime(r.ReceptionDate),
                PatientName = r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : ""
            })
            .ToListAsync(cancellationToken);

        // غیبت ساعتی
        var hourAbsence = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate == date && a.FromTime != null && a.ToTime != null)
            .Select(a => new { a.FromTime, a.ToTime })
            .ToListAsync(cancellationToken);

        var slots = new List<SlotDto>();

        for (var time = schedule.StartTime; time < schedule.EndTime; time = time.AddMinutes(slotDuration))
        {
            var slotEnd = time.AddMinutes(slotDuration);
            var isBreak = schedule.BreakStart is not null && schedule.BreakEnd is not null &&
                          time >= schedule.BreakStart && time < schedule.BreakEnd;
            var inAbsence = hourAbsence.Any(a => time >= a.FromTime && time < a.ToTime);

            // ═══ تداخل بازه‌ای — چرخه ۲۸ ═══
            // بازهٔ نوبت [t, t+d) با بازهٔ اسلات [شروع، پایان) هر تلاقی داشت،
            // اسلات «پر» است — حالت‌های ساعتِ آزاد و گامِ متفاوت هم پوشش
            // داده می‌شوند؛ نام بیمار نخستین نوبتِ تصادمی برای تولتیپ
            var bookedHit = booked.FirstOrDefault(b =>
                b.AppointmentTime < slotEnd && time < b.AppointmentTime.AddMinutes(b.Duration));
            var receptionHit = receptions.FirstOrDefault(r =>
                r.Time < slotEnd && time < r.Time.AddMinutes(slotDuration));

            var isBooked = bookedHit is not null || receptionHit is not null;
            var patient = bookedHit?.PatientName ?? receptionHit?.PatientName;

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
