using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Pharmacy;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Reports;

/// <summary>ردیف خلاصه هر پرسنل در گزارش سود و حقوق درصدی</summary>
public sealed record CommissionSummaryRow(
    long EmployeeId,
    string PersonnelCode,
    string FullName,
    string DepartmentName,
    string PositionName,
    bool IsDoctorLinked,
    string DoctorNames,
    int ServiceCount,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalProfit,
    decimal CommissionPercent,
    decimal CommissionBase,
    decimal ServiceCommission,
    string SalaryStatusText);

/// <summary>ردیف جزئیات — یک قلم فاکتور منتسب به یک پرسنل</summary>
public sealed record CommissionDetailRow(
    long EmployeeId,
    string EmployeeName,
    string InvoiceNumber,
    string InvoiceDateShamsi,
    string PatientName,
    string ServiceName,
    decimal Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal TotalPrice,
    decimal Profit,
    string Attribution);

/// <summary>گزارش کامل سود و حقوق درصدی دوره</summary>
public sealed record CommissionDetailReportDto(
    int Year,
    int Month,
    string PeriodText,
    string GeneratedAtShamsi,
    string ClinicName,
    List<CommissionSummaryRow> Summary,
    List<CommissionDetailRow> Details,
    int EmployeeCount,
    int ServiceCountTotal,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalProfit,
    decimal TotalCommission);

/// <summary>
/// پرس‌وجوی گزارش تفصیلی «سود و حقوق درصدی هر پرسنل» — چرخه دهم
/// </summary>
/// <remarks>
/// قاعده مالک: حقوق درصدی فقط از «سود خدمات انجام‌شده توسط خودِ پرسنل»
/// محاسبه می‌شود — نه از کل فروش و نه از کل دریافتی درمانگاه.
/// سود هر قلم = مبلغ نهایی قلم − (هزینه عکس‌فوری هر واحد × تعداد).
/// مسیر انتسباق اقلام به پرسنل (مطابق تولید حقوق چرخه دهم):
/// ۱) انجام‌دهنده صریح قلم (PerformedById)
/// ۲) پزشکِ متصل به رکورد پرسنل (DoctorId → Doctor.EmployeeId) — حقوق درصدی پزشکان
/// اگر فیش حقوقی دوره تولید شده باشد، عدد رسمی فیش نمایش داده می‌شود؛
/// در غیر این صورت محاسبه لحظه‌ای با همان قاعده (max(0, سود) × درصد).
/// </remarks>
public sealed record GetCommissionDetailReportQuery(
    int Year, int Month, long? DepartmentId = null, long? EmployeeId = null)
    : IRequest<Result<CommissionDetailReportDto>>;

/// <summary>اعتبارسنج دوره و فیلترها</summary>
public sealed class GetCommissionDetailReportQueryValidator
    : AbstractValidator<GetCommissionDetailReportQuery>
{
    /// <summary>قواعد دوره شمسی</summary>
    public GetCommissionDetailReportQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1400, 1500)
            .WithMessage("سال دوره بین ۱۴۰۰ تا ۱۵۰۰ معتبر است");
        RuleFor(x => x.Month).InclusiveBetween(1, 12)
            .WithMessage("ماه دوره بین ۱ تا ۱۲ است");
    }
}

/// <summary>گرداننده گزارش تفصیلی سود و حقوق درصدی</summary>
public sealed class GetCommissionDetailReportQueryHandler
    : IRequestHandler<GetCommissionDetailReportQuery, Result<CommissionDetailReportDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetCommissionDetailReportQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — ساخت گزارش از اقلام معتبر دوره</summary>
    public async Task<Result<CommissionDetailReportDto>> Handle(
        GetCommissionDetailReportQuery request, CancellationToken cancellationToken)
        => await CommissionDetailReportBuilder.BuildAsync(
            _unitOfWork, request.Year, request.Month,
            request.DepartmentId, request.EmployeeId, cancellationToken);
}

