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

/// <summary>
/// گرداننده جستجو.
/// ═══ چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
/// «در اسم بعضی از داروها از نیم‌فاصله استفاده شده و اگر کاربر فاصله بزند
/// آن دارو را نشان نمی‌دهد؛ جستجو را طوری بکن که هندل کند — مثلاً "پنی سیلین"
/// با فاصله هم "پنی‌سیلین" با نیم‌فاصله را نشان بدهد».
/// راه‌حل: علاوه بر مقایسهٔ عادی، مقایسهٔ «فشرده» — هر دو طرفِ نام و عبارت
/// جستجو از فاصله (U+0020)، نیم‌فاصله (U+200C)، نیم‌فاصلهٔ عربی (U+200F)
/// و فاصلهٔ نشکن (U+00A0) پاک می‌شوند و بعد Contains انجام می‌شود؛ بدین
/// ترتیب «پنی سیلین» ≡ «پنی‌سیلین» ≡ «پنیسیلین».
/// </summary>
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
            // نسخهٔ فشردهٔ عبارت جستجو — بدون هیچ‌گونه فاصله/نیم‌فاصله
            var compactTerm = Compact(term);

            query = query.Where(m =>
                // مقایسهٔ عادی (حفظ رفتار قبلی — پیشوندی/Exact هم می‌یابد)
                m.GenericName.Contains(term) ||
                m.GenericNameFa != null && m.GenericNameFa.Contains(term) ||
                m.BrandName != null && m.BrandName.Contains(term) ||
                m.IrcCode != null && m.IrcCode.Contains(term) ||
                m.Barcode != null && m.Barcode.Contains(term) ||
                // مقایسهٔ فشرده — فاصله/نیم‌فاصله نادیده (چرخه بیست‌ویکم)
                m.GenericName.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "").Contains(compactTerm) ||
                m.GenericNameFa != null && m.GenericNameFa.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "").Contains(compactTerm) ||
                m.BrandName != null && m.BrandName.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "").Contains(compactTerm));
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

    /// <summary>
    /// پاک‌سازی فاصله‌های فارسی/لاتین از عبارت — نسخهٔ فشرده برای مقایسهٔ
    /// مستقل از نیم‌فاصله. فقط برای عبارت ورودی کاربر (سمت ستون‌ها در SQL
    /// با Replace ترجمه می‌شود).
    /// </summary>
    private static string Compact(string text)
        => text.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "");
}
