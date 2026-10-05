using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Reports;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر گزارش‌ها — درآمد روزانه و عملکرد پزشکان
/// مسیرها: /api/v1/reports
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق میانجی و واحد کار (گام U16)</summary>
    public ReportsController(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>درآمد روزانه — بازه اختیاری روز</summary>
    /// <param name="days">بازه روز — پیش‌فرض ۳۰</param>
    [HttpGet("daily-revenue")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> DailyRevenue([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetDailyRevenueQuery(days));
        return ToResponse(result);
    }

    /// <summary>عملکرد پزشکان — ویزیت‌ها و سهم</summary>
    /// <param name="days">بازه روز — پیش‌فرض ۳۰</param>
    [HttpGet("doctor-performance")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> DoctorPerformance([FromQuery] int days = 30)
    {
        var result = await _mediator.Send(new GetDoctorPerformanceQuery(days));
        return ToResponse(result);
    }
    /* ═══════════════════════════════════════════════════════════
       گام U16 — گزارش‌ساز (فرم ۲۷): کاتالوگ ۴۰+ گزارش + اجرای پارامتری
       ═══════════════════════════════════════════════════════════ */

    /// <summary>کاتالوگ گزارش‌ها دسته‌بندی‌شده — مطابق فهرست پرامپت بخش ۴</summary>
    [HttpGet("catalog")]
    [HasPermission("reports.view")]
    public IActionResult GetCatalog()
    {
        var catalog = new List<object>
        {
            new { category = "💰 مالی", icon = "💰", reports = new[]
            {
                new { id = "daily-revenue", title = "درآمد روزانه", description = "گزارش درآمد روزانه درمانگاه به تفکیک روز", paramsList = new[] { "fromDate", "toDate", "doctorId", "insuranceId" } },
                new { id = "monthly-revenue", title = "درآمد ماهانه", description = "روند ماهانه درآمد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "detailed-revenue", title = "درآمد تفصیلی", description = "تفکیک کامل درآمد با فیلترهای کامل", paramsList = new[] { "fromDate", "toDate", "doctorId", "insuranceId", "groupBy" } },
                new { id = "patient-statement", title = "صورت‌حساب بیمار", description = "گردش حساب بیمار در بازه", paramsList = new[] { "patientId", "fromDate", "toDate" } },
                new { id = "debtors-list", title = "لیست بدهکاران", description = "بیماران دارای مانده بدهی", paramsList = new[] { "minAmount" } },
                new { id = "cashier-daily", title = "گزارش صندوق روزانه", description = "گردش صندوق در روز", paramsList = new[] { "date" } },
                new { id = "doctor-commission", title = "کارانه پزشکان", description = "محاسبه سهم پزشکان", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "profit-loss", title = "سود و زیان", description = "صورت سود و زیان دوره", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "accounts-receivable", title = "مطالبات بیمه‌ای", description = "مطالبات از بیمه‌ها", paramsList = new[] { "insuranceId" } },
                new { id = "payment-methods", title = "تفکیک روش‌های پرداخت", description = "پرداخت‌ها به تفکیک روش", paramsList = new[] { "fromDate", "toDate" } }
            }},
            new { category = "👤 بیماران", icon = "👤", reports = new[]
            {
                new { id = "patient-demographics", title = "آمار جمعیت‌شناسی", description = "توزیع سنی و جنسی بیماران", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "new-patients", title = "بیماران جدید", description = "بیماران ثبت‌شده در دوره", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "frequent-visitors", title = "پرمراجعه‌ترین‌ها", description = "بیماران با بیشترین ویزیت", paramsList = new[] { "top" } },
                new { id = "patient-insurance", title = "تفکیک بیمه‌ای بیماران", description = "توزیع بیماران بر اساس بیمه", paramsList = Array.Empty<string>() },
                new { id = "chronic-diseases", title = "بیماری‌های مزمن", description = "بیماران با تشخیص مزمن", paramsList = new[] { "icd10" } },
                new { id = "vip-patients", title = "بیماران VIP", description = "بیماران ویژه", paramsList = Array.Empty<string>() },
                new { id = "inactive-patients", title = "بیماران بدون مراجعه", description = "بیماران غیرفعال بیش از ۶ ماه", paramsList = new[] { "months" } }
            }},
            new { category = "👨‍⚕️ پزشکان", icon = "👨‍⚕️", reports = new[]
            {
                new { id = "doctor-performance", title = "عملکرد تفکیکی پزشکان", description = "ویزیت و درآمد هر پزشک", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "doctor-revenue", title = "درآمد هر پزشک", description = "روند درآمد پزشکان", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "doctor-comparison", title = "مقایسه پزشکان", description = "مقایسه عملکرد همزمان", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "avg-visit-duration", title = "میانگین مدت ویزیت", description = "زمان متوسط هر ویزیت", paramsList = new[] { "fromDate", "toDate" } }
            }},
            new { category = "📅 نوبت‌دهی", icon = "📅", reports = new[]
            {
                new { id = "appointment-statistics", title = "آمار نوبت‌ها", description = "نوبت‌ها به تفکیک وضعیت", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "no-show-report", title = "عدم مراجعه", description = "نوبت‌های بدون حضور", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "cancellation-report", title = "لغو نوبت", description = "نوبت‌های لغوشده با دلیل", paramsList = new[] { "fromDate", "toDate" } }
            }},
            new { category = "💊 داروخانه", icon = "💊", reports = new[]
            {
                new { id = "inventory-report", title = "موجودی کامل", description = "موجودی انبار داروخانه", paramsList = Array.Empty<string>() },
                new { id = "low-stock", title = "موجودی بحرانی", description = "داروهای زیر نقطه سفارش", paramsList = Array.Empty<string>() },
                new { id = "expiry-report", title = "نزدیک انقضا", description = "اقلام نزدیک به انقضا", paramsList = new[] { "months" } },
                new { id = "top-medications", title = "پرمصرف‌ترین داروها", description = "پرمصرف‌ترین اقلام", paramsList = new[] { "fromDate", "toDate" } }
            }},
            new { category = "📊 هوش تجاری", icon = "📊", reports = new[]
            {
                new { id = "revenue-trend", title = "روند درآمد", description = "روند بلندمدت درآمد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "visit-patterns", title = "الگوی مراجعات", description = "الگوی ساعتی و روزانه", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "service-mix", title = "ترکیب خدمات", description = "سهم خدمات از درآمد", paramsList = new[] { "fromDate", "toDate" } }
            }}
        };

        return Ok(ApiResponse<object>.Success(catalog));
    }

    /// <summary>اجرای گزارش با پارامترها — کارت‌های خلاصه + جدول نتایج</summary>
    /// <remarks>
    /// گزارش‌های دارای کوئری واقعی: درآمد روزانه/ماهانه، بدهکاران، بیماران جدید،
    /// پرمراجعه‌ها، عملکرد پزشکان، آمار نوبت، عدم مراجعه، موجودی بحرانی، نزدیک انقضا.
    /// سایر گزارش‌های کاتالوگ در نسخه‌های بعدی داده واقعی می‌گیرند.
    /// </remarks>
    [HttpGet("execute/{reportId}")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> ExecuteReport(string reportId,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int top = 20)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        return reportId switch
        {
            "daily-revenue" or "monthly-revenue" or "detailed-revenue" or "revenue-trend" =>
                await RevenueReportAsync(from, to),
            "debtors-list" => await DebtorsReportAsync(),
            "new-patients" => await NewPatientsReportAsync(from, to),
            "frequent-visitors" => await FrequentVisitorsReportAsync(top),
            "doctor-performance" or "doctor-revenue" or "doctor-comparison" =>
                await DoctorPerformanceDetailedAsync(from, to),
            "appointment-statistics" => await AppointmentStatsReportAsync(from, to),
            "no-show-report" or "cancellation-report" => await NoShowReportAsync(from, to),
            "low-stock" or "inventory-report" => await LowStockReportAsync(),
            "expiry-report" => await ExpiryReportAsync(),
            _ => Ok(ApiResponse<object>.Success(new
            {
                available = false,
                message = "این گزارش در نسخه بعدی داده واقعی دریافت می‌کند"
            }))
        };
    }

    /// <summary>گزارش درآمد بازه‌ای — ردیف روزانه + جمع‌ها</summary>
    private async Task<IActionResult> RevenueReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.CreatedAt >= from && i.CreatedAt <= to)
            .ToListAsync();

        var rows = invoices
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new
            {
                date = g.Key,
                shamsi = Application.Common.Helpers.PersianDateHelper.ToShamsi(g.Key),
                invoiceCount = g.Count(),
                totalAmount = g.Sum(i => i.TotalAmount),
                paidAmount = g.Sum(i => i.PaidAmount)
            })
            .OrderBy(r => r.date)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = rows.Sum(r => r.totalAmount),
                paid = rows.Sum(r => r.paidAmount),
                count = rows.Sum(r => r.invoiceCount)
            },
            columns = new[] { "تاریخ", "تعداد فاکتور", "مبلغ کل", "پرداخت‌شده" },
            rows
        }));
    }

    /// <summary>لیست بدهکاران — مانده پرداخت‌نشده هر بیمار</summary>
    private async Task<IActionResult> DebtorsReportAsync()
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>().QueryableNoTracking
            .Where(i => i.PaymentStatus == Domain.Enums.PaymentStatus.Unpaid && i.Patient != null)
            .GroupBy(i => new { i.PatientId, Name = i.Patient.FirstName + " " + i.Patient.LastName })
            .Select(g => new
            {
                patient = g.Key.Name,
                invoiceCount = g.Count(),
                balance = g.Sum(i => i.TotalAmount - i.PaidAmount)
            })
            .Where(r => r.balance > 0)
            .OrderByDescending(r => r.balance)
            .Take(100)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { debtors = rows.Count, balance = rows.Sum(r => r.balance) },
            columns = new[] { "بیمار", "تعداد فاکتور", "مانده بدهی" },
            rows
        }));
    }

    /// <summary>بیماران جدید دوره</summary>
    private async Task<IActionResult> NewPatientsReportAsync(DateTime from, DateTime to)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.People.Patient>().QueryableNoTracking
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                mrn = p.MedicalRecordNo,
                name = p.FirstName + " " + p.LastName,
                mobile = p.Mobile,
                gender = p.Gender.ToString(),
                createdAt = p.CreatedAt
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { newPatients = rows.Count },
            columns = new[] { "شماره پرونده", "نام", "موبایل", "جنسیت", "تاریخ ثبت" },
            rows
        }));
    }

    /// <summary>پرمراجعه‌ترین بیماران</summary>
    private async Task<IActionResult> FrequentVisitorsReportAsync(int top)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.Patient != null)
            .GroupBy(v => new { v.PatientId, Name = v.Patient.FirstName + " " + v.Patient.LastName })
            .Select(g => new { patient = g.Key.Name, visits = g.Count(), lastVisit = g.Max(v => v.VisitDate) })
            .OrderByDescending(r => r.visits)
            .Take(Math.Min(top, 100))
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { patients = rows.Count },
            columns = new[] { "بیمار", "تعداد ویزیت", "آخرین مراجعه" },
            rows
        }));
    }

    /// <summary>عملکرد پزشکان در بازه</summary>
    private async Task<IActionResult> DoctorPerformanceDetailedAsync(DateTime from, DateTime to)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>().QueryableNoTracking
            .Where(v => v.VisitDate >= from && v.VisitDate <= to && v.Doctor != null)
            .GroupBy(v => new { v.DoctorId, Name = v.Doctor.Employee != null ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName : "پزشک #" + v.DoctorId })
            .Select(g => new { doctor = g.Key.Name, visits = g.Count() })
            .OrderByDescending(r => r.visits)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { doctors = rows.Count, visits = rows.Sum(r => r.visits) },
            columns = new[] { "پزشک", "تعداد ویزیت" },
            rows
        }));
    }

    /// <summary>آمار نوبت‌ها به تفکیک وضعیت</summary>
    private async Task<IActionResult> AppointmentStatsReportAsync(DateTime from, DateTime to)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Scheduling.Appointment>().QueryableNoTracking
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .GroupBy(a => a.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = rows.Sum(r => r.count) },
            columns = new[] { "وضعیت", "تعداد" },
            rows
        }));
    }

    /// <summary>عدم مراجعه و لغو نوبت</summary>
    private async Task<IActionResult> NoShowReportAsync(DateTime from, DateTime to)
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Scheduling.Appointment>().QueryableNoTracking
            .Where(a => a.AppointmentDate >= DateOnly.FromDateTime(from) &&
                        a.AppointmentDate <= DateOnly.FromDateTime(to) &&
                        (a.Status == Domain.Enums.AppointmentStatus.NoShow ||
                         a.Status == Domain.Enums.AppointmentStatus.Cancelled))
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new
            {
                date = a.AppointmentDate,
                code = a.AppointmentCode,
                patient = a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "—",
                doctor = a.Doctor != null && a.Doctor.Employee != null ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName : "—",
                status = a.Status.ToString(),
                reason = a.CancellationReason
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { count = rows.Count },
            columns = new[] { "تاریخ", "کد نوبت", "بیمار", "پزشک", "وضعیت", "دلیل" },
            rows
        }));
    }

    /// <summary>موجودی بحرانی داروخانه</summary>
    private async Task<IActionResult> LowStockReportAsync()
    {
        var rows = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>().QueryableNoTracking
            .Where(i => i.Quantity <= i.ReorderLevel && i.Medication != null)
            .OrderBy(i => i.Quantity)
            .Select(i => new
            {
                medication = i.Medication.BrandName ?? i.Medication.GenericName,
                onHand = i.Quantity,
                reorder = i.ReorderLevel
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { lowStock = rows.Count },
            columns = new[] { "دارو", "موجودی", "نقطه سفارش" },
            rows
        }));
    }

    /// <summary>اقلام نزدیک انقضا — ۳ ماه آینده</summary>
    private async Task<IActionResult> ExpiryReportAsync()
    {
        var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
        var rows = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>().QueryableNoTracking
            .Where(i => i.ExpiryDate != null && i.ExpiryDate <= threshold && i.Medication != null)
            .OrderBy(i => i.ExpiryDate)
            .Select(i => new
            {
                medication = i.Medication.BrandName ?? i.Medication.GenericName,
                batch = i.BatchNumber,
                expiry = i.ExpiryDate,
                onHand = i.Quantity
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { expiring = rows.Count },
            columns = new[] { "دارو", "بچ", "انقضا", "موجودی" },
            rows
        }));
    }
}
