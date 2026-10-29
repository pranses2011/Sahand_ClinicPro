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
        if (!VisitVitalsSafety.IsValid(dto.VitalSigns))
            return Result<long>.Failure(new Error("VISIT.VITALS_INVALID", "مقادیر علائم حیاتی نامعتبر یا خارج از دقت قابل ذخیره‌اند."));
        var repo = _unitOfWork.Repository<Domain.Entities.Clinical.Visit>();
        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(dto.PatientId, cancellationToken);
        if (patient is null || patient.IsDeleted)
            return Result<long>.Failure(Error.NotFound("بیمار", dto.PatientId));
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId, cancellationToken);
        if (doctor is null || doctor.IsDeleted)
            return Result<long>.Failure(Error.NotFound("پزشک", dto.DoctorId));
        if (dto.ReceptionId is long receptionId)
        {
            var reception = await _unitOfWork.Repository<Reception>().QueryableNoTracking
                .FirstOrDefaultAsync(r => r.Id == receptionId && !r.IsDeleted, cancellationToken);
            if (reception is null) return Result<long>.Failure(Error.NotFound("پذیرش", receptionId));
            if (reception.PatientId != dto.PatientId || reception.DoctorId != dto.DoctorId || reception.Status == ReceptionStatus.Cancelled)
                return Result<long>.Failure(new Error("VISIT.RECEPTION_MISMATCH", "پذیرش با بیمار یا پزشک ویزیت مطابقت ندارد یا لغو شده است."));
        }

        Domain.Entities.Clinical.Visit visit;
        if (dto.Id.HasValue)
        {
            visit = (await repo.GetByIdAsync(dto.Id.Value, cancellationToken))!;
            if (visit is null || visit.IsDeleted)
                return Result<long>.Failure(Error.NotFound("ویزیت", dto.Id.Value));
            if (visit.PatientId != dto.PatientId || visit.DoctorId != dto.DoctorId || (dto.ReceptionId.HasValue && visit.ReceptionId != dto.ReceptionId))
                return Result<long>.Failure(new Error("VISIT.IDENTITY_MISMATCH", "بیمار، پزشک یا پذیرش با ویزیت موجود مطابقت ندارد."));
            if (visit.Status == VisitStatus.Completed)
                return Result<long>.Failure(new Error("VISIT.COMPLETED", "ویرایش ویزیت تکمیل‌شده بدون سازوکار اصلاحیه مجاز نیست."));
        }
        else
        {
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
                VisitFee = doctor.ConsultationFee
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
        visit.FamilyHistory = dto.FamilyHistory;
        visit.SocialHistory = dto.SocialHistory;
        visit.ReviewOfSystems = dto.ReviewOfSystems;
        visit.SecondaryDiagnoses = dto.SecondaryDiagnoses;
        visit.FollowUpInstructions = dto.FollowUpInstructions;

        // علائم حیاتی — تب ۲ فرم ۱۱: upsert در هر ذخیره
        if (dto.VitalSigns is { } vitals)
        {
            var existingVitals = dto.Id.HasValue
                ? await _unitOfWork.Repository<Domain.Entities.Clinical.VitalSign>().QueryableTracking
                    .Where(v => v.VisitId == visit.Id).OrderByDescending(v => v.Id).FirstOrDefaultAsync(cancellationToken)
                : null;
            if (existingVitals is null)
            {
                await _unitOfWork.Repository<Domain.Entities.Clinical.VitalSign>().AddAsync(
                    new Domain.Entities.Clinical.VitalSign
                    {
                        Visit = visit,
                        PatientId = visit.PatientId,
                        MeasuredAt = DateTime.UtcNow,
                        BloodPressureSystolic = vitals.BloodPressureSystolic,
                        BloodPressureDiastolic = vitals.BloodPressureDiastolic,
                        HeartRate = vitals.HeartRate,
                        Temperature = vitals.Temperature,
                        OxygenSaturation = vitals.OxygenSaturation,
                        Weight = vitals.Weight,
                        Height = vitals.Height,
                        BloodSugar = vitals.BloodSugar,
                        Bmi = vitals.Height > 0 && vitals.Weight > 0
                            ? Math.Round(vitals.Weight.Value / (vitals.Height.Value / 100m) / (vitals.Height.Value / 100m), 1)
                            : 0m
                    }, cancellationToken);
            }
            else
            {
                existingVitals.MeasuredAt = DateTime.UtcNow;
                existingVitals.BloodPressureSystolic = vitals.BloodPressureSystolic;
                existingVitals.BloodPressureDiastolic = vitals.BloodPressureDiastolic;
                existingVitals.HeartRate = vitals.HeartRate;
                existingVitals.Temperature = vitals.Temperature;
                existingVitals.OxygenSaturation = vitals.OxygenSaturation;
                existingVitals.Weight = vitals.Weight;
                existingVitals.Height = vitals.Height;
                existingVitals.BloodSugar = vitals.BloodSugar;
                existingVitals.Bmi = vitals.Height > 0 && vitals.Weight > 0
                    ? Math.Round(vitals.Weight.Value / (vitals.Height.Value / 100m) / (vitals.Height.Value / 100m), 1)
                    : 0m;
                _unitOfWork.Repository<Domain.Entities.Clinical.VitalSign>().Update(existingVitals);
            }
        }

        visit.FollowUpDate = PersianDateHelper.ToGregorian(dto.FollowUpDateShamsi) is { } followUp
            ? DateOnly.FromDateTime(followUp) : null;

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
            patient.LastVisitDate = DateTime.UtcNow;
        }
        else
        {
            visit.Status = VisitStatus.Draft;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        if (dto.Complete)
        {
            // شناسهٔ SQL Identity پس از ذخیره قابل استفاده در رویداد است.
            visit.AddDomainEvent(new VisitCompletedEvent(visit.Id, visit.PatientId, visit.DoctorId));
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        }

        _logger.LogInformation("ویزیت {Code} ذخیره شد — وضعیت {Status}",
            visit.VisitCode, visit.Status);
        return Result<long>.Success(visit.Id);
    }
}
