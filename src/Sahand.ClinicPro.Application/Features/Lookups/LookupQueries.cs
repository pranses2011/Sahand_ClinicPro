using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Entities.Billing;

namespace Sahand.ClinicPro.Application.Features.Lookups;

/// <summary>گزینه بیمه برای فهرست انتخاب</summary>
public record InsuranceCompanyOptionDto(long Id, string Name, string? Code);

/// <summary>گزینه تشخیص ICD-10</summary>
public record DiagnosisOptionDto(long Id, string Code, string Title, string? Description);

/// <summary>
/// گزینه دارو برای اقلام نسخه — چرخهٔ ۳۹: نوع قلم (دارو/لوازم مصرفی) هم
/// همراه می‌آید تا فهرست‌های انتخاب بتوانند اقلام را برچسب‌گذاری کنند
/// </summary>
public record MedicationOptionDto(
    long Id,
    string Name,
    string? Form,
    string? Strength,
    bool IsNarcotic,
    // نوع قلم — ۱ دارو، ۲ لوازم مصرفی (چرخهٔ ۳۹) — کامنت عادی: درس CS1587 چرخهٔ ۳۶
    int ItemKind = 1,
    // متن فارسی نوع — «دارو» یا «لوازم مصرفی»
    string KindText = "دارو");

/// <summary>گزینه آزمایش برای درخواست</summary>
public record LabTestOptionDto(long Id, string Name, string? Category, string? Unit, decimal Price);

/// <summary>
/// پرس‌وجوی شرکت‌های بیمه فعال — برای ComboBox فرم‌ها
/// </summary>
public record GetInsuranceCompaniesQuery : IRequest<Result<List<InsuranceCompanyOptionDto>>>;

/// <summary>گرداننده فهرست بیمه‌ها</summary>
public class GetInsuranceCompaniesQueryHandler
    : IRequestHandler<GetInsuranceCompaniesQuery, Result<List<InsuranceCompanyOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetInsuranceCompaniesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — بیمه‌های فعال به ترتیب نام</summary>
    public async Task<Result<List<InsuranceCompanyOptionDto>>> Handle(GetInsuranceCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Domain.Entities.Billing.InsuranceCompany>()
            .QueryableNoTracking
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new InsuranceCompanyOptionDto(c.Id, c.Name, c.Code))
            .ToListAsync(cancellationToken);

        return Result<List<InsuranceCompanyOptionDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی جستجوی تشخیص ICD-10 — برای ویرایشگر SOAP
/// </summary>
/// <param name="Search">عبارت جستجو در کد یا شرح</param>
/// <param name="Take">حداکثر تعداد نتیجه</param>
public record GetDiagnosesQuery(string? Search = null, int Take = 30)
    : IRequest<Result<List<DiagnosisOptionDto>>>;

/// <summary>گرداننده جستجوی تشخیص</summary>
public class GetDiagnosesQueryHandler
    : IRequestHandler<GetDiagnosesQuery, Result<List<DiagnosisOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetDiagnosesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — جستجو در کد و شرح فارسی و لاتین</summary>
    public async Task<Result<List<DiagnosisOptionDto>>> Handle(GetDiagnosesQuery request,
        CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();
        var query = _unitOfWork.Repository<Domain.Entities.Clinical.Icd10Code>()
            .QueryableNoTracking
            .Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d =>
                d.Code.Contains(search) ||
                (d.DescriptionFa != null && d.DescriptionFa.Contains(search)) ||
                d.Description.Contains(search));

        var items = await query
            .OrderBy(d => d.Code)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(d => new DiagnosisOptionDto(
                d.Id, d.Code,
                d.DescriptionFa ?? d.Description,
                d.DescriptionFa != null ? d.Description : null))
            .ToListAsync(cancellationToken);

        return Result<List<DiagnosisOptionDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی جستجوی دارو — برای اقلام نسخه
