# -*- coding: utf-8 -*-
"""گام‌های ۱۴ تا ۱۶ — پذیرش + ویزیت + نسخه‌نویسی: CQRS + کنترلرها"""
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


# ═══════════════════ گام ۱۴: پذیرش ═══════════════════
write("DTOs/Receptions/ReceptionDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Receptions;

/// <summary>ردیف فهرست پذیرش روز</summary>
public record ReceptionListDto(
    long Id,
    string? ReceptionNumber,
    string PatientName,
    string DoctorName,
    string TimeShamsi,
    ReceptionStatus Status,
    string? InsuranceName,
    int? WaitingTime);

/// <summary>درخواست ثبت پذیرش</summary>
public record CreateReceptionDto(
    long PatientId,
    long DoctorId,
    long? AppointmentId,
    long? InsuranceCompanyId,
    string? ReferralCode,
    string? Notes);
""")

write("Features/Receptions/CreateReception/CreateReceptionCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;

/// <summary>
/// فرمان پذیرش بیمار — از نوبت یا مراجعه حضوری بدون نوبت
/// </summary>
/// <param name="Dto">داده پذیرش</param>
/// <param name="ReceptionistId">شناسه منشی پذیرش‌کننده — از کاربر فعلی پر می‌شود</param>
public record CreateReceptionCommand(CreateReceptionDto Dto, long? ReceptionistId = null)
    : IRequest<Result<long>>;

/// <summary>اعتبارسنج پذیرش</summary>
public class CreateReceptionCommandValidator : AbstractValidator<CreateReceptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateReceptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("انتخاب بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("انتخاب پزشک الزامی است");
    }
}

/// <summary>گرداننده ثبت پذیرش — ورود بیمار به جریان ویزیت روز</summary>
public class CreateReceptionCommandHandler : IRequestHandler<CreateReceptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateReceptionCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateReceptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreateReceptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای فرمان پذیرش</summary>
    public async Task<Result<long>> Handle(CreateReceptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));

        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null)
            return Result<long>.Failure(Error.NotFound("پزشک", dto.DoctorId));

        var now = DateTime.UtcNow;
        var reception = new Reception
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            InsuranceCompanyId = dto.InsuranceCompanyId ?? patient.InsuranceCompanyId,
            ReferralCode = dto.ReferralCode,
            ReceptionDate = now,
            ReceptionDateShamsi = PersianDateHelper.ToShamsi(now),
            Status = ReceptionStatus.Admitted,
            ReceptionistId = request.ReceptionistId,
            EnterTime = now,
            Notes = dto.Notes
        };

        await _unitOfWork.Repository<Reception>().AddAsync(reception, cancellationToken);

        // اگر از نوبت است — وضعیت نوبت به حاضر تغییر می‌کند
        if (dto.AppointmentId.HasValue)
        {
            var appointment = await _unitOfWork.Repository<Appointment>()
                .GetByIdAsync(dto.AppointmentId.Value, cancellationToken);
            if (appointment is not null)
                appointment.Status = AppointmentStatus.Arrived;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("پذیرش {Number} برای بیمار {PatientId} ثبت شد",
            reception.ReceptionNumber, dto.PatientId);

        return Result<long>.Success(reception.Id);
    }
}
""")

