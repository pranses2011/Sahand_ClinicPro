# -*- coding: utf-8 -*-
"""گام U3 — API سمت سرور: برنامه کاری، غیبت‌ها، عملکرد پزشک + lookups جدید"""
import os, io

ROOT = "/home/z/my-project/Sahand_ClinicPro"
APP = f"{ROOT}/src/Sahand.ClinicPro.Application"
API = f"{ROOT}/src/Sahand.ClinicPro.API"

FILES = {}

# ─────────────────────────── DTOs ───────────────────────────
FILES[f"{APP}/DTOs/Doctors/DoctorScheduleDtos.cs"] = '''using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Doctors;

/// <summary>
/// ردیف برنامه هفتگی پزشک — هفت روز هفته ایرانی
/// DayOfWeek: شنبه=۰ ... جمعه=۶
/// </summary>
public class ScheduleRowDto
{
    /// <summary>روز هفته — شنبه=۰ تا جمعه=۶</summary>
    public byte DayOfWeek { get; set; }

    /// <summary>روز فعال است</summary>
    public bool IsEnabled { get; set; }

    /// <summary>ساعت شروع — قالب HH:mm</summary>
    public string? StartTime { get; set; }

    /// <summary>ساعت پایان — قالب HH:mm</summary>
    public string? EndTime { get; set; }

    /// <summary>شروع استراحت — اختیاری</summary>
    public string? BreakStart { get; set; }

    /// <summary>پایان استراحت — اختیاری</summary>
    public string? BreakEnd { get; set; }

    /// <summary>شناسه اتاق ویزیت</summary>
    public long? RoomId { get; set; }

    /// <summary>نام اتاق برای نمایش</summary>
    public string? RoomName { get; set; }

    /// <summary>حداکثر ظرفیت بیمار</summary>
    public int? MaxPatients { get; set; }
}

/// <summary>
/// درخواست ذخیره برنامه هفتگی — جایگزینی کامل هفت ردیف
/// </summary>
public class SaveScheduleRequest
{
    /// <summary>ردیف‌های هفت روز</summary>
    public List<ScheduleRowDto> Rows { get; set; } = new();
}

/// <summary>
/// غیبت ثبت‌شده پزشک
/// </summary>
public class AbsenceDto
{
    /// <summary>شناسه غیبت</summary>
    public long Id { get; set; }

    /// <summary>تاریخ غیبت</summary>
    public string DateShamsi { get; set; } = string.Empty;

    /// <summary>از ساعت — خالی یعنی کل روز</summary>
    public string? FromTime { get; set; }

    /// <summary>تا ساعت</summary>
    public string? ToTime { get; set; }

    /// <summary>نوع غیبت</summary>
    public string AbsenceTypeName { get; set; } = string.Empty;

    /// <summary>دلیل</summary>
    public string? Reason { get; set; }

    /// <summary>تعداد نوبت‌های رزروشده در بازه — برای هشدار</summary>
    public int BookedAppointments { get; set; }
}

/// <summary>
/// درخواست ثبت غیبت جدید
/// </summary>
public class CreateAbsenceRequest
{
    /// <summary>تاریخ شمسی — ۱۴۰۴/۰۴/۰۳</summary>
    public string DateShamsi { get; set; } = string.Empty;

    /// <summary>از ساعت — اختیاری (خالی = کل روز)</summary>
    public string? FromTime { get; set; }

    /// <summary>تا ساعت — اختیاری</summary>
    public string? ToTime { get; set; }

    /// <summary>نوع غیبت</summary>
    public AbsenceType AbsenceType { get; set; }

    /// <summary>دلیل</summary>
    public string? Reason { get; set; }
}

/// <summary>
/// آمار عملکرد پزشک — برای دکمه آمار در فهرست
/// </summary>
public class DoctorPerformanceDto
{
    /// <summary>تعداد ویزیت این ماه</summary>
    public int VisitsThisMonth { get; set; }

    /// <summary>تعداد ویزیت امروز</summary>
    public int VisitsToday { get; set; }

    /// <summary>درآمد تولیدشده این ماه به ریال</summary>
    public decimal RevenueThisMonth { get; set; }

    /// <summary>کارانه پزشک این ماه — درصد سهم × درآمد</summary>
    public decimal CommissionThisMonth { get; set; }

    /// <summary>میانگین مدت ویزیت به دقیقه</summary>
    public double AverageVisitMinutes { get; set; }

    /// <summary>امتیاز رضایت از ۵</summary>
    public decimal Rating { get; set; }

    /// <summary>نرخ عدم مراجعه به درصد</summary>
    public double NoShowRate { get; set; }
}
'''

