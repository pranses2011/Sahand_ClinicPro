using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Personnel;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

/// <summary>
/// کارخانه پایه تست API با دیتابیس درون‌حافظه‌ای — گام V12
/// </summary>
/// <remarks>
/// نکته مهم: کش مدل EF بر اساس نوع Context مشترک است؛ بنابراین همه
/// کارخانه‌های تست باید InMemory باشند تا مدل با مولد MRN ساخته شود.
/// </remarks>
public class InMemoryApiFactory : WebApplicationFactory<Program>
{
    /// <summary>نام دیتابیس درون‌حافظه‌ای — در کلاس‌های مشتق قابل‌بازنویسی</summary>
    protected virtual string DatabaseName => "InMemoryApiTestsDb";

    /// <summary>
    /// ریشهٔ مشترک فروشگاه InMemory — چرخهٔ ۷۰ (تست سرتاسری گزارش‌ها).
    /// بدون ریشهٔ مشترک، کانتکست گزارش و کانتکست اصلی دو InternalServiceProvider
    /// جدا می‌گیرند و دو فروشگاهِ هم‌نام اما جدا می‌سازند؛ نتیجه: گزارش‌ها
    /// همیشه ردیف تهی/جمع صفر برمی‌گردانند با اینکه دادهٔ اصلی نوشته شده —
    /// بازتولیدِ همان «همه چیز صفر» مالک در محیط تست.
    /// </summary>
    private readonly Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot _root = new();

    /// <summary>پیکربندی میزبان تست — دیتابیس درون‌حافظه‌ای</summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // حذف پیکربندی SQL Server موجود و جایگزینی با InMemory
            // نکته: IDbContextOptionsConfiguration در EF8 داخلی است — با اسکن بازتابی حذف می‌شود
            // ═══ چرخهٔ ۶۴ — کانتکست گزارش: حذف کامل ثبت‌های «استخر» + گزینه‌ها ═══
            // ثبت استخر (AddDbContextPool) در DI چند سرویس می‌گذارد (خودِ استخرِ
            // Singleton + اجارهٔ Scoped + گزینه‌های Singleton)؛ برای جایگزینی با
            // کانتکست InMemory ساده باید «همهٔ» آنها کنار برود، وگرنه اعتبارسنجی
            // DI به «خوردنِ سرویس Scoped توسط Singleton» می‌افتد.
            var stale = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ClinicProDbContext>) ||
                d.ServiceType == typeof(DbContextOptions<Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext>) ||
                // چرخهٔ ۷۰ — خودِ «نوع کانتکست گزارش» هم کنار می‌رود: ثبتِ اصلی
                // AddDbContextPool است و کارخانهٔ TContextِ استخری می‌ماند که با
                // IScopedDbContextLease (حذف‌شده) حل می‌شود → ۵۰۰ در گزارش‌ها؛
                // پس از حذف، AddDbContext سادهٔ پایین بی‌رقیب نشسته و InMemory
                // از همان فروشگاه مشترک می‌خواند.
                d.ServiceType == typeof(Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext) ||
                d.ServiceType.IsGenericType && (
                    d.ServiceType.GetGenericArguments().FirstOrDefault() == typeof(Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext) ||
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))).ToList();
            foreach (var descriptor in stale)
                services.Remove(descriptor);

            services.AddDbContext<ClinicProDbContext>((sp, options) =>
                options.UseInMemoryDatabase(DatabaseName, _root)
                    // تراکنش‌های رفتار MediatR روی InMemory پشتیبانی نمی‌شوند — نادیده
                    .ConfigureWarnings(w =>
                        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                    // ═══ چرخهٔ ۷۰ — همان اینترسپتورهای تولید ═══ جراحی DI پیشین
                    // AuditableEntityInterceptor را می‌انداخت؛ نتیجه: CreatedAt همهٔ
                    // رکوردهای تست پیش‌فرض (0001-01-01) می‌ماند و «هر گزارشِ
                    // تاریخ‌محور» (درآمد روزانه/صندوق/…) همهٔ ردیف‌ها را از دست
                    // می‌داد — بازتولید همان «گزارش‌ها همه‌چیز صفر» مالک.
                    .AddInterceptors(
                        sp.GetRequiredService<Sahand.ClinicPro.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>(),
                        sp.GetRequiredService<Sahand.ClinicPro.Infrastructure.Persistence.Interceptors.SoftDeleteInterceptor>()));

            // ═══ چرخهٔ ۶۴ — کانتکست گزارش هم InMemory می‌شود ═══
            // گزارش‌ها از ReportingDbContext می‌خوانند؛ بدون این جایگزینی،
            // تست‌های گزارش به SQL Server واقعی می‌رفتند. نام دیتابیس همان
            // نام اصلی است — در InMemory، نام یکتا = فروشگاه مشترک؛ بذرها و
            // دادهٔ نوشته‌شده از کانتکست اصلی برای گزارش‌ها دیده می‌شود
            // (همان رابطهٔ واقعی: هر دو کانتکست به یک دیتابیس وصل‌اند).
            services.AddDbContext<Sahand.ClinicPro.Infrastructure.Persistence.ReportingDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName, _root)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        });
    }
}

