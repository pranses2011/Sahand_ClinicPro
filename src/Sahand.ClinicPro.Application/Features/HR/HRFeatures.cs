using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.HR;

/// <summary>ردیف فهرست پرسنل</summary>
public record EmployeeListDto(
    long Id, string? PersonnelCode, string FullName, string? PositionName,
    string? DepartmentName, string Mobile, EmploymentType Type, bool IsActive,
    decimal BaseSalary = 0m, decimal CommissionPercent = 0m,
    bool HasPhoto = false); // چرخه هجدهم: عکس پروفایل موجود است؟

/// <summary>
/// جزئیات کامل پرسنل برای فرم ویرایش — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
/// «در لیست پرسنل امکان ویرایش پرسنل وجود ندارد»؛ فرم ویرایش به تمام
/// فیلدهای فرم ثبت نیاز دارد تا همان فرم در حالت ویرایش باز شود.
/// ═══ چرخهٔ چهل‌ودوم (گزارش مالک) ═══ «در ویرایش پرسنل، سمت و دپارتمان رو
/// اصلاح نمیکنه» — نام‌های سمت/دپارتمان هم کنار شناسه‌ها برمی‌گردند تا کلاینت
/// در نبودِ مقدار در فهرست مرجع (مثلاً سمتِ حذف‌شده یا فهرست خالیِ پیش از
/// بذر چرخهٔ ۴۲)، متنِ کامبوی ویرایش را با نام فعلی پر کند و ذخیرهٔ بدون
/// تغییر، مقدار فعلی را پاک نکند (رفع ریشهٔ «هر ویرایش، سمت را صفر می‌کرد»).
/// </summary>
public record EmployeeDetailDto(
    long Id, string FirstName, string LastName, string? NationalCode, string? Mobile,
    long? DepartmentId, long? PositionId, string? PositionName, string? DepartmentName,
    EmploymentType EmploymentType, decimal BaseSalary, decimal CommissionPercent,
    string? PersonnelCode, DateOnly? HireDate, EducationLevel? EducationLevel,
    string? FieldOfStudy, string? EmergencyContact, string? EmergencyPhone, bool HasPhoto,
    bool IsInsured = false);

/// <summary>درخواست ثبت پرسنل — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
/// فیلدهای «چند مورد دیگر» فرم پرسنل مطابق اسکرین‌شات افزوده شدند:
/// کد پرسنلی، تاریخ استخدام، تحصیلات، رشتهٔ تحصیلی و تماس اضطراری</summary>
public record CreateEmployeeDto(
    string FirstName, string LastName, string NationalCode, string Mobile,
    long? DepartmentId, long? PositionId,
    EmploymentType EmploymentType = EmploymentType.FullTime,
    decimal BaseSalary = 0m,
    decimal CommissionPercent = 0m,
    string? PersonnelCode = null,
    DateOnly? HireDate = null,
    EducationLevel? EducationLevel = null,
    string? FieldOfStudy = null,
    string? EmergencyContact = null,
    string? EmergencyPhone = null,
    bool IsInsured = false);

/// <summary>
/// فرمان ثبت پرسنل جدید
/// </summary>
public record CreateEmployeeCommand(CreateEmployeeDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج ثبت پرسنل</summary>
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("نام الزامی است");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("نام خانوادگی الزامی است");
        RuleFor(x => x.Dto.NationalCode)
            .Must(Common.Helpers.NationalCodeValidator.IsValid).WithMessage("کد ملی نامعتبر است");
        RuleFor(x => x.Dto.Mobile).Matches("^09\\d{9}$").WithMessage("موبایل معتبر نیست");
        RuleFor(x => x.Dto.BaseSalary).GreaterThanOrEqualTo(0)
            .WithMessage("حقوق پایه نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.CommissionPercent).InclusiveBetween(0, 100)
            .WithMessage("درصد حقوق از سود خدمات باید بین صفر تا صد باشد");
    }
}

/// <summary>گرداننده ثبت پرسنل</summary>
public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateEmployeeCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت پرسنل</summary>
    public async Task<Result<long>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var exists = await _unitOfWork.Repository<Employee>()
            .AnyAsync(e => e.NationalCode == dto.NationalCode, cancellationToken);
        if (exists)
            return Result<long>.Failure(new Error("DUPLICATE", "پرسنلی با این کد ملی ثبت شده است"));

        var employee = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            NationalCode = dto.NationalCode,
            Mobile = dto.Mobile,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            EmploymentType = dto.EmploymentType,
            BaseSalary = dto.BaseSalary,
            CommissionPercent = dto.CommissionPercent,
            IsInsured = dto.IsInsured,
            // چرخه بیست‌وچهارم: فیلدهای تکمیلی فرم پرسنل — پیش‌تر ارسال
            // می‌شدند اما نادیده گرفته می‌شدند (فرم اصلاً آنها را نداشت)
            PersonnelCode = dto.PersonnelCode,
            HireDate = dto.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            EducationLevel = dto.EducationLevel,
            FieldOfStudy = dto.FieldOfStudy,
            EmergencyContact = dto.EmergencyContact,
            EmergencyPhone = dto.EmergencyPhone,
            IsActive = true
        };

        await _unitOfWork.Repository<Employee>().AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(employee.Id);
    }
}