write("Features/Receptions/GetTodayReceptions/GetTodayReceptionsQuery.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Queries.GetTodayReceptions;

/// <summary>
/// پرس‌وجوی پذیرش‌های امروز — صفحه پذیرش فعال
/// </summary>
/// <param name="DoctorId">فیلتر پزشک — اختیاری</param>
public record GetTodayReceptionsQuery(long? DoctorId = null) : IRequest<Result<List<ReceptionListDto>>>;

/// <summary>گرداننده پذیرش‌های روز</summary>
public class GetTodayReceptionsQueryHandler
    : IRequestHandler<GetTodayReceptionsQuery, Result<List<ReceptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetTodayReceptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<ReceptionListDto>>> Handle(GetTodayReceptionsQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IQueryable<Reception> query = _unitOfWork.Repository<Reception>()
            .QueryableNoTracking
            .Include(r => r.Patient)
            .Include(r => r.Doctor).ThenInclude(d => d!.Employee)
            .Include(r => r.InsuranceCompany)
            .Where(r => r.ReceptionDate != null
                     && DateOnly.FromDateTime(r.ReceptionDate) == today);

        if (request.DoctorId.HasValue)
            query = query.Where(r => r.DoctorId == request.DoctorId.Value);

        var items = await query
            .OrderBy(r => r.ReceptionDate)
            .Select(r => new ReceptionListDto(
                r.Id,
                r.ReceptionNumber,
                r.Patient != null ? r.Patient.FirstName + " " + r.Patient.LastName : "",
                r.Doctor != null && r.Doctor.Employee != null
                    ? r.Doctor.Employee.FirstName + " " + r.Doctor.Employee.LastName : "پزشک",
                r.ReceptionDate.ToString("HH:mm"),
                r.Status,
                r.InsuranceCompany != null ? r.InsuranceCompany.Name : null,
                r.WaitingTime))
            .ToListAsync(cancellationToken);

        return Result<List<ReceptionListDto>>.Success(items);
    }
}
""")

write("Controllers/ReceptionsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.UpdateReceptionStatus;
using Sahand.ClinicPro.Application.Features.Receptions.Queries.GetTodayReceptions;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پذیرش — ثبت و مشاهده پذیرش‌های روز
/// مسیرها: /api/v1/receptions
/// </summary>
[ApiController]
[Route("api/v1/receptions")]
[Authorize]
public class ReceptionsController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ReceptionsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>پذیرش‌های امروز</summary>
    /// <param name="doctorId">فیلتر پزشک</param>
    [HttpGet("today")]
    [HasPermission("receptions.view")]
    public async Task<IActionResult> GetToday([FromQuery] long? doctorId)
    {
        var result = await _mediator.Send(new GetTodayReceptionsQuery(doctorId));
        return ToResponse(result);
    }

    /// <summary>ثبت پذیرش بیمار</summary>
    /// <param name="dto">داده پذیرش</param>
    [HttpPost]
    [HasPermission("receptions.create")]
    public async Task<IActionResult> Create([FromBody] CreateReceptionDto dto)
    {
        var result = await _mediator.Send(new CreateReceptionCommand(dto, _currentUser.UserId));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetToday), new { }, "پذیرش با موفقیت ثبت شد");
        return ToResponse(result);
    }

    /// <summary>تغییر وضعیت پذیرش — در صف، در ویزیت، ترخیص</summary>
    /// <param name="id">شناسه پذیرش</param>
    /// <param name="dto">وضعیت جدید</param>
    [HttpPut("{id:long}/status")]
    [HasPermission("receptions.edit")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusDto dto)
    {
        var reception = await _mediator.Send(new UpdateReceptionStatusCommand(id, dto.Status));
        return ToResponse(reception, "وضعیت پذیرش تغییر یافت");
    }

    /// <summary>مدل تغییر وضعیت</summary>
    public record UpdateStatusDto(ReceptionStatus Status);
}
""", API)

