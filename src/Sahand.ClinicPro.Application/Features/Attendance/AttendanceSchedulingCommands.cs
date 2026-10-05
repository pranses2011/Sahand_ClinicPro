using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Attendance;

// ═══════════════════════════════════════════════════════════════
// شیفت‌ها
// ═══════════════════════════════════════════════════════════════

/// <summary>مدل ذخیره شیفت</summary>
public record SaveShiftDto(
    long? Id,
    string ShiftName,
    string? ShiftCode,
    TimeOnly StartTime,
    TimeOnly EndTime,
    ShiftType ShiftType,
    bool IsNightShift,
    int LateGraceMinutes,
    int EarlyLeaveGraceMinutes,
    int MinMinutesBetweenPunches,
    int BreakMinutes,
    int OvertimeStartAfterMinutes,
    string? Color);

/// <summary>فرمان ثبت/ویرایش شیفت</summary>
public record SaveShiftCommand(SaveShiftDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج شیفت</summary>
public class SaveShiftCommandValidator : AbstractValidator<SaveShiftCommand>
{
    /// <summary>قواعد</summary>
    public SaveShiftCommandValidator()
    {
        RuleFor(x => x.Dto.ShiftName).NotEmpty().MaximumLength(200)
            .WithMessage("نام شیفت الزامی است");
        RuleFor(x => x.Dto.LateGraceMinutes).InclusiveBetween(0, 120)
            .WithMessage("تحمل تأخیر بین ۰ تا ۱۲۰ دقیقه");
        RuleFor(x => x.Dto.EarlyLeaveGraceMinutes).InclusiveBetween(0, 120)
            .WithMessage("تحمل تعجیل بین ۰ تا ۱۲۰ دقیقه");
        RuleFor(x => x.Dto.BreakMinutes).InclusiveBetween(0, 180)
            .WithMessage("استراحت بین ۰ تا ۱۸۰ دقیقه");
    }
}

/// <summary>گرداننده ثبت/ویرایش شیفت</summary>
public class SaveShiftCommandHandler : IRequestHandler<SaveShiftCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SaveShiftCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(SaveShiftCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (dto.ShiftCode is { } code)
        {
            var dup = await _unitOfWork.Repository<Shift>().QueryableNoTracking
                .AnyAsync(s => !s.IsDeleted && s.Id != dto.Id && s.ShiftCode == code, ct);
            if (dup)
                return Result<long>.Failure(new Error("ATT.SHIFT.CODE.DUP",
                    "شیفت دیگری با این کد ثبت شده است"));
        }

        if (dto.Id is { } id)
        {
            var shift = await _unitOfWork.Repository<Shift>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (shift is null)
                return Result<long>.Failure(new Error("ATT.SHIFT.NOTFOUND", "شیفت یافت نشد"));

            shift.ShiftName = dto.ShiftName;
            shift.ShiftCode = dto.ShiftCode;
            shift.StartTime = dto.StartTime;
            shift.EndTime = dto.EndTime;
            shift.ShiftType = dto.ShiftType;
            shift.IsNightShift = dto.IsNightShift;
            shift.LateGraceMinutes = dto.LateGraceMinutes;
            shift.EarlyLeaveGraceMinutes = dto.EarlyLeaveGraceMinutes;
            shift.MinMinutesBetweenPunches = dto.MinMinutesBetweenPunches;
            shift.BreakMinutes = dto.BreakMinutes;
            shift.OvertimeStartAfterMinutes = dto.OvertimeStartAfterMinutes;
            shift.Color = dto.Color;
            _unitOfWork.Repository<Shift>().Update(shift);
            await _unitOfWork.SaveEntitiesAsync(ct);
            return Result<long>.Success(shift.Id);
        }

        var created = await _unitOfWork.Repository<Shift>().AddAsync(new Shift
        {
            ShiftName = dto.ShiftName,
            ShiftCode = dto.ShiftCode,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ShiftType = dto.ShiftType,
            IsNightShift = dto.IsNightShift,
            LateGraceMinutes = dto.LateGraceMinutes,
            EarlyLeaveGraceMinutes = dto.EarlyLeaveGraceMinutes,
            MinMinutesBetweenPunches = dto.MinMinutesBetweenPunches,
            BreakMinutes = dto.BreakMinutes,
            OvertimeStartAfterMinutes = dto.OvertimeStartAfterMinutes,
            Color = dto.Color,
            IsActive = true,
        }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(created.Id);
    }
}

/// <summary>فرمان حذف شیفت — فقط وقتی تخصیص فعالی ندارد</summary>
public record DeleteShiftCommand(long ShiftId) : IRequest<Result<bool>>;

/// <summary>گرداننده حذف شیفت</summary>
public class DeleteShiftCommandHandler : IRequestHandler<DeleteShiftCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeleteShiftCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — رکوردهای تاریخی حضور حفظ می‌شوند</summary>
    public async Task<Result<bool>> Handle(DeleteShiftCommand request, CancellationToken ct)
    {
        var inUse = await _unitOfWork.Repository<EmployeeShiftAssignment>().QueryableNoTracking
            .AnyAsync(a => a.ShiftId == request.ShiftId && a.IsActive && !a.IsDeleted, ct);
        if (inUse)
            return Result<bool>.Failure(new Error("ATT.SHIFT.INUSE",
                "این شیفت به پرسنل تخصیص فعال دارد — ابتدا تخصیص‌ها را پایان دهید"));

        var shift = await _unitOfWork.Repository<Shift>()
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct);
        if (shift is null)
            return Result<bool>.Failure(new Error("ATT.SHIFT.NOTFOUND", "شیفت یافت نشد"));

        shift.IsActive = false;
        _unitOfWork.Repository<Shift>().Update(shift);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════════════════════
// تخصیص شیفت
// ═══════════════════════════════════════════════════════════════

/// <summary>فرمان تخصیص شیفت به پرسنل</summary>
public record AssignShiftCommand(
    long EmployeeId,
    long ShiftId,
    DateOnly FromDate,
    DateOnly? ToDate,
    ShiftAssignmentRecurrence Recurrence,
    int WeekDaysMask,
    int Priority,
    string? Notes) : IRequest<Result<long>>;

/// <summary>گرداننده تخصیص شیفت</summary>
public class AssignShiftCommandHandler : IRequestHandler<AssignShiftCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public AssignShiftCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(AssignShiftCommand request, CancellationToken ct)
    {
        if (request.ToDate is { } to && to < request.FromDate)
            return Result<long>.Failure(new Error("ATT.ASSIGN.RANGE",
                "پایان بازه نمی‌تواند پیش از شروع آن باشد"));

        var shift = await _unitOfWork.Repository<Shift>().QueryableNoTracking
            .AnyAsync(s => s.Id == request.ShiftId && s.IsActive && !s.IsDeleted, ct);
        if (!shift)
            return Result<long>.Failure(new Error("ATT.SHIFT.NOTFOUND", "شیفت فعال یافت نشد"));

        var employee = await _unitOfWork.Repository<Domain.Entities.People.Employee>()
            .AnyAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, ct);
        if (!employee)
            return Result<long>.Failure(new Error("ATT.EMPLOYEE.NOTFOUND", "پرسنل یافت نشد"));

        var assignment = await _unitOfWork.Repository<EmployeeShiftAssignment>().AddAsync(
            new EmployeeShiftAssignment
            {
                EmployeeId = request.EmployeeId,
                ShiftId = request.ShiftId,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Recurrence = request.Recurrence,
                WeekDaysMask = request.WeekDaysMask == 0 ? 0b01111111 : request.WeekDaysMask,
                Priority = request.Priority,
                Notes = request.Notes,
                IsActive = true,
            }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(assignment.Id);
    }
}