/// <summary>
/// فرمان ویرایش اطلاعات حقوقی پرسنل — حقوق پایه + درصد از سود خدمات
/// (قاعده چرخه نهم: درصد فقط از سود خدمات انجامی خود پرسنل اعمال می‌شود)
/// </summary>
public record UpdateEmployeeSalaryCommand(
    long EmployeeId, decimal BaseSalary, decimal CommissionPercent) : IRequest<Result>;

/// <summary>
/// پرس‌وجوی جزئیات یک پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
/// بارگذاری فرم ویرایش پرسنل با تمام فیلدهای ثبت
/// </summary>
public record GetEmployeeByIdQuery(long EmployeeId) : IRequest<Result<EmployeeDetailDto>>;

/// <summary>گرداننده جزئیات پرسنل</summary>
public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetEmployeeByIdQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو — موجود نیست یا حذف‌شدهٔ نرم است → NotFound</summary>
    public async Task<Result<EmployeeDetailDto>> Handle(GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .Where(e => e.Id == request.EmployeeId && !e.IsDeleted)
            .Select(e => new EmployeeDetailDto(
                e.Id, e.FirstName, e.LastName, e.NationalCode, e.Mobile,
                e.DepartmentId, e.PositionId,
                // چرخهٔ ۴۲: نام‌های سمت/دپارتمان — پروجکشنِ ناوبری داخل Select
                // را EF Core خودکار به JOIN ترجمه می‌کند (بدون Include)
                e.Position != null ? e.Position.Title : null,
                e.Department != null ? e.Department.Name : null,
                e.EmploymentType ?? EmploymentType.FullTime,
                e.BaseSalary, e.CommissionPercent,
                e.PersonnelCode, e.HireDate, e.EducationLevel,
                e.FieldOfStudy, e.EmergencyContact, e.EmergencyPhone,
                e.ProfilePicture != null && e.ProfilePicture != "",
                e.IsInsured))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<EmployeeDetailDto>.Failure(Error.NotFound("پرسنل", request.EmployeeId))
            : Result<EmployeeDetailDto>.Success(dto);
    }
}

/// <summary>
/// فرمان ویرایش کامل اطلاعات پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
/// همان فیلدهای فرم ثبت؛ کد ملی یکتایی‌اش فقط نسبت به «دیگران» سنجیده
/// می‌شود و تاریخ استخدامِ ارسال‌نشده دست‌نخورده می‌ماند.
/// </summary>
public record UpdateEmployeeCommand(long EmployeeId, CreateEmployeeDto Dto) : IRequest<Result>;

/// <summary>اعتبارسنج ویرایش پرسنل</summary>
public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>قواعد اعتبارسنجی — همان قواعد ثبت</summary>
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("شناسه پرسنل نامعتبر است");
        RuleFor(x => x.Dto.FirstName).NotEmpty().WithMessage("نام الزامی است");
        RuleFor(x => x.Dto.LastName).NotEmpty().WithMessage("نام خانوادگی الزامی است");
        RuleFor(x => x.Dto.NationalCode)
            .Must(Common.Helpers.NationalCodeValidator.IsValid).WithMessage("کد ملی نامعتبر است");
        RuleFor(x => x.Dto.Mobile).Matches("^09\\d{9}$").WithMessage("موبایل معتبر نیست");
        RuleFor(x => x.Dto.BaseSalary).GreaterThanOrEqualTo(0)
            .WithMessage("حقوق پایه نمی‌تواند منفی باشد");
        RuleFor(x => x.Dto.CommissionPercent).InclusiveBetween(0, 100)
            .WithMessage("درصد حقوق از سود خدمات باید بین صفر تا صد باشد");
    }
}

/// <summary>گرداننده ویرایش کامل پرسنل</summary>
public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateEmployeeCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ویرایش — یکتایی کد ملی با استثنای خود رکورد</summary>
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var employee = await _unitOfWork.Repository<Employee>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("پرسنل", request.EmployeeId));

        // یکتایی کد ملی — فقط نسبت به سایر پرسنل (خود رکورد مستثناست)
        var duplicateNational = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .AnyAsync(e => e.Id != request.EmployeeId && !e.IsDeleted
                && dto.NationalCode != null && e.NationalCode == dto.NationalCode,
                cancellationToken);
        if (duplicateNational)
            return Result.Failure(new Error("DUPLICATE", "پرسنل دیگری با این کد ملی ثبت شده است"));

        employee.FirstName = dto.FirstName;
        employee.LastName = dto.LastName;
        employee.NationalCode = dto.NationalCode;
        employee.Mobile = dto.Mobile;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.EmploymentType = dto.EmploymentType;
        employee.BaseSalary = dto.BaseSalary;
        employee.CommissionPercent = dto.CommissionPercent;
        employee.IsInsured = dto.IsInsured;
        employee.PersonnelCode = dto.PersonnelCode;
        employee.HireDate = dto.HireDate ?? employee.HireDate; // تهی = بدون تغییر
        employee.EducationLevel = dto.EducationLevel;
        employee.FieldOfStudy = dto.FieldOfStudy;
        employee.EmergencyContact = dto.EmergencyContact;
        employee.EmergencyPhone = dto.EmergencyPhone;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>