/// </summary>
/// <param name="Search">عبارت جستجو در نام</param>
/// <param name="Take">حداکثر تعداد نتیجه</param>
/// <param name="ItemKind">فیلتر نوع قلم — چرخهٔ ۳۹: دارو (۱)، لوازم مصرفی (۲) یا هر دو (تهی)</param>
public record GetMedicationsQuery(string? Search = null, int Take = 30, MedicationItemKind? ItemKind = null)
    : IRequest<Result<List<MedicationOptionDto>>>;

/// <summary>گرداننده جستجوی دارو</summary>
public class GetMedicationsQueryHandler
    : IRequestHandler<GetMedicationsQuery, Result<List<MedicationOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetMedicationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — جستجو در نام ژنریک فارسی/لاتین و برند</summary>
    public async Task<Result<List<MedicationOptionDto>>> Handle(GetMedicationsQuery request,
        CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();
        var query = _unitOfWork.Repository<Domain.Entities.Clinical.Medication>()
            .QueryableNoTracking
            .Where(m => !m.IsDeleted && m.IsActive);

        // چرخهٔ ۳۹ — فیلتر نوع قلم: داروخانه مصرفی فقط لوازم، نسخه‌نویسی فقط دارو
        if (request.ItemKind.HasValue)
            query = query.Where(m => m.ItemKind == request.ItemKind.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m =>
                (m.GenericNameFa != null && m.GenericNameFa.Contains(search)) ||
                m.GenericName.Contains(search) ||
                (m.BrandName != null && m.BrandName.Contains(search)));

        var items = await query
            .OrderBy(m => m.GenericNameFa ?? m.GenericName)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(m => new MedicationOptionDto(
                m.Id, m.GenericNameFa ?? m.GenericName, m.Form, m.Strength, m.IsNarcotic,
                // چرخهٔ ۳۹ — نوع قلم + متن فارسی آن (ترجمهٔ CASE به SQL)
                (int)m.ItemKind,
                m.ItemKind == MedicationItemKind.Consumable ? "لوازم مصرفی" : "دارو"))
            .ToListAsync(cancellationToken);

        return Result<List<MedicationOptionDto>>.Success(items);
    }
}

/// <summary>
/// پرس‌وجوی آزمایش‌های فعال — برای فرم درخواست آزمایش
/// </summary>
/// <param name="Search">فیلتر نام — اختیاری</param>
public record GetLabTestsQuery(string? Search = null) : IRequest<Result<List<LabTestOptionDto>>>;

/// <summary>گرداننده فهرست آزمایش‌ها</summary>
public class GetLabTestsQueryHandler
    : IRequestHandler<GetLabTestsQuery, Result<List<LabTestOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetLabTestsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — آزمایش‌های فعال به ترتیب دسته و نام</summary>
    public async Task<Result<List<LabTestOptionDto>>> Handle(GetLabTestsQuery request,
        CancellationToken cancellationToken)
    {
        var search = request.Search?.Trim();
        var query = _unitOfWork.Repository<Domain.Entities.Paraclinical.LabTest>()
            .QueryableNoTracking
            .Where(t => !t.IsDeleted && t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.TestName.Contains(search) ||
                (t.TestNameEn != null && t.TestNameEn.Contains(search)) ||
                (t.TestCode != null && t.TestCode.Contains(search)));

        var items = await query
            .OrderBy(t => t.Category).ThenBy(t => t.TestName)
            .Take(200)
            .Select(t => new LabTestOptionDto(t.Id, t.TestName, t.Category, t.Unit, t.Price))
            .ToListAsync(cancellationToken);

        return Result<List<LabTestOptionDto>>.Success(items);
    }
}

/// <summary>قلم آزمایش در جزئیات درخواست</summary>
public record LabOrderItemDetailDto(
    long Id, string TestName, string? Unit, string? NormalRange,
    LabItemStatus Status, string? Result, decimal? ResultNumeric,
    bool IsAbnormal, string? Comment);

