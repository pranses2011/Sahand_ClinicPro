using Sahand.ClinicPro.Application.Common.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// گرداننده دریافت جزئیات بیمار — همراه بیمه و سن محاسبه‌شده
/// </summary>
public class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, Result<PatientDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PatientMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetPatientByIdQueryHandler(IUnitOfWork unitOfWork, PatientMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PatientDetailDto>> Handle(GetPatientByIdQuery request,
        CancellationToken cancellationToken)
    {
        var patient = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .Include(p => p.InsuranceCompany)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (patient is null)
            return Result<PatientDetailDto>.Failure(Error.NotFound("بیمار", request.Id));

        var dto = _mapper.ToPatientDetailDto(patient);
        dto.InsuranceName = patient.InsuranceCompany?.Name;
        dto.Age = patient.BirthDate.HasValue
            ? PersianDateHelper.CalculateAge(patient.BirthDate.Value.ToDateTime(TimeOnly.MinValue))
            : null;
        // چرخه بیست‌ویکم: تأهل به نام فارسی برای نمایش در فرم ویرایش دسکتاپ
        dto.MaritalStatusFa = MaritalStatusTextMapper.ToFaText(patient.MaritalStatus);

        return Result<PatientDetailDto>.Success(dto);
    }
}
