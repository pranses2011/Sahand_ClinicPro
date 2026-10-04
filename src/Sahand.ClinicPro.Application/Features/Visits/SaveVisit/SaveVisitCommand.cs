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