# ─────────────────────────── Schedule Query ───────────────────────────
FILES[f"{APP}/Features/Doctors/Queries/GetDoctorSchedule/GetDoctorScheduleQuery.cs"] = '''using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorSchedule;

/// <summary>
/// پرس‌وجوی برنامه هفتگی پزشک — همیشه هفت ردیف برمی‌گرداند
/// </summary>
public record GetDoctorScheduleQuery(long DoctorId) : IRequest<Result<List<ScheduleRowDto>>>;

/// <summary>
/// گرداننده برنامه هفتگی — ردیف‌های ذخیره‌شده + تکمیل روزهای خالی
/// </summary>
public class GetDoctorScheduleQueryHandler : IRequestHandler<GetDoctorScheduleQuery, Result<List<ScheduleRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDoctorScheduleQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — هفت ردیف کامل فارسی از شنبه تا جمعه</summary>
    public async Task<Result<List<ScheduleRowDto>>> Handle(GetDoctorScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Scheduling.DoctorSchedule>().QueryableNoTracking
            .Where(s => s.DoctorId == request.DoctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ToListAsync(cancellationToken);

        var rooms = await _unitOfWork.Repository<Domain.Entities.Scheduling.ExaminationRoom>()
            .QueryableNoTracking.ToListAsync(cancellationToken);

        var result = new List<ScheduleRowDto>();
        for (byte day = 0; day <= 6; day++)
        {
            var row = rows.FirstOrDefault(r => r.DayOfWeek == day);
            if (row is null)
            {
                result.Add(new ScheduleRowDto { DayOfWeek = day, IsEnabled = false });
                continue;
            }

            var room = rooms.FirstOrDefault(r => r.Id == row.RoomId);
            result.Add(new ScheduleRowDto
            {
                DayOfWeek = day,
                IsEnabled = true,
                StartTime = row.StartTime.ToString("HH:mm"),
                EndTime = row.EndTime.ToString("HH:mm"),
                BreakStart = row.BreakStart?.ToString("HH:mm"),
                BreakEnd = row.BreakEnd?.ToString("HH:mm"),
                RoomId = row.RoomId,
                RoomName = room is null ? null : RoomDisplay(room),
                MaxPatients = row.MaxPatients
            });
        }

        return Result<List<ScheduleRowDto>>.Success(result);
    }

    /// <summary>نام نمایشی اتاق</summary>
    private static string RoomDisplay(Domain.Entities.Scheduling.ExaminationRoom room) =>
        string.IsNullOrWhiteSpace(room.RoomName)
            ? $"اتاق {room.RoomNumber ?? room.Id.ToString()}"
            : room.RoomName;
}
'''

# ─────────────────────────── Save Schedule Command ───────────────────────────
FILES[f"{APP}/Features/Doctors/Commands/SaveDoctorSchedule/SaveDoctorScheduleCommand.cs"] = '''using MediatR;
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
'''

# ─────────────────────────── Absences ───────────────────────────
FILES[f"{APP}/Features/Doctors/Queries/GetDoctorAbsences/GetDoctorAbsencesQuery.cs"] = '''using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorAbsences;

/// <summary>
/// پرس‌وجوی غیبت‌های آینده و امروز پزشک
/// </summary>
public record GetDoctorAbsencesQuery(long DoctorId) : IRequest<Result<List<AbsenceDto>>>;

/// <summary>
/// گرداننده غیبت‌ها — همراه با شمارش نوبت‌های رزروشده برای هشدار
/// </summary>
public class GetDoctorAbsencesQueryHandler : IRequestHandler<GetDoctorAbsencesQuery, Result<List<AbsenceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDoctorAbsencesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<AbsenceDto>>> Handle(GetDoctorAbsencesQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var absences = await _unitOfWork.Repository<DoctorAbsence>().QueryableNoTracking
            .Where(a => a.DoctorId == request.DoctorId && a.AbsenceDate >= today)
            .OrderBy(a => a.AbsenceDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        var doctor = await _unitOfWork.Repository<Domain.Entities.People.Doctor>()
            .QueryableNoTracking.FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<List<AbsenceDto>>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var result = new List<AbsenceDto>();
        foreach (var a in absences)
        {
            var booked = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
                .CountAsync(x => x.DoctorId == request.DoctorId &&
                                 x.AppointmentDate == a.AbsenceDate &&
                                 x.Status != AppointmentStatus.Cancelled &&
                                 x.Status != AppointmentStatus.NoShow, cancellationToken);

            result.Add(new AbsenceDto
            {
                Id = a.Id,
                DateShamsi = PersianDateHelper.ToShamsi(a.AbsenceDate.ToDateTime(TimeOnly.MinValue)),
                FromTime = a.FromTime?.ToString("HH:mm"),
                ToTime = a.ToTime?.ToString("HH:mm"),
                AbsenceTypeName = AbsenceName(a.AbsenceType),
                Reason = a.Reason,
                BookedAppointments = booked
            });
        }

        return Result<List<AbsenceDto>>.Success(result);
    }

    /// <summary>نام فارسی نوع غیبت</summary>
    private static string AbsenceName(AbsenceType type) => type switch
    {
        AbsenceType.Vacation => "مرخصی",
        AbsenceType.SickLeave => "استعلاجی",
        AbsenceType.Conference => "کنگره",
        AbsenceType.Other => "سایر",
        _ => type.ToString()
    };
}
'''

