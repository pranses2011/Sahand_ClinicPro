using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Laboratory;

/// <summary>
/// ردیف کاتالوگ آزمایش — نسخهٔ کامل با بازه‌های مرجع ساخت‌یافته — چرخهٔ ۷۲
/// (گزارش مالک: «بازه‌های مرجع و حدود بحرانی هر آزمایش رو بطور کامل و دقیق
/// تکمیل کن… در صورت نیاز بتونم اصلاحشون بکنم»). برخلاف LabTestOptionDto
/// سبکِ lookup (نام/واحد/قیمت)، این DTO همهٔ ستون‌های بازهٔ مرجع بزرگسال
/// (مرد/زن)، بازهٔ کودک + سقف سن، حدود بحرانی و متن‌های نمایشی قدیمی را
/// می‌آورد تا جدول «کاتالوگ تست‌ها» و فرم ویرایش مالک از همین تغذیه شوند.
/// </summary>
public record LabTestCatalogDto(
    long Id,
    string? TestCode,
    string TestName,
    string? TestNameEn,
    string? Category,
    string? SampleType,
    string? Unit,
    decimal Price,
    int? TurnAroundTime,
    string? NormalRangeMale,
    string? NormalRangeFemale,
    decimal? RefMinAdultMale, decimal? RefMaxAdultMale,
    decimal? RefMinAdultFemale, decimal? RefMaxAdultFemale,
    decimal? RefMinChild, decimal? RefMaxChild, int? ChildAgeMaxYears,
    decimal? CriticalLow, decimal? CriticalHigh,
    bool IsActive);

/// <summary>
/// پرس‌وجوی کاتالوگ کامل آزمایش‌ها — چرخهٔ ۷۲.
/// جستجو روی نام فارسی/انگلیسی/کد؛ ردیف‌های غیرفعال هم می‌آیند تا مالک
/// بتواند کل کاتالوگ را ببیند و ویرایش کند (مرتب‌سازی: دسته، سپس نام).
/// </summary>
public record GetLabTestCatalogQuery(string? Search = null)
    : IRequest<Result<List<LabTestCatalogDto>>>;

/// <summary>گردانندهٔ پرس‌وجوی کاتالوگ کامل</summary>
public class GetLabTestCatalogQueryHandler
    : IRequestHandler<GetLabTestCatalogQuery, Result<List<LabTestCatalogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetLabTestCatalogQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<LabTestCatalogDto>>> Handle(GetLabTestCatalogQuery request,
        CancellationToken cancellationToken)
    {
        // جستجوی بدون حساسیت به بزرگی حروف — نرمال‌سازی سمت کاربر (الگوی Secure
        // EF: EF.Functions.Like روی ستون نرمال‌شده ممکن نیست چون ستون نیست؛
        // شرط ترکیبی ToLower سمت سرور به SQL LIKE ترجمه می‌شود)
        var search = request.Search?.Trim();
        IQueryable<LabTest> query = _unitOfWork.Repository<LabTest>().QueryableNoTracking;

        if (!string.IsNullOrEmpty(search))
        {
            var needle = search.ToLower();
            query = query.Where(t =>
                (t.TestName != null && t.TestName.ToLower().Contains(needle))
                || (t.TestNameEn != null && t.TestNameEn.ToLower().Contains(needle))
                || (t.TestCode != null && t.TestCode.ToLower().Contains(needle)));
        }

        var items = await query
            .OrderBy(t => t.Category)
            .ThenBy(t => t.TestName)
            .Select(t => new LabTestCatalogDto(
                t.Id, t.TestCode, t.TestName, t.TestNameEn, t.Category, t.SampleType,
                t.Unit, t.Price, t.TurnAroundTime,
                t.NormalRangeMale, t.NormalRangeFemale,
                t.RefMinAdultMale, t.RefMaxAdultMale,
                t.RefMinAdultFemale, t.RefMaxAdultFemale,
                t.RefMinChild, t.RefMaxChild, t.ChildAgeMaxYears,
                t.CriticalLow, t.CriticalHigh,
                t.IsActive))
            .ToListAsync(cancellationToken);

        return Result<List<LabTestCatalogDto>>.Success(items);
    }
}