/// <summary>جزئیات درخواست آزمایش با اقلام</summary>
public record LabOrderDetailDto(
    long Id, string? OrderNumber, string PatientName, string DoctorName,
    string DateShamsi, LabOrderStatus Status, Priority Priority,
    string? ClinicalInfo, List<LabOrderItemDetailDto> Items);

/// <summary>
/// پرس‌وجوی جزئیات درخواست آزمایش — برای ثبت نتیجه قلم‌به‌قلم
/// </summary>
/// <param name="Id">شناسه درخواست</param>
public record GetLabOrderByIdQuery(long Id) : IRequest<Result<LabOrderDetailDto>>;

/// <summary>گرداننده جزئیات درخواست آزمایش</summary>
public class GetLabOrderByIdQueryHandler
    : IRequestHandler<GetLabOrderByIdQuery, Result<LabOrderDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetLabOrderByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — درخواست با اقلام و نام آزمایش‌ها</summary>
    public async Task<Result<LabOrderDetailDto>> Handle(GetLabOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Repository<Domain.Entities.Paraclinical.LabOrder>()
            .QueryableNoTracking
            .Include(o => o.Patient)
            .Include(o => o.Doctor).ThenInclude(d => d!.Employee)
            .Include(o => o.Items).ThenInclude(i => i.Test)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
            return Result<LabOrderDetailDto>.Failure(Error.NotFound("درخواست آزمایش", request.Id));

        var dto = new LabOrderDetailDto(
            order.Id, order.OrderNumber,
            order.Patient != null ? order.Patient.FirstName + " " + order.Patient.LastName : "",
            order.Doctor != null && order.Doctor.Employee != null
                ? order.Doctor.Employee.FirstName + " " + order.Doctor.Employee.LastName : "پزشک",
            order.OrderDate.HasValue ? PersianDateHelper.ToShamsi(order.OrderDate.Value) : string.Empty,
            order.Status, order.Priority, order.ClinicalInfo,
            order.Items
                .OrderBy(i => i.Id)
                .Select(i => new LabOrderItemDetailDto(
                    i.Id,
                    i.Test != null ? i.Test.TestName : "آزمایش",
                    i.Unit, i.NormalRange, i.Status,
                    i.Result, i.ResultNumeric, i.IsAbnormal, i.Comment))
                .ToList());

        return Result<LabOrderDetailDto>.Success(dto);
    }
}


/// <summary>آیتم تخصص برای لیست انتخاب</summary>
public class SpecialtyItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام تخصص</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>شناسه والد — فوق‌تخصص‌ها بر اساس والد دسته می‌شوند</summary>
    public long? ParentId { get; set; }
}

/// <summary>آیتم اتاق معاینه</summary>
public class RoomItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام نمایشی</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>آیتم پرسنل برای انتخاب در فرم پزشک</summary>
public class EmployeeItemDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>نام کامل</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>شماره پرسنلی</summary>
    public string? PersonnelCode { get; set; }
}

/// <summary>پرس‌وجوی تخصص‌های فعال</summary>
public record GetSpecialtiesQuery(bool OnlyParents = false) : IRequest<Result<List<SpecialtyItemDto>>>;

/// <summary>گرداننده تخصص‌ها</summary>
public class GetSpecialtiesQueryHandler : IRequestHandler<GetSpecialtiesQuery, Result<List<SpecialtyItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetSpecialtiesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<SpecialtyItemDto>>> Handle(GetSpecialtiesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Specialty>().QueryableNoTracking
            .Where(s => s.IsActive);
        if (request.OnlyParents)
            query = query.Where(s => s.ParentId == null);

        var items = await query.OrderBy(s => s.Name)
            .Select(s => new SpecialtyItemDto { Id = s.Id, Name = s.Name, ParentId = s.ParentId })
            .ToListAsync(cancellationToken);

        return Result<List<SpecialtyItemDto>>.Success(items);
    }
}