/// <summary>فرمان پایان تخصیص</summary>
public record EndShiftAssignmentCommand(long AssignmentId, DateOnly? EndDate) : IRequest<Result<bool>>;

/// <summary>گرداننده پایان تخصیص — تاریخ پایان به امروز/تاریخ داده‌شده تنظیم می‌شود</summary>
public class EndShiftAssignmentCommandHandler : IRequestHandler<EndShiftAssignmentCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public EndShiftAssignmentCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<bool>> Handle(EndShiftAssignmentCommand request, CancellationToken ct)
    {
        var assignment = await _unitOfWork.Repository<EmployeeShiftAssignment>()
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, ct);
        if (assignment is null)
            return Result<bool>.Failure(new Error("ATT.ASSIGN.NOTFOUND", "تخصیص یافت نشد"));

        assignment.ToDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        _unitOfWork.Repository<EmployeeShiftAssignment>().Update(assignment);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════════════════════
// تعطیلات
// ═══════════════════════════════════════════════════════════════

/// <summary>فرمان ثبت تعطیلی</summary>
public record CreateHolidayCommand(
    DateOnly Date,
    string Title,
    HolidayType HolidayType,
    bool IsRecurring,
    TimeOnly? PartialStartTime,
    TimeOnly? PartialEndTime) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت تعطیلی</summary>
