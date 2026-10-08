using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Entities.Licensing;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Identity;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Interceptors;
using Sahand.ClinicPro.Infrastructure.Persistence.Repositories;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;
using Sahand.ClinicPro.Infrastructure.Services;

namespace Sahand.ClinicPro.Infrastructure;

/// <summary>
/// نقطه ورود ثبت سرویس‌های لایه زیرساخت در کانتینر تزریق وابستگی
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ثبت DbContext اصلی با SQL Server، اینترسپتورها، ریپازیتوری‌ها و بذرها
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // رشته اتصال — از appsettings یا متغیر محیطی
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("رشته اتصال DefaultConnection یافت نشد");

        // DbContext با اینترسپتورهای حسابرسی و حذف نرم
        //
        // ── چرخه چهاردهم (گزارش ششم مالک — خطای ۵۰۰ لاگین): چرا EnableRetryOnFailure ممنوع است؟ ──
        // TransactionBehavior همه فرمان‌های MediatR را در تراکنش دستیِ آغازشده با
        // Database.BeginTransaction می‌پیچد؛ اما EF Core ترکیب «استراتژی تلاش‌مجدد»
        // (SqlServerRetryingExecutionStrategy، فعال‌شده با گزینه یادشده) با تراکنش دستی
        // را ناسازگار می‌داند و نخستین عملیات داخل تراکنش با InvalidOperationException
        // («...does not support user-initiated transactions...») می‌شکند — یعنی همه
        // endpointهای نوشتنی روی SQL Server واقعی ۵۰۰ می‌دادند و نخستین موردِ تاریخ
        // پروژه، لاگین مالک بود. این تعارض در تست‌های یکپارچگی InMemory هرگز دیده
        // نمی‌شود چون InMemory تراکنش را نادیده می‌گیرد و اصلاً چک استراتژی را فعال
        // نمی‌کند (اثبات و گارد: ExecutionStrategyTransactionTests). راه‌حل جایگزین،
        // پیچیدن فرمان در CreateExecutionStrategy بود که تلاش‌مجدد ناایدومپوونت روی
        // منطق دلخواه گرداننده‌ها می‌شد؛ برای استقرار شبکه محلی یک درمانگاه، درستیِ
        // تراکنش‌های صریح بر تاب‌آوری خطاهای گذرا مقدم است.
        services.AddDbContext<ClinicProDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(60);
            });
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        // اینترسپتورها و سرویس کاربر فعلی
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // واحد کار و ریپازیتوری‌ها
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();

        // ── یکپارچه‌سازی سامانه‌های دولتی — سپاس + نسخه الکترونیک ──
        services.AddHttpClient<Services.Government.SepasService>();
        services.AddHttpClient<Services.Government.ElectronicPrescriptionService>();
        services.AddScoped<Application.Interfaces.ISepasService>(sp =>
            sp.GetRequiredService<Services.Government.SepasService>());
        services.AddScoped<Application.Interfaces.IElectronicPrescriptionService>(sp =>
            sp.GetRequiredService<Services.Government.ElectronicPrescriptionService>());

        // ── سامانه لایسنس سمت کلاینت ──
        // سرویس لایسنس با HttpClient typed — اتصال به سرور لایسنس PHP/بلزور
        services.AddHttpClient<Application.Interfaces.ILicenseService, Services.License.LicenseService>();

        // استعلام برخط بیمه — گام V10 (کش TTL + سه تلاش + fallback برون‌خط)
        services.AddHttpClient("insurance-eligibility");
        services.AddScoped<Application.Interfaces.IInsuranceEligibilityService,
            Services.Insurance.OnlineInsuranceEligibilityService>();

        // ── کارهای پس‌زمینه Hangfire — فقط وقتی دیتابیس واقعی در دسترس است ──
        // در محیط تست/CI با نبود SQL Server مقدار Jobs:Enabled=false می‌ماند
        if (configuration.GetValue("Jobs:Enabled", false))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));
            services.AddHangfireServer();
        }

        // ── سامانه پیامک — کارخانه پنل‌ها + زنجیره جایگزینی ──
        services.AddHttpClient<Services.SMS.Providers.KavenegarProvider>();
        services.AddScoped<Application.Interfaces.ISmsProvider>(sp =>
            sp.GetRequiredService<Services.SMS.Providers.KavenegarProvider>());
        services.AddScoped<Services.SMS.SmsProviderFactory>();
        services.AddScoped<Application.Interfaces.ISmsService, Services.SMS.SmsService>();
        services.AddScoped<Services.SMS.SmsQueueProcessor>();

        // سرویس‌های انتزاعی Application پیاده‌سازی‌شده در زیرساخت
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddSingleton<IDateTimeService, Services.DateTimeService>();
        services.AddSingleton<IEncryptionService, Services.Security.EncryptionService>();

        // بذرهای داده اولیه
        services.AddTransient<IDataSeeder, RoleSeeder>();
        services.AddTransient<IDataSeeder, PermissionSeeder>();
        services.AddTransient<IDataSeeder, AdminSeeder>();
        services.AddTransient<IDataSeeder, BasicDataSeeder>();
        services.AddTransient<IDataSeeder, AccountingAccountSeeder>();
        services.AddTransient<IDataSeeder, ReferenceDataSeeder>();
        services.AddTransient<IDataSeeder, PrintTemplateSeeder>(); // چرخه ۱۳ — قالب‌های چاپ پیش‌فرض

        // خروجی PDF گزارش‌ها با سربرگ درمانگاه — چرخه سیزدهم (درخواست ۴-۶)
        services.AddSingleton<Services.Reporting.ReportPdfService>();

        // سرویس آرشیو سالانه — چرخه سیزدهم (درخواست ۷-۷)
        services.AddScoped<Services.Archive.ArchiveService>();

        // ═══ کارگر پشتیبان‌گیری خودکار — چرخه بیست‌ویکم (گزارش پانزدهم مالک) ═══
        // «بکاپ خودکار انجام نمی‌شود» — هیچ کارگر پس‌زمینه‌ای وجود نداشت؛
        // این HostedService بر اساس تنظیمات (SystemSettings دستهٔ Backup) هر روز
        // در ساعت مقرر BACKUP DATABASE واقعی می‌گیرد و پوشهٔ مقصد را در نبود می‌سازد.
        services.AddHostedService<Services.Backup.AutomaticBackupWorker>();

        return services;
    }

    /// <summary>
    /// اجرای مهاجرت‌های معلق + بذرها پس از راه‌اندازی — خطاها ثبت می‌شوند اما برنامه را متوقف نمی‌کنند.
    /// چرخه بیستم (گزارش چهاردهم مالک): پیش‌تر مهاجرت‌ها فقط با دستور «dotnet ef database
    /// update» روی سرور اعمال می‌شدند؛ مالک فقط فایل‌های تغییرکرده را جایگزین می‌کند و
    /// ابزار توسعه در اختیار ندارد. اکنون مهاجرت‌های معلق در همان راه‌اندازی سرور
    /// به‌صورت خودکار اعمال می‌شوند تا ارتقای بستهٔ آپدیت بدون گام دستی ممکن باشد.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            // ۱) اعمال مهاجرت‌های معلق — idempotent؛ اگر دیتابیس به‌روز باشد بی‌اثر است
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Persistence.ClinicProDbContext>();
                var logger = scope.ServiceProvider
                    .GetRequiredService<ILogger<Microsoft.Extensions.DependencyInjection.ServiceProvider>>();
                try
                {
                    var pending = db.Database.GetPendingMigrations().ToList();
                    if (pending.Count > 0)
                    {
                        logger.LogInformation(
                            "اعمال {Count} مهاجرت معلق دیتابیس در راه‌اندازی…",
                            pending.Count);
                        await db.Database.MigrateAsync();

                        // ═══ چرخه ۲۵ — گارد پس از مهاجرت (درس‌گرفته از خطای
                        // «Invalid column name 'ServiceId'» سرور مالک) ═══
                        // اگر پس از MigrateAsync هنوز مهاجرت معلق بماند، یعنی
                        // اسکیمای دیتابیس از کد عقب است و پرس‌وجوها به‌زودی با
                        // SqlException 207 «Invalid column name» می‌شکنند؛ این
                        // وضعیت باید همان‌جا با بلندترین سطح لاگ اعلام شود تا
                        // پیش از خطاهای زمانِ اجرا دیده و پیگیری شود.
                        var stillPending = db.Database.GetPendingMigrations().ToList();
                        if (stillPending.Count > 0)
                        {
                            logger.LogCritical(
                                "اسکیمای دیتابیس از کد عقب مانده است: {Count} مهاجرت اعمال‌نشده ({Migrations}). " +
                                "پرس‌وجوها با خطای «Invalid column name» شکست خواهند خورد — " +
                                "لاگ خطای مهاجرت را در بالا ببینید، علت را رفع کنید و سرور را ری‌استارت کنید",
                                stillPending.Count, string.Join(", ", stillPending));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // خطای مهاجرت جلوی اجرای سرویس را نمی‌گیرد — لاگ و ادامه
                    // (مثلاً سرور در حال اجرا با اتصال موقتاً قطع‌شده)
                    logger.LogError(ex,
                        "اعمال مهاجرت‌های دیتابیس در راه‌اندازی ناموفق بود — اسکیمای دیتابیس " +
                        "ممکن است از کد عقب بماند و پرس‌وجوها با خطای «Invalid column name» شکست بخورند");
                }
            }

            // ۲) اجرای بذرها
            await DataSeederRunner.RunAllAsync(serviceProvider);
        }
        catch
        {
            // در محیط بدون دیتابیس (تست یا CI) بی‌صدا عبور می‌کنیم —
            // خطای واقعی توسط لاگر داخل RunAllAsync ثبت شده است
        }
    }
}