/// فرمان حذف نرم پرسنل — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
/// «کلیدهای عملیاتی پرسنل ناقص‌اند»؛ حذف، رکورد را به‌صورت نرم پنهان
/// می‌کند (سابقهٔ مالی/حضور مرتبط سالم می‌ماند) و پرسنل را غیرفعال می‌کند.
/// </summary>
public record DeleteEmployeeCommand(long EmployeeId) : IRequest<Result>;

/// <summary>گرداننده حذف نرم پرسنل</summary>
public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public DeleteEmployeeCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای حذف نرم + غیرفعال‌سازی</summary>
    public async Task<Result> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork.Repository<Employee>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted, cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("پرسنل", request.EmployeeId));

        employee.IsDeleted = true;
        employee.IsActive = false;
        employee.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>اعتبارسنج ویرایش اطلاعات حقوقی</summary>
public class UpdateEmployeeSalaryCommandValidator : AbstractValidator<UpdateEmployeeSalaryCommand>
{
    /// <summary>قواعد حقوق پایه و درصد</summary>
    public UpdateEmployeeSalaryCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("شناسه پرسنل نامعتبر است");
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0)
            .WithMessage("حقوق پایه نمی‌تواند منفی باشد");
        RuleFor(x => x.CommissionPercent).InclusiveBetween(0, 100)
            .WithMessage("درصد حقوق از سود خدمات باید بین صفر تا صد باشد");
    }
}

/// <summary>گرداننده ویرایش اطلاعات حقوقی پرسنل</summary>
public class UpdateEmployeeSalaryCommandHandler
    : IRequestHandler<UpdateEmployeeSalaryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public UpdateEmployeeSalaryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ویرایش حقوق پایه و درصد سود</summary>
    public async Task<Result> Handle(UpdateEmployeeSalaryCommand request,
        CancellationToken cancellationToken)
    {
        var employee = await _unitOfWork.Repository<Employee>().QueryableTracking
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && !e.IsDeleted,
                cancellationToken);
        if (employee is null)
            return Result.Failure(Error.NotFound("پرسنل", request.EmployeeId));

        employee.BaseSalary = request.BaseSalary;
        employee.CommissionPercent = request.CommissionPercent;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// فرمان ثبت مرخصی
/// </summary>
public record RequestLeaveCommand(
    long EmployeeId, LeaveType Type, string FromDateShamsi,
    string ToDateShamsi, decimal Days, string? Reason) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت مرخصی</summary>
public class RequestLeaveCommandHandler : IRequestHandler<RequestLeaveCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RequestLeaveCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت مرخصی</summary>
    public async Task<Result<long>> Handle(RequestLeaveCommand request, CancellationToken cancellationToken)
    {
        var from = PersianDateHelper.ToGregorian(request.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(request.ToDateShamsi);

        if (from is null || to is null)
            return Result<long>.Failure(new Error("INVALID_DATE", "تاریخ مرخصی شمسی نامعتبر است"));

        var leave = new EmployeeLeave
        {
            EmployeeId = request.EmployeeId,
            LeaveType = request.Type,
            FromDate = DateOnly.FromDateTime(from.Value),
            ToDate = DateOnly.FromDateTime(to.Value),
            Days = request.Days,
            Reason = request.Reason,
            Status = LeaveStatus.Pending
        };

        await _unitOfWork.Repository<EmployeeLeave>().AddAsync(leave, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(leave.Id);
    }
}

/// <summary>
/// پرس‌وجوی فهرست پرسنل
/// </summary>
public record GetEmployeesQuery(long? DepartmentId = null) : IRequest<Result<List<EmployeeListDto>>>;

/// <summary>گرداننده فهرست پرسنل</summary>
public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetEmployeesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<EmployeeListDto>>> Handle(GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = _unitOfWork.Repository<Employee>()
            .QueryableNoTracking
            .Include(e => e.Department)
            .Include(e => e.Position);

        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);

        var items = await query
            .OrderBy(e => e.LastName)
            .Select(e => new EmployeeListDto(
                e.Id, e.PersonnelCode,
                e.FirstName + " " + e.LastName,
                e.Position != null ? e.Position.Title : null,
                e.Department != null ? e.Department.Name : null,
                e.Mobile ?? string.Empty,
                e.EmploymentType ?? EmploymentType.FullTime,
                e.IsActive,
                e.BaseSalary,
                e.CommissionPercent,
                e.ProfilePicture != null && e.ProfilePicture != ""))
            .ToListAsync(cancellationToken);

        return Result<List<EmployeeListDto>>.Success(items);
    }
}
