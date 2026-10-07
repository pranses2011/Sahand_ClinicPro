using Sahand.ClinicPro.Application.Common.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorById;

/// <summary>
/// پرس‌وجوی جزئیات یک پزشک
/// </summary>
/// <param name="Id">شناسه پزشک</param>
public record GetDoctorByIdQuery(long Id) : IRequest<Result<DoctorDetailDto>>;

/// <summary>
/// گرداننده جزئیات پزشک
/// </summary>
public class GetDoctorByIdQueryHandler : IRequestHandler<GetDoctorByIdQuery, Result<DoctorDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly DoctorMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetDoctorByIdQueryHandler(IUnitOfWork unitOfWork, DoctorMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<DoctorDetailDto>> Handle(GetDoctorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _unitOfWork.Repository<Doctor>()
            .QueryableNoTracking
            .Include(d => d.Specialty)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor is null)
            return Result<DoctorDetailDto>.Failure(Error.NotFound("پزشک", request.Id));

        var dto = _mapper.ToDoctorDetailDto(doctor);
        dto.SpecialtyName = doctor.Specialty?.Name;
        dto.SignatureImageBase64 = doctor.SignatureImage is { Length: > 0 } sig ? Convert.ToBase64String(sig) : null;
        dto.StampImageBase64 = doctor.StampImage is { Length: > 0 } stamp ? Convert.ToBase64String(stamp) : null;
        dto.HasPhoto = doctor.Employee is not null
                        && !string.IsNullOrEmpty(doctor.Employee.ProfilePicture); // چرخه هجدهم
        return Result<DoctorDetailDto>.Success(dto);
    }
}