/// <summary>
/// کارخانه تست جریان کامل — بذرهای راه‌اندازی + داده‌های مکمل
/// </summary>
/// <remarks>
/// گام V12: بذر داده‌های موردنیاز جریان (پزشک + دارو + لایسنس معتبر)
/// پس از اجرای بذرهای اصلی برنامه روی InMemory.
/// </remarks>
public class FlowApiFactory : InMemoryApiFactory
{
    /// <summary>نام دیتابیس اختصاصی جریان</summary>
    protected override string DatabaseName => "PatientJourneyFlowDb";

    /// <summary>بذر داده‌های مکمل جریان — پزشک و دارو و لایسنس</summary>
    public async Task<long> SeedFlowDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();

        if (!context.Doctors.Any())
        {
            var employee = new Employee
            {
                FirstName = "پزشک",
                LastName = "آزمایشی",
                NationalCode = "0011122233",
                Mobile = "09120000000",
                IsActive = true
            };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var doctor = new Doctor
            {
                EmployeeId = employee.Id,
                NezamPezeshkiCode = "TEST-123",
                ConsultationFee = 500000m,
                VisitDuration = 15,
                IsActive = true
            };
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
        }

        if (!context.Medications.Any())
        {
            context.Medications.Add(new Medication
            {
                GenericName = "Acetaminophen",
                GenericNameFa = "استامینوفن",
                Form = "قرص",
                Strength = "500mg",
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        if (!context.ClinicLicense.Any())
        {
            // لایسنس آزمایشی — همه ماژول‌ها فعال و بدون انقضا (دائمی)
            context.ClinicLicense.Add(new ClinicLicense
            {
                LicenseKey = "SCPRO-TEST0-00000-00000",
                MaxUsers = 100,
                MaxDoctors = 100,
                MaxPatients = 1_000_000,
                // چرخهٔ ۷۰ — «Reports» افزوده شد: میدلور لایسنس مسیر api/v1/reports
                // را می‌بندد و تست سرتاسری گزارش‌ها (ReportsEndToEndTests) بدون آن
                // ۴۰۳ MODULE_NOT_LICENSED می‌گرفت — همان سناریویی که در ایستگاه مالک
                // «گزارش‌ها کار نمی‌کنند/همه‌چیز صفر» را می‌سازد.
                EnabledModules = System.Text.Json.JsonSerializer.Serialize(new[]
                {
                    "Patients", "Appointments", "Receptions", "Visits",
                    "Prescriptions", "Billing", "Dashboard", "Reports"
                })
            });
            await context.SaveChangesAsync();
        }

        var doctorId = await context.Doctors.Select(d => d.Id).FirstAsync();
        return doctorId;
    }
}

/// <summary>مجموعه تست جریان — کارخانه اختصاصی جدا از مجموعه Api</summary>
[CollectionDefinition("Flow")]
public class FlowCollectionFixture : ICollectionFixture<FlowApiFactory>
{
}

/// <summary>
/// تست جریان سرتاسری بیمار — گام V12
/// ورود ادمین → ثبت بیمار → نوبت → پذیرش → ویزیت → نسخه → فاکتور
/// </summary>
/// <remarks>
/// پوشش یکپارچه‌سازی واقعی: احراز هویت + مجوزها + میدلور لایسنس +
/// اعتبارسنجی‌ها + CQRS + EF InMemory + انتشار رویداد دامنه.
/// </remarks>
[Collection("Flow")]
public class PatientJourneyFlowTests
{
    private readonly FlowApiFactory _factory;
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>سازنده تست — تزریق کارخانه</summary>
    public PatientJourneyFlowTests(FlowApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// جریان کامل بیمار از ثبت تا فاکتور باید بدون خطا طی شود
    /// </summary>
    [Fact]
    public async Task Full_Patient_Journey_Should_Succeed()
    {
        // ── آماده‌سازی: بذر پزشک/دارو/لایسنس و ورود ادمین ──
        var doctorId = await _factory.SeedFlowDataAsync();
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { usernameOrEmail = "admin", password = "Admin@123" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: $"بذر ادمین باید ورود را ممکن کند — بدنه: {await loginResponse.Content.ReadAsStringAsync()}");

        var login = await ReadAsync<LoginPacket>(loginResponse);
        login.Data.AccessToken.Should().NotBeNullOrEmpty();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Data.AccessToken);

        // ── گام ۱: ثبت بیمار جدید ──
        var createPatient = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "زهرا",
            lastName = "محمدی",
            nationalCode = "0061457892",
            mobile = "09121234567",
            gender = 2,
            birthDateShamsi = "1370/05/15"
        });
        createPatient.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "داده بیمار معتبر است");
        var patient = await ReadAsync<EntityPacket>(createPatient);
        patient.Data.Id.Should().BeGreaterThan(0);
        patient.Data.NationalCode.Should().Be("0061457892");

