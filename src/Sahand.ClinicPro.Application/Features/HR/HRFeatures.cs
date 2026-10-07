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

/// <summary>درخواست ثبت پرسنل</summary>
public record CreateEmployeeDto(
    string FirstName, string LastName, string NationalCode, string Mobile,
    long? DepartmentId, long? PositionId,
    EmploymentType EmploymentType = EmploymentType.FullTime,
    decimal BaseSalary = 0m,
    decimal CommissionPercent = 0m);

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
            HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
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