/// <summary>
/// سازنده مشترک گزارش — هم نمایش JSON و هم خروجی اکسل از همین مجرا
/// </summary>
internal static class CommissionDetailReportBuilder
{
    /// <summary>ساخت کامل گزارش دوره</summary>
    public static async Task<Result<CommissionDetailReportDto>> BuildAsync(
        IUnitOfWork unitOfWork, int year, int month,
        long? departmentId, long? employeeId, CancellationToken ct)
    {
        // بازه تقویمی میلادی ماه شمسی دوره — مشابه تولید حقوق
        var (rangeFrom, rangeTo) = PersianDateHelper.MonthRange(year, month);
        var fromDt = rangeFrom.ToDateTime(TimeOnly.MinValue);
        var toDt = rangeTo.ToDateTime(TimeOnly.MinValue);

        // ═══ ۱) پرسنل منطبق بر فیلتر (فعال و حذف‌نشده) ═══
        var employees = await unitOfWork.Repository<Employee>().QueryableNoTracking
            .Where(e => !e.IsDeleted && e.IsActive &&
                        (departmentId == null || e.DepartmentId == departmentId) &&
                        (employeeId == null || e.Id == employeeId))
            .Select(e => new
            {
                e.Id, e.PersonnelCode, e.FirstName, e.LastName,
                e.CommissionPercent,
                DepartmentName = e.Department != null ? e.Department.Name : "",
                PositionName = e.Position != null ? e.Position.Title : ""
            })
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(ct);

        if (employees.Count == 0)
            return Result<CommissionDetailReportDto>.Failure(new Error(
                "REPORT.NO_EMPLOYEE", "پرسنلی منطبق بر فیلتر یافت نشد"));

        var employeeIds = employees.Select(e => e.Id).ToList();

        // ═══ ۲) پزشکانِ متصل به همین پرسنل‌ها — مسیر انتساب پزشکان ═══
        var doctorLinks = await unitOfWork.Repository<Doctor>().QueryableNoTracking
            .Where(d => !d.IsDeleted && d.EmployeeId != null &&
                        employeeIds.Contains(d.EmployeeId.Value))
            .Select(d => new
            {
                d.Id, d.EmployeeId,
                DoctorName = "دکتر " + d.Employee!.FirstName + " " + d.Employee.LastName
            })
            .ToListAsync(ct);

        // پزشک → پرسنل مرتبط
        var doctorToEmployee = doctorLinks
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.First().EmployeeId!.Value);

        // نام پزشکان متصل هر پرسنل برای ستون شناسنامه
        var doctorsByEmployee = doctorLinks
            .GroupBy(d => d.EmployeeId!.Value)
            .ToDictionary(
                g => g.Key,
                g => string.Join("، ", g.Select(d => d.DoctorName)));

        // ═══ ۳) همه اقلام منتسب‌پذیر دوره با فاکتور معتبر ═══
        var doctorIdsAll = doctorToEmployee.Keys.ToList();
        var items = await unitOfWork.Repository<InvoiceItem>().QueryableNoTracking
            .Where(i => !i.IsDeleted &&
                        i.Invoice != null && !i.Invoice.IsDeleted &&
                        i.Invoice.InvoiceDate >= fromDt && i.Invoice.InvoiceDate < toDt &&
                        (i.Invoice.Status == InvoiceStatus.Issued ||
                         i.Invoice.Status == InvoiceStatus.PartiallyPaid ||
                         i.Invoice.Status == InvoiceStatus.Paid) &&
                        ((i.PerformedById != null && employeeIds.Contains(i.PerformedById.Value)) ||
                         (i.DoctorId != null && doctorIdsAll.Contains(i.DoctorId.Value)) ||
                         (i.Invoice.Reception != null &&
                          i.Invoice.Reception.PerformedByEmployeeId != null &&
                          employeeIds.Contains(i.Invoice.Reception.PerformedByEmployeeId.Value)) ||
                         (i.Invoice.Reception != null &&
                          i.Invoice.Reception.DoctorId != null &&
                          doctorIdsAll.Contains(i.Invoice.Reception.DoctorId.Value))))
            .Select(i => new ItemProjection(
                i.Id, i.PerformedById, i.DoctorId, i.Invoice!.ReceptionId,
                i.Invoice.Reception != null ? i.Invoice.Reception.PerformedByEmployeeId : null,
                i.Invoice.Reception != null ? i.Invoice.Reception.DoctorId : null,
                i.Quantity, i.UnitPrice, i.UnitCost, i.TotalPrice,
                i.Service != null ? i.Service.ServiceName :
                    (i.Description ?? "خدمت بدون عنوان"),
                i.Invoice!.InvoiceNumber ?? "—",
                i.Invoice.InvoiceDateShamsi,
                i.Invoice.Patient!.FirstName + " " + i.Invoice.Patient.LastName))
            .ToListAsync(ct);

