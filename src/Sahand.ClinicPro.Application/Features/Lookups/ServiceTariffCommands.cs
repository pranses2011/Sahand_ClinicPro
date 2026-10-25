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

// ═══ چرخهٔ سی‌وهفتم (گزارش مالک) ═══ سهم پیش‌فرض بیمهٔ هدف برای ردیف‌های
// «پایه» (بدون تعرفهٔ اختصاصی) — الگوی اولویت همان FractionmentCalculator:
// DefaultCoveragePercent مقدم، در نبود آن DefaultInsuranceShare، در نبود ۷۰.
internal static class TariffFallbackShares
{
    /// <summary>
    /// درصد سهم بیمه برای هدف بیمه‌ایِ فاقد ردیف تعرفهٔ اختصاصی —
    /// مقدار را از تنظیمات خود شرکت بیمه می‌خواند (نه از ردیف آزاد).
    /// </summary>
    public static decimal InsurancePercentOf(InsuranceCompany? company) =>
        company is null ? 0m
        : company.DefaultCoveragePercent > 0 ? company.DefaultCoveragePercent
        : company.DefaultInsuranceShare > 0 ? company.DefaultInsuranceShare
        : 70m;
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
    bool HasTargetTariff = false,
    decimal DefaultCost = 0m);

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

        // ═══ چرخهٔ سی‌وهفتم (گزارش مالک) ═══ «تعرفه همه بیمه‌ها بهم ریخته و
        // همشون ۱۰۰٪ سهم بیمار شدن» — ریشه: در نبود ردیف اختصاصی برای بیمهٔ
        // هدف، سهم‌ها از ردیف آزاد خوانده می‌شد که پس از مهاجرت سی‌ودوم
        // (قاعدهٔ درستِ آزاد = ۰/۱۰۰) دیگر برای بیمه معنایی ندارد و همه‌جا
        // ۱۰۰٪ بیمار نشان می‌داد. اکنون سهم‌های ردیفِ «پایه» از تنظیمات خودِ
        // بیمه (DefaultCoveragePercent و هم‌خانواده‌هایش — همان الگوی تسویه)
        // محاسبه می‌شود؛ قیمت همچنان از تعرفهٔ پایه است و ردیف اختصاصی
        // (نشان «اختصاصی») همچنان مقدم و بی‌تغییر است.
        InsuranceCompany? targetCompany = request.InsuranceCompanyId is long companyId
            ? await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            : null;
        var fallbackInsurancePercent = TariffFallbackShares.InsurancePercentOf(targetCompany);
        var fallbackPatientPercent = 100m - fallbackInsurancePercent;

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
            // ═══ چرخهٔ سی‌ودوم (گزارش مالک) ═══ «وقتی آزاد را انتخاب می‌کنم در
            // لیست همانند بیمه‌ها درصد بیمه و بیمار دارد» — هدف «آزاد» همیشه
            // ۰٪ بیمه / ۱۰۰٪ بیمار نمایش می‌دهد؛ مقادیر بیمه‌ای قدیمیِ ردیف آزاد
            // (پیش از مهاجرت سی‌ودوم) هم پنهان و اصلاح‌شده دیده می‌شوند.
            var isFreeTarget = request.InsuranceCompanyId is null;
            // ردیف حل‌شده «اختصاصیِ همین بیمه» است یا سقوط به تعرفهٔ پایه؟
            // (برای هدف بیمه‌ای، هر ردیف حل‌شده با InsuranceCompanyId مقداری،
            // همان ردیف اختصاصیِ بیمهٔ هدف است — شرط کوئری بالا)
            var isDedicatedRow = !isFreeTarget && t is not null && t.InsuranceCompanyId != null;
            return new ServiceTariffManageDto(
                s.Id, s.ServiceCode, s.ServiceName,
                t is not null ? t.Price : s.DefaultPrice,
                isFreeTarget ? 0m
                    : isDedicatedRow ? t!.InsuranceShare
                    : fallbackInsurancePercent,
                isFreeTarget ? 100m
                    : isDedicatedRow ? t!.PatientShare
                    : fallbackPatientPercent,
                s.IsActive, hasTarget, s.DefaultCost);
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
    long? InsuranceCompanyId = null,
    decimal DefaultCost = 0m) : IRequest<Result<long>>;