        // ── گام ۲: جستجوی بیمار ثبت‌شده ──
        var search = await client.GetAsync("/api/v1/patients/search?term=0061457892");
        search.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchResult = await ReadAsync<ListPacket<EntityDto>>(search);
        searchResult.Data.Should().Contain(p => p.NationalCode == "0061457892");

        // ── گام ۳: ثبت نوبت برای فردا ──
        var tomorrowShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
            .ToShamsi(DateTime.UtcNow.AddDays(1));
        var createAppointment = await client.PostAsJsonAsync("/api/v1/appointments", new
        {
            patientId = patient.Data.Id,
            doctorId,
            dateShamsi = tomorrowShamsi,
            time = "10:30",
            type = 1,
            source = 1
        });
        createAppointment.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "نوبت با ظرفیت آزاد و بدون تداخل است");
        var appointment = await ReadAsync<EntityPacket>(createAppointment);
        appointment.Data.Id.Should().BeGreaterThan(0);

        // ── گام ۴: ثبت پذیرش مرتبط با نوبت ──
        var createReception = await client.PostAsJsonAsync("/api/v1/receptions", new
        {
            patientId = patient.Data.Id,
            doctorId,
            appointmentId = appointment.Data.Id
        });
        createReception.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "پذیرش با نوبت معتبر ثبت می‌شود");

        // ── گام ۵: ذخیره پیش‌نویس ویزیت (SOAP تدریجی) ──
        var saveVisit = await client.PostAsJsonAsync("/api/v1/visits", new
        {
            patientId = patient.Data.Id,
            doctorId,
            type = 1,
            chiefComplaint = "سردرد از دو روز پیش",
            soapSubjective = "درد ضربان‌دار پیشانی",
            soapObjective = "تب ۳۷.۸ درجه",
            complete = false
        });
        saveVisit.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "ذخیره تدریجی ویزیت بدون تشخیص مجاز است");
        var visit = await ReadAsync<LongPacket>(saveVisit);
        visit.Data.Should().BeGreaterThan(0, because: "شناسه ویزیت باید برگردد");

        // ── گام ۶: صدور نسخه با یک قلم دارو ──
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var medicationId = await context.Medications.Select(m => m.Id).FirstAsync();

            var createPrescription = await client.PostAsJsonAsync("/api/v1/prescriptions", new
            {
                patientId = patient.Data.Id,
                doctorId,
                visitId = visit.Data,
                type = 1,
                isElectronic = false,
                items = new[]
                {
                    new
                    {
                        medicationId,
                        orderIndex = 1,
                        dosage = "500mg",
                        frequency = "هر ۸ ساعت",
                        duration = "۵ روز",
                        quantity = 15m,
                        unit = "قرص"
                    }
                }
            });
            createPrescription.StatusCode.Should().Be(HttpStatusCode.Created,
                because: "نسخه بدون تداخل ممنوع مصرف صادر می‌شود");
        }

        // ── گام ۷: صدور فاکتور ویزیت ──
        var createInvoice = await client.PostAsJsonAsync("/api/v1/invoices", new
        {
            patientId = patient.Data.Id,
            visitId = visit.Data,
            type = 1,
            taxPercent = 0m,
            items = new[]
            {
                new
                {
                    description = "حق ویزیت",
                    quantity = 1m,
                    unitPrice = 500000m,
                    discountPercent = 0m
                }
            }
        });
        createInvoice.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "فاکتور با قلم معتبر صادر می‌شود");

        // ── گام ۸: فهرست فاکتورها باید فاکتور جدید را نشان دهد ──
        var invoices = await client.GetAsync("/api/v1/invoices?page=1&pageSize=10");
        invoices.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoiceList = await ReadAsync<PagedPacket<EntityDto>>(invoices);
        invoiceList.Data.Should().NotBeEmpty(
            because: "فاکتور صادرشده باید در فهرست باشد");
    }

    /// <summary>خواندن پاسخ و نگاشت به پاکت</summary>
    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(json, Json);
        result.Should().NotBeNull(because: "پاسخ باید JSON معتبر باشد");
        return result!;
    }

    /// <summary>پاکت شناسه عددی — برای فرمان‌های Result&lt;long&gt;</summary>
    private sealed class LongPacket
    {
        public long Data { get; set; }
    }

    /// <summary>پاکت ورود</summary>
    private sealed class LoginPacket
    {
        public TokenDto Data { get; set; } = new();
    }

    /// <summary>توکن</summary>
    private sealed class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    /// <summary>پاکت موجودیت با شناسه</summary>
    private sealed class EntityPacket
    {
        public EntityDto Data { get; set; } = new();
    }

    /// <summary>موجودیت</summary>
    private sealed class EntityDto
    {
        public long Id { get; set; }
        public string NationalCode { get; set; } = string.Empty;
    }

    /// <summary>پاکت فهرست</summary>
    private sealed class ListPacket<T>
    {
        public List<T> Data { get; set; } = new();
    }

    /// <summary>پاکت صفحه‌بندی‌شده — از چرخه ۱۹ پاسخ صفحه‌بندی تک‌لایه است (ToPagedResponse)</summary>
    private sealed class PagedPacket<T>
    {
        public int TotalCount { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
