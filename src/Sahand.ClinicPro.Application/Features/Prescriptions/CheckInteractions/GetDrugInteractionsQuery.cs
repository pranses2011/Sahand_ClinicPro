using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Prescriptions.CheckInteractions;

/// <summary>
/// پرس‌وجوی کامل کاتالوگ تداخل‌های دارویی — چرخه بیستم (گزارش چهاردهم مالک):
/// «تمامی تداخل‌ها داخل برنامه باشد و دکمهٔ بروزرسانی هم داشته باشد».
/// کلاینت دسکتاپ این فهرست را دانلود و در کش محلی (%AppData%) ذخیره می‌کند تا
/// بررسی تداخل حتی در قطعی شبکه از پایگاه داخلی انجام شود؛ دکمهٔ «بروزرسانی
/// تداخل‌ها» همین پرس‌وجو را دوباره اجرا و کش را نو می‌کند.
/// </summary>
public record GetDrugInteractionsQuery : IRequest<Result<List<DrugInteractionCatalogDto>>>;

/// <summary>گرداننده — همهٔ جفت‌های تداخلی فعال با نام ژنریک برای تطبیق محلی کلاینت</summary>
public class GetDrugInteractionsQueryHandler
    : IRequestHandler<GetDrugInteractionsQuery, Result<List<DrugInteractionCatalogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDrugInteractionsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — پیوند هر دو سمت تداخل با نام ژنریک دارو</summary>
    public async Task<Result<List<DrugInteractionCatalogDto>>> Handle(
        GetDrugInteractionsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<DrugInteraction>().QueryableNoTracking
            .Join(_unitOfWork.Repository<Medication>().QueryableNoTracking,
                d => d.Medication1Id, m => m.Id,
                (d, m1) => new { d, m1 })
            .Join(_unitOfWork.Repository<Medication>().QueryableNoTracking,
                x => x.d.Medication2Id, m => m.Id,
                (x, m2) => new { x.d, x.m1, m2 })
            .Where(x => !x.m1.IsDeleted && !x.m2.IsDeleted)
            .Select(x => new DrugInteractionCatalogDto(
                x.m1.GenericName,
                x.m1.GenericNameFa ?? x.m1.GenericName,
                x.m2.GenericName,
                x.m2.GenericNameFa ?? x.m2.GenericName,
                (int)x.d.Severity,
                SeverityText(x.d.Severity),
                SeverityColor(x.d.Severity),
                x.d.Mechanism,
                x.d.Recommendation))
            .ToListAsync(cancellationToken);

        return Result<List<DrugInteractionCatalogDto>>.Success(rows);
    }

    /// <summary>متن شدت — هماهنگ با CheckInteractionsCommandHandler</summary>
    private static string SeverityText(DrugInteractionSeverity severity) => severity switch
    {
        DrugInteractionSeverity.Minor => "خفیف",
        DrugInteractionSeverity.Moderate => "متوسط",
        DrugInteractionSeverity.Major => "شدید",
        DrugInteractionSeverity.Contraindicated => "ممنوع مصرف",
        _ => "نامشخص"
    };

    /// <summary>رنگ سطح — هماهنگ با CheckInteractionsCommandHandler</summary>
    private static string SeverityColor(DrugInteractionSeverity severity) => severity switch
    {
        DrugInteractionSeverity.Minor => "#FFF9C4",
        DrugInteractionSeverity.Moderate => "#FFE0B2",
        DrugInteractionSeverity.Major => "#FFCDD2",
        DrugInteractionSeverity.Contraindicated => "#B71C1C",
        _ => "#ECEFF1"
    };
}

/// <summary>DTO یک جفت تداخل برای کش محلی کلاینت — بر پایهٔ نام ژنریک</summary>
public sealed record DrugInteractionCatalogDto(
    string Medication1Generic,
    string Medication1NameFa,
    string Medication2Generic,
    string Medication2NameFa,
    int Severity,
    string SeverityText,
    string SeverityColor,
    string? Mechanism,
    string? Recommendation);
