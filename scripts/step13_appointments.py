# -*- coding: utf-8 -*-
"""گام ۱۳ — ماژول نوبت‌دهی: CQRS کامل + کنترلر"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ DTOها ═══════════
write("DTOs/Appointments/AppointmentDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Appointments;

/// <summary>ردیف فهرست نوبت‌ها</summary>
public record AppointmentListDto(
    long Id,
    string? AppointmentCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    string Time,
    AppointmentStatus Status,
    AppointmentType Type,
    int QueueNumber);

/// <summary>جزئیات نوبت</summary>
public record AppointmentDetailDto(
    long Id,
    string? AppointmentCode,
    long PatientId,
    string PatientName,
    long DoctorId,
    string DoctorName,
    string? SpecialtyName,
    string DateShamsi,
    string Time,
    int Duration,
    AppointmentStatus Status,
    AppointmentType Type,
    int? QueueNumber,
    decimal EstimatedFee,
    bool IsFirstVisit,
    string? Notes);

/// <summary>درخواست ثبت نوبت</summary>
public record CreateAppointmentDto(
    long PatientId,
    long DoctorId,
    long? SpecialtyId,
    string DateShamsi,
    string Time,
    AppointmentType Type = AppointmentType.Initial,
    AppointmentSource Source = AppointmentSource.InPerson,
    bool IsFirstVisit = false,
    string? Notes = null);
""")

# ═══════════ Create ═══════════
write("Features/Appointments/Commands/CreateAppointment/CreateAppointmentCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.CreateAppointment;

/// <summary>
/// فرمان رزرو نوبت — کنترل ظرفیت پزشک و بررسی غیبت برنامه‌ریزی‌شده
/// </summary>
/// <param name="Dto">داده نوبت جدید</param>
public record CreateAppointmentCommand(CreateAppointmentDto Dto) : IRequest<Result<AppointmentDetailDto>>;

/// <summary>اعتبارسنج رزرو نوبت — با پیام فارسی</summary>
public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
        RuleFor(x => x.Dto.DateShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi).WithMessage("تاریخ نوبت شمسی نامعتبر است");
        RuleFor(x => x.Dto.Time)
            .Matches("^([01]?\\\\d|2[0-3]):[0-5]\\\\d$\").WithMessage("ساعت نوبت نامعتبر است — الگوی 09:30");
    }
}

