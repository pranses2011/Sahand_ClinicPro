using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Features.Reports;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر گزارش‌ها — درآمد روزانه و عملکرد پزشکان
/// مسیرها: /api/v1/reports
/// گام P1 (چرخه ششم): هر ۳۱ گزارش کاتالوگ با کوئری واقعی
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public partial class ReportsController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _catalogCache;

    /// <summary>
    /// کانتکست فقط-خواندنی گزارش‌ها — چرخهٔ ۶۴ (A-01 گام اول).
    /// همهٔ خواندن‌های گزارش از این کانتکست می‌گذرند (بدون ChangeTracker،
    /// بدون تداخل با تراکنش‌های نوشتن)؛ نوشتن‌های نادر گزارش (تعریف/اجرا
    /// گزارش دلخواه) همان‌طور روی کانتکست اصلی می‌مانند.
    /// </summary>
    private readonly Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext _reporting;

    // ═══ چرخهٔ ۷۲ — منطقهٔ زمانی رسمی ایران برای مرزبندی روزهای گزارش ═══
    // گزارش مالک: «هیچکدام از گزارش‌ها کار نمیکنند و برای همه چیز صفر می‌نویسن».
    // دو ریشهٔ زندهٔ صفر (ریشه‌یابی چرخهٔ ۷۲):
    //   ۱) گرهِ نیمه‌شبِ «تا تاریخ»: کلاینت تاریخ را yyyy-MM-dd می‌فرستد؛ سرور
    //      آن را ۰۰:۰۰ همان روز می‌پنداشت و کوئری‌های «...<= to» کل روزِ پایانی
    //      را حذف می‌کردند — انتخاب «از = تا = امروز» یعنی بازهٔ عملاً تهی و
    //      همهٔ جمع‌ها صفر؛ حتی وقتی دادهٔ امروز موجود بود.
    //   ۲) اختلاف منطقهٔ زمانی: کاربر تاریخ را به تقویم شمسی (وقت ایران)
    //      انتخاب می‌کند ولی ستون‌های زمانی دیتابیس (CreatedAt/InvoiceDate/
    //      PaymentDate/...) UTC هستند؛ ایران UTC+3:30 است (از ۱۴۰۱ بدون
    //      ساعت تابستانی) و مرز روزها ۳:۳۰ ساعت جابه‌جا می‌افتاد.
    // راه‌حل: هر دو مرز بازه به «نیمه‌شب تا پایان‌روز به وقت ایران» تبدیل
    // می‌شوند (ایران‌محور) — کلاس partial در همهٔ فایل‌های ReportsController.*
    // به این اعضا دسترسی دارد.
    /// <summary>اختلاف ساعت رسمی ایران با UTC — ثابت +3:30 (بدون ساعت تابستانی)</summary>
    private static readonly TimeSpan IranOffset = TimeSpan.FromHours(3.5);

    /// <summary>نیمه‌شب UTC روز آغازین به وقت ایران — برای ستون‌های زمانی UTC</summary>
    private static DateTime IranDayStartUtc(DateTime iranDate) => iranDate.Date - IranOffset;

    /// <summary>پایان روز ایران در UTC (۲۳:۵۹:۵۹.۹۹…) — سازگار با مقایسه‌های «کوچک‌تر-مساوی to»</summary>
    private static DateTime IranDayEndUtc(DateTime iranDate)
        => iranDate.Date.AddDays(1) - IranOffset - TimeSpan.FromTicks(1);

    /// <summary>تاریخِ ایرانِ یک لحظهٔ UTC — برای فیلتر ستون‌های DateOnly</summary>
    private static DateOnly IranDateOf(DateTime utc) => DateOnly.FromDateTime(utc + IranOffset);

    /// <summary>
    /// ═══ چرخهٔ ۷۹ — گارد تاریخِ جلالیِ ارسالی از کلاینت‌های قدیمی ═══
    /// ریشهٔ واقعی «گزارش‌های همه-صفر» (هشتمین گزارش مالک): نسخه‌های پیشین
    /// دسکتاپ به‌دلیل تقویمِ فرهنگ fa-IR، رشتهٔ «yyyy-MM-dd» با اجزای جلالی
    /// می‌فرستادند (مثلاً 1405-07-22 به‌جای 2026-10-14)؛ مدل‌بایند [FromQuery]
    /// آن را «سال ۱۴۰۵ میلادی» بی‌صدا می‌پنداشت → بازهٔ قرن پانزدهم → همهٔ
    /// کوئری‌ها تهی → همهٔ گزارش‌ها صفر با پیام «در این بازه ردیفی ثبت نشده».
    /// سالِ میلادیِ ۱۳۰۰..۱۵۰۰ برای داده‌های درمانگاه هرگز معتبر نیست؛ چنین
    /// مقداری به‌عنوان «جلالی» شناسایی و به میلادی واقعی برگردانده می‌شود تا
    /// ایستگاه‌هایی که هنوز بیلد قدیمی اجرا می‌کنند هم گزارش درست بگیرند.
    /// </summary>
    private static DateTime NormalizeClientDate(DateTime d)
    {
        if (d.Year is >= 1300 and <= 1500)
        {
            try
            {
                return Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
                    .ToGregorian(d.Year, d.Month, d.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                // روز/ماه جلالی نامعتبر — همان مقدار خام برگردد (بازهٔ تهی می‌ماند
                // ولی لاگ‌پذیر است؛ سرور نباید با ورودی خراب کرش کند)
            }
        }
        return d;
    }

    /// <summary>سازنده با تزریق میانجی، واحد کار (نوشتن‌های نادر) و کانتکست گزارش + کش حافظه‌ای (چرخه ۱۲ B5)</summary>
    public ReportsController(IMediator mediator, IUnitOfWork unitOfWork,
        Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext reporting,
        IMemoryCache catalogCache)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _reporting = reporting;
        _catalogCache = catalogCache;
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

    /// <summary>
    /// گزارش تفصیلی «سود و حقوق درصدی هر پرسنل» دوره — چرخه دهم
    /// </summary>
    /// <param name="year">سال شمسی دوره</param>
    /// <param name="month">ماه شمسی دوره — ۱ تا ۱۲</param>
    /// <param name="departmentId">فیلتر اختیاری بخش</param>
    /// <param name="employeeId">فیلتر اختیاری یک پرسنل</param>
    [HttpGet("commission-detail")]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> CommissionDetail([FromQuery] int year, [FromQuery] int month,
        [FromQuery] long? departmentId = null, [FromQuery] long? employeeId = null)
    {
        var result = await _mediator.Send(
            new GetCommissionDetailReportQuery(year, month, departmentId, employeeId));
        return ToResponse(result);
    }

    /// <summary>
    /// خروجی اکسل گزارش سود و حقوق درصدی — فایل xlsx دو برگ (خلاصه + جزئیات)
    /// </summary>
    /// <param name="year">سال شمسی دوره</param>
    /// <param name="month">ماه شمسی دوره — ۱ تا ۱۲</param>
    /// <param name="departmentId">فیلتر اختیاری بخش</param>
    /// <param name="employeeId">فیلتر اختیاری یک پرسنل</param>
    [HttpGet("commission-detail/excel")]
    [HasPermission("payroll.manage")]
    public async Task<IActionResult> CommissionDetailExcel([FromQuery] int year, [FromQuery] int month,
        [FromQuery] long? departmentId = null, [FromQuery] long? employeeId = null)
    {
        var result = await _mediator.Send(
            new ExportCommissionDetailExcelQuery(year, month, departmentId, employeeId));
        if (result.IsFailure)
            return ToResponse(result);

        return File(result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"گزارش-سود-و-حقوق-درصدی-{year}-{month:00}.xlsx");
    }
    /* ═══════════════════════════════════════════════════════════
       گام U16 — گزارش‌ساز (فرم ۲۷): کاتالوگ ۳۱ گزارش + اجرای پارامتری
       گام P1 (v1.4) — تکمیل هر ۱۵ گزارش باقی‌مانده با کوئری واقعی
       چرخه دوازدهم (B5): کش حافظه‌ای یک‌ساعته کاتالوگ — فهرست ثابت است
       و هر بار ساختن آن هزینه بی‌دلیل دارد؛ باطل‌شدن با تعویض سند
       ═══════════════════════════════════════════════════════════ */

    /// <summary>گزینه‌های کش کاتالوگ — انقضای مطلق یک‌ساعته</summary>
    private static readonly MemoryCacheEntryOptions _catalogCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        Size = 1,
    };

    /// <summary>کاتالوگ گزارش‌ها دسته‌بندی‌شده — مطابق فهرست پرامپت بخش ۴ (کش یک‌ساعته)</summary>
    [HttpGet("catalog")]
    [HasPermission("reports.view")]
    public IActionResult GetCatalog()
    {
        var catalog = _catalogCache.GetOrCreate("reports-catalog", entry =>
        {
            entry.SetOptions(_catalogCacheOptions);
            return BuildCatalog();
        }) ?? BuildCatalog();
        return Ok(ApiResponse<object>.Success(catalog));
    }

    private List<object> BuildCatalog()
    {
        var catalog = new List<object>
        {
            new { category = "💰 مالی", icon = "💰", reports = new[]
            {
                new { id = "daily-revenue", title = "درآمد روزانه", description = "گزارش درآمد روزانه درمانگاه به تفکیک روز", paramsList = new[] { "fromDate", "toDate", "doctorId", "insuranceId" } },
                new { id = "monthly-revenue", title = "درآمد ماهانه", description = "روند ماهانه درآمد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "detailed-revenue", title = "درآمد تفصیلی", description = "جزئیات هر فاکتور در بازهٔ انتخاب‌شده", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "patient-statement", title = "صورت‌حساب بیمار", description = "گردش حساب بیمار در بازه", paramsList = new[] { "patientId", "fromDate", "toDate" } },
                new { id = "debtors-list", title = "لیست بدهکاران", description = "بیماران دارای مانده بدهی", paramsList = new[] { "minAmount" } },
                new { id = "cashier-daily", title = "گزارش صندوق روزانه", description = "گردش صندوق در روز", paramsList = new[] { "date" } },
                new { id = "doctor-commission", title = "کارانه پزشکان", description = "محاسبه سهم پزشکان", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "profit-loss", title = "سود و زیان", description = "صورت سود و زیان دوره", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "accounts-receivable", title = "مطالبات بیمه‌ای", description = "ماندهٔ مطالبات باز هر بیمه‌گر", paramsList = new[] { "insuranceId" } },
                new { id = "insurance-claims", title = "گردش مطالبات بیمه", description = "ریز وضعیت و مبالغ مطالبات بیمه در بازهٔ ارسال/ثبت", paramsList = new[] { "fromDate", "toDate", "insuranceId" } },
                new { id = "payment-methods", title = "تفکیک روش‌های پرداخت", description = "پرداخت‌ها به تفکیک روش", paramsList = new[] { "fromDate", "toDate" } }
            }},
            new { category = "👤 بیماران", icon = "👤", reports = new[]
            {
                new { id = "patient-demographics", title = "آمار جمعیت‌شناسی", description = "توزیع سنی و جنسی بیماران", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "new-patients", title = "بیماران جدید", description = "بیماران ثبت‌شده در دوره", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "frequent-visitors", title = "پرمراجعه‌ترین‌ها", description = "بیماران با بیشترین ویزیت", paramsList = new[] { "top" } },
                new { id = "patient-insurance", title = "تفکیک بیمه‌ای بیماران", description = "توزیع پوشش اصلی و تکمیلی و بیماران فاقد بیمه", paramsList = Array.Empty<string>() },
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
                new { id = "top-medications", title = "پرمصرف‌ترین داروها", description = "پرمصرف‌ترین اقلام", paramsList = new[] { "fromDate", "toDate", "top" } }
            }},
            new { category = "📊 هوش تجاری", icon = "📊", reports = new[]
            {
                new { id = "revenue-trend", title = "روند درآمد", description = "روند بلندمدت درآمد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "visit-patterns", title = "الگوی مراجعات", description = "الگوی ساعتی و روزانه", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "service-mix", title = "ترکیب خدمات", description = "سهم خدمات از درآمد", paramsList = new[] { "fromDate", "toDate" } }
            }},
            // چرخه یازدهم — گزارش‌های نموداری متعدد؛ هر کدام با نوع نمودارِ درستِ ماهیتش
            new { category = "📈 گزارش‌های نموداری", icon = "📈", reports = new[]
            {
                new { id = "chart-revenue-trend", title = "روند درآمد روزانه", description = "نمودار سطحی روند درآمد در بازه — مناسب پایش رشد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-revenue-monthly", title = "درآمد ۱۲ ماه اخیر", description = "مقایسهٔ درآمد ۱۲ ماه شمسی تا امروز", paramsList = Array.Empty<string>() },
                new { id = "chart-doctor-share", title = "سهم پزشکان از ویزیت", description = "نمودار دونات سهم هر پزشک از کل ویزیت‌ها", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-service-mix", title = "ترکیب خدمات از درآمد", description = "نمودار دونات سهم خدمات از درآمد", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-visit-hourly", title = "الگوی ساعتی مراجعات", description = "نمودار ستونی تعداد نوبت هر ساعت روز", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-visit-weekday", title = "الگوی روزهای هفته", description = "نمودار ستونی نوبت‌ها به تفکیک روز هفته (شنبه تا جمعه)", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-payment-methods", title = "سهم روش‌های پرداخت", description = "نمودار دونات تفکیک درآمد نقدی/کارت/آنلاین/…", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-gender-mix", title = "توزیع جنسیت بیماران", description = "نمودار دونات نسبت مرد/زن کل بیماران", paramsList = Array.Empty<string>() },
                new { id = "chart-age-brackets", title = "توزیع سنی بیماران", description = "نمودار ستونی رده‌های سنی (کودک تا سالمند)", paramsList = Array.Empty<string>() },
                new { id = "chart-top-services", title = "پردرآمدترین خدمات", description = "نمودار میله‌ای افقی رتبه‌بندی خدمات", paramsList = new[] { "fromDate", "toDate", "top" } },
                // چرخه دوازدهم — دسته دوم گزارش‌های نموداری (فاز H)
                new { id = "chart-branch-revenue", title = "درآمد به تفکیک شعبه", description = "نمودار ستونی مقایسه درآمد شعبه‌ها", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-insurance-mix", title = "سهم بیمه‌گران از فاکتورها", description = "نمودار دونات مبلغ فاکتورهای هر بیمه‌گر", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-receivable-aging", title = "سن بدهی مطالبات", description = "نمودار ستونی مانده‌ها در رده‌های ۰-۳۰/۳۱-۶۰/۶۱-۹۰/۹۰+ روز", paramsList = Array.Empty<string>() },
                new { id = "chart-new-vs-returning", title = "سهم بیمار جدید از نوبت‌ها", description = "نمودار دونات ویزیت اول در برابر بازگشتی", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-new-patients-trend", title = "روند ماهانه بیمار جدید", description = "نمودار سطحی ثبت بیمار جدید به تفکیک ماه شمسی", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-top-debtors", title = "بدهکاران برتر", description = "نمودار میله‌ای افقی بیشترین مانده بدهی بیماران", paramsList = new[] { "fromDate", "toDate", "top" } },
                new { id = "chart-appointment-status", title = "ترکیب وضعیت نوبت‌ها", description = "نمودار دونات نوبت‌ها به تفکیک وضعیت", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-lab-top-tests", title = "پرتکرارترین آزمون‌ها", description = "نمودار میله‌ای افقی پرتکرارترین تست‌های آزمایشگاه", paramsList = new[] { "fromDate", "toDate", "top" } },
                new { id = "chart-top-medications", title = "پرمصرف‌ترین داروها", description = "نمودار میله‌ای افقی پرمصرف‌ترین داروهای نسخه", paramsList = new[] { "fromDate", "toDate", "top" } },
                new { id = "chart-salary-share", title = "سهم پرسنل از حقوق درصدی", description = "نمودار دونات حقوق درصدی پرسنل در آخرین دوره", paramsList = Array.Empty<string>() },
                new { id = "chart-prescription-trend", title = "روند روزانه صدور نسخه", description = "نمودار سطحی تعداد نسخه در هر روز", paramsList = new[] { "fromDate", "toDate" } },
                // چرخه سیزدهم — هوش تجاری (D1/D4/D5)
                new { id = "chart-growth-mom", title = "رشد ماه‌به‌ماه (MoM)", description = "نمودار ستونی دوقلو با درصد رشد و جهت صعود/نزول", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-growth-yoy", title = "مقایسه سال‌به‌سال (YoY)", description = "ستونی دوقلو: همین ماهِ امسال در برابر سال قبل", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-rfm-analysis", title = "تحلیل RFM بیماران", description = "دونات رده‌بندی ۸گانه + فهرست قهرمانان و در معرض خطر", paramsList = new[] { "fromDate", "toDate" } },
                new { id = "chart-revenue-forecast", title = "پیش‌بینی روند درآمد", description = "سطحی: رگرسیون ۶ ماه اخیر + پیش‌بینی ماه بعد با بازه اطمینان ۸۰٪", paramsList = new[] { "fromDate", "toDate" } }
            }}
        };

        return catalog;
    }

    /// <summary>اجرای گزارش با پارامترها — کارت‌های خلاصه + جدول نتایج</summary>
    /// <remarks>
    /// گام P1 (v1.4): هر ۳۱ گزارش کاتالوگ کوئری واقعی دارند.
    /// پارامترهای عمومی: fromDate / toDate / top — پارامترهای اختصاصی:
    /// patientId (صورت‌حساب بیمار) | date (صندوق روزانه) | months (انقضا/بدون مراجعه) |
    /// icd10 (بیماری مزمن) | minAmount (بدهکاران) | insuranceId / doctorId (رزرو آینده)
    /// </remarks>
    [HttpGet("execute/{reportId}")]
    [HasPermission("reports.view")]
    public async Task<IActionResult> ExecuteReport(string reportId,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int top = 20,
        [FromQuery] long? patientId = null, [FromQuery] DateTime? date = null,
        [FromQuery] int months = 6, [FromQuery] string? icd10 = null,
        [FromQuery] decimal? minAmount = null, [FromQuery] long? insuranceId = null,
        [FromQuery] long? doctorId = null)
    {
        // ═══ چرخهٔ ۷۲ — بازهٔ «روزِ کامل به وقت ایران» (رفع دو ریشهٔ «همه‌چیز صفر») ═══
        // تاریخ‌های ورودی کلاینت، تاریخِ تقویم شمسی به وقت ایران هستند؛ نیمه‌شبِ
        // آن روز در UTC ذخیره می‌شود و پایان روز هم کاملِ همان روز را می‌پوشاند
        // (پیش‌تر «تا تاریخ» = ۰۰:۰۰ همان روز بود و کل روزِ پایانی حذف می‌شد).
        // ═══ چرخهٔ ۷۹ — ابتدا گارد جلالی: کلاینت قدیمی اجزای جلالی فرستاده؟ ═══
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        if (fromDate.HasValue)
            from = IranDayStartUtc(NormalizeClientDate(fromDate.Value));
        if (toDate.HasValue)
            to = IranDayEndUtc(NormalizeClientDate(toDate.Value));
        if (date.HasValue)
            date = NormalizeClientDate(date.Value);
        top = Math.Clamp(top, 1, 200);
        months = Math.Clamp(months, 1, 120);

        return reportId switch
        {
            // ── مالی ──
            "daily-revenue" => await DailyRevenueReportAsync(from, to),
            "monthly-revenue" => await MonthlyRevenueReportAsync(from, to),
            "detailed-revenue" => await DetailedRevenueReportAsync(from, to),
            "revenue-trend" => await RevenueTrendReportAsync(from, to),
            "patient-statement" => await PatientStatementAsync(patientId, from, to),
            "debtors-list" => await DebtorsReportAsync(minAmount),
            "cashier-daily" => await CashierDailyReportAsync(date ?? (DateTime.UtcNow + IranOffset)),
            "doctor-commission" => await DoctorCommissionReportAsync(from, to),
            "profit-loss" => await ProfitLossReportAsync(from, to),
            "accounts-receivable" => await AccountsReceivableReportAsync(insuranceId),
            "insurance-claims" => await InsuranceClaimsReportAsync(from, to, insuranceId),
            "payment-methods" => await PaymentMethodsReportAsync(from, to),
            // ── بیماران ──
            "patient-demographics" => await DemographicsReportAsync(from, to),
            "new-patients" => await NewPatientsReportAsync(from, to),
            "frequent-visitors" => await FrequentVisitorsReportAsync(top),
            "patient-insurance" => await PatientInsuranceReportAsync(),
            "chronic-diseases" => await ChronicDiseasesReportAsync(icd10),
            "vip-patients" => await VipPatientsReportAsync(),
            "inactive-patients" => await InactivePatientsReportAsync(months),
            // ── پزشکان ──
            "doctor-performance" => await DoctorReportAsync(from, to, "performance"),
            "doctor-revenue" => await DoctorReportAsync(from, to, "revenue"),
            "doctor-comparison" => await DoctorReportAsync(from, to, "comparison"),
            "avg-visit-duration" => await AvgVisitDurationReportAsync(from, to),
            // ── نوبت‌دهی ──
            "appointment-statistics" => await AppointmentStatsReportAsync(from, to),
            "no-show-report" => await NoShowReportAsync(from, to),
            "cancellation-report" => await CancellationReportAsync(from, to),
            // ── داروخانه ──
            "low-stock" => await LowStockReportAsync(),
            "inventory-report" => await InventoryReportAsync(),
            "expiry-report" => await ExpiryReportAsync(months),
            "top-medications" => await TopMedicationsReportAsync(from, to, top),
            // ── هوش تجاری ──
            "visit-patterns" => await VisitPatternsReportAsync(from, to),
            "service-mix" => await ServiceMixReportAsync(from, to),
            // ── گزارش‌های نموداری — چرخه یازدهم ──
            "chart-revenue-trend" => await ChartRevenueTrendAsync(from, to),
            "chart-revenue-monthly" => await ChartRevenueMonthlyAsync(),
            "chart-doctor-share" => await ChartDoctorShareAsync(from, to),
            "chart-service-mix" => await ChartServiceMixAsync(from, to),
            "chart-visit-hourly" => await ChartVisitHourlyAsync(from, to),
            "chart-visit-weekday" => await ChartVisitWeekdayAsync(from, to),
            "chart-payment-methods" => await ChartPaymentMethodsAsync(from, to),
            "chart-gender-mix" => await ChartGenderMixAsync(),
            "chart-age-brackets" => await ChartAgeBracketsAsync(),
            "chart-top-services" => await ChartTopServicesAsync(from, to, top),
            // ── گزارش‌های نموداری دسته دوم — چرخه دوازدهم ──
            "chart-branch-revenue" => await ChartBranchRevenueAsync(from, to),
            "chart-insurance-mix" => await ChartInsuranceMixAsync(from, to),
            "chart-receivable-aging" => await ChartReceivableAgingAsync(),
            "chart-new-vs-returning" => await ChartNewVsReturningAsync(from, to),
            "chart-new-patients-trend" => await ChartNewPatientsTrendAsync(from, to),
            "chart-top-debtors" => await ChartTopDebtorsAsync(from, to, top),
            "chart-appointment-status" => await ChartAppointmentStatusAsync(from, to),
            "chart-lab-top-tests" => await ChartLabTopTestsAsync(from, to, top),
            "chart-top-medications" => await ChartTopMedicationsAsync(from, to, top),
            "chart-salary-share" => await ChartSalaryShareAsync(),
            "chart-prescription-trend" => await ChartPrescriptionTrendAsync(from, to),
            // ── گزارش‌های هوش تجاری — چرخه سیزدهم ──
            "chart-growth-mom" => await ChartGrowthMomAsync(from, to),
            "chart-growth-yoy" => await ChartGrowthYoyAsync(from, to),
            "chart-rfm-analysis" => await ChartRfmAnalysisAsync(from, to),
            "chart-revenue-forecast" => await ChartRevenueForecastAsync(from, to),
            _ => Ok(ApiResponse<object>.Success(new
            {
                available = false,
                message = "شناسه گزارش ناشناخته است"
            }))
        };
    }

    /* ═══════════════════════════════════════════════════════════
       بخش ۱ — گزارش‌های مالی
       ═══════════════════════════════════════════════════════════ */

    /// <summary>درآمد روزانه بر مبنای تاریخ صدور فاکتور (نه زمان ایجاد رکورد)</summary>
    private async Task<IActionResult> DailyRevenueReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded &&
                        i.InvoiceDate >= from && i.InvoiceDate <= to)
            .Select(i => new { i.InvoiceDate, i.TotalAmount, i.PaidAmount })
            .ToListAsync();

        var rows = invoices.GroupBy(i => (i.InvoiceDate + IranOffset).Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = Application.Common.Helpers.PersianDateHelper.ToShamsi(g.Key),
                invoiceCount = g.Count(),
                totalAmount = g.Sum(i => i.TotalAmount),
                paidAmount = g.Sum(i => i.PaidAmount)
            }).ToList();

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

    /// <summary>درآمد ماهانه — تجمیع واقعی در ماه‌های تقویم شمسی</summary>
    private async Task<IActionResult> MonthlyRevenueReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded &&
                        i.InvoiceDate >= from && i.InvoiceDate <= to)
            .Select(i => new { i.InvoiceDate, i.TotalAmount, i.PaidAmount })
            .ToListAsync();

        var rows = invoices
            .Select(i => new { Invoice = i, Jalali = Application.Common.Helpers.PersianDateHelper.ToJalali(i.InvoiceDate + IranOffset) })
            .GroupBy(x => (x.Jalali.Year, x.Jalali.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                month = $"{g.Key.Year:0000}/{g.Key.Month:00}",
                invoiceCount = g.Count(),
                totalAmount = g.Sum(x => x.Invoice.TotalAmount),
                paidAmount = g.Sum(x => x.Invoice.PaidAmount)
            }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = rows.Sum(r => r.totalAmount), paid = rows.Sum(r => r.paidAmount), months = rows.Count },
            columns = new[] { "ماه", "تعداد فاکتور", "مبلغ کل", "پرداخت‌شده" },
            rows
        }));
    }

    /// <summary>روند تجمعی درآمد روزانه — تاریخ صدور فاکتور و حذف فاکتورهای ابطال‌شده</summary>
    private async Task<IActionResult> RevenueTrendReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded &&
                        i.InvoiceDate >= from && i.InvoiceDate <= to)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        var daily = invoices.GroupBy(i => (i.InvoiceDate + IranOffset).Date)
            .OrderBy(g => g.Key)
            .Select(g => new { date = g.Key, amount = g.Sum(i => i.TotalAmount) })
            .ToList();
        decimal cumulative = 0m;
        var rows = daily.Select(d =>
        {
            cumulative += d.amount;
            return new
            {
                date = Application.Common.Helpers.PersianDateHelper.ToShamsi(d.date),
                dailyRevenue = d.amount,
                cumulativeRevenue = cumulative
            };
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = cumulative, days = rows.Count },
            columns = new[] { "تاریخ", "درآمد روز", "درآمد تجمعی" },
            rows
        }));
    }

    /// <summary>جزئیات فاکتورهای بازه — یک ردیف برای هر فاکتور، ستون‌ها و داده‌ها هم‌تعداد</summary>
    private async Task<IActionResult> DetailedRevenueReportAsync(DateTime from, DateTime to)
    {
        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded &&
                        i.InvoiceDate >= from && i.InvoiceDate <= to)
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new
            {
                i.InvoiceDate,
                i.InvoiceNumber,
                patient = i.Patient != null ? i.Patient.FirstName + " " + i.Patient.LastName : "—",
                doctor = i.Visit != null && i.Visit.Doctor != null && i.Visit.Doctor.Employee != null
                    ? i.Visit.Doctor.Employee.FirstName + " " + i.Visit.Doctor.Employee.LastName : "—",
                insurance = i.InsuranceCompany != null ? i.InsuranceCompany.Name : "—",
                i.TotalAmount,
                i.PaidAmount,
                i.RemainingAmount
            })
            .Take(500)
            .ToListAsync();
        var rows = invoices.Select(i => new
        {
            date = Application.Common.Helpers.PersianDateHelper.ToShamsi(i.InvoiceDate + IranOffset),
            invoiceNumber = i.InvoiceNumber ?? "—",
            i.patient,
            i.doctor,
            i.insurance,
            totalAmount = i.TotalAmount,
            paidAmount = i.PaidAmount,
            remainingAmount = i.RemainingAmount
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                invoices = rows.Count,
                total = rows.Sum(r => r.totalAmount),
                paid = rows.Sum(r => r.paidAmount),
                remaining = rows.Sum(r => r.remainingAmount)
            },
            columns = new[] { "تاریخ", "شماره فاکتور", "بیمار", "پزشک", "بیمه‌گر", "مبلغ کل", "پرداخت‌شده", "مانده" },
            rows
        }));
    }

    /// <summary>صورت‌حساب بیمار — گردش حساب: فاکتورها (بدهی) و پرداخت‌ها (بستانکار) با مانده جاری — گام P1</summary>
    private async Task<IActionResult> PatientStatementAsync(long? patientId, DateTime from, DateTime to)
    {
        if (patientId is null || patientId <= 0)
            return Ok(ApiResponse<object>.Success(new { available = false, message = "برای این گزارش ابتدا بیمار را از پنل پارامترها انتخاب کنید" }));

        var patient = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient is null)
            return Ok(ApiResponse<object>.Success(new { available = false, message = "بیماری با این شناسه یافت نشد" }));

        var invoices = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => i.PatientId == patientId && !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded &&
                        i.InvoiceDate <= to)
            .Select(i => new { i.InvoiceNumber, i.InvoiceDate, i.TotalAmount })
            .ToListAsync();

        var payments = await _reporting.Set<Domain.Entities.Billing.Payment>().AsNoTracking()
            .Where(p => p.PatientId == patientId && !p.IsDeleted && p.PaymentDate <= to)
            .Select(p => new { p.PaymentNumber, p.PaymentDate, p.Amount, p.Method, p.IsReturned })
            .ToListAsync();

        // ماندهٔ افتتاحیه شامل گردش‌های پیش از بازه است؛ مرجوعی وجه نیز
        // بدهی بیمار را افزایش می‌دهد و نباید مانند یک پرداخت عادی کسر شود.
        var openingBalance = invoices.Where(i => i.InvoiceDate < from).Sum(i => i.TotalAmount)
            - payments.Where(p => p.PaymentDate < from && !p.IsReturned).Sum(p => p.Amount)
            + payments.Where(p => p.PaymentDate < from && p.IsReturned).Sum(p => p.Amount);

        var events = new List<(DateTime at, string kind, string desc, decimal debit, decimal credit)>();
        foreach (var i in invoices.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to))
            events.Add((i.InvoiceDate, "فاکتور", "فاکتور " + i.InvoiceNumber, i.TotalAmount, 0m));
        foreach (var p in payments.Where(p => p.PaymentDate >= from && p.PaymentDate <= to))
            events.Add((p.PaymentDate, p.IsReturned ? "مرجوعی پرداخت" : "پرداخت",
                "پرداخت " + (p.PaymentNumber ?? "") + " — " + PaymentMethodName(p.Method),
                p.IsReturned ? p.Amount : 0m, p.IsReturned ? 0m : p.Amount));
        events = events.OrderBy(e => e.at)
            .ThenBy(e => e.kind == "فاکتور" ? 0 : 1)
            .ToList();

        decimal balance = openingBalance;
        var rows = events.Select(e =>
        {
            balance += e.debit - e.credit;
            return new
            {
                date = Application.Common.Helpers.PersianDateHelper.ToShamsi(e.at + IranOffset),
                kind = e.kind,
                desc = e.desc,
                debit = e.debit,
                credit = e.credit,
                balance
            };
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                patient = patient.FirstName + " " + patient.LastName,
                mrn = patient.MedicalRecordNo,
                invoices = invoices.Count(i => i.InvoiceDate >= from && i.InvoiceDate <= to),
                payments = payments.Count(p => p.PaymentDate >= from && p.PaymentDate <= to),
                openingBalance,
                closingBalance = balance
            },
            columns = new[] { "تاریخ", "نوع", "شرح", "بدهی", "بستانکار", "مانده" },
            rows
        }));
    }

    /// <summary>لیست بدهکاران — مانده پرداخت‌نشده هر بیمار (با حداقل مبلغ اختیاری)</summary>
    private async Task<IActionResult> DebtorsReportAsync(decimal? minAmount)
    {
        var threshold = minAmount ?? 0m;
        var rows = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded && i.RemainingAmount > 0 && i.Patient != null)
            .GroupBy(i => new { i.PatientId, Name = i.Patient!.FirstName + " " + i.Patient.LastName })
            .Select(g => new
            {
                patient = g.Key.Name,
                invoiceCount = g.Count(),
                balance = g.Sum(i => i.RemainingAmount)
            })
            .Where(r => r.balance > threshold)
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

    /// <summary>صندوق روزانه — گردش نقدی روز به تفکیک روش پرداخت با مرجوعی و خالص — گام P1</summary>
    private async Task<IActionResult> CashierDailyReportAsync(DateTime date)
    {
        // ═══ چرخهٔ ۷۲ — «روز» به وقت ایران (پیش‌تر مرز روزِ UTC بود و نیمی از
        // روزِ کاری درمانگاه — که تا پاسیب شب ادامه دارد — در گزارش صندوق
        // روزانه نمی‌افتاد یا به روز بعدی می‌افتاد) ═══
        var dayStartUtc = IranDayStartUtc(date);   // ۰۰:۰۰ ایران روز انتخابی
        var nextDayUtc = dayStartUtc.AddDays(1);    // ۰۰:۰۰ ایران روز بعد
        var payments = await _reporting.Set<Domain.Entities.Billing.Payment>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.PaymentDate >= dayStartUtc && p.PaymentDate < nextDayUtc)
            .Select(p => new { p.Method, p.Amount, p.IsReturned })
            .ToListAsync();

        var rows = payments
            .GroupBy(p => p.Method)
            .Select(g => new
            {
                method = PaymentMethodName(g.Key),
                count = g.Count(),
                gross = g.Sum(p => p.Amount),
                refunds = g.Where(p => p.IsReturned).Sum(p => p.Amount),
                net = g.Sum(p => p.IsReturned ? -p.Amount : p.Amount)
            })
            .OrderByDescending(r => r.net)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                date = Application.Common.Helpers.PersianDateHelper.ToShamsi(date.Date),
                payments = payments.Count,
                gross = payments.Sum(p => p.Amount),
                refunds = payments.Where(p => p.IsReturned).Sum(p => p.Amount),
                net = payments.Sum(p => p.IsReturned ? -p.Amount : p.Amount)
            },
            columns = new[] { "روش پرداخت", "تعداد", "مبلغ ناخالص", "مرجوعی", "خالص" },
            rows
        }));
    }

    /// <summary>کارانه پزشکان — از رکوردهای DoctorPayment با وضعیت پرداخت — گام P1</summary>
    private async Task<IActionResult> DoctorCommissionReportAsync(DateTime from, DateTime to)
    {
        var source = await _reporting.Set<Domain.Entities.Billing.DoctorPayment>().AsNoTracking()
            .Where(d => d.PeriodEnd != null && d.PeriodEnd >= IranDateOf(from) && d.PeriodEnd <= IranDateOf(to))
            .OrderByDescending(d => d.NetAmount)
            .Select(d => new
            {
                doctor = d.Doctor != null && d.Doctor.Employee != null
                    ? d.Doctor.Employee.FirstName + " " + d.Doctor.Employee.LastName
                    : "پزشک #" + d.DoctorId,
                d.PeriodStart,
                d.PeriodEnd,
                visits = d.TotalVisits,
                gross = d.GrossAmount,
                deduction = d.DeductionAmount,
                net = d.NetAmount,
                paidAt = d.PaidAt
            })
            .Take(200)
            .ToListAsync();
        var rows = source.Select(d => new
        {
            d.doctor,
            period = d.PeriodStart.HasValue && d.PeriodEnd.HasValue
                ? Application.Common.Helpers.PersianDateHelper.ToShamsi(d.PeriodStart.Value.ToDateTime(TimeOnly.MinValue)) +
                  " تا " + Application.Common.Helpers.PersianDateHelper.ToShamsi(d.PeriodEnd.Value.ToDateTime(TimeOnly.MinValue))
                : "—",
            d.visits,
            d.gross,
            d.deduction,
            d.net,
            status = d.paidAt != null ? "پرداخت شده" : "در انتظار پرداخت"
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                doctors = rows.Count,
                gross = rows.Sum(r => r.gross),
                net = rows.Sum(r => r.net),
                unpaid = rows.Count(r => r.status == "در انتظار پرداخت")
            },
            columns = new[] { "پزشک", "دوره", "ویزیت‌ها", "ناخالص", "کسورات", "خالص", "وضعیت" },
            rows
        }));
    }

    /// <summary>سود و زیان دوره — درآمد خدمات منهای هزینه‌های ثبت‌شده به تفکیک دسته — گام P1</summary>
    private async Task<IActionResult> ProfitLossReportAsync(DateTime from, DateTime to)
    {
        var revenue = await _reporting.Set<Domain.Entities.Billing.Invoice>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Refunded && i.InvoiceDate >= from && i.InvoiceDate <= to)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

        var expenses = await _reporting.Set<Domain.Entities.Billing.Expense>().AsNoTracking()
            .Where(e => e.ExpenseDate != null && e.ExpenseDate >= IranDateOf(from) && e.ExpenseDate <= IranDateOf(to))
            .GroupBy(e => e.Category)
            .Select(g => new { category = g.Key, amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var totalExpense = expenses.Sum(e => e.amount);
        var profit = revenue - totalExpense;

        var rows = new List<object>
        {
            new { item = "درآمد خدمات", amount = revenue, share = "۱۰۰٪" }
        };
        foreach (var e in expenses.OrderByDescending(x => x.amount))
            rows.Add(new { item = "هزینه — " + ExpenseCategoryName(e.category), amount = e.amount, share = revenue > 0 ? Math.Round(e.amount * 100m / revenue, 1).ToString("0.#") + "٪" : "—" });
        rows.Add(new { item = "جمع کل هزینه‌ها", amount = totalExpense, share = revenue > 0 ? Math.Round(totalExpense * 100m / revenue, 1).ToString("0.#") + "٪" : "—" });
        rows.Add(new { item = profit >= 0 ? "سود خالص دوره" : "زیان خالص دوره", amount = Math.Abs(profit), share = revenue > 0 ? Math.Round(Math.Abs(profit) * 100m / revenue, 1).ToString("0.#") + "٪" : "—" });

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                revenue,
                expenses = totalExpense,
                profit,
                margin = revenue > 0 ? Math.Round(profit * 100m / revenue, 1).ToString("0.#") + "٪" : "—"
            },
            columns = new[] { "شرح", "مبلغ", "درصد از درآمد" },
            rows
        }));
    }

    /// <summary>مطالبات بیمه‌ای — مانده مطالبات هر بیمه‌گر از رکوردهای مطالبه — گام P1</summary>
    private async Task<IActionResult> AccountsReceivableReportAsync(long? insuranceId)
    {
        var query = _reporting.Set<Domain.Entities.Billing.InsuranceClaim>().AsNoTracking()
            .Where(c => c.Status != ClaimStatus.Draft);
        if (insuranceId is > 0)
            query = query.Where(c => c.InsuranceCompanyId == insuranceId);

        var rows = await query
            .GroupBy(c => new { c.InsuranceCompanyId, Name = c.InsuranceCompany != null ? c.InsuranceCompany.Name : "بیمه‌گر #" + c.InsuranceCompanyId })
            .Select(g => new
            {
                insurance = g.Key.Name,
                claims = g.Count(),
                claimed = g.Sum(c => c.TotalAmount),
                approved = g.Sum(c => c.ApprovedAmount),
                rejected = g.Sum(c => c.RejectedAmount),
                received = g.Sum(c => c.ReceivedAmount),
                outstanding = g.Sum(c => c.TotalAmount - c.ReceivedAmount)
            })
            .Where(r => r.outstanding > 0)
            .OrderByDescending(r => r.outstanding)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                insurances = rows.Count,
                claims = rows.Sum(r => r.claims),
                claimed = rows.Sum(r => r.claimed),
                outstanding = rows.Sum(r => r.outstanding)
            },
            columns = new[] { "بیمه‌گر", "تعداد مطالبه", "مبلغ مطالبه", "تأییدشده", "ردشده", "دریافتی", "مانده مطالبه" },
            rows
        }));
    }

    /// <summary>ریز گردش مطالبات بیمه، شامل مبالغ ارسال، تأیید، رد و دریافت‌شده</summary>
    private async Task<IActionResult> InsuranceClaimsReportAsync(DateTime from, DateTime to, long? insuranceId)
    {
        var query = _reporting.Set<Domain.Entities.Billing.InsuranceClaim>().AsNoTracking()
            .Where(c => !c.IsDeleted && (c.SubmittedAt ?? c.CreatedAt) >= from &&
                        (c.SubmittedAt ?? c.CreatedAt) <= to);
        if (insuranceId is > 0)
            query = query.Where(c => c.InsuranceCompanyId == insuranceId);

        var claims = await query
            .Select(c => new
            {
                c.ClaimNumber,
                insurance = c.InsuranceCompany != null ? c.InsuranceCompany.Name : "—",
                c.PeriodStart,
                c.PeriodEnd,
                c.TotalAmount,
                c.ApprovedAmount,
                c.RejectedAmount,
                c.ReceivedAmount,
                c.Status,
                claimDate = c.SubmittedAt ?? c.CreatedAt
            })
            .OrderByDescending(c => c.claimDate)
            .ToListAsync();

        var rows = claims.Select(c => new
        {
            claimNumber = string.IsNullOrWhiteSpace(c.ClaimNumber) ? "—" : c.ClaimNumber,
            insurance = c.insurance,
            claimDate = Application.Common.Helpers.PersianDateHelper.ToShamsiFa(c.claimDate + IranOffset),
            periodStart = c.PeriodStart.HasValue
                ? Application.Common.Helpers.PersianDateHelper.ToShamsiFa(c.PeriodStart.Value.ToDateTime(TimeOnly.MinValue)) : "—",
            periodEnd = c.PeriodEnd.HasValue
                ? Application.Common.Helpers.PersianDateHelper.ToShamsiFa(c.PeriodEnd.Value.ToDateTime(TimeOnly.MinValue)) : "—",
            status = c.Status switch
            {
                ClaimStatus.Draft => "پیش‌نویس",
                ClaimStatus.Submitted => "ارسال‌شده",
                ClaimStatus.Approved => "تأییدشده",
                ClaimStatus.PartiallyApproved => "تأیید جزئی",
                ClaimStatus.Rejected => "ردشده",
                ClaimStatus.Paid => "دریافت‌شده",
                _ => "نامشخص"
            },
            totalAmount = c.TotalAmount,
            approvedAmount = c.ApprovedAmount,
            rejectedAmount = c.RejectedAmount,
            receivedAmount = c.ReceivedAmount,
            remainingAmount = c.TotalAmount - c.ReceivedAmount
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                claims = rows.Count,
                totalAmount = rows.Sum(r => r.totalAmount),
                approvedAmount = rows.Sum(r => r.approvedAmount),
                rejectedAmount = rows.Sum(r => r.rejectedAmount),
                receivedAmount = rows.Sum(r => r.receivedAmount),
                remainingAmount = rows.Sum(r => r.remainingAmount)
            },
            columns = new[] { "شماره مطالبه", "بیمه‌گر", "تاریخ", "شروع دوره", "پایان دوره", "وضعیت", "مبلغ مطالبه", "تأییدشده", "ردشده", "دریافتی", "مانده" },
            rows
        }));
    }

    /// <summary>تفکیک روش‌های پرداخت با سهم درصدی — گام P1</summary>
    private async Task<IActionResult> PaymentMethodsReportAsync(DateTime from, DateTime to)
    {
        var payments = await _reporting.Set<Domain.Entities.Billing.Payment>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.PaymentDate >= from && p.PaymentDate <= to)
            .Select(p => new { p.Method, p.Amount, p.IsReturned })
            .ToListAsync();

        var total = payments.Sum(p => p.IsReturned ? -p.Amount : p.Amount);
        var rows = payments
            .GroupBy(p => p.Method)
            .Select(g => new
            {
                method = PaymentMethodName(g.Key),
                count = g.Count(),
                amount = g.Sum(p => p.IsReturned ? -p.Amount : p.Amount),
                share = total > 0 ? Math.Round(g.Sum(p => p.IsReturned ? -p.Amount : p.Amount) * 100m / total, 1).ToString("0.#") + "٪" : "—"
            })
            .OrderByDescending(r => r.amount)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { payments = payments.Count, total },
            columns = new[] { "روش پرداخت", "تعداد", "مبلغ", "سهم" },
            rows
        }));
    }

    /* ═══════════════════════════════════════════════════════════
       بخش ۲ — گزارش‌های بیماران
       ═══════════════════════════════════════════════════════════ */

    /// <summary>بیماران جدید دوره</summary>
    private async Task<IActionResult> NewPatientsReportAsync(DateTime from, DateTime to)
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.RegistrationDate != null &&
                        p.RegistrationDate >= from && p.RegistrationDate <= to)
            .OrderByDescending(p => p.RegistrationDate)
            .Select(p => new
            {
                mrn = p.MedicalRecordNo,
                name = p.FirstName + " " + p.LastName,
                mobile = p.Mobile,
                p.Gender,
                registered = p.RegistrationDate
            })
            .Take(200)
            .ToListAsync();
        var rows = patients.Select(p => new
        {
            p.mrn,
            p.name,
            p.mobile,
            gender = p.Gender == Gender.Male ? "مرد" : p.Gender == Gender.Female ? "زن" : "نامشخص",
            p.registered
        }).ToList();

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
        var rows = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => !v.IsDeleted && v.Patient != null)
            .GroupBy(v => new { v.PatientId, Name = v.Patient!.FirstName + " " + v.Patient.LastName })
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

    /// <summary>جمعیت‌شناسی بیماران — ماتریس گروه سنی × جنسیت — گام P1</summary>
    private async Task<IActionResult> DemographicsReportAsync(DateTime from, DateTime to)
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.BirthDate != null && p.RegistrationDate != null &&
                        p.RegistrationDate >= from && p.RegistrationDate <= to)
            .Select(p => new { p.BirthDate, p.Gender })
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var buckets = new (int minAge, int maxAge, string label)[]
        {
            (0, 2, "نوزاد و شیرخوار (۰-۲)"),
            (3, 12, "کودک (۳-۱۲)"),
            (13, 18, "نوجوان (۱۳-۱۸)"),
            (19, 35, "جوان (۱۹-۳۵)"),
            (36, 59, "میانسال (۳۶-۵۹)"),
            (60, 199, "سالمند (۶۰+)")
        };

        var total = patients.Count;
        var rows = buckets.Select(b =>
        {
            var inBucket = patients.Where(p =>
            {
                var age = today.Year - p.BirthDate!.Value.Year -
                          (today.DayOfYear < p.BirthDate!.Value.DayOfYear ? 1 : 0);
                return age >= b.minAge && age <= b.maxAge;
            }).ToList();
            var male = inBucket.Count(p => p.Gender == Gender.Male);
            var female = inBucket.Count(p => p.Gender == Gender.Female);
            return new
            {
                group = b.label,
                male,
                female,
                total = inBucket.Count,
                share = total > 0 ? Math.Round(inBucket.Count * 100m / total, 1).ToString("0.#") + "٪" : "—"
            };
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                patients = total,
                withBirthDate = patients.Count,
                male = patients.Count(p => p.Gender == Gender.Male),
                female = patients.Count(p => p.Gender == Gender.Female)
            },
            columns = new[] { "گروه سنی", "مرد", "زن", "جمع", "سهم" },
            rows
        }));
    }

    /// <summary>تفکیک بیمه‌ای بیماران — توزیع بر اساس بیمه‌گر اصلی و تکمیلی — گام P1</summary>
    private async Task<IActionResult> PatientInsuranceReportAsync()
    {
        var patients = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.InsuranceCompanyId, p.SupplementaryInsuranceId })
            .ToListAsync();

        var companies = await _reporting.Set<Domain.Entities.Billing.InsuranceCompany>().AsNoTracking()
            .Where(c => !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var coverageRows = patients
            .SelectMany(p => new[]
            {
                (InsuranceId: p.InsuranceCompanyId, Type: "اصلی"),
                (InsuranceId: p.SupplementaryInsuranceId, Type: "تکمیلی")
            })
            .Where(x => x.InsuranceId.HasValue)
            .GroupBy(x => new { InsuranceId = x.InsuranceId!.Value, x.Type })
            .Select(g => new
            {
                insurance = companies.TryGetValue(g.Key.InsuranceId, out var name) ? name : "بیمه‌گر #" + g.Key.InsuranceId,
                coverageType = g.Key.Type,
                patients = g.Count(),
                share = patients.Count == 0 ? "۰٪" :
                    Math.Round(g.Count() * 100m / patients.Count, 1).ToString("0.#") + "٪"
            })
            .OrderByDescending(r => r.patients)
            .Cast<object>()
            .ToList();

        var without = patients.Count(p => p.InsuranceCompanyId == null && p.SupplementaryInsuranceId == null);
        if (without > 0)
            coverageRows.Add(new
            {
                insurance = "بدون بیمه",
                coverageType = "—",
                patients = without,
                share = Math.Round(without * 100m / patients.Count, 1).ToString("0.#") + "٪"
            });

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                patients = patients.Count,
                insured = patients.Count(p => p.InsuranceCompanyId != null || p.SupplementaryInsuranceId != null),
                primaryInsured = patients.Count(p => p.InsuranceCompanyId != null),
                supplementary = patients.Count(p => p.SupplementaryInsuranceId != null),
                uninsured = without
            },
            columns = new[] { "بیمه‌گر", "نوع پوشش", "تعداد بیمار", "سهم از کل بیماران" },
            rows = coverageRows
        }));
    }

    /// <summary>بیماری‌های مزمن — رجیستری تشخیص‌های ICD-10 مزمن + فیلد اظهاری پرونده — گام P1</summary>
    private async Task<IActionResult> ChronicDiseasesReportAsync(string? icd10)
    {
        // پیشوندهای فصل‌های مزمن ICD-10: غدد/دیابت، هیپرتانسیون، ایسکمیک قلبی، نارسایی قلبی، تنفسی مزمن، گوارشی، آرتروپاتی، کلیه مزمن، خلقی/اضطرابی، نورولوژیک
        var chronicPrefixes = new[]
        {
            "E1", "I1", "I2", "I5", "J4", "K2", "M1", "M2", "N18", "F3", "F4", "G2", "G3", "G4"
        };

        var visits = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => v.PrimaryDiagnosis != null && v.Patient != null)
            .Select(v => new
            {
                code = v.PrimaryDiagnosis!.Code,
                title = v.PrimaryDiagnosis.DescriptionFa ?? v.PrimaryDiagnosis.Description,
                patientId = v.PatientId,
                patientName = v.Patient!.FirstName + " " + v.Patient.LastName,
                date = v.VisitDate
            })
            .ToListAsync();

        // مجموعه اقلام مزمن — از فصل‌های مزمن ICD-10 (یا پیشوند اختصاصی پارامتر icd10)
        List<(string code, string title, long patientId, string patientName, DateTime date)> chronic;
        if (!string.IsNullOrWhiteSpace(icd10))
            chronic = visits
                .Where(v => v.code.StartsWith(icd10.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(v => (v.code, v.title, v.patientId, v.patientName, v.date))
                .ToList();
        else
            chronic = visits
                .Where(v => chronicPrefixes.Any(p => v.code.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                .Select(v => (v.code, v.title, v.patientId, v.patientName, v.date))
                .ToList();

        var rows = chronic
            .GroupBy(v => v.code.Trim().ToUpperInvariant())
            .Select(g => new
            {
                code = g.Key,
                title = g.First().title,
                patients = g.Select(x => x.patientId).Distinct().Count(),
                visits = g.Count(),
                lastVisit = g.Max(x => x.date)
            })
            .OrderByDescending(r => r.patients)
            .Take(100)
            .ToList();

        var declaredCount = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .CountAsync(p => p.ChronicDiseases != null && p.ChronicDiseases != "");

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                diagnoses = rows.Count,
                patients = rows.Sum(r => r.patients),
                visits = rows.Sum(r => r.visits),
                declared = declaredCount
            },
            columns = new[] { "کد ICD", "تشخیص", "بیماران یکتا", "کل ویزیت‌ها", "آخرین ویزیت" },
            rows
        }));
    }

    /// <summary>بیماران VIP و کارمند — گام P1</summary>
    private async Task<IActionResult> VipPatientsReportAsync()
    {
        var rows = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => p.PatientType != PatientType.Normal && !p.IsDeleted)
            .OrderByDescending(p => p.LastVisitDate)
            .Select(p => new
            {
                mrn = p.MedicalRecordNo,
                name = p.FirstName + " " + p.LastName,
                type = p.PatientType == PatientType.Vip ? "ویژه (VIP)" : "کارمند",
                mobile = p.Mobile,
                lastVisit = p.LastVisitDate
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                total = rows.Count,
                vip = rows.Count(r => r.type.StartsWith("ویژه")),
                staff = rows.Count(r => r.type == "کارمند")
            },
            columns = new[] { "شماره پرونده", "نام", "نوع", "موبایل", "آخرین مراجعه" },
            rows
        }));
    }

    /// <summary>بیماران بدون مراجعه — بیش از N ماه — گام P1</summary>
    private async Task<IActionResult> InactivePatientsReportAsync(int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var rows = await _reporting.Set<Domain.Entities.People.Patient>().AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive &&
                        ((p.LastVisitDate != null && p.LastVisitDate < cutoff) ||
                         (p.LastVisitDate == null && p.RegistrationDate != null && p.RegistrationDate < cutoff)))
            .OrderBy(p => p.LastVisitDate ?? p.RegistrationDate)
            .Select(p => new
            {
                mrn = p.MedicalRecordNo,
                name = p.FirstName + " " + p.LastName,
                mobile = p.Mobile,
                lastVisit = p.LastVisitDate ?? p.RegistrationDate,
                registered = p.RegistrationDate
            })
            .Take(300)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                patients = rows.Count,
                months,
                oldest = rows.FirstOrDefault()?.lastVisit
            },
            columns = new[] { "شماره پرونده", "نام", "موبایل", "آخرین مراجعه/ثبت", "تاریخ ثبت" },
            rows
        }));
    }

    /* ═══════════════════════════════════════════════════════════
       بخش ۳ — گزارش‌های پزشکان و نوبت‌دهی
       ═══════════════════════════════════════════════════════════ */

    /// <summary>گزارش‌های پزشک: عملکرد، درآمد مستقل و مقایسهٔ هر دو سنجه</summary>
    private async Task<IActionResult> DoctorReportAsync(DateTime from, DateTime to, string mode)
    {
        var visits = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => !v.IsDeleted && v.VisitDate >= from && v.VisitDate <= to && v.Doctor != null)
            .Select(v => new { v.DoctorId, name = v.Doctor!.Employee != null
                ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName
                : "پزشک #" + v.DoctorId })
            .ToListAsync();
        var visitCounts = visits.GroupBy(v => v.DoctorId)
            .ToDictionary(g => g.Key, g => new { name = g.First().name, count = g.Count() });

        var billedItems = await _reporting.Set<Domain.Entities.Billing.InvoiceItem>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.DoctorId != null && i.Invoice != null && !i.Invoice.IsDeleted &&
                        i.Invoice.Status != InvoiceStatus.Cancelled && i.Invoice.Status != InvoiceStatus.Draft && i.Invoice.Status != InvoiceStatus.Refunded &&
                        i.Invoice.InvoiceDate >= from && i.Invoice.InvoiceDate <= to)
            .Select(i => new { doctorId = i.DoctorId!.Value, i.TotalPrice })
            .ToListAsync();
        var itemGroups = billedItems.GroupBy(i => i.doctorId).ToList();
        var revenues = itemGroups.ToDictionary(g => g.Key, g => g.Sum(i => i.TotalPrice));
        var itemCounts = itemGroups.ToDictionary(g => g.Key, g => g.Count());
        var doctorIds = visitCounts.Keys.Union(revenues.Keys).Distinct().ToList();
        var names = await _reporting.Set<Domain.Entities.People.Doctor>().AsNoTracking()
            .Where(d => doctorIds.Contains(d.Id) && !d.IsDeleted)
            .Select(d => new { d.Id, name = d.Employee != null
                ? d.Employee.FirstName + " " + d.Employee.LastName : "پزشک #" + d.Id })
            .ToDictionaryAsync(d => d.Id, d => d.name);

        var rows = doctorIds.Select(id => new
            {
                doctor = visitCounts.TryGetValue(id, out var visit) ? visit.name : names.GetValueOrDefault(id, "پزشک #" + id),
                visits = visitCounts.GetValueOrDefault(id)?.count ?? 0,
                invoiceItems = itemCounts.GetValueOrDefault(id),
                revenue = revenues.GetValueOrDefault(id)
            })
            .OrderByDescending(r => mode == "revenue" ? r.revenue : r.visits)
            .ToList();

        var columns = mode switch
        {
            "revenue" => new[] { "پزشک", "تعداد اقلام فاکتور", "درآمد ثبت‌شده" },
            "comparison" => new[] { "پزشک", "تعداد ویزیت", "درآمد ثبت‌شده", "درآمد به‌ازای ویزیت" },
            _ => new[] { "پزشک", "تعداد ویزیت", "درآمد ثبت‌شده" }
        };
        object resultRows = mode switch
        {
            "revenue" => rows.Select(r => new { r.doctor, r.invoiceItems, r.revenue }).ToList(),
            "comparison" => rows.Select(r => new
            {
                r.doctor,
                r.visits,
                r.revenue,
                revenuePerVisit = r.visits > 0 ? Math.Round(r.revenue / r.visits, 0) : 0m
            }).ToList(),
            _ => rows.Select(r => new { r.doctor, r.visits, r.revenue }).ToList()
        };

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                doctors = rows.Count,
                visits = rows.Sum(r => r.visits),
                revenue = rows.Sum(r => r.revenue)
            },
            columns,
            rows = resultRows
        }));
    }

    /// <summary>میانگین مدت ویزیت هر پزشک — از ویزیت‌های با زمان شروع/پایان ثبت‌شده — گام P1</summary>
    private async Task<IActionResult> AvgVisitDurationReportAsync(DateTime from, DateTime to)
    {
        var timedVisits = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => !v.IsDeleted && v.VisitDate >= from && v.VisitDate <= to &&
                        v.StartTime != null && v.EndTime != null && v.Doctor != null)
            .Select(v => new
            {
                doctor = v.Doctor!.Employee != null ? v.Doctor.Employee.FirstName + " " + v.Doctor.Employee.LastName : "پزشک #" + v.DoctorId,
                start = v.StartTime!.Value,
                end = v.EndTime!.Value
            })
            .ToListAsync();

        // محاسبه پس از materialize برای استقلال از توابع اختصاصی SQL Server.
        var visits = timedVisits.Select(v => new
        {
            v.doctor,
            minutes = Math.Max(0, (int)(v.end - v.start).TotalMinutes)
        }).ToList();

        var rows = visits
            .GroupBy(v => v.doctor)
            .Select(g => new
            {
                doctor = g.Key,
                visits = g.Count(),
                avg = Math.Round(g.Average(v => v.minutes), 1),
                shortest = g.Min(v => v.minutes),
                longest = g.Max(v => v.minutes)
            })
            .OrderByDescending(r => r.visits)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                doctors = rows.Count,
                visits = rows.Sum(r => r.visits),
                overallAvg = rows.Count > 0 ? Math.Round(rows.Sum(r => r.avg * r.visits) / rows.Sum(r => r.visits), 1) : 0
            },
            columns = new[] { "پزشک", "ویزیت‌های زمان‌دار", "میانگین (دقیقه)", "کمینه", "بیشینه" },
            rows
        }));
    }

    /// <summary>آمار نوبت‌ها به تفکیک وضعیت</summary>
    private async Task<IActionResult> AppointmentStatsReportAsync(DateTime from, DateTime to)
    {
        var grouped = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => !a.IsDeleted && a.AppointmentDate >= IranDateOf(from) && a.AppointmentDate <= IranDateOf(to))
            .GroupBy(a => a.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();
        var rows = grouped.Select(g => new { status = AppointmentStatusName(g.status), g.count }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { total = rows.Sum(r => r.count) },
            columns = new[] { "وضعیت", "تعداد" },
            rows
        }));
    }

    /// <summary>عدم مراجعه و لغو نوبت</summary>
    private Task<IActionResult> NoShowReportAsync(DateTime from, DateTime to)
        => AppointmentStatusDetailReportAsync(from, to, Domain.Enums.AppointmentStatus.NoShow);

    /// <summary>لغو نوبت‌ها با دلیل لغو</summary>
    private Task<IActionResult> CancellationReportAsync(DateTime from, DateTime to)
        => AppointmentStatusDetailReportAsync(from, to, Domain.Enums.AppointmentStatus.Cancelled);

    private async Task<IActionResult> AppointmentStatusDetailReportAsync(
        DateTime from, DateTime to, Domain.Enums.AppointmentStatus status)
    {
        var source = await _reporting.Set<Domain.Entities.Scheduling.Appointment>().AsNoTracking()
            .Where(a => !a.IsDeleted && a.AppointmentDate >= IranDateOf(from) &&
                        a.AppointmentDate <= IranDateOf(to) && a.Status == status)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new
            {
                date = a.AppointmentDate,
                code = a.AppointmentCode,
                patient = a.Patient != null ? a.Patient.FirstName + " " + a.Patient.LastName : "—",
                doctor = a.Doctor != null && a.Doctor.Employee != null ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName : "—",
                a.Status,
                reason = a.CancellationReason
            })
            .Take(200)
            .ToListAsync();
        var rows = source.Select(a => new
        {
            a.date,
            a.code,
            a.patient,
            a.doctor,
            status = AppointmentStatusName(a.Status),
            a.reason
        }).ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { count = rows.Count },
            columns = new[] { "تاریخ", "کد نوبت", "بیمار", "پزشک", "وضعیت", "دلیل" },
            rows
        }));
    }

    /* ═══════════════════════════════════════════════════════════
       بخش ۴ — داروخانه و هوش تجاری
       ═══════════════════════════════════════════════════════════ */

    /// <summary>موجودی بحرانی داروخانه</summary>
    private async Task<IActionResult> LowStockReportAsync()
    {
        var rows = await _reporting.Set<Domain.Entities.Pharmacy.PharmacyInventory>().AsNoTracking()
            .Where(i => i.Quantity <= i.ReorderLevel && i.Medication != null)
            .OrderBy(i => i.Quantity)
            .Select(i => new
            {
                medication = i.Medication!.BrandName ?? i.Medication.GenericName,
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

    /// <summary>موجودی کامل داروخانه — همهٔ بچ‌ها و وضعیت موجودی</summary>
    private async Task<IActionResult> InventoryReportAsync()
    {
        var rows = await _reporting.Set<Domain.Entities.Pharmacy.PharmacyInventory>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.Medication != null)
            .OrderBy(i => i.Medication!.BrandName ?? i.Medication.GenericName)
            .ThenBy(i => i.ExpiryDate)
            .Select(i => new
            {
                medication = i.Medication!.BrandName ?? i.Medication.GenericName,
                batch = i.BatchNumber,
                onHand = i.Quantity,
                reorder = i.ReorderLevel,
                expiry = i.ExpiryDate
            })
            .Take(1000)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { items = rows.Count, totalOnHand = rows.Sum(r => r.onHand) },
            columns = new[] { "دارو", "بچ", "موجودی", "نقطه سفارش", "انقضا" },
            rows
        }));
    }

    /// <summary>اقلام نزدیک انقضا — پارامتری تا N ماه آینده — گام P1 (توسعه از ۳ ماه ثابت)</summary>
    private async Task<IActionResult> ExpiryReportAsync(int months)
    {
        var threshold = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(months));
        var rows = await _reporting.Set<Domain.Entities.Pharmacy.PharmacyInventory>().AsNoTracking()
            .Where(i => i.ExpiryDate != null && i.ExpiryDate <= threshold && i.Medication != null)
            .OrderBy(i => i.ExpiryDate)
            .Select(i => new
            {
                medication = i.Medication!.BrandName ?? i.Medication.GenericName,
                batch = i.BatchNumber,
                expiry = i.ExpiryDate,
                onHand = i.Quantity
            })
            .Take(200)
            .ToListAsync();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new { expiring = rows.Count, windowMonths = months },
            columns = new[] { "دارو", "بچ", "انقضا", "موجودی" },
            rows
        }));
    }

    /// <summary>پرمصرف‌ترین داروها — از تراکنش‌های تحویل نسخه — گام P1</summary>
    private async Task<IActionResult> TopMedicationsReportAsync(DateTime from, DateTime to, int top)
    {
        var rows = await _reporting.Set<Domain.Entities.Pharmacy.PharmacyTransaction>().AsNoTracking()
            .Where(t => !t.IsDeleted && t.TransactionType == PharmacyTransactionType.Dispense &&
                        (t.TransactionDate != null ? t.TransactionDate >= from && t.TransactionDate <= to
                                                   : t.CreatedAt >= from && t.CreatedAt <= to))
            .Select(t => new
            {
                name = t.Inventory != null && t.Inventory.Medication != null
                    ? (t.Inventory.Medication.BrandName ?? t.Inventory.Medication.GenericName)
                    : "موجودی #" + t.InventoryId,
                qty = t.Quantity,
                at = t.TransactionDate ?? t.CreatedAt
            })
            .ToListAsync();

        var grouped = rows
            .GroupBy(t => t.name)
            .Select(g => new
            {
                medication = g.Key,
                dispenses = g.Count(),
                totalQty = g.Sum(x => x.qty),
                lastUse = g.Max(x => x.at)
            })
            .OrderByDescending(r => r.totalQty)
            .Take(Math.Min(top, 100))
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                medications = grouped.Count,
                dispenses = grouped.Sum(r => r.dispenses),
                totalQty = grouped.Sum(r => r.totalQty)
            },
            columns = new[] { "دارو", "تعداد نسخه", "جمع مصرف", "آخرین مصرف" },
            rows = grouped
        }));
    }

    /// <summary>الگوی مراجعات — ماتریس بازه ساعتی × روز هفته ایرانی — گام P1</summary>
    private async Task<IActionResult> VisitPatternsReportAsync(DateTime from, DateTime to)
    {
        var visits = await _reporting.Set<Domain.Entities.Clinical.Visit>().AsNoTracking()
            .Where(v => !v.IsDeleted && v.VisitDate >= from && v.VisitDate <= to)
            .Select(v => v.VisitDate)
            .ToListAsync();
        var periodDays = Math.Max(1, (IranDateOf(to).DayNumber - IranDateOf(from).DayNumber) + 1);

        // بازه‌های ساعتی کاری درمانگاه
        var buckets = new (int fromHour, int toHour, string label)[]
        {
            (7, 10, "۷ تا ۱۰ (صبح)"),
            (10, 13, "۱۰ تا ۱۳ (نیمروز)"),
            (13, 16, "۱۳ تا ۱۶ (بعدازظهر)"),
            (16, 20, "۱۶ تا ۲۰ (عصر)"),
            (20, 31, "۲۰ به بعد (شب)")
        };
        var dayNames = new[] { "شنبه", "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه" };

        // ایندکس روز هفته ایرانی: شنبه=۰ تا جمعه=۶
        static int PersianDayIndex(DateTime d) => ((int)d.DayOfWeek + 1) % 7;

        var counts = new int[buckets.Length, 7];
        foreach (var v in visits)
        {
            var hour = v.Hour < 7 ? 24 + v.Hour : v.Hour; // شب بعد از نیمه‌شب در بازه شب
            for (var b = 0; b < buckets.Length; b++)
            {
                if (hour >= buckets[b].fromHour && hour < buckets[b].toHour)
                {
                    counts[b, PersianDayIndex(v)]++;
                    break;
                }
            }
        }

        var rows = new List<object>();
        for (var b = 0; b < buckets.Length; b++)
        {
            var dayCounts = Enumerable.Range(0, 7).Select(d => (object)counts[b, d]).ToList();
            var total = Enumerable.Range(0, 7).Sum(d => counts[b, d]);
            var row = new Dictionary<string, object> { ["بازه ساعتی"] = buckets[b].label };
            for (var d = 0; d < 7; d++) row[dayNames[d]] = counts[b, d];
            row["جمع"] = total;
            rows.Add(row);
        }

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                visits = visits.Count,
                busiestDay = visits.Count > 0
                    ? dayNames[visits.GroupBy(PersianDayIndex).OrderByDescending(g => g.Count()).First().Key]
                    : "—",
                avgPerDay = visits.Count > 0 ? Math.Round(visits.Count / (double)periodDays, 1) : 0
            },
            columns = new[] { "بازه ساعتی" }.Concat(dayNames).Append("جمع").ToArray(),
            rows
        }));
    }

    /// <summary>ترکیب خدمات — سهم هر خدمت از درآمد دوره — گام P1</summary>
    private async Task<IActionResult> ServiceMixReportAsync(DateTime from, DateTime to)
    {
        var items = await _reporting.Set<Domain.Entities.Billing.InvoiceItem>().AsNoTracking()
            .Where(i => !i.IsDeleted && i.ServiceId != null && i.Invoice != null && !i.Invoice.IsDeleted &&
                        i.Invoice.Status != InvoiceStatus.Cancelled && i.Invoice.Status != InvoiceStatus.Draft && i.Invoice.Status != InvoiceStatus.Refunded && i.Invoice.InvoiceDate >= from && i.Invoice.InvoiceDate <= to)
            .Select(i => new
            {
                service = i.Service != null ? i.Service.ServiceName : "خدمت #" + i.ServiceId,
                qty = i.Quantity,
                total = i.TotalPrice
            })
            .ToListAsync();

        var revenue = items.Sum(i => i.total);
        var rows = items
            .GroupBy(i => i.service)
            .Select(g => new
            {
                service = g.Key,
                count = g.Count(),
                qty = g.Sum(x => x.qty),
                revenue = g.Sum(x => x.total),
                share = revenue > 0 ? Math.Round(g.Sum(x => x.total) * 100m / revenue, 1).ToString("0.#") + "٪" : "—"
            })
            .OrderByDescending(r => r.revenue)
            .Take(100)
            .ToList();

        return Ok(ApiResponse<object>.Success(new
        {
            available = true,
            summary = new
            {
                services = rows.Count,
                items = items.Count,
                revenue
            },
            columns = new[] { "خدمت", "تعداد ردیف", "جمع کمیت", "درآمد", "سهم از درآمد" },
            rows
        }));
    }

    /* ═══════════════════════════════════════════════════════════
       کمکی‌های فارسی‌سازی برچسب‌ها
       ═══════════════════════════════════════════════════════════ */

    /// <summary>نام فارسی روش پرداخت</summary>
    private static string PaymentMethodName(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقدی",
        PaymentMethod.Card => "کارت‌خوان",
        PaymentMethod.Online => "آنلاین",
        PaymentMethod.Cheque => "چک",
        PaymentMethod.BankTransfer => "انتقال بانکی",
        PaymentMethod.Insurance => "بیمه",
        _ => method.ToString()
    };

    /// <summary>نام فارسی وضعیت نوبت — در جدول و آمار گزارش</summary>
    private static string AppointmentStatusName(Domain.Enums.AppointmentStatus status) => status switch
    {
        Domain.Enums.AppointmentStatus.Booked => "رزروشده",
        Domain.Enums.AppointmentStatus.Confirmed => "تأییدشده",
        Domain.Enums.AppointmentStatus.Arrived => "حاضر",
        Domain.Enums.AppointmentStatus.InProgress => "در حال ویزیت",
        Domain.Enums.AppointmentStatus.Completed => "تکمیل‌شده",
        Domain.Enums.AppointmentStatus.Cancelled => "لغوشده",
        Domain.Enums.AppointmentStatus.NoShow => "عدم مراجعه",
        _ => status.ToString()
    };

    /// <summary>نام فارسی دسته هزینه</summary>
    private static string ExpenseCategoryName(ExpenseCategoryType category) => category switch
    {
        ExpenseCategoryType.Purchase => "خرید",
        ExpenseCategoryType.Administrative => "اداری",
        ExpenseCategoryType.Utility => "قبوض",
        ExpenseCategoryType.Salary => "حقوق",
        ExpenseCategoryType.Rent => "اجاره",
        ExpenseCategoryType.Maintenance => "تعمیرات",
        _ => "سایر"
    };
}
