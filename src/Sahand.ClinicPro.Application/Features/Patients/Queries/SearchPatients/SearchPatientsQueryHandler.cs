using Sahand.ClinicPro.Application.Common.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// گرداننده جستجوی سریع بیمار — سبک برای تکمیل خودکار
/// </summary>
public class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, Result<List<PatientListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PatientMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public SearchPatientsQueryHandler(IUnitOfWork unitOfWork, PatientMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای جستجوی سریع</summary>
    public async Task<Result<List<PatientListDto>>> Handle(SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
            return Result<List<PatientListDto>>.Success(new List<PatientListDto>());

        var term = request.SearchTerm.Trim();

        var patients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .Where(p => p.FirstName.Contains(term)
                        || p.LastName.Contains(term)
                        || p.NationalCode.Contains(term)
                        || p.Mobile.Contains(term)
                        || p.MedicalRecordNo.Contains(term))
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Take(request.MaxResults)
            .ToListAsync(cancellationToken);

        return Result<List<PatientListDto>>.Success(_mapper.ToPatientListDtos(patients));
    }
}
