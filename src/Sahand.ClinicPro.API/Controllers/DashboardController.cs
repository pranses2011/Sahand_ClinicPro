using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داشبورد — آمار روزانه برای صفحه آغازین کلاینت دسکتاپ
/// مسیرها: /api/v1/dashboard
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ApiBaseController
{
    // ═══ چرخهٔ ۷۲ (بازطراحی داشبورد) — منطقهٔ زمانی ایران ═══
    // «امروزِ» داشبورد قبلاً با DateTime.UtcNow محاسبه می‌شد: در ایران
    // (UTC+3:30 بدون ساعت تابستانی از ۱۴۰۱) یعنی روزِ داشبورد ۳:۵۰ بامداد
    // می‌پرید و آمارِ «امروز» نیمه‌شب تا ۳:۳۰ را از دست می‌داد — همان خانوادهٔ
    // باگ «همه‌چیز صفر» گزارش‌ها که در همین چرخه ریشه‌کن شد. مرزهای روز
    // اکنون به وقت ایران است (هم‌الگوی ReportsController چرخهٔ ۷۲).
    /// <summary>اختلاف زمانی ایران از UTC — ۳:۳۰ بدون ساعت تابستانی</summary>
    private static readonly TimeSpan IranOffset = TimeSpan.FromHours(3.5);

    /// <summary>تاریخ «امروز» به وقت ایران</summary>
    private static DateOnly IranToday() => DateOnly.FromDateTime(DateTime.UtcNow + IranOffset);

    /// <summary>شروع روز ایران (۰۰:۰۰ تهران) به UTC — برای ستون‌های DateTime</summary>
    private static DateTime IranDayStartUtc(DateOnly iranDate) =>
        iranDate.ToDateTime(TimeOnly.MinValue) - IranOffset;

    /// <summary>پایان روز ایران (۲۳:۵۹:۵۹ تهران) به UTC</summary>
    private static DateTime IranDayEndUtc(DateOnly iranDate) =>
        iranDate.ToDateTime(TimeOnly.MinValue).AddDays(1) - IranOffset - TimeSpan.FromTicks(1);

    /// <summary>برچسب شمسی یک روز ایران</summary>
    private static string ShamsiLabel(DateOnly iranDate)
        => Application.Common.Helpers.PersianDateHelper.ToShamsi(iranDate.ToDateTime(TimeOnly.MinValue));

    private readonly IUnitOfWork _unitOfWork;
    private readonly Application.Interfaces.ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق واحد کار و کاربر جاری</summary>
    public DashboardController(IUnitOfWork unitOfWork,
        Application.Interfaces.ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    /// <summary>
    /// آمار خلاصه امروز — بیماران ثبت‌شده، نوبت‌ها، ویزیت‌ها و درآمد
    /// ═══ چرخهٔ ۷۲ — «امروز» به وقت ایران ═══ ستون‌های DateOnly با تاریخ
    /// ایرانِ امروز مقایسه می‌شوند و ستون‌های DateTime (UTC) با مرز کامل
    /// روز ایران (۰۰:۰۰ تا ۲۳:۵۹ تهران) — پیش‌تر نیمه‌شبِ UTC مرز بود و
    /// آمار بین ۰۰:۰۰ تا ۳:۳۰ بامداد تهران گم می‌شد.
    /// </summary>
    [HttpGet("stats")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetTodayStats(CancellationToken ct)
    {
        var today = IranToday();                     // تاریخ ایرانِ امروز
        var dayStart = IranDayStartUtc(today);       // ۰۰:۰۰ تهران به UTC
        var dayEnd = IranDayEndUtc(today);           // ۲۳:۵۹:۵۹ تهران به UTC

        var newPatients = await _unitOfWork.Repository<Patient>()
            .QueryableNoTracking
            .CountAsync(p => p.RegistrationDate != null
                          && p.RegistrationDate.Value >= dayStart
                          && p.RegistrationDate.Value <= dayEnd, ct);

        var todayAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today, ct);

        var activeAppointments = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .CountAsync(a => a.AppointmentDate == today
                          && (a.Status == AppointmentStatus.Booked
                              || a.Status == AppointmentStatus.Confirmed
                              || a.Status == AppointmentStatus.Arrived), ct);

        var todayRevenue = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InvoiceDate >= dayStart
                     && i.InvoiceDate <= dayEnd
                     && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.PaidAmount, ct) ?? 0m;

        var todayVisits = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .CountAsync(v => v.VisitDate >= dayStart && v.VisitDate <= dayEnd, ct);

        var stats = new
        {
            newPatients,
            todayAppointments,
            activeAppointments,
            todayVisits,
            todayRevenue,
            todayDateShamsi = ShamsiLabel(today),
            serverTime = DateTime.UtcNow
        };

        return Ok(Application.Common.Models.ApiResponse<object>.Success(stats,
            "آمار امروز دریافت شد"));
    }

    /// <summary>
    /// نمودار درآمد روزانه — مشخصات پرامپت UI بخش ۱ (فرم ۲)
    /// خروجی: برچسب شمسی + مبلغ پرداخت‌شده برای N روز اخیر
    /// ═══ چرخهٔ ۷۲ — روزها به مرز ایران‌اند (همان رفعِ «تا-تاریخِ نیمه‌شبِ
    /// UTC» گزارش‌ها): هر روز از ۰۰:۰۰ تا ۲۳:۵۹ تهران حساب می‌شود نه از
    /// ۳:۳۰ بامداد؛ در غیر این صورت روزِ تقویمیِ کاربر و روزِ نمودار
    /// اختلاف داشت و آخرین ستون همیشه ناقص/صفر می‌شد.
    /// </summary>
    [HttpGet("revenue-chart")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var today = IranToday();
        var from = today.AddDays(-(days - 1));

        var fromDateTime = IranDayStartUtc(from);
        var toDateTime = IranDayEndUtc(today);

        var rows = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InvoiceDate >= fromDateTime
                     && i.InvoiceDate <= toDateTime
                     && i.Status != InvoiceStatus.Cancelled)
            .Select(i => new { i.InvoiceDate, i.PaidAmount })
            .ToListAsync(ct);

        var chart = Enumerable.Range(0, days)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                var dayStart = IranDayStartUtc(day);
                var dayEnd = IranDayEndUtc(day);
                return new
                {
                    label = ShamsiLabel(day),
                    amount = rows
                        .Where(r => r.InvoiceDate >= dayStart && r.InvoiceDate <= dayEnd)
                        .Sum(r => r.PaidAmount),
                };
            })
            .ToList();

        return Ok(Application.Common.Models.ApiResponse<object>.Success(chart,
            "داده نمودار درآمد دریافت شد"));
    }

    /// <summary>
    /// نمودار ویزیت روزانه — چرخهٔ ۷۲ (بازطراحی داشبورد با کارت‌های آماری و
    /// نموداری): تعداد ویزیت‌های هر روز برای N روز اخیر به مرز روز ایران؛
    /// منبع کارت دوم «ویزیت‌شده» و نمودار ستونی داشبورد جدید.
    /// </summary>
    /// <param name="days">طول بازه — ۱ تا ۹۰ روز</param>
    /// <param name="ct">توکن لغو هم‌زمان</param>
    [HttpGet("visits-chart")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetVisitsChart([FromQuery] int days = 7,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var today = IranToday();
        var from = today.AddDays(-(days - 1));

        var rows = await _unitOfWork.Repository<Domain.Entities.Clinical.Visit>()
            .QueryableNoTracking
            .Where(v => v.VisitDate >= IranDayStartUtc(from)
                     && v.VisitDate <= IranDayEndUtc(today))
            .Select(v => new { v.VisitDate })
            .ToListAsync(ct);

        var chart = Enumerable.Range(0, days)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                var dayStart = IranDayStartUtc(day);
                var dayEnd = IranDayEndUtc(day);
                return new
                {
                    label = ShamsiLabel(day),
                    count = rows.Count(r => r.VisitDate >= dayStart && r.VisitDate <= dayEnd),
                };
            })
            .ToList();

        return Ok(Application.Common.Models.ApiResponse<object>.Success(chart,
            "داده نمودار ویزیت دریافت شد"));
    }

    /// <summary>
    /// ترکیب وضعیت نوبت‌های امروز — چرخهٔ ۷۲ (نمودار دونات داشبورد جدید):
    /// توزیع نوبت‌های امروز بر اساس وضعیت (رزرو/تایید/حاضر/در حال ویزیت/
    /// تمام‌شده/لغو/عدم حضور) — مرکز دونات جمع امروز را نشان می‌دهد.
    /// </summary>
    [HttpGet("status-mix")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetStatusMix(CancellationToken ct)
    {
        var today = IranToday();

        var rows = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .Where(a => a.AppointmentDate == today)
            .Select(a => new { a.Status })
            .ToListAsync(ct);

        // نام فارسی وضعیت‌ها — همان واژگان نوبت‌دهی برنامه
        string Fa(AppointmentStatus s) => s switch
        {
            AppointmentStatus.Booked => "رزرو شده",
            AppointmentStatus.Confirmed => "تاییدشده",
            AppointmentStatus.Arrived => "حاضر در مطب",
            AppointmentStatus.InProgress => "در حال ویزیت",
            AppointmentStatus.Completed => "تمام‌شده",
            AppointmentStatus.Cancelled => "لغو شده",
            AppointmentStatus.NoShow => "عدم حضور",
            _ => s.ToString()
        };

        var mix = rows
            .GroupBy(r => r.Status)
            .Select(g => new
            {
                status = g.Key.ToString(),
                label = Fa(g.Key),
                count = g.Count()
            })
            .OrderByDescending(x => x.count)
            .ToList();

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            total = rows.Count,
            mix
        }, "ترکیب وضعیت نوبت‌های امروز دریافت شد"));
    }

    /// <summary>
    /// نوبت‌های امروز — مشخصات پرامپت UI بخش ۱ (فرم ۲: نوبت‌های پیش‌رو)
    /// </summary>
    [HttpGet("appointments-today")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetAppointmentsToday([FromQuery] int limit = 8,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var today = IranToday(); // چرخهٔ ۷۲ — مرز روز ایران

        var list = await _unitOfWork.Repository<Appointment>()
            .QueryableNoTracking
            .Where(a => a.AppointmentDate == today
                     && a.Status != AppointmentStatus.Cancelled
                     && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.AppointmentTime)
            .Take(limit)
            .Select(a => new
            {
                id = a.Id,
                queueNumber = a.QueueNumber,
                time = a.AppointmentTime.ToString("HH:mm"),
                patientName = a.Patient != null
                    ? a.Patient.FirstName + " " + a.Patient.LastName
                    : "ناشناس",
                doctorName = a.Doctor != null && a.Doctor.Employee != null
                    ? a.Doctor.Employee.FirstName + " " + a.Doctor.Employee.LastName
                    : "پزشک",
                status = a.Status.ToString(),
                statusText = a.Status == AppointmentStatus.Completed ? "تمام‌شده"
                    : a.Status == AppointmentStatus.InProgress ? "در حال ویزیت"
                    : a.Status == AppointmentStatus.Arrived ? "حاضر"
                    : a.Status == AppointmentStatus.Confirmed ? "تاییدشده"
                    : "رزرو شده",
            })
            .ToListAsync(ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(list,
            "نوبت‌های امروز دریافت شد"));
    }

    /// <summary>
    /// فعالیت‌های اخیر — مشخصات پرامپت UI بخش ۱ (فرم ۲: جدول فعالیت‌ها از AuditLog)
    /// </summary>
    [HttpGet("recent-activities")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var list = await _unitOfWork.Repository<Domain.Entities.Infrastructure.AuditLog>()
            .QueryableNoTracking
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .Select(l => new
            {
                time = l.Timestamp,
                timeShamsi = Application.Common.Helpers.PersianDateHelper
                    .ToShamsiWithTime(l.Timestamp),
                user = l.Username ?? "سیستم",
                action = l.ActionType.ToString(),
                entity = l.EntityName ?? string.Empty,
                entityId = l.EntityId,
                module = l.Module ?? string.Empty,
            })
            .ToListAsync(ct);

        // ═══ بازطراحی چرخهٔ ۷۰ (گزارش مالک) — فارسی‌سازی ردیف‌های قدیمی ═══
        // «ستون موجودیت همه انگلیسی است»: ردیف‌های پیش از این نسخه با نام
        // انگلیسی در دیتابیس نشسته‌اند؛ در لحظهٔ خواندن از همان کاتالوگ معنایی
        // ترجمه می‌شوند. ردیف‌های تازه از پیش فارسی‌اند و «شفاف» پاس داده می‌شوند
        // (PersianizeEntity متن فارسی را دست‌نخورده برمی‌گرداند).
        // ═══ نام کاربر ردیف‌های قدیمی ═══ پیش از این نسخه، میدلور «کد/نام
        // کاربری» را در Username می‌نوشت (گزارش مالک: «به‌جای نام کامل، کد ۱»).
        // ردیف‌هایی که Username آن‌ها خالصِ عددی است از همین محل با یک پرس‌وجوی
        // کوچک به «نام کامل پرسنل» تبدیل می‌شوند (User → Employee: نام+نام‌خانوادگی)
        // تا کارت فعالیت‌های اخیر نیز همان صفحهٔ لاگ عملیات را نشان دهد.
        var legacyUserIds = list
            .Where(l => l.user is not null && l.user.All(char.IsDigit) && l.user.Length > 0)
            .Select(l => long.TryParse(l.user, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        var nameById = new Dictionary<long, string>();
        if (legacyUserIds.Count > 0)
        {
            var displayNames = await _unitOfWork.Repository<Domain.Entities.Security.User>()
                .QueryableNoTracking
                .Where(u => legacyUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Full = u.EmployeeId != null
                        ? (u.Employee != null
                            ? (u.Employee.FirstName + " " + u.Employee.LastName).Trim()
                            : u.Username)
                        : u.Username
                })
                .ToListAsync(ct);
            nameById = displayNames
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First().Full);
        }

        // خروجی نهایی — نام کاربر و موجودیت فارسی (anonymous جدید؛ عناصر
        // «فهرست خام» فقط‌خواندنی‌اند و در این مرحله نگاشت می‌شوند)
        var result = list.Select(l => new
        {
            l.time,
            l.timeShamsi,
            user = l.user is not null && l.user.All(char.IsDigit)
                    && long.TryParse(l.user, out var uid)
                    && nameById.TryGetValue(uid, out var full) && !string.IsNullOrWhiteSpace(full)
                ? full
                : l.user,
            l.action,
            entity = Application.Common.AuditSemanticCatalog.PersianizeEntity(l.entity),
            l.entityId,
            module = Application.Common.AuditSemanticCatalog.PersianizeModule(l.module),
        }).ToList();

        return Ok(Application.Common.Models.ApiResponse<object>.Success(result,
            "فعالیت‌های اخیر دریافت شد"));
    }

    /// <summary>
    /// هشدارهای داشبورد — موجودی کم، نزدیک انقضا (پلکانی ۹۰/۶۰/۳۰/۷)، آزمایش در انتظار
    /// چرخهٔ ۶۹ (فاز ۳ / F-08): انقضا از «یک عدد ۳۰ روزه» به چهار پلهٔ رنگی ارتقا یافت —
    /// هر پله سیاست عملیاتی متفاوتی دارد: ≤۷ روز (قرمز — مصرف فوری/مرجوعی) ·
    /// ≤۳۰ روز (نارنجی — اولویت تجویز از این بچ‌ها) · ≤۶۰ روز (کهربایی —
    /// برنامه‌ریزی سفارش مجدد) · ≤۹۰ روز (زرد — پایش).
    /// </summary>
    [HttpGet("alerts")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        var today = IranToday(); // چرخهٔ ۷۲ — مرز روز ایران

        // موجودی کم — اقلام فعال زیر حداقل موجودی
        var lowStock = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .CountAsync(i => i.IsActive
                           && i.MinStock > 0
                           && i.Quantity <= i.MinStock, ct);

        // پله‌های انقضا — فقط بچ‌های هنوز معتبر (منقضی‌شده سیاست مصرف ندارد، مرجوعی/اسقاط است)
        async Task<int> ExpiryCount(int days) => await _unitOfWork
            .Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .CountAsync(i => i.IsActive
                     && i.ExpiryDate != null
                     && i.ExpiryDate >= today
                     && i.ExpiryDate <= today.AddDays(days), ct);

        var expiring7 = await ExpiryCount(7);
        var expiring30 = await ExpiryCount(30);
        var expiring60 = await ExpiryCount(60);
        var expiring90 = await ExpiryCount(90);

        var pendingLabOrders = await _unitOfWork.Repository<Domain.Entities.Paraclinical.LabOrder>()
            .QueryableNoTracking
            .CountAsync(o => o.Status == Domain.Enums.LabOrderStatus.Ordered, ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            lowStock,
            // پله‌های انقضای پلکانی — چرخهٔ ۶۹ (F-08)
            expiring7,
            expiring30,
            expiring60,
            expiring90,
            // نگه‌داشته شد برای سازگاری کلاینت‌های قبلی = مجموع پلهٔ ۳۰ روزه
            expiringSoon = expiring30,
            pendingLabOrders,
        }, "هشدارهای داشبورد دریافت شد"));
    }

    /// <summary>
    /// اقلام هشدار انبار — جزئیات مرکز هشدار داشبورد (چرخهٔ ۶۹ / فاز ۳ F-08)
    /// دو فهرست برمی‌گرداند: بچ‌های در حال انقضا (مرتب از نزدیک‌ترین) با پلهٔ محاسبه‌شده
    /// و اقلام زیر حداقل موجودی (مرتب از کم‌ترین) — هر کدام حداکثر ۲۰ ردیف.
    /// </summary>
    [HttpGet("alerts/inventory")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetInventoryAlerts(CancellationToken ct)
    {
        var today = IranToday(); // چرخهٔ ۷۲ — مرز روز ایران

        // بچ‌های در حال انقضا — نام دارو + بچ + روزهای مانده + موجودی
        // (روزهای مانده در حافظه محاسبه می‌شود نه با DateDiffDay — سازگار با
        // هر provider تست؛ فقط ۲۰ ردیف هست و هزینه ناچیز است)
        var expiringRaw = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .Where(i => i.IsActive && i.ExpiryDate != null
                     && i.ExpiryDate >= today
                     && i.ExpiryDate <= today.AddDays(90))
            .OrderBy(i => i.ExpiryDate)
            .Take(20)
            .Select(i => new
            {
                medicationName = i.Medication != null
                    ? (i.Medication.GenericNameFa ?? i.Medication.GenericName)
                    : "",
                batchNumber = i.BatchNumber,
                expiryDate = i.ExpiryDate,
                quantity = i.Quantity
            })
            .ToListAsync(ct);

        var expiring = expiringRaw
            .Select(i => new
            {
                i.medicationName,
                i.batchNumber,
                i.expiryDate,
                daysLeft = i.expiryDate!.Value.DayNumber - today.DayNumber,
                i.quantity
            })
            .ToList();

        // اقلام زیر حداقل موجودی — مرتب از بدترین وضعیت
        var lowStockItems = await _unitOfWork.Repository<Domain.Entities.Pharmacy.PharmacyInventory>()
            .QueryableNoTracking
            .Where(i => i.IsActive && i.MinStock > 0 && i.Quantity <= i.MinStock)
            .OrderBy(i => i.Quantity / i.MinStock)
            .Take(20)
            .Select(i => new
            {
                medicationName = i.Medication != null
                    ? (i.Medication.GenericNameFa ?? i.Medication.GenericName)
                    : "",
                batchNumber = i.BatchNumber,
                quantity = i.Quantity,
                minStock = i.MinStock
            })
            .ToListAsync(ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            expiring,
            lowStockItems
        }, "اقلام هشدار انبار دریافت شد"));
    }

    /* ═══════════ داشبورد شخصی‌سازی‌شده — چرخه ۱۳ (درخواست ۴-۲) ═══════════ */

    /// <summary>کلید تنظیم چیدمان داشبورد هر کاربر</summary>
    private string LayoutKey => $"dashboard-layout:{_currentUser.UserId ?? 0}";

    /// <summary>
    /// چیدمان شخصی داشبورد کاربر جاری — JSON ترتیب کارت‌ها و نمایش بخش‌ها
    /// </summary>
    [HttpGet("layout")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> GetLayout(CancellationToken ct)
    {
        var setting = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(x => x.Key == LayoutKey && !x.IsDeleted, ct);

        return Ok(Application.Common.Models.ApiResponse<object>.Success(new
        {
            layout = setting?.Value
        }));
    }

    /// <summary>
    /// ذخیره چیدمان شخصی داشبورد — هر کاربر چیدمان مستقل خودش را دارد
    /// </summary>
    /// <param name="request">JSON چیدمان — ترتیب کارت‌ها و نمایش بخش‌ها</param>
    /// <param name="ct">توکن لغو هم‌زمان (چرخه بیست‌وسوم رفع CS1573)</param>
    [HttpPut("layout")]
    [HasPermission("dashboard.view")]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Layout) || request.Layout.Length > 4000)
            return BadRequest(Application.Common.Models.ApiResponse<object>.Failure(
                "چیدمان نامعتبر است (حداکثر ۴۰۰۰ نویسه)"));

        // ═══ چرخهٔ ۷۶ — ریشه‌کن DbUpdateConcurrencyException روی PUT layout ═══
        // چیدمان داشبورد «آخرین‌نوشتنی» است: هر درخواست JSON کامل را با خودش
        // دارد. الگوی قبلی «بارگذاری با ChangeTracker + ذخیره» روی نشانگر
        // همزمانی RowVersion اجرا می‌شد؛ کلاینت هنگام جابه‌جایی/مخفی‌سازی
        // پیاپی کارت‌ها چند PUT پشت‌سرهم می‌فرستاد و درخواست‌های هم‌زمان
        // روی همان RowVersion برخورد می‌کردند: «expected 1, affected 0»
        // و خطای ۵۰۰ در لاگ سرور. راه‌حل: UPDATE اتمیِ مستقیم (بدون بارگذاری
        // موجودیت و بدون توکن همزمانی در WHERE) — آخرین درخواست همیشه
        // برنده است و برخوردی باقی نمی‌ماند.
        var updated = await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
            .QueryableNoTracking
            .Where(x => x.Key == LayoutKey && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Value, request.Layout)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                .SetProperty(x => x.UpdatedBy, _currentUser.UserId), ct);

        // ردیفی نبود → اولین ذخیرهٔ چیدمان این کاربر؛ درج رکورد جدید
        if (updated == 0)
        {
            var setting = new Domain.Entities.Infrastructure.SystemSetting
            {
                Category = "DashboardLayout",
                Key = LayoutKey,
                Value = request.Layout,
                DataType = Domain.Enums.SettingDataType.Json,
                Description = $"چیدمان داشبورد شخصی کاربر {_currentUser.Username}"
            };
            await _unitOfWork.Repository<Domain.Entities.Infrastructure.SystemSetting>()
                .AddAsync(setting, ct);
            await _unitOfWork.SaveEntitiesAsync(ct);
        }

        return Ok(Application.Common.Models.ApiResponse<object>.SuccessMessage("چیدمان داشبورد ذخیره شد"));
    }
}

/// <summary>درخواست ذخیره چیدمان داشبورد</summary>
public record SaveLayoutRequest(string Layout);
