using MediatR;
using Sahand.ClinicPro.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.Features.Lookups;

// ═══════════════════════════════════════════════════════════════════
// چرخه بیست‌وسوم (گزارش شانزدهم مالک):
// «در صفحه بیمه، بتوان تعرفه جدید تعریف کرد و یا تعرفه‌های موجود را ویرایش کرد»
// در این سامانه «تعرفه» = خدمت + قیمت تعرفه‌ای آزاد (ردیف Tariff با
// InsuranceCompanyId تهی برای همان سال). این ماژول ساخت/ویرایش/غیرفعال‌سازی
// خدمت‌های تعرفه‌داردار را با upsert خودکار ردیف تعرفهٔ آزاد انجام می‌دهد.
// ═══════════════════════════════════════════════════════════════════

/// <summary>سال تعرفهٔ جاری — سال شمسی امروز</summary>
internal static class TariffYear
{
    /// <summary>سال شمسی جاری از تاریخ میلادی UTC محاسبه می‌شود</summary>
    public static int Current
    {
        get
        {
            // تبدیل تقریبی میلادی→شمسی با همان الگوریتم PersianDateHelper (بدون وابستگی)
            var now = DateTime.UtcNow;
            // فرمول تبدیل جلالی استاندارد (الگوریتم بیرشند/کره‌ای ساده‌شده)
            var persian = new System.Globalization.PersianCalendar();
            return persian.GetYear(now);
        }
    }
}

/// <summary>آیتم خدمت/تعرفه برای پنجرهٔ مدیریت تعرفه‌ها.
/// HasTargetTariff (چرخه ۲۴): تعرفهٔ اختصاصی برای هدف منتخب (بیمه) دارد؟</summary>
public record ServiceTariffManageDto(
    long Id,
    string? ServiceCode,
    string ServiceName,
    decimal Price,
    decimal InsuranceShare,
    decimal PatientShare,
    bool IsActive,
    bool HasTargetTariff = false);

/// <summary>پرس‌وجوی فهرست خدمت‌های تعرفه‌دار (پنجرهٔ مدیریت تعرفه‌ها) — شامل غیرفعال.
/// چرخه بیست‌وچهارم: پارامتر InsuranceCompanyId هدف را تعیین می‌کند — تهی = آزاد.</summary>
public record GetServicesTariffsForManageQuery(long? InsuranceCompanyId = null)
    : IRequest<Result<List<ServiceTariffManageDto>>>;