/// <summary>
/// ساخت کد خودکار خدمت — قالب SRV-0001 و یکتا. وقتی کاربر فیلد کد را خالی
/// می‌گذارد صدا زده می‌شود؛ روی کد خدمت ایندکس یکتا هست و SQL Server فقط
/// یک کد خالی (NULL) را می‌پذیرد، پس خالی‌گذاشتن کد پیش‌تر با خدمت دوم به
/// خطای دیتابیس و پیام «خطای غیرمنتظره در سرور» می‌رسید (گزارش مالک).
/// </summary>
internal static class ServiceCodeGenerator
{
    public static async Task<string> GenerateAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var repo = unitOfWork.Repository<Service>();
        var next = await repo.QueryableNoTracking.CountAsync(cancellationToken) + 1;
        while (true)
        {
            var code = $"SRV-{next:0000}";
            if (!await repo.QueryableNoTracking.AnyAsync(s => s.ServiceCode == code, cancellationToken))
                return code;
            next++;
        }
    }
}

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
        if (request.ServiceName.Trim().Length > 50)
            return Result<long>.Failure(new Error("TARIFF.NAME", "نام خدمت حداکثر ۵۰ نویسه می‌تواند باشد"));
        if (!string.IsNullOrWhiteSpace(request.ServiceCode) && request.ServiceCode.Trim().Length > 20)
            return Result<long>.Failure(new Error("TARIFF.CODE", "کد خدمت حداکثر ۲۰ نویسه می‌تواند باشد"));
        if (request.Price < 0m || request.DefaultCost < 0m)
            return Result<long>.Failure(new Error("TARIFF.PRICE", "قیمت و هزینه خدمت نمی‌توانند منفی باشند"));

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
            // کد خالی = ساخت خودکار کد یکتا (SRV-0001...) — درخواست مالک
            ServiceCode = string.IsNullOrWhiteSpace(request.ServiceCode)
                ? await ServiceCodeGenerator.GenerateAsync(_unitOfWork, cancellationToken)
                : request.ServiceCode.Trim(),
            // نوع پیش‌فرض «ویزیت» — از پنجرهٔ مدیریت تعرفه صرفاً قیمت مدنظر است
            ServiceType = ServiceType.Visit,
            DefaultPrice = request.Price,
            DefaultCost = request.DefaultCost
        };
        await _unitOfWork.Repository<Service>().AddAsync(service, cancellationToken);

        // ═══ ذخیرهٔ دو مرحله‌ای (گزارش مالک — خطای FK_Tariffs_Services_ServiceId) ═══
        // پیش‌تر تعرفه در همان ذخیرهٔ اول با ServiceId = service.Id ساخته می‌شد؛
        // اما شناسهٔ خدمت پیش از ذخیره هنوز مقدار موقتِ EF است و روی SQL Server
        // واقعی (برخلاف InMemory که کلید خارجی را کنترل نمی‌کند) انتشار شناسهٔ
        // موقت به ردیف تعرفه همیشه قابل اتکا نیست و درج تعرفه با شناسه‌ای انجام
        // می‌شد که در جدول Services وجود نداشت → خطای کلید خارجی. اکنون اول خودِ
        // خدمت ذخیره می‌شود تا شناسهٔ واقعی از دیتابیس برگردد، بعد تعرفه با همان
        // شناسهٔ قطعی ساخته و ذخیره می‌شود.
        try
        {
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            if (detail.Length > 200) detail = detail[..200] + "…";
            return Result<long>.Failure(new Error("TARIFF.DB", $"ذخیرهٔ خدمت در دیتابیس ناموفق بود: {detail}"));
        }

        if (service.Id <= 0)
            return Result<long>.Failure(new Error("TARIFF.DB",
                "شناسهٔ خدمت از دیتابیس برنگشت؛ ساختار جدول Services روی سرور نیازمند بررسی است (ستون Id باید Identity باشد)"));

        // ردیف تعرفهٔ هدف منتخب سال جاری — InsuranceCompanyId تهی یعنی تعرفهٔ آزاد
        // ═══ چرخهٔ سی‌ودوم ═══ تعرفهٔ آزاد همیشه ۱۰۰٪ سهم بیمار است — سهم‌های
        // ارسالی کلاینت برای هدف آزاد نادیده و قاعدهٔ استاندارد (۰/۱۰۰) ذخیره
        // می‌شود تا هیچ مسیری (حتی کلاینت قدیمی) درصد بیمه‌ای روی ردیف آزاد ننویسد
        var isFreeTarget = request.InsuranceCompanyId is null;
        var tariff = new Tariff
        {
            ServiceId = service.Id,
            InsuranceCompanyId = request.InsuranceCompanyId,
            Year = TariffYear.Current,
            Price = request.Price,
            InsuranceShare = isFreeTarget ? 0m : request.InsuranceShare,
            PatientShare = isFreeTarget ? 100m : request.PatientShare
        };
        await _unitOfWork.Repository<Tariff>().AddAsync(tariff, cancellationToken);

        try
        {
            await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // خطای دیتابیس (یکتایی/طول/محدودیت) نباید «خطای غیرمنتظره» شود —
            // جزئیات کوتاهِ واقعی به کاربر برمی‌گردد تا علت دیده شود
            var detail = ex.InnerException?.Message ?? ex.Message;
            if (detail.Length > 200) detail = detail[..200] + "…";
            return Result<long>.Failure(new Error("TARIFF.DB", $"ذخیرهٔ خدمت در دیتابیس ناموفق بود: {detail}"));
        }
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
    long? InsuranceCompanyId = null,
    decimal? DefaultCost = null) : IRequest<Result>;

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
        if (request.ServiceName.Trim().Length > 50)
            return Result.Failure(new Error("TARIFF.NAME", "نام خدمت حداکثر ۵۰ نویسه می‌تواند باشد"));
        if (!string.IsNullOrWhiteSpace(request.ServiceCode) && request.ServiceCode.Trim().Length > 20)
            return Result.Failure(new Error("TARIFF.CODE", "کد خدمت حداکثر ۲۰ نویسه می‌تواند باشد"));

        if (request.Price < 0m || (request.DefaultCost.HasValue && request.DefaultCost.Value < 0m))
            return Result.Failure(new Error("TARIFF.PRICE", "قیمت و هزینه خدمت نمی‌توانند منفی باشند"));

        // کد یکتا — به‌جز خود رکورد
        if (!string.IsNullOrWhiteSpace(request.ServiceCode))
        {
            var dupCode = await _unitOfWork.Repository<Service>().QueryableNoTracking
                .AnyAsync(s => s.ServiceCode == request.ServiceCode.Trim() && s.Id != request.Id, cancellationToken);
            if (dupCode)
                return Result.Failure(new Error("TARIFF.CODE", "خدمت دیگری با این کد ثبت شده است"));
        }

        service.ServiceName = request.ServiceName.Trim();
        // کد خالی در ویرایش: اگر خدمت از قبل کد دارد همان می‌ماند، وگرنه
        // خودکار ساخته می‌شود تا ردیف بدون کد (NULL دوم = خطای ایندکس یکتا) نماند
        service.ServiceCode = string.IsNullOrWhiteSpace(request.ServiceCode)
            ? service.ServiceCode ?? await ServiceCodeGenerator.GenerateAsync(_unitOfWork, cancellationToken)
            : request.ServiceCode.Trim();
        service.DefaultPrice = request.Price;
        if (request.DefaultCost.HasValue)
            service.DefaultCost = request.DefaultCost.Value;
        service.IsActive = request.IsActive;

        // ═══ چرخه ۲۴: upsert تعرفهٔ هدف منتخب (بیمه یا آزاد) سال جاری ═══
        // چرخهٔ سی‌ودوم: برای هدف آزاد سهم‌ها همیشه ۰/۱۰۰ ذخیره می‌شود
        var isFreeTarget = request.InsuranceCompanyId is null;
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
                InsuranceShare = isFreeTarget ? 0m : request.InsuranceShare,
                PatientShare = isFreeTarget ? 100m : request.PatientShare
            }, cancellationToken);
        }
        else
        {
            // موجودیت ردیابی‌شده لازم است — پرس‌وجوی ردیابی‌دار
            var tracked = await _unitOfWork.Repository<Tariff>().QueryableTracking
                .FirstAsync(t => t.Id == currentYearTariff.Id, cancellationToken);
            tracked.Price = request.Price;
            tracked.InsuranceShare = isFreeTarget ? 0m : request.InsuranceShare;
            tracked.PatientShare = isFreeTarget ? 100m : request.PatientShare;
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