write("Features/Receptions/UpdateReceptionStatus/UpdateReceptionStatusCommand.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Receptions.Commands.UpdateReceptionStatus;

/// <summary>
/// فرمان تغییر وضعیت پذیرش — در صف، در ویزیت، ویزیت‌شده، ترخیص، لغو
/// </summary>
/// <param name="Id">شناسه پذیرش</param>
/// <param name="Status">وضعیت جدید</param>
public record UpdateReceptionStatusCommand(long Id, ReceptionStatus Status) : IRequest<Result>;

/// <summary>گرداننده تغییر وضعیت پذیرش — همراه محاسبه زمان انتظار هنگام ترخیص</summary>
public class UpdateReceptionStatusCommandHandler
    : IRequestHandler<UpdateReceptionStatusCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateReceptionStatusCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای تغییر وضعیت</summary>
    public async Task<Result> Handle(UpdateReceptionStatusCommand request,
        CancellationToken cancellationToken)
    {
        var reception = await _unitOfWork.Repository<Reception>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (reception is null)
            return Result.Failure(Error.NotFound("پذیرش", request.Id));

        reception.Status = request.Status;

        // محاسبه زمان انتظار و ساعت خروج هنگام ترخیص
        if (request.Status == ReceptionStatus.Discharged)
        {
            reception.ExitTime = DateTime.UtcNow;
            if (reception.EnterTime.HasValue)
            {
                reception.WaitingTime = (int)(reception.ExitTime.Value - reception.EnterTime.Value).TotalMinutes;
            }
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
""")

# ═══════════════════ گام ۱۵: ویزیت ═══════════════════
write("DTOs/Visits/VisitDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Visits;

/// <summary>ردیف فهرست ویزیت‌ها</summary>
public record VisitListDto(
    long Id,
    string? VisitCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    VisitStatus Status,
    string? ChiefComplaint);

/// <summary>جزئیات کامل ویزیت با الگوی SOAP</summary>
public record VisitDetailDto(
    long Id,
    string? VisitCode,
    long PatientId,
    long DoctorId,
    string DateShamsi,
    VisitType Type,
    VisitStatus Status,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PastMedicalHistory,
    string? PhysicalExamination,
    string? SoapSubjective,
    string? SoapObjective,
    string? SoapAssessment,
    string? SoapPlan,
    long? PrimaryDiagnosisId,
    string? PrimaryDiagnosisCode,
    string? TreatmentPlan,
    string? FollowUpDateShamsi,
    decimal VisitFee);

/// <summary>درخواست شروع یا ذخیره ویزیت</summary>
public record SaveVisitDto(
    long? Id,
    long PatientId,
    long DoctorId,
    long? ReceptionId,
    VisitType Type = VisitType.Initial,
    string? ChiefComplaint = null,
    string? HistoryOfPresentIllness = null,
    string? PastMedicalHistory = null,
    string? PhysicalExamination = null,
    string? SoapSubjective = null,
    string? SoapObjective = null,
    string? SoapAssessment = null,
    string? SoapPlan = null,
    long? PrimaryDiagnosisId = null,
    string? TreatmentPlan = null,
    string? FollowUpDateShamsi = null,
    bool Complete = false);
""")

write("Features/Visits/SaveVisit/SaveVisitCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;

/// <summary>
/// فرمان ذخیره یا تکمیل ویزیت — ذخیره تدریجی SOAP و ثبت تشخیص ICD-10
/// </summary>
/// <param name="Dto">داده ویزیت</param>
public record SaveVisitCommand(SaveVisitDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ویزیت</summary>
public class SaveVisitCommandValidator : AbstractValidator<SaveVisitCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public SaveVisitCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("شناسه پزشک الزامی است");
        RuleFor(x => x.Dto.ChiefComplaint).MaximumLength(2000)
            .WithMessage("شکایت اصلی حداکثر ۲۰۰۰ کاراکتر است");
        RuleFor(x => x.Dto.FollowUpDateShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi)
            .WithMessage("تاریخ مراجعه بعدی شمسی نامعتبر است");
        RuleFor(x => x.Dto.PrimaryDiagnosisId)
            .NotNull().When(x => x.Dto.Complete)
            .WithMessage("برای تکمیل ویزیت ثبت تشخیص اصلی الزامی است");
    }
}

/// <summary>گرداننده ذخیره ویزیت — ساخت جدید یا ویرایش پیش‌نویس و انتشار رویداد تکمیل</summary>
public class SaveVisitCommandHandler : IRequestHandler<SaveVisitCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveVisitCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SaveVisitCommandHandler(IUnitOfWork unitOfWork, ILogger<SaveVisitCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای ذخیره ویزیت</summary>
    public async Task<Result<long>> Handle(SaveVisitCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var repo = _unitOfWork.Repository<Domain.Entities.Clinical.Visit>();

        Domain.Entities.Clinical.Visit visit;
        if (dto.Id.HasValue)
        {
            visit = await repo.GetByIdAsync(dto.Id.Value, cancellationToken);
            if (visit is null)
                return Result<long>.Failure(Error.NotFound("ویزیت", dto.Id.Value));
        }
        else
        {
            var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
            visit = new Domain.Entities.Clinical.Visit
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                ReceptionId = dto.ReceptionId,
                VisitDate = DateTime.UtcNow,
                VisitDateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow),
                VisitType = dto.Type,
                Status = VisitStatus.Started,
                StartTime = DateTime.UtcNow,
                VisitFee = doctor?.ConsultationFee ?? 0
            };
            await repo.AddAsync(visit, cancellationToken);
        }

        // اعمال فیلدهای بالینی
        visit.ChiefComplaint = dto.ChiefComplaint;
        visit.HistoryOfPresentIllness = dto.HistoryOfPresentIllness;
        visit.PastMedicalHistory = dto.PastMedicalHistory;
        visit.PhysicalExamination = dto.PhysicalExamination;
        visit.SoapSubjective = dto.SoapSubjective;
        visit.SoapObjective = dto.SoapObjective;
        visit.SoapAssessment = dto.SoapAssessment;
        visit.SoapPlan = dto.SoapPlan;
        visit.PrimaryDiagnosisId = dto.PrimaryDiagnosisId;
        visit.TreatmentPlan = dto.TreatmentPlan;

        if (dto.FollowUpDateShamsi is not null
            && PersianDateHelper.ToGregorian(dto.FollowUpDateShamsi) is { } followUp)
            visit.FollowUpDate = DateOnly.FromDateTime(followUp);

        if (dto.Complete)
        {
            visit.Status = VisitStatus.Completed;
            visit.EndTime = DateTime.UtcNow;
            visit.Duration = (int?)((visit.EndTime - visit.StartTime)?.TotalMinutes ?? 0);

            // وضعیت پذیرش مرتبط به ویزیت‌شده تغییر می‌کند
            if (visit.ReceptionId.HasValue)
            {
                var reception = await _unitOfWork.Repository<Reception>()
                    .GetByIdAsync(visit.ReceptionId.Value, cancellationToken);
                if (reception is not null)
                    reception.Status = ReceptionStatus.Visited;
            }

            // به‌روزرسانی آخرین مراجعه بیمار
            var patient = await _unitOfWork.Repository<Patient>()
                .GetByIdAsync(dto.PatientId, cancellationToken);
            if (patient is not null)
                patient.LastVisitDate = DateTime.UtcNow;

            visit.AddDomainEvent(new VisitCompletedEvent(visit.Id, dto.PatientId, dto.DoctorId));
        }
        else
        {
            visit.Status = VisitStatus.Draft;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("ویزیت {Code} ذخیره شد — وضعیت {Status}",
            visit.VisitCode, visit.Status);
        return Result<long>.Success(visit.Id);
    }
}
""")

write("Features/Visits/GetVisits/GetVisitsQueries.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Visits.Queries;

/// <summary>
/// پرس‌وجوی ویزیت‌های یک بیمار — تاریخچه پزشکی
/// </summary>
/// <param name="PatientId">شناسه بیمار</param>
public record GetPatientVisitsQuery(long PatientId) : IRequest<Result<List<VisitListDto>>>;

/// <summary>گرداننده تاریخچه ویزیت بیمار — جدیدترین اول</summary>
public class GetPatientVisitsQueryHandler
    : IRequestHandler<GetPatientVisitsQuery, Result<List<VisitListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetPatientVisitsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<VisitListDto>>> Handle(GetPatientVisitsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .Include(v => v.Patient)
            .Include(v => v.Doctor).ThenInclude(d => d!.Employee)
            .Where(v => v.PatientId == request.PatientId)
            .OrderByDescending(v => v.VisitDate)
            .Select(v => new VisitListDto(
                v.Id,
                v.VisitCode,
                v.Patient != null ? v.Patient.FirstName + " " + v.Patient.LastName : "",
                v.Doctor != null && v.Doctor.Employee != null
                    ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName : "پزشک",
                v.VisitDateShamsi ?? string.Empty,
                v.Status,
                v.ChiefComplaint))
            .ToListAsync(cancellationToken);

        return Result<List<VisitListDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی جزئیات یک ویزیت با تشخیص
/// </summary>
/// <param name="Id">شناسه ویزیت</param>
public record GetVisitByIdQuery(long Id) : IRequest<Result<VisitDetailDto>>;

/// <summary>گرداننده جزئیات ویزیت</summary>
public class GetVisitByIdQueryHandler : IRequestHandler<GetVisitByIdQuery, Result<VisitDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetVisitByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<VisitDetailDto>> Handle(GetVisitByIdQuery request,
        CancellationToken cancellationToken)
    {
        var v = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .Include(x => x.PrimaryDiagnosis)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (v is null)
            return Result<VisitDetailDto>.Failure(Error.NotFound("ویزیت", request.Id));

        var dto = new VisitDetailDto(
            v.Id, v.VisitCode, v.PatientId, v.DoctorId,
            v.VisitDateShamsi ?? string.Empty,
            v.VisitType, v.Status,
            v.ChiefComplaint, v.HistoryOfPresentIllness, v.PastMedicalHistory,
            v.PhysicalExamination,
            v.SoapSubjective, v.SoapObjective, v.SoapAssessment, v.SoapPlan,
            v.PrimaryDiagnosisId, v.PrimaryDiagnosis?.Code,
            v.TreatmentPlan,
            v.FollowUpDate.HasValue ? PersianDateHelper.ToShamsi(v.FollowUpDate.Value.ToDateTime(TimeOnly.MinValue)) : null,
            v.VisitFee);

        return Result<VisitDetailDto>.Success(dto);
    }
}
""")

