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
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (v is null)
            return Result<VisitDetailDto>.Failure(Error.NotFound("ویزیت", request.Id));

        var vitals = await _unitOfWork.Repository<Domain.Entities.Clinical.VitalSign>()
            .QueryableNoTracking
            .Where(vs => vs.VisitId == v.Id)
            .OrderByDescending(vs => vs.Id)
            .Select(vs => new VitalSignsViewDto(
                vs.BloodPressureSystolic, vs.BloodPressureDiastolic, vs.HeartRate,
                vs.Temperature, vs.OxygenSaturation, vs.Weight, vs.Height, vs.Bmi, vs.BloodSugar))
            .FirstOrDefaultAsync(cancellationToken);

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
            v.VisitFee,
            v.FamilyHistory, v.SocialHistory, v.ReviewOfSystems,
            v.SecondaryDiagnoses, v.FollowUpInstructions,
            v.Patient is null ? null : new PatientContextDto(
                v.Patient.FirstName + " " + v.Patient.LastName,
                v.Patient.MedicalRecordNo ?? "",
                v.Patient.NationalCode,
                v.Patient.BirthDate.HasValue
                    ? DateTime.UtcNow.Year - v.Patient.BirthDate.Value.Year -
                      (DateTime.UtcNow.DayOfYear < v.Patient.BirthDate.Value.DayOfYear ? 1 : 0)
                    : null,
                v.Patient.Gender == Domain.Enums.Gender.Male ? "مرد" : v.Patient.Gender == Domain.Enums.Gender.Female ? "زن" : "—",
                v.Patient.BloodType,
                v.Patient.Mobile ?? "",
                null,
                v.Patient.Allergies,
                v.Patient.ChronicDiseases,
                v.Patient.CurrentMedications),
            vitals,
            // چرخهٔ ۷۸ — عنوان تشخیص اصلی (ترجیح فارسی، بازگشت به انگلیسی)
            v.PrimaryDiagnosis is null
                ? null
                : v.PrimaryDiagnosis.DescriptionFa ?? v.PrimaryDiagnosis.Description,
            v.ReceptionId);

        return Result<VisitDetailDto>.Success(dto);
    }
}
