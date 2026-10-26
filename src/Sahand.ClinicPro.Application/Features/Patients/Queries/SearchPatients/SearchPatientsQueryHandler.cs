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

    /// <summary>
    /// اجرای جستجوی سریع.
    /// چرخه بیست‌ویکم (گزارش پانزدهم مالک — به پیروی از قانون «رفع مورد مشابه
    /// در جاهای دیگر برنامه»): نام‌های فارسی گاهی با نیم‌فاصله ذخیره می‌شوند و
    /// جستجو با فاصله آنها را نمی‌یافت؛ مقایسهٔ فشرده (حذف فاصله/نیم‌فاصله از
    /// هر دو طرف) به نام/نام‌خانوادگی اضافه شد تا «محمد رضا» ≡ «محمدرضا».
    /// </summary>
    public async Task<Result<List<PatientListDto>>> Handle(SearchPatientsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
            return Result<List<PatientListDto>>.Success(new List<PatientListDto>());

        var term = request.SearchTerm.Trim();
        // نسخهٔ فشردهٔ عبارت — بدون فاصله/نیم‌فاصله برای مقایسهٔ مستقل از نیم‌فاصله
        var compactTerm = term
            .Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "");

        var patients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — همان اصلاح فهرست اصلی ═══
            // بدون Include بیمه، نام بیمه تهی می‌ماند و رابط «آزاد» می‌چپاند
            .Include(p => p.InsuranceCompany)
            .Where(p => p.FirstName.Contains(term)
                        || p.LastName.Contains(term)
                        || p.NationalCode.Contains(term)
                        || p.Mobile.Contains(term)
                        || (p.TemporaryCode != null && p.TemporaryCode.Contains(term))
                        || p.MedicalRecordNo.Contains(term)
                        // مقایسهٔ فشردهٔ نام — چرخه بیست‌ویکم (فاصله/نیم‌فاصله نادیده)
                        || p.FirstName.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "").Contains(compactTerm)
                        || p.LastName.Replace(" ", "").Replace("\u200C", "").Replace("\u200F", "").Replace("\u00A0", "").Contains(compactTerm))
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Take(request.MaxResults)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.ToPatientListDtos(patients);

        // چرخه بیست‌وسوم: جستجوی سریع هم همان ستون‌های فهرست را می‌دهد — سن و آخرین
        // مراجعه اینجا هم پر می‌شود تا نتیجهٔ جستجو با فهرست اصلی هم‌ارز باشد (قانون مشابه‌یابی)
        PatientMapper.EnrichListComputedFields(dtos, patients);

        return Result<List<PatientListDto>>.Success(dtos);
    }
}