/// <summary>گرداننده رزرو نوبت — ظرفیت و غیبت پزشک و تداخل زمان بیمار بررسی می‌شود</summary>
public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Result<AppointmentDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAppointmentCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateAppointmentCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان رزرو</summary>
    public async Task<Result<AppointmentDetailDto>> Handle(CreateAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<AppointmentDetailDto>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null || !doctor.IsActive)
            return Result<AppointmentDetailDto>.Failure(Error.NotFound("پزشک", dto.DoctorId));

        var gregorianDate = PersianDateHelper.ToGregorian(dto.DateShamsi);
        if (gregorianDate is null || gregorianDate.Value.Date < DateTime.UtcNow.Date)
            return Result<AppointmentDetailDto>.Failure(
                new Error("INVALID_DATE", "تاریخ نوبت نمی‌تواند در گذشته باشد"));

        var date = DateOnly.FromDateTime(gregorianDate.Value);
        if (!TimeOnly.TryParse(dto.Time, out var time))
            return Result<AppointmentDetailDto>.Failure(
                new Error("INVALID_TIME", "ساعت نوبت نامعتبر است"));

        // بررسی غیبت برنامه‌ریزی‌شده پزشک در آن روز
        var doctorAbsent = await _unitOfWork.Repository<DoctorAbsence>()
            .AnyAsync(a => a.DoctorId == dto.DoctorId && a.AbsenceDate == date, cancellationToken);
        if (doctorAbsent)
            return Result<AppointmentDetailDto>.Failure(
                new Error("DOCTOR_ABSENT", "پزشک در این تاریخ مرخصی دارد"));

        // کنترل ظرفیت — سقف بیمار در هر شیفت
        var appointmentRepo = (IAppointmentRepository)_unitOfWork.Repository<Appointment>();
        var activeCount = await appointmentRepo.CountActiveByDoctorAndDateAsync(
            dto.DoctorId, date, cancellationToken);
        if (doctor.MaxPatientsPerShift.HasValue && activeCount >= doctor.MaxPatientsPerShift.Value)
            return Result<AppointmentDetailDto>.Failure(
                new Error("CAPACITY_FULL", "ظرفیت نوبت این پزشک در این تاریخ تکمیل است"));

        // تداخل زمانی بیمار — نوبت دیگر در همان ساعت
        var patientBusy = await _unitOfWork.Repository<Appointment>().AnyAsync(
            a => a.PatientId == dto.PatientId
                 && a.AppointmentDate == date
                 && a.AppointmentTime == time
                 && a.Status != AppointmentStatus.Cancelled
                 && a.Status != AppointmentStatus.NoShow,
            cancellationToken);
        if (patientBusy)
            return Result<AppointmentDetailDto>.Failure(
                new Error("PATIENT_CONFLICT", "بیمار در این ساعت نوبت دیگری دارد"));

        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            SpecialtyId = dto.SpecialtyId ?? doctor.SpecialtyId,
            AppointmentDate = date,
            AppointmentDateShamsi = dto.DateShamsi,
            AppointmentTime = time,
            Duration = doctor.VisitDuration,
            Status = AppointmentStatus.Booked,
            AppointmentType = dto.Type,
            Source = dto.Source,
            IsFirstVisit = dto.IsFirstVisit,
            Priority = Priority.Normal,
            EstimatedFee = doctor.ConsultationFee,
            QueueNumber = activeCount + 1,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Appointment>().AddAsync(appointment, cancellationToken);

        // رویداد دامنه — پیامک تأیید نوبت
        appointment.AddDomainEvent(new AppointmentBookedEvent(
            appointment.Id, dto.PatientId, patient.Mobile));

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("نوبت {Code} برای بیمار {PatientId} با پزشک {DoctorId} ثبت شد",
            appointment.AppointmentCode, dto.PatientId, dto.DoctorId);

        return Result<AppointmentDetailDto>.Success(new AppointmentDetailDto(
            appointment.Id, appointment.AppointmentCode, dto.PatientId,
            $"{patient.FirstName} {patient.LastName}", dto.DoctorId,
            "پزشک", null, dto.DateShamsi, dto.Time, appointment.Duration,
            appointment.Status, appointment.AppointmentType, appointment.QueueNumber,
            appointment.EstimatedFee, appointment.IsFirstVisit, appointment.Notes));
    }
}
""")

# ═══════════ Cancel / Confirm ═══════════
write("Features/Appointments/Commands/ChangeAppointmentStatus/ChangeAppointmentStatusCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;

/// <summary>
/// فرمان تغییر وضعیت نوبت — تأیید، لغو، حضور، تکمیل
/// </summary>
/// <param name="Id">شناسه نوبت</param>
/// <param name="NewStatus">وضعیت جدید</param>
/// <param name="Reason">دلیل لغو — الزامی هنگام لغو</param>
public record ChangeAppointmentStatusCommand(long Id, AppointmentStatus NewStatus, string? Reason = null)
    : IRequest<Result>;

/// <summary>اعتبارسنج تغییر وضعیت</summary>
public class ChangeAppointmentStatusCommandValidator : AbstractValidator<ChangeAppointmentStatusCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public ChangeAppointmentStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("شناسه نوبت نامعتبر است");
        RuleFor(x => x.Reason).NotEmpty()
            .When(x => x.NewStatus == AppointmentStatus.Cancelled)
            .WithMessage("دلیل لغو نوبت الزامی است");
    }
}

/// <summary>گرداننده تغییر وضعیت نوبت</summary>
public class ChangeAppointmentStatusCommandHandler : IRequestHandler<ChangeAppointmentStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ChangeAppointmentStatusCommandHandler> _logger;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ChangeAppointmentStatusCommandHandler(IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<ChangeAppointmentStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>اجرای تغییر وضعیت</summary>
    public async Task<Result> Handle(ChangeAppointmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Repository<Appointment>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (appointment is null)
            return Result.Failure(Error.NotFound("نوبت", request.Id));

        if (appointment.Status == AppointmentStatus.Completed)
            return Result.Failure(new Error("ALREADY_COMPLETED", "نوبت تکمیل‌شده قابل تغییر نیست"));

        var patient = await _unitOfWork.Repository<Patient>()
            .GetByIdAsync(appointment.PatientId, cancellationToken);

        if (request.NewStatus == AppointmentStatus.Cancelled)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancellationReason = request.Reason;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancelledBy = _currentUser.UserId;

            if (patient is not null)
                appointment.AddDomainEvent(new AppointmentCancelledEvent(
                    appointment.Id, patient.Id, request.Reason ?? string.Empty));
        }
        else
        {
            appointment.Status = request.NewStatus;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("وضعیت نوبت {Id} به {Status} تغییر یافت",
            request.Id, request.NewStatus);
        return Result.Success();
    }
}

/// <summary>پوشش سرویس کاربر فعلی برای جلوگیری از وابستگی مستقیم به Application.Interfaces</summary>
public interface ICurrentUserService
{
    /// <summary>شناسه کاربر فعلی</summary>
    long? UserId { get; }
}
""")