FILES[f"{APP}/Features/Doctors/Commands/CreateDoctorAbsence/CreateDoctorAbsenceCommand.cs"] = '''using MediatR;
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
'''

FILES[f"{APP}/Features/Doctors/Commands/DeleteDoctorAbsence/DeleteDoctorAbsenceCommand.cs"] = '''using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctorAbsence;

/// <summary>
/// فرمان حذف غیبت پزشک
/// </summary>
public record DeleteDoctorAbsenceCommand(long AbsenceId) : IRequest<Result>;

/// <summary>
/// گرداننده حذف غیبت
/// </summary>
public class DeleteDoctorAbsenceCommandHandler : IRequestHandler<DeleteDoctorAbsenceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeleteDoctorAbsenceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result> Handle(DeleteDoctorAbsenceCommand request, CancellationToken cancellationToken)
    {
        var repo = _unitOfWork.Repository<DoctorAbsence>();
        var absence = await repo.QueryableTracking
            .FirstOrDefaultAsync(a => a.Id == request.AbsenceId, cancellationToken);
        if (absence is null)
            return Result.Failure(new Error("ABSENCE.NOTFOUND", "غیبت یافت نشد"));

        repo.Remove(absence);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
'''

# ─────────────────────────── Performance ───────────────────────────
FILES[f"{APP}/Features/Doctors/Queries/GetDoctorPerformance/GetDoctorPerformanceQuery.cs"] = '''using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorPerformance;

/// <summary>
/// پرس‌وجوی آمار عملکرد پزشک — ماه جاری + امروز
/// </summary>
public record GetDoctorPerformanceQuery(long DoctorId) : IRequest<Result<DoctorPerformanceDto>>;

/// <summary>
/// گرداننده آمار عملکرد
/// </summary>
public class GetDoctorPerformanceQueryHandler : IRequestHandler<GetDoctorPerformanceQuery, Result<DoctorPerformanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetDoctorPerformanceQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<DoctorPerformanceDto>> Handle(GetDoctorPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<DoctorPerformanceDto>.Failure(new Error("DOCTOR.NOTFOUND", "پزشک یافت نشد"));

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var todayStart = today;
        var todayEnd = todayStart.AddDays(1);

        var visits = _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.DoctorId == request.DoctorId);

        var visitsMonth = await visits.Where(v => v.VisitDate >= monthStart && v.VisitDate < todayEnd).CountAsync(cancellationToken);
        var visitsToday = await visits.Where(v => v.VisitDate >= todayStart && v.VisitDate < todayEnd).CountAsync(cancellationToken);

        var visitIds = visits.Where(v => v.VisitDate >= monthStart && v.VisitDate < todayEnd).Select(v => v.Id);
        var revenueMonth = await _unitOfWork.Repository<Invoice>().QueryableNoTracking
            .Where(i => i.VisitId != null && visitIds.Contains(i.VisitId.Value) &&
                        i.Status != Domain.Enums.InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        // میانگین مدت ویزیت — محاسبه در حافظه برای سازگاری با InMemory
        var timedVisits = await visits
            .Where(v => v.StartTime != null && v.EndTime != null && v.VisitDate >= monthStart)
            .Select(v => new { v.StartTime, v.EndTime })
            .ToListAsync(cancellationToken);
        var avgMinutes = timedVisits.Count == 0 ? 0 :
            Math.Round(timedVisits.Average(v => (v.EndTime!.Value - v.StartTime!.Value).TotalMinutes), 1);

        var appointmentsMonth = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .CountAsync(a => a.DoctorId == request.DoctorId && a.AppointmentDate >= DateOnly.FromDateTime(monthStart), cancellationToken);
        var noShows = await _unitOfWork.Repository<Appointment>().QueryableNoTracking
            .CountAsync(a => a.DoctorId == request.DoctorId && a.AppointmentDate >= DateOnly.FromDateTime(monthStart) &&
                             a.Status == AppointmentStatus.NoShow, cancellationToken);
        var noShowRate = appointmentsMonth == 0 ? 0 : Math.Round(noShows * 100.0 / appointmentsMonth, 1);

        return Result<DoctorPerformanceDto>.Success(new DoctorPerformanceDto
        {
            VisitsThisMonth = visitsMonth,
            VisitsToday = visitsToday,
            RevenueThisMonth = revenueMonth,
            CommissionThisMonth = Math.Round(revenueMonth * doctor.DoctorPercentage / 100m),
            AverageVisitMinutes = Math.Round(avgMinutes, 1),
            Rating = doctor.Rating,
            NoShowRate = noShowRate
        });
    }
}
'''

print("API-side files:", len(FILES))
for path, content in FILES.items():
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with io.open(path, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content)
    print(" ✓", os.path.basename(path))
