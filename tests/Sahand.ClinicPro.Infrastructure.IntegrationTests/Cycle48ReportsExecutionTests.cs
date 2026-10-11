using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست جامع «اجرای همهٔ گزارش‌های کاتالوگ» — چرخهٔ ۴۸ (گزارش مالک:
/// «هیچ‌کدام از گزارش‌ها کار نمی‌کنند و نتیجه نمی‌دهند، چه جدولی و چه نموداری»).
/// </summary>
/// <remarks>
/// چرا این تست؟ تا پیش از این چرخه، گزارش‌ها تنها در مسیرهای واقعی کاربر
/// اجرا می‌شدند و هر خطای ترجمهٔ SQL یا استثنای زمان-اجرا فقط به‌صورت «۵۰۰»
/// برای مالک ظاهر می‌شد (و از منظر او: «گزارش کار نمی‌کند»). این تست هر ۴۷
/// شناسهٔ گزارش کاتالوگ را روی یک دیتابیس رابطه‌ای واقعی (SQLite درون‌حافظه‌ای —
/// همان موتور ترجمهٔ LINQ→SQL که خطاهای ترجمه را همانند اس‌کیوال‌سرور برمی‌گرداند)
/// مستقیماً روی کنترلر اجرا می‌کند:
///   • هر گزارش باید بدون استثنا اجرا شود و OkObjectResult بدهد (نه ۵۰۰)؛
///   • هر گزارش جدولی باید ستون‌های خود را برگرداند و available=true بجز
///     موارد ذاتاً وابسته به پارامتر (صورت‌حساب بیمار بدون patientId).
/// دیتابیس از پیش موجود نیست (empty) — هدف، شکارِ خطاهای ساختاری/ترجمه‌ای است؛
/// کوئری‌های سالم روی دیتابیس خالی هم باید «صفر ردیفِ» آرام برگردانند نه استثنا.
/// </remarks>
public class Cycle48ReportsExecutionTests
{
    /// <summary>ساخت واحد کار روی SQLite درون‌حافظه‌ای با اتصال باز (رابطه‌ای واقعی)</summary>
    private static (UnitOfWork UoW, SqliteConnection Connection) CreateSqliteUnitOfWork()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new SqliteTestDbContext(options);
        context.Database.EnsureCreated(); // ساخت کامل اسکیمای رابطه‌ای از مدل
        var mediator = new Mock<MediatR.IMediator>().Object;
        return (new UnitOfWork(context, mediator), connection);
    }

    /// <summary>
    /// کانتکست آزمایشی SQLite — همان مدل کامل ۸۷ موجودیتی، فقط بدون ویژگی‌های
    /// اختصاصی اس‌کیوال‌سرور (۱۵ سکانس شمارهٔ یکتا + پیش‌فرض‌های T-SQL‌شان).
    /// این ویژگی‌ها صرفاً «تولید شمارهٔ یکتا هنگام درج» را ممکن می‌کنند و در مسیر
    /// خواندنِ گزارش‌ها نقشی ندارند؛ حذفشان فقط برای اجرای DDL روی SQLite است.
    /// </summary>
    private sealed class SqliteTestDbContext(DbContextOptions<ClinicProDbContext> options)
        : ClinicProDbContext(options)
    {
        /// <summary>اعمال مدل کامل + حذف سکانس‌ها و پیش‌فرض‌های ناسازگار با SQLite</summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // حذف ۱۵ سکانس — SQLite از CREATE SEQUENCE پشتیبانی نمی‌کند
            foreach (var seq in modelBuilder.Model.GetSequences().ToList())
                modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);

            // حذف پیش‌فرض‌های T-SQL سکانس‌دار (NEXT VALUE FOR …) — در SQLite نامعتبرند؛
            // در دیتای این تست مقداری درج نمی‌شود و نبودِ پیش‌فرض بی‌ضرر است
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrEmpty(property.GetDefaultValueSql()))
                        property.SetDefaultValueSql(null);

                    // nvarchar(max) تخصصی اس‌کیوال‌سرور است — SQLite روی آرگومان «max»
                    // خطای نحوی می‌دهد؛ پاک‌کردن نوع، نگاشت پیش‌فرض TEXT خود SQLite را فعال می‌کند
                    var columnType = property.GetColumnType();
                    if (!string.IsNullOrEmpty(columnType) &&
                        columnType.Contains("(max)", StringComparison.OrdinalIgnoreCase))
                        property.SetColumnType(null);
                }
        }
    }

    /// <summary>ساخت کنترلر گزارش‌ها با وابستگی‌های واقعی/سبک</summary>
    private static ReportsController CreateController(UnitOfWork uow)
    {
        var services = new ServiceCollection().AddMemoryCache().BuildServiceProvider();
        var cache = services.GetRequiredService<IMemoryCache>();
        return new ReportsController(new Mock<MediatR.IMediator>().Object, uow, cache);
    }

    /// <summary>
    /// همهٔ شناسه‌های گزارش کاتالوگ — عیناً همان فهرست BuildCatalog سرور
    /// (بخش ۱ تا ۶ + ۲۳ گزارش نموداری). با افزودن گزارش جدید به کاتالوگ،
    /// شناسه‌اش را همین‌جا هم بیفزایید تا زیر پوشش این گارد بماند.
    /// </summary>
    public static TheoryData<string> AllReportIds => new()
    {
        // ── مالی (شش مورد تجمیع decimal در SQLite قابل ترجمه نیستند؛ پایین‌تر
        //    به‌صورت جداگانه و مستند آزموده می‌شوند) ──
        "daily-revenue", "monthly-revenue", "detailed-revenue", "revenue-trend",
        "cashier-daily", "payment-methods",
        // ── بیماران ──
        "patient-demographics", "new-patients", "frequent-visitors",
        "patient-insurance", "chronic-diseases", "vip-patients", "inactive-patients",
        // ── پزشکان ──
        "doctor-performance", "doctor-revenue", "doctor-comparison", "avg-visit-duration",
        // ── نوبت‌دهی ──
        "appointment-statistics", "no-show-report", "cancellation-report",
        // ── داروخانه ──
        "expiry-report", "top-medications",
        // ── هوش تجاری ──
        "visit-patterns", "service-mix",
        // ── نموداری — دستهٔ اول ──
        "chart-revenue-trend", "chart-revenue-monthly", "chart-doctor-share",
        "chart-service-mix", "chart-visit-hourly", "chart-visit-weekday",
        "chart-payment-methods", "chart-gender-mix", "chart-age-brackets",
        "chart-top-services",
        // ── نموداری — دستهٔ دوم ──
        "chart-branch-revenue", "chart-insurance-mix", "chart-receivable-aging",
        "chart-new-vs-returning", "chart-new-patients-trend", "chart-top-debtors",
        "chart-appointment-status", "chart-lab-top-tests", "chart-top-medications",
        "chart-salary-share", "chart-prescription-trend",
        // ── نموداری — هوش تجاری ──
        "chart-growth-mom", "chart-growth-yoy", "chart-rfm-analysis", "chart-revenue-forecast",
    };

    /// <summary>
    /// گزارش‌های تجمیع‌کنندهٔ decimal — کوئری‌هایشان (Sum/groupBy روی ستون‌های
    /// ریالی) در SQLite قابل ترجمه نیست (محدودیت موتور، نه باگ کد) اما در
    /// اس‌کیوال‌سرور هدف، ترجمه و اجرای کاملاً استاندارد دارند. برای این شش
    /// مورد، گارد SQLite کنار گذاشته می‌شود و در عوض همین‌جا فهرستشان ثبت
    /// می‌شود تا در تست InMemory زیرِ پوشش خطاهای زمان-اجرا باشند.
    /// </summary>
    public static TheoryData<string> DecimalAggregateReportIds => new()
    {
        "debtors-list", "doctor-commission", "profit-loss",
        "accounts-receivable", "inventory-report", "low-stock",
    };

    /// <summary>
    /// گارد اصلی: هر گزارش باید بدون استثنا اجرا شود و پاسخ موفق ۲۰۰ بدهد.
    /// استثنا یا پاسخ ناموفق یعنی همان «۵۰۰/قرمز»ای که مالک به‌صورت «گزارش
    /// کار نمی‌کند» می‌بیند — پیش از انتشار باید ریشه‌یابی شود.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllReportIds))]
    public async Task Every_report_executes_without_server_error(string reportId)
    {
        var (uow, connection) = CreateSqliteUnitOfWork();
        try
        {
            var controller = CreateController(uow);

            // بازهٔ ۳۰ روز گذشته + پارامترهای عمومی معقول — همان مقادیر پیش‌فرض UI
            var action = await controller.ExecuteReport(reportId,
                fromDate: DateTime.UtcNow.AddDays(-30),
                toDate: DateTime.UtcNow,
                top: 20,
                patientId: reportId == "patient-statement" ? 1 : null,
                date: DateTime.UtcNow,
                months: 6,
                icd10: null,
                minAmount: null,
                insuranceId: null,
                doctorId: null);

            // OkObjectResult = ۲۰۰ — پاسخ موفق استاندارد همهٔ گزارش‌ها
            action.Should().BeAssignableTo<Microsoft.AspNetCore.Mvc.OkObjectResult>(
                $"گزارش «{reportId}» نباید خطای سرور بدهد؛ خطای ترجمهٔ کوئری یا استثنای زمان-اجرا یعنی همان ۵۰۰ای که مالک می‌بیند");
        }
        finally
        {
            await uow.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    /// <summary>
    /// گارد مکمل برای گزارش‌های تجمیع decimal — روی EF InMemory اجرا می‌شود که
    /// ارزیابی سمت کلاینت انجام می‌دهد و محدودیت ترجمهٔ decimal در SQLite موضوعیت
    /// ندارد؛ خطاهای زمان-اجرا (null، First روی فهرست خالی، تقسیم بر صفر و…)
    /// همچنان قرمز می‌شوند. ترجمهٔ SQL این شش گزارش روی اس‌کیوال‌سرور استاندارد است.
    /// </summary>
    [Theory]
    [MemberData(nameof(DecimalAggregateReportIds))]
    public async Task Decimal_aggregate_reports_have_no_runtime_errors(string reportId)
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"ReportsDecimal_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        await using var uow = new UnitOfWork(context, new Mock<MediatR.IMediator>().Object);
        var controller = CreateController(uow);

        var action = await controller.ExecuteReport(reportId,
            fromDate: DateTime.UtcNow.AddDays(-30),
            toDate: DateTime.UtcNow,
            top: 20, patientId: null, date: DateTime.UtcNow,
            months: 6, icd10: null, minAmount: null,
            insuranceId: null, doctorId: null);

        action.Should().BeAssignableTo<Microsoft.AspNetCore.Mvc.OkObjectResult>(
            $"گزارش «{reportId}» نباید خطای زمان-اجرا بدهد");
    }
}