/// <summary>
/// بدنهٔ ویرایش یک ردیف کاتالوگ آزمایش — چرخهٔ ۷۲.
/// همهٔ فیلدها «تهی‌پذیر»اند چون آزمایش کیفی ممکن است هیچ بازه‌ای نداشته
/// باشد؛ اعتبارسنج درستیِ «کف ≤ سقف» هر بازه را شکیبا بررسی می‌کند.
/// </summary>
public record UpdateLabTestCatalogDto(
    string? Unit,
    decimal Price,
    int? TurnAroundTime,
    string? NormalRangeMale,
    string? NormalRangeFemale,
    decimal? RefMinAdultMale, decimal? RefMaxAdultMale,
    decimal? RefMinAdultFemale, decimal? RefMaxAdultFemale,
    decimal? RefMinChild, decimal? RefMaxChild, int? ChildAgeMaxYears,
    decimal? CriticalLow, decimal? CriticalHigh);

/// <summary>فرمان ویرایش بازه‌های مرجع و حدود بحرانی یک آزمایش — چرخهٔ ۷۲</summary>
public record UpdateLabTestCatalogCommand(long TestId, UpdateLabTestCatalogDto Dto)
    : IRequest<Result>;

/// <summary>
/// اعتبارسنج ویرایش کاتالوگ — قواعد سازگاری بازه‌ها:
///  ۱) هر «کف» از «سقف» خودش بزرگ‌تر نباشد (مرد/زن/کودک/بحرانی)؛
///  ۲) حدود بحرانی باید بیرون بازهٔ مرجع بزرگسالِ همان جنسیت باشد
///     (CriticalLow ≤ کف و CriticalHigh ≥ سقف) — حد بحرانیِ داخل بازهٔ
///     طبیعی بی‌معناست و موتور تفسیر را گیج می‌کرد؛
///  ۳) قیمت و زمان جواب نامنفی.
/// فیلدهای تهی (=بدون بازه) هر قاعده‌ای را رد می‌کنند — آزمایش کیفی آزاد است.
/// </summary>
public class UpdateLabTestCatalogCommandValidator : AbstractValidator<UpdateLabTestCatalogCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public UpdateLabTestCatalogCommandValidator()
    {
        RuleFor(x => x.TestId).GreaterThan(0).WithMessage("شناسه آزمایش نامعتبر است");
        RuleFor(x => x.Dto.Price).GreaterThanOrEqualTo(0).WithMessage("قیمت نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.TurnAroundTime).GreaterThan(0)
            .When(x => x.Dto.TurnAroundTime.HasValue)
            .WithMessage("زمان جواب باید مثبت باشد (ساعت)");
        RuleFor(x => x.Dto.ChildAgeMaxYears).InclusiveBetween(1, 18)
            .When(x => x.Dto.ChildAgeMaxYears.HasValue)
            .WithMessage("سقف سن کودک باید بین ۱ تا ۱۸ سال باشد");

        // ─── کف ≤ سقف در هر بازه ───
        RuleFor(x => x.Dto.RefMinAdultMale).LessThanOrEqualTo(x => x.Dto.RefMaxAdultMale)
            .When(x => x.Dto.RefMinAdultMale.HasValue && x.Dto.RefMaxAdultMale.HasValue)
            .WithMessage("کف بازهٔ مردان باید کمتر یا مساوی سقف آن باشد");
        RuleFor(x => x.Dto.RefMinAdultFemale).LessThanOrEqualTo(x => x.Dto.RefMaxAdultFemale)
            .When(x => x.Dto.RefMinAdultFemale.HasValue && x.Dto.RefMaxAdultFemale.HasValue)
            .WithMessage("کف بازهٔ زنان باید کمتر یا مساوی سقف آن باشد");
        RuleFor(x => x.Dto.RefMinChild).LessThanOrEqualTo(x => x.Dto.RefMaxChild)
            .When(x => x.Dto.RefMinChild.HasValue && x.Dto.RefMaxChild.HasValue)
            .WithMessage("کف بازهٔ کودک باید کمتر یا مساوی سقف آن باشد");

        // ─── حدود بحرانی بیرونِ بازهٔ مرجع ───
        RuleFor(x => x.Dto.CriticalHigh).GreaterThanOrEqualTo(x => x.Dto.RefMaxAdultMale)
            .When(x => x.Dto.CriticalHigh.HasValue && x.Dto.RefMaxAdultMale.HasValue)
            .WithMessage("حد بحرانی بالا نباید از سقف بازهٔ مرجع مردان کمتر باشد");
        RuleFor(x => x.Dto.CriticalHigh).GreaterThanOrEqualTo(x => x.Dto.RefMaxAdultFemale)
            .When(x => x.Dto.CriticalHigh.HasValue && x.Dto.RefMaxAdultFemale.HasValue)
            .WithMessage("حد بحرانی بالا نباید از سقف بازهٔ مرجع زنان کمتر باشد");
        RuleFor(x => x.Dto.CriticalLow).LessThanOrEqualTo(x => x.Dto.RefMinAdultMale)
            .When(x => x.Dto.CriticalLow.HasValue && x.Dto.RefMinAdultMale.HasValue)
            .WithMessage("حد بحرانی پایین نباید از کف بازهٔ مرجع مردان بیشتر باشد");
        RuleFor(x => x.Dto.CriticalLow).LessThanOrEqualTo(x => x.Dto.RefMinAdultFemale)
            .When(x => x.Dto.CriticalLow.HasValue && x.Dto.RefMinAdultFemale.HasValue)
            .WithMessage("حد بحرانی پایین نباید از کف بازهٔ مرجع زنان بیشتر باشد");
    }
}