/// <summary>پرس‌وجوی اتاق‌های معاینه فعال</summary>
public record GetRoomsQuery : IRequest<Result<List<RoomItemDto>>>;

/// <summary>گرداننده اتاق‌ها</summary>
public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, Result<List<RoomItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetRoomsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<RoomItemDto>>> Handle(GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<ExaminationRoom>().QueryableNoTracking
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => new RoomItemDto
            {
                Id = r.Id,
                DisplayName = r.RoomName != null && r.RoomName != "" ? r.RoomName :
                    ("اتاق " + (r.RoomNumber != null ? r.RoomNumber : r.Id.ToString()))
            })
            .ToListAsync(cancellationToken);

        return Result<List<RoomItemDto>>.Success(items);
    }
}

/// <summary>پرس‌وجوی پرسنل فعال برای Combobox فرم پزشک</summary>
public record GetEmployeesQuery : IRequest<Result<List<EmployeeItemDto>>>;

/// <summary>گرداننده پرسنل</summary>
public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeItemDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetEmployeesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<List<EmployeeItemDto>>> Handle(GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName)
            .Select(e => new EmployeeItemDto
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                PersonnelCode = e.PersonnelCode
            })
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeItemDto>>.Success(items);
    }
}


/// <summary>آیتم خدمت با قیمت تعرفه جاری — برای اقلام فاکتور (گام U8)</summary>
public record ServiceTariffOptionDto(
    long ServiceId,
    string? ServiceCode,
    string ServiceName,
    string ServiceTypeName,
    decimal Price,
    decimal InsuranceShare,
    decimal PatientShare);

/// <summary>پرس‌وجوی خدمات فعال با قیمت تعرفه سال جاری — چرخه ۲۴: پارامتر
/// InsuranceCompanyId تعیین می‌کند تعرفهٔ «کدام هدف» خوانده شود؛ تهی = آزاد.
/// تعرفهٔ بیمهٔ منتخب → در نبود، تعرفهٔ آزاد → در نبود، قیمت پایهٔ خدمت</summary>
public record GetServicesQuery(string? Search = null, long? InsuranceCompanyId = null)
    : IRequest<Result<List<ServiceTariffOptionDto>>>;

