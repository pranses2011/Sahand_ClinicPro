using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctors;

/// <summary>
/// پرس‌وجوی فهرست پزشکان با صفحه‌بندی و جستجو
/// </summary>
/// <param name="Page">پارامترهای صفحه‌بندی</param>
/// <param name="SpecialtyId">فیلتر تخصص — اختیاری</param>
/// <param name="OnlyActive">فقط پزشکان فعال</param>
public record GetDoctorsQuery(PagedRequest? Page = null, long? SpecialtyId = null, bool OnlyActive = true)
    : IRequest<Result<PagedResponse<DoctorListDto>>>;

/// <summary>
/// گرداننده فهرست پزشکان
/// </summary>
public class GetDoctorsQueryHandler : IRequestHandler<GetDoctorsQuery, Result<PagedResponse<DoctorListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetDoctorsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<PagedResponse<DoctorListDto>>> Handle(GetDoctorsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        IQueryable<Doctor> query = _unitOfWork.Repository<Doctor>().QueryableNoTracking
            .Include(d => d.Specialty)
            .Include(d => d.Employee);

        if (request.OnlyActive)
            query = query.Where(d => d.IsActive);
        if (request.SpecialtyId.HasValue)
            query = query.Where(d => d.SpecialtyId == request.SpecialtyId.Value);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            query = query.Where(d =>
                (d.Employee != null && d.Employee.FirstName.Contains(term)) ||
                (d.Employee != null && d.Employee.LastName.Contains(term)) ||
                d.NezamPezeshkiCode != null && d.NezamPezeshkiCode.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.Id)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(d => new DoctorListDto
        {
            Id = d.Id,
            FullName = d.Employee is not null ? $"{d.Employee.FirstName} {d.Employee.LastName}" : $"پزشک {d.Id}",
            NezamPezeshkiCode = d.NezamPezeshkiCode,
            SpecialtyName = d.Specialty?.Name,
            ConsultationFee = d.ConsultationFee,
            VisitDuration = d.VisitDuration,
            IsActive = d.IsActive
        }).ToList();

        return Result<PagedResponse<DoctorListDto>>.Success(
            PagedResponse<DoctorListDto>.Create(dtos, page.PageNumber, page.PageSize, totalCount));
    }
}