public class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateHolidayCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — تکرار همان تاریخ مجاز نیست</summary>
    public async Task<Result<long>> Handle(CreateHolidayCommand request, CancellationToken ct)
    {
        var exists = await _unitOfWork.Repository<Holiday>().QueryableNoTracking
            .AnyAsync(h => !h.IsDeleted && h.Date == request.Date
                        && h.HolidayType == request.HolidayType, ct);
        if (exists)
            return Result<long>.Failure(new Error("ATT.HOLIDAY.DUP",
                "برای این تاریخ تعطیلی هم‌نوع ثبت شده است"));

        var holiday = await _unitOfWork.Repository<Holiday>().AddAsync(new Holiday
        {
            Date = request.Date,
            Title = request.Title,
            HolidayType = request.HolidayType,
            IsRecurring = request.IsRecurring,
            PartialStartTime = request.PartialStartTime,
            PartialEndTime = request.PartialEndTime,
        }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(holiday.Id);
    }
}

/// <summary>فرمان حذف تعطیلی</summary>
public record DeleteHolidayCommand(long HolidayId) : IRequest<Result<bool>>;

/// <summary>گرداننده حذف تعطیلی</summary>
public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeleteHolidayCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<bool>> Handle(DeleteHolidayCommand request, CancellationToken ct)
    {
        var holiday = await _unitOfWork.Repository<Holiday>()
            .FirstOrDefaultAsync(h => h.Id == request.HolidayId, ct);
        if (holiday is null)
            return Result<bool>.Failure(new Error("ATT.HOLIDAY.NOTFOUND", "تعطیلی یافت نشد"));

        _unitOfWork.Repository<Holiday>().Remove(holiday);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}

// ═══════════════════════════════════════════════════════════════
// درخواست اصلاح کارت
// ═══════════════════════════════════════════════════════════════

/// <summary>فرمان ثبت درخواست اصلاح کارت</summary>
public record SubmitCorrectionCommand(
    long EmployeeId,
    DateOnly AttendanceDate,
    string FieldName,
    string? OldValue,
    string NewValue,
    string Reason) : IRequest<Result<long>>;

/// <summary>اعتبارسنج درخواست اصلاح</summary>
public class SubmitCorrectionCommandValidator : AbstractValidator<SubmitCorrectionCommand>
{
    /// <summary>قواعد — مقدار ساعت معتبر و فیلد مجاز</summary>
    public SubmitCorrectionCommandValidator()
    {
        RuleFor(x => x.FieldName)
            .Must(f => f is "CheckIn" or "CheckOut")
            .WithMessage("فیلد اصلاح فقط ورود یا خروج می‌تواند باشد");
        RuleFor(x => x.NewValue)
            .Matches(@"^([01]\d|2[0-3]):[0-5]\d$")
            .WithMessage("ساعت باید با قالب HH:MM باشد");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300)
            .WithMessage("دلیل اصلاح الزامی است");
    }
}

