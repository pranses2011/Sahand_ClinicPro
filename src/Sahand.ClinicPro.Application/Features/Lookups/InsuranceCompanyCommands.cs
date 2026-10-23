using MediatR;
using Sahand.ClinicPro.Application.Common.Caching;
using Sahand.ClinicPro.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Features.Lookups;

// ═══════════════════════════════════════════════════════════════════
// چرخه بیست‌وسوم (گزارش شانزدهم مالک):
// «در صفحه بیمه، بتوان بیمه جدید تعریف کرد و یا بیمه‌های موجود را ویرایش کرد»
// «در صفحه بیمه، بتوان تعرفه جدید تعریف کرد و یا تعرفه‌های موجود را ویرایش کرد»
// پیش‌تر فقط خواندن (GET) وجود داشت؛ چهار دستور زیر برای بیمه‌ها ساخته شدند.
// الگو همان معماری مدیریت اتاق‌ها (RoomCommands) است: نوشتن نیازمند
// نقش مدیریتی در کنترلر، خواندن عمومی.
// ═══════════════════════════════════════════════════════════════════

/// <summary>آیتم فهرست بیمه‌ها برای پنجرهٔ مدیریت — شامل غیرفعال‌ها</summary>
public record InsuranceCompanyManageDto(
    long Id,
    string Name,
    string? Code,
    int Type,
    string TypeName,
    decimal CoveragePercent,
    bool IsActive,
    bool IsContracted);

/// <summary>پرس‌وجوی فهرست کامل بیمه‌ها (پنجرهٔ مدیریت بیمه‌ها)</summary>
public record GetInsuranceCompaniesForManageQuery
    : IRequest<Result<List<InsuranceCompanyManageDto>>>;

/// <summary>گردانندهٔ فهرست بیمه‌ها برای مدیریت</summary>
public class GetInsuranceCompaniesForManageQueryHandler
    : IRequestHandler<GetInsuranceCompaniesForManageQuery, Result<List<InsuranceCompanyManageDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetInsuranceCompaniesForManageQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — همهٔ بیمه‌ها شامل غیرفعال، مرتب بر نام</summary>
    public async Task<Result<List<InsuranceCompanyManageDto>>> Handle(GetInsuranceCompaniesForManageQuery request,
        CancellationToken cancellationToken)
    {
        var companies = await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var items = companies.Select(c => new InsuranceCompanyManageDto(
            c.Id, c.Name, c.Code, (int)c.Type, TypeNameFa(c.Type),
            c.DefaultCoveragePercent, c.IsActive, c.IsContracted)).ToList();

        return Result<List<InsuranceCompanyManageDto>>.Success(items);
    }

    /// <summary>نام فارسی نوع بیمه برای نمایش در گرید</summary>
    internal static string TypeNameFa(InsuranceType type) => type switch
    {
        InsuranceType.Public => "عمومی",
        InsuranceType.Salamat => "سلامت",
        InsuranceType.Artesh => "ارتش",
        InsuranceType.Supplementary => "تکمیلی",
        _ => "خصوصی"
    };
}

/// <summary>فرمان ساخت شرکت بیمه جدید</summary>
public record CreateInsuranceCompanyCommand(
    string Name,
    string? Code,
    int Type,
    decimal CoveragePercent,
    bool IsActive = true,
    bool? IsContracted = null) : IRequest<Result<long>>;

