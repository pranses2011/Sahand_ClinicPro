using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.Queries;

/// <summary>
/// پرس‌وجوی فهرست صفحه‌بندی‌شده نسخه‌ها — برای ماژول دسکتاپ و داروخانه
/// </summary>
/// <param name="Page">پارامترهای صفحه‌بندی و جستجو</param>
/// <param name="PatientId">فیلتر بیمار — اختیاری</param>
/// <param name="Status">فیلتر وضعیت — اختیاری</param>
public record GetPrescriptionsQuery(PagedRequest? Page = null, long? PatientId = null,
    PrescriptionStatus? Status = null) : IRequest<Result<PagedResponse<PrescriptionListDto>>>;

/// <summary>گرداننده فهرست نسخه‌ها</summary>
public class GetPrescriptionsQueryHandler
    : IRequestHandler<GetPrescriptionsQuery, Result<PagedResponse<PrescriptionListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetPrescriptionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — فیلتر وضعیت/بیمار + جستجوی کد نسخه یا نام بیمار</summary>
    public async Task<Result<PagedResponse<PrescriptionListDto>>> Handle(GetPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page ?? new PagedRequest { PageNumber = 1, PageSize = 20 };
        var search = page.Search?.Trim();

        IQueryable<Domain.Entities.Clinical.Prescription> query = _unitOfWork.Repository<Domain.Entities.Clinical.Prescription>()
            .QueryableNoTracking
            .Include(p => p.Patient)
            .Include(p => p.Doctor).ThenInclude(d => d!.Employee);

        if (request.PatientId.HasValue)
            query = query.Where(p => p.PatientId == request.PatientId.Value);
        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                (p.PrescriptionCode != null && p.PrescriptionCode.Contains(search)) ||
                (p.Patient != null &&
                    (p.Patient.FirstName + " " + p.Patient.LastName).Contains(search)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.PrescriptionDate)
            .Skip((page.PageNumber - 1) * page.PageSize)
            .Take(page.PageSize)
            .Select(p => new PrescriptionListDto(
                p.Id,
                p.PrescriptionCode,
                p.Patient != null ? p.Patient.FirstName + " " + p.Patient.LastName : "",
                p.Doctor != null && p.Doctor.Employee != null
                    ? p.Doctor.Employee.FirstName + " " + p.Doctor.Employee.LastName : "پزشک",
                p.PrescriptionDate.HasValue
                    ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
                        .ToShamsi(p.PrescriptionDate.Value)
                    : string.Empty,
                p.Type, p.Status, p.IsElectronic))
            .ToListAsync(cancellationToken);

        return Result<PagedResponse<PrescriptionListDto>>.Success(
            PagedResponse<PrescriptionListDto>.Create(items, page.PageNumber, page.PageSize, totalCount));
    }
}