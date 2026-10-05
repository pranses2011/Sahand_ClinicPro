using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Payroll.GenerateSalaries;

/// <summary>
/// فرمان تولید حقوق دوره — گام R3 + حقوق درصدی چرخه نهم
/// برای همه کارکنان فعال (یا یک کارمند مشخص) از قرارداد و حضوروغیاب همان ماه
/// محاسبه می‌کند و در EmployeeSalary ثبت/به‌روزرسانی می‌کند (idempotent).
/// حقوق درصدی هر پرسنل از «سود خدمات انجام‌شده توسط خودش» در همان ماه
/// (اقلام فاکتور منتسب به او با فاکتور معتبر) × درصد پرسنل محاسبه می‌شود —
/// نه از کل دریافتی درمانگاه و نه از کل فروش.
/// </summary>
public sealed record GenerateSalariesCommand(int Year, int Month, long? EmployeeId = null)
    : IRequest<Result<int>>;

/// <summary>اعتبارسنج دوره</summary>
public sealed class GenerateSalariesCommandValidator : AbstractValidator<GenerateSalariesCommand>
{
    /// <summary>قواعد دوره شمسی/میلادی مشترک</summary>
    public GenerateSalariesCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1400, 1500)
            .WithMessage("سال دوره بین ۱۴۰۰ تا ۱۵۰۰ معتبر است");
        RuleFor(x => x.Month).InclusiveBetween(1, 12)
            .WithMessage("ماه دوره بین ۱ تا ۱۲ است");
    }
}