/// <summary>گردانندهٔ ساخت بیمه</summary>
public class CreateInsuranceCompanyCommandHandler
    : IRequestHandler<CreateInsuranceCompanyCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کش مرجع — باطل‌سازی پس از درج (چرخهٔ ۶۰)</summary>
    private readonly IReferenceCache _cache;

    /// <summary>سازنده</summary>
    public CreateInsuranceCompanyCommandHandler(IUnitOfWork unitOfWork, IReferenceCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>اجرا — اعتبارسنجی نام یکتا و درج</summary>
    public async Task<Result<long>> Handle(CreateInsuranceCompanyCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<long>.Failure(new Error("INSURANCE.NAME", "نام شرکت بیمه الزامی است"));

        // نام تکراری نباشد (حتی در غیرفعال‌ها — برای جلوگیری از دوگانه‌نویسی)
        var duplicate = await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
            .AnyAsync(c => c.Name == request.Name.Trim(), cancellationToken);
        if (duplicate)
            return Result<long>.Failure(new Error("INSURANCE.DUPLICATE", "شرکت بیمه‌ای با این نام قبلاً ثبت شده است"));

        var entity = new InsuranceCompany
        {
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Type = (InsuranceType)request.Type,
            DefaultCoveragePercent = request.CoveragePercent,
            IsActive = request.IsActive,
            IsContracted = request.IsContracted ?? false
        };
        await _unitOfWork.Repository<InsuranceCompany>().AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // چرخهٔ ۶۰ — فهرست کش‌شدهٔ بیمه‌ها معتبر نیست؛ باطل می‌شود
        _cache.Evict(ReferenceCacheKeys.InsuranceCompanies);
        return Result<long>.Success(entity.Id);
    }
}

/// <summary>فرمان ویرایش شرکت بیمه</summary>
public record UpdateInsuranceCompanyCommand(
    long Id,
    string Name,
    string? Code,
    int Type,
    decimal CoveragePercent,
    bool IsActive,
    bool? IsContracted = null) : IRequest<Result>;

/// <summary>گردانندهٔ ویرایش بیمه</summary>
public class UpdateInsuranceCompanyCommandHandler
    : IRequestHandler<UpdateInsuranceCompanyCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کش مرجع — باطل‌سازی پس از ویرایش (چرخهٔ ۶۰)</summary>
    private readonly IReferenceCache _cache;

    /// <summary>سازنده</summary>
    public UpdateInsuranceCompanyCommandHandler(IUnitOfWork unitOfWork, IReferenceCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>اجرا — بازیابی، اعتبارسنجی و به‌روزرسانی</summary>
    public async Task<Result> Handle(UpdateInsuranceCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.Repository<InsuranceCompany>()
            .GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("شرکت بیمه", request.Id));

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure(new Error("INSURANCE.NAME", "نام شرکت بیمه الزامی است"));

        // نام یکتا — به‌جز خود رکورد
        var duplicate = await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
            .AnyAsync(c => c.Name == request.Name.Trim() && c.Id != request.Id, cancellationToken);
        if (duplicate)
            return Result.Failure(new Error("INSURANCE.DUPLICATE", "شرکت بیمه‌ای با این نام قبلاً ثبت شده است"));

        entity.Name = request.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        entity.Type = (InsuranceType)request.Type;
        entity.DefaultCoveragePercent = request.CoveragePercent;
        entity.IsActive = request.IsActive;
        if (request.IsContracted.HasValue) entity.IsContracted = request.IsContracted.Value;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // چرخهٔ ۶۰ — باطل‌سازی فهرست بیمه‌ها پس از ویرایش
        _cache.Evict(ReferenceCacheKeys.InsuranceCompanies);
        return Result.Success();
    }
}

/// <summary>فرمان غیرفعال‌سازی (حذف نرم) بیمه — سوابق تاریخی خراب نمی‌شوند</summary>
public record DeactivateInsuranceCompanyCommand(long Id) : IRequest<Result>;

/// <summary>گردانندهٔ غیرفعال‌سازی بیمه</summary>
public class DeactivateInsuranceCompanyCommandHandler
    : IRequestHandler<DeactivateInsuranceCompanyCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>کش مرجع — باطل‌سازی پس از غیرفعال‌سازی (چرخهٔ ۶۰)</summary>
    private readonly IReferenceCache _cache;

    /// <summary>سازنده</summary>
    public DeactivateInsuranceCompanyCommandHandler(IUnitOfWork unitOfWork, IReferenceCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    /// <summary>اجرا — فقط غیرفعال می‌کند؛ سوابق پذیرش/فاکتور بیمه دست‌نخورده می‌مانند</summary>
    public async Task<Result> Handle(DeactivateInsuranceCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.Repository<InsuranceCompany>()
            .GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("شرکت بیمه", request.Id));

        entity.IsActive = false;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        // چرخهٔ ۶۰ — باطل‌سازی فهرست بیمه‌ها پس از غیرفعال‌سازی
        _cache.Evict(ReferenceCacheKeys.InsuranceCompanies);
        return Result.Success();
    }
}
