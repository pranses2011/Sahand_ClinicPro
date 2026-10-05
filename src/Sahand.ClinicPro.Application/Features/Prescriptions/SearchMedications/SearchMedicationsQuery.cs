using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Application.DTOs.Prescriptions;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.SearchMedications;

/// <summary>جستجوی دارو برای نسخه‌نویسی — نام ژنریک/تجاری/بارکد/IRC</summary>
public record SearchMedicationsQuery(string? Term = null, int Take = 15)
    : IRequest<Result<List<MedicationSearchResultDto>>>;

/// <summary>گرداننده جستجو</summary>
public class SearchMedicationsQueryHandler
    : IRequestHandler<SearchMedicationsQuery, Result<List<MedicationSearchResultDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public SearchMedicationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<MedicationSearchResultDto>>> Handle(SearchMedicationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Medication>().QueryableNoTracking
            .Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Term))
        {
            var term = request.Term.Trim();
            query = query.Where(m =>
                m.GenericName.Contains(term) ||
                m.GenericNameFa != null && m.GenericNameFa.Contains(term) ||
                m.BrandName != null && m.BrandName.Contains(term) ||
                m.IrcCode != null && m.IrcCode.Contains(term) ||
                m.Barcode != null && m.Barcode.Contains(term));
        }

        var items = await query
            .OrderBy(m => m.GenericName)
            .Take(request.Take)
            .Select(m => new MedicationSearchResultDto(
                m.Id,
                m.GenericNameFa ?? m.GenericName,
                m.GenericName,
                m.Form,
                m.Strength,
                m.IrcCode,
                m.Manufacturer,
                m.Country == "IR" || m.Country == "ایران" || m.Country == null ? "داخلی" : "خارجی"))
            .ToListAsync(cancellationToken);

        return Result<List<MedicationSearchResultDto>>.Success(items);
    }
}