/// <summary>گرداننده تولید حقوق دوره</summary>
public sealed class GenerateSalariesCommandHandler
    : IRequestHandler<GenerateSalariesCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GenerateSalariesCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — محاسبه و ثبت حقوق همه کارکنان فعال دوره</summary>
    public async Task<Result<int>> Handle(GenerateSalariesCommand request,
        CancellationToken cancellationToken)
    {
        // کارکنان فعال — بدون پارامتر همه، با پارامتر فقط همان کارمند
        var employees = await _unitOfWork.Repository<Employee>().QueryableNoTracking
            .Where(e => !e.IsDeleted && e.IsActive &&
                        (request.EmployeeId == null || e.Id == request.EmployeeId))
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
            return Result<int>.Failure(new Error("PAYROLL.NO_EMPLOYEE",
                "کارمند فعالی برای این دوره یافت نشد"));

        // بازه شمسی ماه → بازه میلادی برای جمع اضافه‌کاری حضوروغیاب
        // نام سال/ماه در این سامانه شمسی است؛ بازه تقویمی تقریبی ماه کافی است
        var (rangeFrom, rangeTo) = MonthRange(request.Year, request.Month);

        var processed = 0;
        foreach (var employee in employees)
        {
            // حقوق پایه: آخرین قرارداد فعال، وگرنه فیلد خود کارمند
            var baseSalary = await _unitOfWork.Repository<PersonnelContract>().QueryableNoTracking
                .Where(c => c.EmployeeId == employee.Id && !c.IsDeleted &&
                            c.Salary > 0)
                .OrderByDescending(c => c.Id)
                .Select(c => (decimal?)c.Salary)
                .FirstOrDefaultAsync(cancellationToken)
                ?? employee.BaseSalary;

            // جمع اضافه‌کاری ثبت‌شده ماه
            var overtimeHours = await _unitOfWork.Repository<EmployeeAttendance>().QueryableNoTracking
                .Where(a => a.EmployeeId == employee.Id && !a.IsDeleted &&
                            a.AttendanceDate >= rangeFrom && a.AttendanceDate < rangeTo)
                .SumAsync(a => (decimal?)a.OvertimeHours, cancellationToken) ?? 0m;

            // ═══ حقوق درصدی — مبنا: سود خدمات انجام‌شده توسط خودِ این پرسنل ═══
            // فقط اقلام فاکتورِ منتسب به او (PerformedById) با فاکتور معتبر
            // (بدون پیش‌نویس/ابطال/برگشت هزینه) در بازه ماه؛ سود هر قلم =
            // مبلغ نهایی قلم − (هزینه عکس‌فوری هر واحد × تعداد)
            var rangeFromDt = rangeFrom.ToDateTime(TimeOnly.MinValue);
            var rangeToDt = rangeTo.ToDateTime(TimeOnly.MinValue);
            var serviceProfitBase = await _unitOfWork.Repository<InvoiceItem>().QueryableNoTracking
                .Where(i => i.PerformedById == employee.Id && !i.IsDeleted &&
                            i.Invoice != null && !i.Invoice.IsDeleted &&
                            i.Invoice.InvoiceDate >= rangeFromDt && i.Invoice.InvoiceDate < rangeToDt &&
                            (i.Invoice.Status == InvoiceStatus.Issued ||
                             i.Invoice.Status == InvoiceStatus.PartiallyPaid ||
                             i.Invoice.Status == InvoiceStatus.Paid))
                .SumAsync(i => (decimal?)(i.TotalPrice - i.UnitCost * i.Quantity),
                    cancellationToken) ?? 0m;

            // محاسبه با قواعد ۱۴۰۴ + حقوق درصدی از سود خدمات انجامی
            var result = PayrollCalculator.Calculate(
                new PayrollCalculator.PayrollInput(
                    baseSalary, overtimeHours,
                    CommissionPercent: employee.CommissionPercent,
                    ServiceProfit: serviceProfitBase));

            // ثبت یا به‌روزرسانی — idempotent برای هر کارمند×دوره
            var existing = await _unitOfWork.Repository<EmployeeSalary>().QueryableTracking
                .FirstOrDefaultAsync(s => s.EmployeeId == employee.Id &&
                                          s.PeriodYear == request.Year &&
                                          s.PeriodMonth == request.Month &&
                                          !s.IsDeleted, cancellationToken);

            if (existing is not null)
            {
                // ردیف پرداخت‌شده دوباره تولید نمی‌شود
                if (existing.Status == SalaryStatus.Paid) continue;

                existing.BaseSalary = baseSalary;
                existing.CommissionPercent = employee.CommissionPercent;
                existing.ServiceProfitBase = serviceProfitBase;
                existing.ServiceCommission = result.ServiceCommission;
                existing.Overtime = result.OvertimePay;
                existing.Deductions = 0m;
                existing.Insurance = result.Insurance;
                existing.Tax = result.Tax;
                existing.NetAmount = result.NetAmount;
                existing.Status = SalaryStatus.Calculated;
            }
            else
            {
                await _unitOfWork.Repository<EmployeeSalary>().AddAsync(new EmployeeSalary
                {
                    EmployeeId = employee.Id,
                    PeriodYear = request.Year,
                    PeriodMonth = request.Month,
                    BaseSalary = baseSalary,
                    CommissionPercent = employee.CommissionPercent,
                    ServiceProfitBase = serviceProfitBase,
                    ServiceCommission = result.ServiceCommission,
                    Overtime = result.OvertimePay,
                    Deductions = 0m,
                    Insurance = result.Insurance,
                    Tax = result.Tax,
                    NetAmount = result.NetAmount,
                    Status = SalaryStatus.Calculated
                }, cancellationToken);
            }

            processed++;
        }

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<int>.Success(processed);
    }

    /// <summary>
    /// بازه تقویمی ماه دوره — سال/ماه شمسی به تقریب میلادی برای فیلتر حضوروغیاب
    /// </summary>
    private static (DateOnly From, DateOnly To) MonthRange(int year, int month)
    {
        // تقویم شمسی: ماه ۱ از حدود ۲۱ مارس شروع می‌شود؛ تقریب ابتدای ماه کافی است
        var startMonth = month + 2; // فروردین → ژانویه سال میلادی بعدی نیست! (فروردین=مارس)
        var startYear = year - 622; // ۱۴۰۴ شمسی ≈ ۲۰۲۵ میلادی

        // نگاشت ماه شمسی به ماه میلادی آغازین: فروردین→مارس(3) ... اسفند→فوریه(2)
        var gregorianMonth = month switch
        {
            1 => 3, 2 => 4, 3 => 5, 4 => 6, 5 => 7, 6 => 8,
            7 => 9, 8 => 10, 9 => 11, 10 => 12, 11 => 1, _ => 2
        };
        // اسفند و بهمن به سال میلادی بعدی تعلق دارند
        if (month is 11 or 12) startYear++;

        var from = new DateOnly(startYear, gregorianMonth, 1);
        var to = from.AddMonths(1);
        return (from, to);
    }
}

/// <summary>
/// فرمان پرداخت حقوق — علامت‌گذاری ردیف به‌عنوان پرداخت‌شده (گام R3)
/// </summary>
public sealed record PaySalaryCommand(long SalaryId) : IRequest<Result>;

/// <summary>گرداننده پرداخت حقوق</summary>
public sealed class PaySalaryCommandHandler : IRequestHandler<PaySalaryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public PaySalaryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — وضعیت Paid و زمان پرداخت</summary>
    public async Task<Result> Handle(PaySalaryCommand request, CancellationToken cancellationToken)
    {
        var salary = await _unitOfWork.Repository<EmployeeSalary>().QueryableTracking
            .FirstOrDefaultAsync(s => s.Id == request.SalaryId && !s.IsDeleted, cancellationToken);
        if (salary is null)
            return Result.Failure(Error.NotFound("حقوق", request.SalaryId));

        if (salary.Status == SalaryStatus.Paid)
            return Result.Failure(new Error("PAYROLL.ALREADY_PAID", "این حقوق قبلاً پرداخت شده است"));

        salary.Status = SalaryStatus.Paid;
        salary.PaidAt = DateTime.UtcNow;
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result.Success();
    }
}
