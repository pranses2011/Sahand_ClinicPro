using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Payroll.PayrollQueries;

/// <summary>ردیف فهرست حقوق دوره</summary>
public sealed record SalaryListRow(long Id, long EmployeeId, string EmployeeName,
    string PersonnelCode, decimal BaseSalary, decimal CommissionPercent,
    decimal ServiceProfitBase, decimal ServiceCommission,
    decimal Overtime, decimal Bonus,
    decimal Insurance, decimal Tax, decimal Deductions, decimal NetAmount,
    int Status, DateTime? PaidAt);

/// <summary>پرس‌وجوی فهرست حقوق یک دوره — گام R3</summary>
public sealed record GetSalariesQuery(int Year, int Month) : IRequest<Result<List<SalaryListRow>>>;

/// <summary>گرداننده فهرست حقوق دوره</summary>
public sealed class GetSalariesQueryHandler : IRequestHandler<GetSalariesQuery, Result<List<SalaryListRow>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetSalariesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — ردیف‌های دوره با نام کارمند</summary>
    public async Task<Result<List<SalaryListRow>>> Handle(GetSalariesQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await _unitOfWork.Repository<EmployeeSalary>().QueryableNoTracking
            .Where(s => s.PeriodYear == request.Year && s.PeriodMonth == request.Month && !s.IsDeleted)
            .OrderBy(s => s.Employee!.LastName).ThenBy(s => s.Employee!.FirstName)
            .Select(s => new SalaryListRow(
                s.Id, s.EmployeeId,
                s.Employee!.FirstName + " " + s.Employee.LastName,
                s.Employee.PersonnelCode ?? "",
                s.BaseSalary, s.CommissionPercent,
                s.ServiceProfitBase, s.ServiceCommission,
                s.Overtime, s.Bonus,
                s.Insurance, s.Tax, s.Deductions, s.NetAmount,
                (int)s.Status, s.PaidAt))
            .ToListAsync(cancellationToken);

        return Result<List<SalaryListRow>>.Success(rows);
    }
}

/// <summary>داده کامل فیش حقوقی قابل چاپ — گام R3 + حقوق درصدی چرخه نهم</summary>
public sealed record PayslipData(
    long SalaryId, long EmployeeId, string EmployeeName, string PersonnelCode,
    string NationalCode, string Position, string BankAccount,
    int PeriodYear, int PeriodMonth, string PeriodText,
    decimal BaseSalary, decimal CommissionPercent,
    decimal ServiceProfitBase, decimal ServiceCommission,
    decimal Overtime, decimal Bonus,
    decimal Insurance, decimal Tax, decimal Deductions,
    decimal GrossEarnings, decimal TotalDeductions, decimal NetAmount,
    int Status, DateTime? PaidAt,
    string ClinicName, string ClinicEconomicCode, string ClinicAddress);

/// <summary>پرس‌وجوی فیش حقوقی برای چاپ</summary>
public sealed record GetPayslipQuery(long SalaryId) : IRequest<Result<PayslipData>>;

/// <summary>گرداننده فیش حقوقی</summary>
public sealed class GetPayslipQueryHandler : IRequestHandler<GetPayslipQuery, Result<PayslipData>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetPayslipQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — همه داده‌های لازم برای فیش استاندارد</summary>
    public async Task<Result<PayslipData>> Handle(GetPayslipQuery request,
        CancellationToken cancellationToken)
    {
        var salary = await _unitOfWork.Repository<EmployeeSalary>().QueryableNoTracking
            .Where(s => s.Id == request.SalaryId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id, s.EmployeeId, s.PeriodYear, s.PeriodMonth,
                s.BaseSalary, s.CommissionPercent,
                s.ServiceProfitBase, s.ServiceCommission,
                s.Overtime, s.Bonus, s.Insurance, s.Tax, s.Deductions,
                s.NetAmount, s.Status, s.PaidAt,
                EmployeeName = s.Employee!.FirstName + " " + s.Employee.LastName,
                s.Employee.PersonnelCode, s.Employee.NationalCode,
                s.Employee.BankAccount
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (salary is null)
            return Result<PayslipData>.Failure(Error.NotFound("حقوق", request.SalaryId));

        // اطلاعات سربرگ درمانگاه از جدول ClinicInfo
        var clinic = await _unitOfWork.Repository<ClinicInfo>().QueryableNoTracking
            .OrderByDescending(c => c.Id)
            .Select(c => new { c.ClinicName, c.EconomicCode, c.Address })
            .FirstOrDefaultAsync(cancellationToken);

        var gross = salary.BaseSalary + salary.ServiceCommission +
                    salary.Overtime + salary.Bonus;
        var totalDeductions = salary.Insurance + salary.Tax + salary.Deductions;

        return Result<PayslipData>.Success(new PayslipData(
            salary.Id, salary.EmployeeId, salary.EmployeeName,
            salary.PersonnelCode ?? "—", salary.NationalCode ?? "—", "—",
            salary.BankAccount ?? "—",
            salary.PeriodYear, salary.PeriodMonth,
            $"{salary.PeriodYear}/{salary.PeriodMonth:00}",
            salary.BaseSalary, salary.CommissionPercent,
            salary.ServiceProfitBase, salary.ServiceCommission,
            salary.Overtime, salary.Bonus,
            salary.Insurance, salary.Tax, salary.Deductions,
            gross, totalDeductions, salary.NetAmount,
            (int)salary.Status, salary.PaidAt,
            clinic?.ClinicName ?? "درمانگاه سهند",
            clinic?.EconomicCode ?? "—",
            clinic?.Address ?? "—"));
    }
}