/// <summary>گرداننده ثبت درخواست اصلاح</summary>
public class SubmitCorrectionCommandHandler : IRequestHandler<SubmitCorrectionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SubmitCorrectionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — درخواست در انتظار تأیید سرپرست ثبت می‌شود</summary>
    public async Task<Result<long>> Handle(SubmitCorrectionCommand request, CancellationToken ct)
    {
        var pending = await _unitOfWork.Repository<AttendanceCorrection>().QueryableNoTracking
            .AnyAsync(c => !c.IsDeleted && c.EmployeeId == request.EmployeeId
                        && c.AttendanceDate == request.AttendanceDate
                        && c.FieldName == request.FieldName
                        && c.Status == CorrectionStatus.Pending, ct);
        if (pending)
            return Result<long>.Failure(new Error("ATT.CORR.DUP",
                "درخواست اصلاح در انتظاری برای این روز و فیلد وجود دارد"));

        var correction = await _unitOfWork.Repository<AttendanceCorrection>().AddAsync(
            new AttendanceCorrection
            {
                EmployeeId = request.EmployeeId,
                AttendanceDate = request.AttendanceDate,
                FieldName = request.FieldName,
                OldValue = request.OldValue,
                NewValue = request.NewValue,
                Reason = request.Reason,
                Status = CorrectionStatus.Pending,
            }, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<long>.Success(correction.Id);
    }
}

/// <summary>فرمان تصمیم درباره اصلاحیه — تأیید و اعمال یا رد</summary>
public record DecideCorrectionCommand(long CorrectionId, bool Approve, string? Note)
    : IRequest<Result<bool>>;

/// <summary>گرداننده تصمیم — در صورت تأیید، رکورد حضور اصلاح و پرچم ممیزی می‌شود</summary>
public class DecideCorrectionCommandHandler : IRequestHandler<DecideCorrectionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DecideCorrectionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<bool>> Handle(DecideCorrectionCommand request, CancellationToken ct)
    {
        var correction = await _unitOfWork.Repository<AttendanceCorrection>()
            .FirstOrDefaultAsync(c => c.Id == request.CorrectionId, ct);
        if (correction is null)
            return Result<bool>.Failure(new Error("ATT.CORR.NOTFOUND", "درخواست اصلاح یافت نشد"));

        if (correction.Status != CorrectionStatus.Pending)
            return Result<bool>.Failure(new Error("ATT.CORR.DECIDED",
                "این درخواست قبلاً تعیین تکلیف شده است"));

        correction.Status = request.Approve
            ? CorrectionStatus.Approved
            : CorrectionStatus.Rejected;
        correction.DecidedAt = DateTime.UtcNow;
        correction.ApproverNote = request.Note;

        if (request.Approve)
        {
            if (!TimeOnly.TryParse(correction.NewValue, out var newTime))
                return Result<bool>.Failure(new Error("ATT.CORR.BADVALUE",
                    "مقدار درخواستی قابل تفسیر نیست"));

            var record = await _unitOfWork.Repository<EmployeeAttendance>().QueryableTracking
                .FirstOrDefaultAsync(a => a.EmployeeId == correction.EmployeeId
                                       && a.AttendanceDate == correction.AttendanceDate, ct);

            if (record is null)
            {
                record = new EmployeeAttendance
                {
                    EmployeeId = correction.EmployeeId,
                    AttendanceDate = correction.AttendanceDate,
                };
                await _unitOfWork.Repository<EmployeeAttendance>().AddAsync(record, ct);
            }

            if (correction.FieldName == "CheckIn")
                record.CheckIn = newTime;
            else
                record.CheckOut = newTime;

            record.IsManuallyEdited = true;
            record.Notes = string.IsNullOrEmpty(record.Notes)
                ? $"اصلاحیه #{correction.Id}"
                : $"{record.Notes} | اصلاحیه #{correction.Id}";
            correction.AppliedAttendanceId = record.Id;
        }

        await _unitOfWork.SaveEntitiesAsync(ct);
        return Result<bool>.Success(true);
    }
}