/// <summary>گردانندهٔ ویرایش کاتالوگ آزمایش — چرخهٔ ۷۲</summary>
public class UpdateLabTestCatalogCommandHandler
    : IRequestHandler<UpdateLabTestCatalogCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateLabTestCatalogCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ویرایش</summary>
    public async Task<Result> Handle(UpdateLabTestCatalogCommand request,
        CancellationToken cancellationToken)
    {
        var test = await _unitOfWork.Repository<LabTest>()
            .QueryableTracking
            .FirstOrDefaultAsync(t => t.Id == request.TestId, cancellationToken);

        if (test is null)
            return Result.Failure(new Error("TEST_NOT_FOUND", "آزمایش مورد نظر یافت نشد"));

        var dto = request.Dto;

        // ─── فیلدهای عمومی کاتالوگ ───
        test.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();
        test.Price = dto.Price;
        test.TurnAroundTime = dto.TurnAroundTime;
        test.NormalRangeMale = string.IsNullOrWhiteSpace(dto.NormalRangeMale)
            ? null : dto.NormalRangeMale.Trim();
        test.NormalRangeFemale = string.IsNullOrWhiteSpace(dto.NormalRangeFemale)
            ? null : dto.NormalRangeFemale.Trim();

        // ─── بازه‌های ساخت‌یافته — موتور تفسیر (LabResultInterpreter) ───
        test.RefMinAdultMale = dto.RefMinAdultMale;
        test.RefMaxAdultMale = dto.RefMaxAdultMale;
        test.RefMinAdultFemale = dto.RefMinAdultFemale;
        test.RefMaxAdultFemale = dto.RefMaxAdultFemale;
        test.RefMinChild = dto.RefMinChild;
        test.RefMaxChild = dto.RefMaxChild;
        test.ChildAgeMaxYears = dto.ChildAgeMaxYears;
        test.CriticalLow = dto.CriticalLow;
        test.CriticalHigh = dto.CriticalHigh;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