        // ═══ ۴) فیش‌های حقوقی تولیدشده دوره — عدد رسمی فیش اولویت دارد ═══
        var salaryRows = await unitOfWork.Repository<EmployeeSalary>().QueryableNoTracking
            .Where(s => s.PeriodYear == year && s.PeriodMonth == month &&
                        !s.IsDeleted && employeeIds.Contains(s.EmployeeId))
            .Select(s => new
            {
                s.EmployeeId, s.ServiceCommission, s.ServiceProfitBase, s.Status
            })
            .ToListAsync(ct);
        var salaryByEmployee = salaryRows.ToDictionary(s => s.EmployeeId);

        // ═══ ۵) انتساب اقلام و ساخت جزئیات ═══
        var employeeNames = employees.ToDictionary(e => e.Id,
            e => e.FirstName + " " + e.LastName);

        // سوابق قدیمیِ لوازم مصرفی UnitCost ندارند؛ هزینهٔ مصرف همان پذیرش را
        // از عکس‌فوری تراکنش انبار می‌خوانیم و برای نمایش جزئیات، میان اقلام
        // مصرفی همان پذیرش به نسبت تعداد توزیع می‌کنیم.
        var legacyReceptionIds = items
            .Where(i => i.UnitCost == 0m && i.ReceptionId.HasValue &&
                        i.ServiceName.StartsWith("لوازم مصرفی:"))
            .Select(i => i.ReceptionId!.Value).Distinct().ToList();
        if (legacyReceptionIds.Count > 0)
        {
            var legacyCosts = await unitOfWork.Repository<PharmacyTransaction>().QueryableNoTracking
                .Where(t => !t.IsDeleted &&
                            t.ReferenceType == PharmacyReferenceType.Reception &&
                            t.ReferenceId.HasValue && legacyReceptionIds.Contains(t.ReferenceId.Value) &&
                            t.TransactionType == PharmacyTransactionType.StockOut && t.Inventory != null)
                .Select(t => new
                {
                    ReceptionId = t.ReferenceId!.Value,
                    t.Quantity,
                    UnitCost = t.PurchaseUnitCost ?? t.Inventory!.PurchasePrice
                })
                .ToListAsync(ct);
            var costsByReception = legacyCosts.GroupBy(t => t.ReceptionId)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Quantity * t.UnitCost));
            var quantitiesByReception = items
                .Where(i => i.UnitCost == 0m && i.ReceptionId.HasValue &&
                            i.ServiceName.StartsWith("لوازم مصرفی:"))
                .GroupBy(i => i.ReceptionId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            items = items.Select(item =>
            {
                if (item.UnitCost == 0m && item.ReceptionId.HasValue &&
                    item.ServiceName.StartsWith("لوازم مصرفی:") &&
                    costsByReception.TryGetValue(item.ReceptionId.Value, out var totalCost) &&
                    quantitiesByReception.TryGetValue(item.ReceptionId.Value, out var totalQuantity) &&
                    totalQuantity > 0m)
                    return item with { UnitCost = totalCost / totalQuantity };
                return item;
            }).ToList();
        }

        var details = new List<CommissionDetailRow>();
        // تجمیع به تفکیک پرسنل: (تعداد، فروش، هزینه، سود)
        var agg = employees.ToDictionary(e => e.Id,
            _ => (Count: 0, Revenue: 0m, Cost: 0m, Profit: 0m));

        foreach (var item in items)
        {
            var profit = item.TotalPrice - item.UnitCost * item.Quantity;
            var cost = item.UnitCost * item.Quantity;
            var performerId = item.PerformedById ?? item.ReceptionPerformerId;
            var doctorId = item.DoctorId ?? item.ReceptionDoctorId;

            // مسیر ۱ — انجام‌دهندهٔ صریح یا انجام‌دهندهٔ پذیرش برای فاکتورهای قدیمی
            if (performerId.HasValue && employeeNames.ContainsKey(performerId.Value))
            {
                var empId = performerId.Value;
                AddAttributed(details, agg, empId, employeeNames[empId], item, profit, cost,
                    doctorId.HasValue && doctorToEmployee.GetValueOrDefault(doctorId.Value) == empId
                        ? "انجام‌دهنده + پزشک"
                        : "انجام‌دهنده مستقیم");
            }

            // مسیر ۲ — پزشک متصل (فقط اگر با مسیر ۱ یکی نباشد تا دوبار شمرده نشود)
            if (doctorId.HasValue &&
                doctorToEmployee.TryGetValue(doctorId.Value, out var doctorEmpId) &&
                employeeNames.ContainsKey(doctorEmpId) &&
                doctorEmpId != performerId)
            {
                AddAttributed(details, agg, doctorEmpId, employeeNames[doctorEmpId], item, profit, cost,
                    "خدمت پزشک");
            }
        }

        // ═══ ۶) خلاصه هر پرسنل ═══
        var summary = employees.Select(e =>
        {
            var a = agg[e.Id];
            var commissionBase = Math.Max(0m, a.Profit);

            // عدد رسمی فیش تولیدشده؛ وگرنه محاسبه لحظه‌ای با همان قاعده
            decimal commission;
            string statusText;
            if (salaryByEmployee.TryGetValue(e.Id, out var salary))
            {
                commission = salary.ServiceCommission;
                statusText = salary.Status switch
                {
                    SalaryStatus.Paid => "پرداخت‌شده",
                    SalaryStatus.Approved => "تأییدشده",
                    _ => "محاسبه‌شده"
                };
            }
            else
            {
                commission = Math.Round(commissionBase * e.CommissionPercent / 100m, 0);
                statusText = "فیش تولید نشده";
            }

            return new CommissionSummaryRow(
                e.Id, e.PersonnelCode ?? "—", employeeNames[e.Id],
                string.IsNullOrWhiteSpace(e.DepartmentName) ? "—" : e.DepartmentName,
                string.IsNullOrWhiteSpace(e.PositionName) ? "—" : e.PositionName,
                doctorsByEmployee.ContainsKey(e.Id),
                doctorsByEmployee.GetValueOrDefault(e.Id, "—"),
                a.Count, a.Revenue, a.Cost, a.Profit,
                e.CommissionPercent, commissionBase, commission, statusText);
        }).ToList();

        // ═══ ۷) سربرگ درمانگاه و جمع کل ═══
        var clinic = await unitOfWork.Repository<ClinicInfo>().QueryableNoTracking
            .OrderByDescending(c => c.Id)
            .Select(c => c.ClinicName)
            .FirstOrDefaultAsync(ct);

        var dto = new CommissionDetailReportDto(
            year, month,
            $"{PersianDateHelper.MonthNames[month - 1]} {year}",
            PersianDateHelper.ToShamsiWithTime(DateTime.UtcNow),
            clinic ?? "درمانگاه سهند",
            summary,
            details.OrderByDescending(d => d.Profit).ToList(),
            summary.Count,
            details.Count,
            summary.Sum(s => s.TotalRevenue),
            summary.Sum(s => s.TotalCost),
            summary.Sum(s => s.TotalProfit),
            summary.Sum(s => s.ServiceCommission));

        return Result<CommissionDetailReportDto>.Success(dto);
    }

    /// <summary>افزودن یک قلم منتسب‌شده به جزئیات و تجمیع پرسنل</summary>
    private static void AddAttributed(
        List<CommissionDetailRow> details,
        Dictionary<long, (int Count, decimal Revenue, decimal Cost, decimal Profit)> agg,
        long employeeId, string employeeName,
        ItemProjection item, decimal profit, decimal cost, string attribution)
    {
        var a = agg[employeeId];
        agg[employeeId] = (a.Count + 1, a.Revenue + item.TotalPrice, a.Cost + cost, a.Profit + profit);

        details.Add(new CommissionDetailRow(
            employeeId, employeeName,
            item.InvoiceNumber, item.InvoiceDateShamsi ?? "—", item.PatientName,
            item.ServiceName, item.Quantity, item.UnitPrice, item.UnitCost,
            item.TotalPrice, profit, attribution));
    }

    /// <summary>برش اقلام فاکتور برای انتساب درون‌حافظه‌ای</summary>
    private sealed record ItemProjection(
        long Id, long? PerformedById, long? DoctorId, long? ReceptionId,
        long? ReceptionPerformerId, long? ReceptionDoctorId, decimal Quantity,
        decimal UnitPrice, decimal UnitCost, decimal TotalPrice,
        string ServiceName, string InvoiceNumber, string? InvoiceDateShamsi, string PatientName);
}
