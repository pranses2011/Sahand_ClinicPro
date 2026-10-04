using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Lookups;

/// <summary>گزینه بیمه برای فهرست انتخاب</summary>
public record InsuranceCompanyOptionDto(long Id, string Name, string? Code);

/// <summary>گزینه تشخیص ICD-10</summary>
public record DiagnosisOptionDto(long Id, string Code, string Title, string? Description);

/// <summary>گزینه دارو برای اقلام نسخه</summary>
public record MedicationOptionDto(long Id, string Name, string? Form, string? Strength, bool IsNarcotic);

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
public record GetMedicationsQuery(string? Search = null, int Take = 30)
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

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m =>
                (m.GenericNameFa != null && m.GenericNameFa.Contains(search)) ||
                m.GenericName.Contains(search) ||
                (m.BrandName != null && m.BrandName.Contains(search)));

        var items = await query
            .OrderBy(m => m.GenericNameFa ?? m.GenericName)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(m => new MedicationOptionDto(
                m.Id, m.GenericNameFa ?? m.GenericName, m.Form, m.Strength, m.IsNarcotic))
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