/// <summary>گردانندهٔ فهرست تعرفه‌ها برای مدیریت</summary>
public class GetServicesTariffsForManageQueryHandler
    : IRequestHandler<GetServicesTariffsForManageQuery, Result<List<ServiceTariffManageDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetServicesTariffsForManageQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// اجرا — خدمت‌ها (شامل غیرفعال) + تعرفهٔ هدف منتخب (بیمه یا آزاد).
    /// برای هدف بیمه: تعرفهٔ بیمه مقدم، در نبود آن تعرفهٔ آزاد (نمایش
    /// «مقدار پایه»)؛ HasTargetTariff نشان می‌دهد تعرفهٔ اختصاصی دارد یا نه.
    /// </summary>
    public async Task<Result<List<ServiceTariffManageDto>>> Handle(GetServicesTariffsForManageQuery request,
        CancellationToken cancellationToken)
    {
        var services = await _unitOfWork.Repository<Service>().QueryableNoTracking
            .OrderBy(s => s.ServiceName)
            .ToListAsync(cancellationToken);
        var serviceIds = services.Select(s => s.Id).ToList();

        // ردیف‌های تعرفهٔ هدف منتخب + آزاد (برای fallback نمایشی)
        var tariffs = await _unitOfWork.Repository<Tariff>().QueryableNoTracking
            .Where(t => serviceIds.Contains(t.ServiceId) &&
                        (t.InsuranceCompanyId == request.InsuranceCompanyId ||
                         (request.InsuranceCompanyId != null && t.InsuranceCompanyId == null)))
            .ToListAsync(cancellationToken);

        Tariff? Resolve(long serviceId) => tariffs
            .Where(t => t.ServiceId == serviceId)
            .OrderByDescending(t => t.InsuranceCompanyId == request.InsuranceCompanyId &&
                                    request.InsuranceCompanyId != null)
            .ThenByDescending(t => t.Year)
            .FirstOrDefault();

        var items = services.Select(s =>
        {
            var t = Resolve(s.Id);
            var hasTarget = request.InsuranceCompanyId != null && tariffs.Any(x =>
                x.ServiceId == s.Id && x.InsuranceCompanyId == request.InsuranceCompanyId);
            return new ServiceTariffManageDto(
                s.Id, s.ServiceCode, s.ServiceName,
                t is not null ? t.Price : s.DefaultPrice,
                t is not null ? t.InsuranceShare : 0m,
                t is not null ? t.PatientShare : 0m,
                s.IsActive, hasTarget);
        }).ToList();

        return Result<List<ServiceTariffManageDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ساخت خدمت تعرفه‌دار جدید — هم خدمت ساخته می‌شود هم ردیف تعرفهٔ
/// هدف منتخب سال جاری با قیمت/سهم‌های داده‌شده (upsert). چرخه ۲۴: پارامتر
/// InsuranceCompanyId هدف را تعیین می‌کند (تهی = تعرفهٔ آزاد/بدون بیمه).
/// </summary>
public record CreateServiceTariffCommand(
    string ServiceName,
    string? ServiceCode,
    decimal Price,
    decimal InsuranceShare,
    decimal PatientShare,
    long? InsuranceCompanyId = null) : IRequest<Result<long>>;

/// <summary>گردانندهٔ ساخت خدمت تعرفه‌دار</summary>
public class CreateServiceTariffCommandHandler
    : IRequestHandler<CreateServiceTariffCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateServiceTariffCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — اعتبارسنجی + درج خدمت + درج تعرفهٔ آزاد سال جاری</summary>
    public async Task<Result<long>> Handle(CreateServiceTariffCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName))
            return Result<long>.Failure(new Error("TARIFF.NAME", "نام خدمت الزامی است"));

        // کد خدمت اگر داده شود باید یکتا باشد
        if (!string.IsNullOrWhiteSpace(request.ServiceCode))
        {
            var dupCode = await _unitOfWork.Repository<Service>().QueryableNoTracking
                .AnyAsync(s => s.ServiceCode == request.ServiceCode.Trim(), cancellationToken);
            if (dupCode)
                return Result<long>.Failure(new Error("TARIFF.CODE", "خدمتی با این کد قبلاً ثبت شده است"));
        }

        var service = new Service
        {
            ServiceName = request.ServiceName.Trim(),
            ServiceCode = string.IsNullOrWhiteSpace(request.ServiceCode) ? null : request.ServiceCode.Trim(),
            // نوع پیش‌فرض «ویزیت» — از پنجرهٔ مدیریت تعرفه صرفاً قیمت مدنظر است
            ServiceType = ServiceType.Visit,
            DefaultPrice = request.Price
        };
        await _unitOfWork.Repository<Service>().AddAsync(service, cancellationToken);

        // ردیف تعرفهٔ هدف منتخب سال جاری — InsuranceCompanyId تهی یعنی تعرفهٔ آزاد
        var tariff = new Tariff
        {
            ServiceId = service.Id,
            InsuranceCompanyId = request.InsuranceCompanyId,
            Year = TariffYear.Current,
            Price = request.Price,
            InsuranceShare = request.InsuranceShare,
            PatientShare = request.PatientShare
        };
        await _unitOfWork.Repository<Tariff>().AddAsync(tariff, cancellationToken);

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(service.Id);
    }
}

