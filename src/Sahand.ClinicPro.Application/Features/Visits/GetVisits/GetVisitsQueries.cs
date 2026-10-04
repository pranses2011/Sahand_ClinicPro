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
