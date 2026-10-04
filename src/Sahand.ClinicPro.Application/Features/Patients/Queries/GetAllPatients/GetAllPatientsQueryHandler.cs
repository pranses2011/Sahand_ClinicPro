using Sahand.ClinicPro.Application.Common.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;

/// <summary>
/// گرداننده فهرست بیماران — صفحه‌بندی سروری با پروجکشن سبک
/// </summary>
public class GetAllPatientsQueryHandler : IRequestHandler<GetAllPatientsQuery, Result<PagedResponse<PatientListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PatientMapper _mapper;

    /// <summary>سازنده با تزریق واحد کار و نقشه</summary>
    public GetAllPatientsQueryHandler(IUnitOfWork unitOfWork, PatientMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>اجرای پرس‌وجو با صفحه‌بندی</summary>
    public async Task<Result<PagedResponse<PatientListDto>>> Handle(GetAllPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest();
        var query = _unitOfWork.Repository<Patient>().QueryableNoTracking;

        // جستجوی عمومی روی نام و کد ملی و موبایل و شماره پرونده
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var term = page.Search.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(term) ||
                p.LastName.Contains(term) ||
                p.NationalCode.Contains(term) ||
                p.Mobile.Contains(term) ||
                p.MedicalRecordNo.Contains(term));
        }

        // شمارش کل
        var totalCount = await query.CountAsync(cancellationToken);

        // مرتب‌سازی پیش‌فرض: جدیدترین ثبت‌نام
        query = query.OrderByDescending(p => p.Id);

        var items = await query
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.ToPatientListDtos(items);

        return Result<PagedResponse<PatientListDto>>.Success(
            PagedResponse<PatientListDto>.Create(dtos, page.PageNumber, page.PageSize, totalCount));
    }
}