/// <summary>
/// فرمان ویرایش خدمت تعرفه‌دار — نام/کد خدمت به‌روزرسانی و ردیف تعرفهٔ
/// هدف منتخب سال جاری upsert می‌شود (اگر تعرفهٔ امسال نباشد ساخته می‌شود).
/// چرخه ۲۴: InsuranceCompanyId هدف تعرفه (تهی = آزاد).
/// </summary>
public record UpdateServiceTariffCommand(
    long Id,
    string ServiceName,
    string? ServiceCode,
    decimal Price,
    decimal InsuranceShare,
    decimal PatientShare,
    bool IsActive,
    long? InsuranceCompanyId = null) : IRequest<Result>;

/// <summary>گردانندهٔ ویرایش خدمت تعرفه‌دار</summary>
public class UpdateServiceTariffCommandHandler
    : IRequestHandler<UpdateServiceTariffCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public UpdateServiceTariffCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — به‌روزرسانی خدمت + upsert تعرفهٔ آزاد سال جاری</summary>
    public async Task<Result> Handle(UpdateServiceTariffCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.Id, cancellationToken);
        if (service is null)
            return Result.Failure(Error.NotFound("خدمت/تعرفه", request.Id));

        if (string.IsNullOrWhiteSpace(request.ServiceName))
            return Result.Failure(new Error("TARIFF.NAME", "نام خدمت الزامی است"));

        // کد یکتا — به‌جز خود رکورد
        if (!string.IsNullOrWhiteSpace(request.ServiceCode))
        {
            var dupCode = await _unitOfWork.Repository<Service>().QueryableNoTracking
                .AnyAsync(s => s.ServiceCode == request.ServiceCode.Trim() && s.Id != request.Id, cancellationToken);
            if (dupCode)
                return Result.Failure(new Error("TARIFF.CODE", "خدمت دیگری با این کد ثبت شده است"));
        }

        service.ServiceName = request.ServiceName.Trim();
        service.ServiceCode = string.IsNullOrWhiteSpace(request.ServiceCode) ? null : request.ServiceCode.Trim();
        service.DefaultPrice = request.Price;
        service.IsActive = request.IsActive;

        // ═══ چرخه ۲۴: upsert تعرفهٔ هدف منتخب (بیمه یا آزاد) سال جاری ═══
        var currentYearTariff = await _unitOfWork.Repository<Tariff>().QueryableNoTracking
            .Where(t => t.ServiceId == service.Id &&
                        t.InsuranceCompanyId == request.InsuranceCompanyId &&
                        t.Year == TariffYear.Current)
            .FirstOrDefaultAsync(cancellationToken);
        if (currentYearTariff is null)
        {
            await _unitOfWork.Repository<Tariff>().AddAsync(new Tariff
            {
                ServiceId = service.Id,
                InsuranceCompanyId = request.InsuranceCompanyId,
                Year = TariffYear.Current,
                Price = request.Price,
                InsuranceShare = request.InsuranceShare,
                PatientShare = request.PatientShare
            }, cancellationToken);
        }
        else
        {
            // موجودیت ردیابی‌شده لازم است — پرس‌وجوی ردیابی‌دار
            var tracked = await _unitOfWork.Repository<Tariff>().QueryableTracking
                .FirstAsync(t => t.Id == currentYearTariff.Id, cancellationToken);
            tracked.Price = request.Price;
            tracked.InsuranceShare = request.InsuranceShare;
            tracked.PatientShare = request.PatientShare;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>فرمان غیرفعال‌سازی خدمت — سوابق فاکتور دست‌نخورده می‌مانند</summary>
public record DeactivateServiceCommand(long Id) : IRequest<Result>;

/// <summary>گردانندهٔ غیرفعال‌سازی خدمت</summary>
public class DeactivateServiceCommandHandler
    : IRequestHandler<DeactivateServiceCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public DeactivateServiceCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — غیرفعال‌سازی نرم</summary>
    public async Task<Result> Handle(DeactivateServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await _unitOfWork.Repository<Service>().GetByIdAsync(request.Id, cancellationToken);
        if (service is null)
            return Result.Failure(Error.NotFound("خدمت", request.Id));

        service.IsActive = false;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