# ═══════════ Query فهرست روزانه ═══════════
write("Features/Appointments/Queries/GetAppointmentsByDate/GetAppointmentsByDateQuery.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;

/// <summary>
/// پرس‌وجوی نوبت‌های یک تاریخ — با فیلتر اختیاری پزشک و وضعیت
/// </summary>
/// <param name="DateShamsi">تاریخ شمسی مثل 1403/07/12 — تهی یعنی امروز</param>
/// <param name="DoctorId">فیلتر پزشک</param>
/// <param name="Status">فیلتر وضعیت</param>
public record GetAppointmentsByDateQuery(
    string? DateShamsi = null,
    long? DoctorId = null,
    Domain.Enums.AppointmentStatus? Status = null) : IRequest<Result<List<AppointmentListDto>>>;

/// <summary>گرداننده نوبت‌های روز — مرتب بر اساس ساعت</summary>
public class GetAppointmentsByDateQueryHandler
    : IRequestHandler<GetAppointmentsByDateQuery, Result<List<AppointmentListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetAppointmentsByDateQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<AppointmentListDto>>> Handle(GetAppointmentsByDateQuery request,
        CancellationToken cancellationToken)
    {
        var date = PersianDateHelper.ToGregorian(request.DateShamsi)?.Date
                   ?? DateTime.UtcNow.Date;

        IQueryable<Appointment> query = _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Employee)
            .Where(a => a.AppointmentDate == DateOnly.FromDateTime(date));

        if (request.DoctorId.HasValue)
            query = query.Where(a => a.DoctorId == request.DoctorId.Value);
        if (request.Status.HasValue)
            query = query.Where(a => a.Status == request.Status.Value);

        var items = await query
            .OrderBy(a => a.AppointmentTime)
            .Select(a => new AppointmentListDto(
                a.Id,
                a.AppointmentCode,
                a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "",
                a.Doctor != null && a.Doctor.Employee != null
                    ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName
                    : "پزشک",
                a.AppointmentDateShamsi ?? string.Empty,
                a.AppointmentTime.ToString("HH:mm"),
                a.Status,
                a.AppointmentType,
                a.QueueNumber ?? 0))
            .ToListAsync(cancellationToken);

        return Result<List<AppointmentListDto>>.Success(items);
    }
}
""")

# ═══════════ کنترلر ═══════════
write("Controllers/AppointmentsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Appointments;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.CreateAppointment;
using Sahand.ClinicPro.Application.Features.Appointments.Queries.GetAppointmentsByDate;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نوبت‌دهی — رزرو و مدیریت نوبت‌ها
/// مسیرها: /api/v1/appointments
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public class AppointmentsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>نوبت‌های یک تاریخ — با فیلتر پزشک و وضعیت</summary>
    /// <param name="date">تاریخ شمسی — تهی یعنی امروز</param>
    /// <param name="doctorId">فیلتر پزشک</param>
    /// <param name="status">فیلتر وضعیت</param>
    [HttpGet]
    [HasPermission("appointments.view")]
    public async Task<IActionResult> GetByDate([FromQuery] string? date,
        [FromQuery] long? doctorId, [FromQuery] AppointmentStatus? status)
    {
        var result = await _mediator.Send(new GetAppointmentsByDateQuery(date, doctorId, status));
        return ToResponse(result);
    }

    /// <summary>رزرو نوبت جدید — با کنترل ظرفیت و تداخل</summary>
    /// <param name="dto">داده نوبت</param>
    [HttpPost]
    [HasPermission("appointments.create")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetByDate), new { }, "نوبت با موفقیت ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تغییر وضعیت نوبت — تأیید یا لغو یا حضور یا تکمیل</summary>
    /// <param name="id">شناسه نوبت</param>
    /// <param name="dto">وضعیت جدید و دلیل</param>
    [HttpPut("{id:long}/status")]
    [HasPermission("appointments.edit")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] ChangeStatusDto dto)
    {
        var result = await _mediator.Send(new ChangeAppointmentStatusCommand(id, dto.Status, dto.Reason));
        return ToResponse(result, "وضعیت نوبت تغییر یافت");
    }

    /// <summary>مدل درخواست تغییر وضعیت</summary>
    public record ChangeStatusDto(AppointmentStatus Status, string? Reason);
}
""", API)

print("✅ گام ۱۳ — ماژول Appointments کامل شد")