write("Controllers/VisitsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;
using Sahand.ClinicPro.Application.Features.Visits.Queries;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر ویزیت — پرونده بالینی با الگوی SOAP
/// مسیرها: /api/v1/visits
/// </summary>
[ApiController]
[Route("api/v1/visits")]
[Authorize]
public class VisitsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public VisitsController(IMediator mediator) => _mediator = mediator;

    /// <summary>تاریخچه ویزیت‌های یک بیمار</summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("patient/{patientId:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetByPatient(long patientId)
    {
        var result = await _mediator.Send(new GetPatientVisitsQuery(patientId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل یک ویزیت</summary>
    /// <param name="id">شناسه ویزیت</param>
    [HttpGet("{id:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetVisitByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>ذخیره یا تکمیل ویزیت — ذخیره تدریجی SOAP پشتیبانی می‌شود</summary>
    /// <param name="dto">داده ویزیت</param>
    [HttpPost]
    [HasPermission("visits.create")]
    public async Task<IActionResult> Save([FromBody] SaveVisitDto dto)
    {
        var result = await _mediator.Send(new SaveVisitCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "ویزیت ذخیره شد");
        return ToResponse(result);
    }
}
""", API)

# ═══════════════════ گام ۱۶: نسخه ═══════════════════
write("DTOs/Prescriptions/PrescriptionDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Prescriptions;

/// <summary>ردیف فهرست نسخه</summary>
public record PrescriptionListDto(
    long Id,
    string? PrescriptionCode,
    string PatientName,
    string DoctorName,
    string DateShamsi,
    PrescriptionType Type,
    PrescriptionStatus Status,
    bool IsElectronic);

/// <summary>قلم نسخه برای ثبت</summary>
public record PrescriptionItemDto(
    long MedicationId,
    int OrderIndex,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? Route,
    decimal Quantity,
    string? Unit,
    string? Instructions,
    bool IsSos = false);

/// <summary>درخواست ثبت نسخه</summary>
public record CreatePrescriptionDto(
    long PatientId,
    long DoctorId,
    long? VisitId,
    PrescriptionType Type = PrescriptionType.Medication,
    bool IsElectronic = false,
    InsuranceSystemType? InsuranceSystemType = null,
    bool IsNarcotic = false,
    string? Notes = null,
    List<PrescriptionItemDto> Items = null!);

/// <summary>قلم نسخه در پاسخ</summary>
public record PrescriptionItemDetailDto(
    long MedicationId,
    string MedicationName,
    int OrderIndex,
    string? Dosage,
    string? Frequency,
    string? Duration,
    decimal Quantity,
    string? Unit,
    string? Instructions);

/// <summary>جزئیات کامل نسخه</summary>
public record PrescriptionDetailDto(
    long Id,
    string? PrescriptionCode,
    long PatientId,
    long DoctorId,
    long? VisitId,
    string DateShamsi,
    PrescriptionType Type,
    PrescriptionStatus Status,
    bool IsElectronic,
    bool IsNarcotic,
    string? Notes,
    List<PrescriptionItemDetailDto> Items);
""")

write("Features/Prescriptions/CreatePrescription/CreatePrescriptionCommand.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Events;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.Commands.CreatePrescription;

/// <summary>
/// فرمان صدور نسخه — با بررسی تداخل دارویی و اعتبارسنجی اقلام
/// </summary>
/// <param name="Dto">داده نسخه</param>
public record CreatePrescriptionCommand(CreatePrescriptionDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج صدور نسخه</summary>
public class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Dto.PatientId).GreaterThan(0).WithMessage("شناسه بیمار الزامی است");
        RuleFor(x => x.Dto.DoctorId).GreaterThan(0).WithMessage("شناسه پزشک الزامی است");
        RuleFor(x => x.Dto.Items)
            .NotEmpty().WithMessage("نسخه باید حداقل یک قلم دارو داشته باشد");
        RuleForEach(x => x.Dto.Items).SetValidator(new PrescriptionItemDtoValidator());
    }
}

/// <summary>اعتبارسنج قلم نسخه</summary>
public class PrescriptionItemDtoValidator : AbstractValidator<PrescriptionItemDto>
{
    /// <summary>قواعد اعتبارسنجی قلم</summary>
    public PrescriptionItemDtoValidator()
    {
        RuleFor(x => x.MedicationId).GreaterThan(0).WithMessage("انتخاب دارو الزامی است");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("تعداد باید بیشتر از صفر باشد");
        RuleFor(x => x.Dosage).MaximumLength(100).WithMessage("دوز حداکثر ۱۰۰ کاراکتر است");
    }
}

/// <summary>گرداننده صدور نسخه — بررسی تداخل شدید قبل از ثبت</summary>
public class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePrescriptionCommandHandler> _logger;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreatePrescriptionCommandHandler(IUnitOfWork unitOfWork,
        ILogger<CreatePrescriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>اجرای صدور نسخه</summary>
    public async Task<Result<long>> Handle(CreatePrescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // بررسی تداخل دارویی جفت‌ها — تداخل ممنوع رد می‌شود
        var medIds = dto.Items.Select(i => i.MedicationId).Distinct().ToList();
        var dangerousInteractions = await _unitOfWork.Repository<DrugInteraction>()
            .QueryableNoTracking
            .Where(x => medIds.Contains(x.Medication1Id) && medIds.Contains(x.Medication2Id))
            .ToListAsync(cancellationToken);

        if (dangerousInteractions.Any(x => x.Severity == DrugInteractionSeverity.Contraindicated))
        {
            var interaction = dangerousInteractions
                .First(x => x.Severity == DrugInteractionSeverity.Contraindicated);
            return Result<long>.Failure(new Error("DRUG_CONTRAINDICATION",
                $"تداخل ممنوع مصرف بین داروهای نسخه شناسایی شد — {interaction.Description}"));
        }

        var now = DateTime.UtcNow;
        var prescription = new Prescription
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            VisitId = dto.VisitId,
            PrescriptionDate = now,
            Type = dto.Type,
            IsElectronic = dto.IsElectronic,
            InsuranceSystemType = dto.InsuranceSystemType,
            IsNarcotic = dto.IsNarcotic,
            Status = PrescriptionStatus.Issued,
            Notes = dto.Notes,
            ValidUntil = DateOnly.FromDateTime(now.AddDays(dto.IsNarcotic ? 3 : 30))
        };

        foreach (var item in dto.Items)
        {
            prescription.Items.Add(new PrescriptionItem
            {
                MedicationId = item.MedicationId,
                OrderIndex = item.OrderIndex,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Duration = item.Duration,
                Route = item.Route,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Instructions = item.Instructions,
                IsSos = item.IsSos
            });
        }

        await _unitOfWork.Repository<Prescription>().AddAsync(prescription, cancellationToken);

        prescription.AddDomainEvent(new PrescriptionCreatedEvent(
            prescription.Id, dto.PatientId, dto.DoctorId, dto.IsElectronic));

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        _logger.LogInformation("نسخه {Code} با {Count} قلم صادر شد",
            prescription.PrescriptionCode, dto.Items.Count);

        return Result<long>.Success(prescription.Id);
    }
}
""")

write("Features/Prescriptions/GetPrescriptions/GetPrescriptionsQueries.cs", """
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.Queries;

/// <summary>
/// پرس‌وجوی نسخه‌های یک ویزیت
/// </summary>
/// <param name="VisitId">شناسه ویزیت</param>
public record GetVisitPrescriptionsQuery(long VisitId) : IRequest<Result<List<PrescriptionListDto>>>;

/// <summary>گرداننده نسخه‌های ویزیت</summary>
public class GetVisitPrescriptionsQueryHandler
    : IRequestHandler<GetVisitPrescriptionsQuery, Result<List<PrescriptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetVisitPrescriptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<PrescriptionListDto>>> Handle(GetVisitPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            .Include(p => p.Patient)
            .Include(p => p.Doctor).ThenInclude(d => d!.Employee)
            .Where(p => p.VisitId == request.VisitId)
            .OrderByDescending(p => p.PrescriptionDate)
            .Select(p => new PrescriptionListDto(
                p.Id,
                p.PrescriptionCode,
                p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "",
                p.Doctor != null && p.Doctor.Employee != null
                    ? p.Doctor.Employee.FirstName + " " + p.Doctor.Employee.LastName : "پزشک",
                p.PrescriptionDate.HasValue
                    ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(p.PrescriptionDate.Value)
                    : string.Empty,
                p.Type, p.Status, p.IsElectronic))
            .ToListAsync(cancellationToken);

        return Result<List<PrescriptionListDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی جزئیات کامل نسخه با اقلام
/// </summary>
/// <param name="Id">شناسه نسخه</param>
public record GetPrescriptionByIdQuery(long Id) : IRequest<Result<PrescriptionDetailDto>>;

/// <summary>گرداننده جزئیات نسخه</summary>
public class GetPrescriptionByIdQueryHandler
    : IRequestHandler<GetPrescriptionByIdQuery, Result<PrescriptionDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetPrescriptionByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PrescriptionDetailDto>> Handle(GetPrescriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var p = await _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            .Include(x => x.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (p is null)
            return Result<PrescriptionDetailDto>.Failure(Error.NotFound("نسخه", request.Id));

        var dto = new PrescriptionDetailDto(
            p.Id, p.PrescriptionCode, p.PatientId, p.DoctorId, p.VisitId,
            p.PrescriptionDate.HasValue
                ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(p.PrescriptionDate.Value)
                : string.Empty,
            p.Type, p.Status, p.IsElectronic, p.IsNarcotic, p.Notes,
            p.Items.OrderBy(i => i.OrderIndex)
                .Select(i => new PrescriptionItemDetailDto(
                    i.MedicationId,
                    i.Medication != null ? i.Medication.GenericNameFa ?? i.Medication.GenericName : "",
                    i.OrderIndex ?? 0, i.Dosage, i.Frequency, i.Duration,
                    i.Quantity, i.Unit, i.Instructions))
                .ToList());

        return Result<PrescriptionDetailDto>.Success(dto);
    }
}
""")

write("Controllers/PrescriptionsController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Application.Features.Prescriptions.Commands.CreatePrescription;
using Sahand.ClinicPro.Application.Features.Prescriptions.Queries;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر نسخه — صدور و مشاهده نسخه‌ها با بررسی تداخل دارویی
/// مسیرها: /api/v1/prescriptions
/// </summary>
[ApiController]
[Route("api/v1/prescriptions")]
[Authorize]
public class PrescriptionsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PrescriptionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>نسخه‌های یک ویزیت</summary>
    /// <param name="visitId">شناسه ویزیت</param>
    [HttpGet("visit/{visitId:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetByVisit(long visitId)
    {
        var result = await _mediator.Send(new GetVisitPrescriptionsQuery(visitId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل نسخه با اقلام دارو</summary>
    /// <param name="id">شناسه نسخه</param>
    [HttpGet("{id:long}")]
    [HasPermission("prescriptions.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetPrescriptionByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>صدور نسخه جدید — با بررسی تداخل ممنوع مصرف</summary>
    /// <param name="dto">داده نسخه و اقلام</param>
    [HttpPost]
    [HasPermission("prescriptions.create")]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        var result = await _mediator.Send(new CreatePrescriptionCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "نسخه صادر شد");
        return ToResponse(result);
    }
}
""", API)

print("✅ گام‌های ۱۴ تا ۱۶ — پذیرش + ویزیت + نسخه کامل شد")