/// <summary>گرداننده خدمات با تعرفه</summary>
public class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, Result<List<ServiceTariffOptionDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetServicesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — خدمت فعال + قیمت تعرفهٔ هدف منتخب (بیمه یا آزاد) یا قیمت پایه</summary>
    public async Task<Result<List<ServiceTariffOptionDto>>> Handle(GetServicesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Service>().QueryableNoTracking
            .Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.ServiceName.Contains(term) ||
                                     x.ServiceCode != null && x.ServiceCode.Contains(term));
        }

        var services = await query.OrderBy(x => x.ServiceName).Take(60).ToListAsync(cancellationToken);
        var serviceIds = services.Select(x => x.Id).ToList();

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — تعرفهٔ به‌تفکیک بیمه ═══
        // «تعرفه‌ها برای بیمه‌ها متفاوت می‌تواند باشد و برای آزاد هم تعرفه
        // تعریف بشود» — هر دو ردیف تعرفه (هدف بیمه + آزاد) خوانده می‌شود و
        // حل‌کننده: ۱) تعرفهٔ بیمهٔ منتخب ۲) تعرفهٔ آزاد ۳) قیمت پایهٔ خدمت
        var tariffs = await _unitOfWork.Repository<Tariff>().QueryableNoTracking
            .Where(t => serviceIds.Contains(t.ServiceId) && t.IsActive &&
                        (t.InsuranceCompanyId == request.InsuranceCompanyId ||
                         (request.InsuranceCompanyId != null && t.InsuranceCompanyId == null)))
            .ToListAsync(cancellationToken);
        var tariffByService = tariffs.GroupBy(t => t.ServiceId)
            .ToDictionary(g => g.Key, g => g
                // اولویت: ردیف بیمهٔ منتخب (تهی نبودن InsuranceCompanyId) بالاتر از آزاد
                .OrderByDescending(t => t.InsuranceCompanyId == request.InsuranceCompanyId &&
                                        request.InsuranceCompanyId != null)
                .ThenByDescending(t => t.Year)
                .First());

        // ═══ چرخهٔ سی‌وهفتم (گزارش مالک) ═══ «تعرفه همه بیمه‌ها بهم ریخته و
        // همشون ۱۰۰٪ سهم بیمار شدن» — این کوئری مسیر پذیرش/صدور فاکتور است و
        // همان ریشهٔ پنجرهٔ مدیریت تعرفه را داشت: در نبود ردیف اختصاصی برای
        // بیمهٔ هدف، سهم‌ها از ردیف آزاد (۰/۱۰۰ پس از مهاجرت سی‌ودوم) خوانده
        // می‌شد و فاکتور بیمه‌ای تمام بار را به بیمار می‌انداخت. اکنون سهم‌های
        // ردیفِ «پایه» از تنظیمات خودِ بیمه (DefaultCoveragePercent و
        // هم‌خانواده‌ها — همان الگوی تسویه و پنجرهٔ مدیریت) محاسبه می‌شود؛
        // ردیف اختصاصی بیمه همچنان مقدم و بی‌تغییر است.
        InsuranceCompany? targetCompany = request.InsuranceCompanyId is long companyId
            ? await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            : null;
        var fallbackInsurancePercent = TariffFallbackShares.InsurancePercentOf(targetCompany);
        var fallbackPatientPercent = 100m - fallbackInsurancePercent;

        var items = new List<ServiceTariffOptionDto>();
        foreach (var x in services)
        {
            var tariff = tariffByService.TryGetValue(x.Id, out var t) ? t : null;
            // ═══ چرخهٔ سی‌ودوم (گزارش مالک) ═══ «برای بیمهٔ آزاد هم سهم بیمه و
            // سهم بیمار می‌اندازد که اشتباه است — در بدون بیمه باید تمام تعرفه را
            // بیمار پرداخت کند»: برای هدف «آزاد» (InsuranceCompanyId تهی) هر مقدار
            // ذخیره‌شده‌ای (ازجمله ردیف‌های قدیمیِ با درصدهای بیمه‌ای پیش از
            // مهاجرت سی‌ودوم) نادیده گرفته و همیشه ۰٪ بیمه / ۱۰۰٪ بیمار
            // برمی‌گردد — دفاع در عمق؛ درصد سهم فقط برای بیمهٔ واقعی معنا دارد.
            var isFreeTarget = request.InsuranceCompanyId is null;
            // ردیف حل‌شده «اختصاصیِ همین بیمه» است یا سقوط به تعرفهٔ پایه؟
            // (برای هدف بیمه‌ای، هر ردیف حل‌شده با InsuranceCompanyId مقداری،
            // همان ردیف اختصاصیِ بیمهٔ هدف است — شرط کوئری بالا)
            var isDedicatedRow = !isFreeTarget && tariff is not null && tariff.InsuranceCompanyId != null;
            items.Add(new ServiceTariffOptionDto(
                x.Id, x.ServiceCode, x.ServiceName,
                x.ServiceType.ToString(),
                tariff is not null ? tariff.Price : x.DefaultPrice,
                isFreeTarget ? 0m
                    : isDedicatedRow ? tariff!.InsuranceShare
                    : fallbackInsurancePercent,
                isFreeTarget ? 100m
                    : isDedicatedRow ? tariff!.PatientShare
                    : fallbackPatientPercent));
        }

        return Result<List<ServiceTariffOptionDto>>.Success(items);
    }
}
