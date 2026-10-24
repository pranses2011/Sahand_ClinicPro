using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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

            // ═══ رفع خطای نصب سرور ۱۵۲۳۳ «Property 'MS_Description' already exists» ═══
            // گزارش مالک (v2.22.5): Install-Server-Package.ps1 هنگام «ساخت دیتابیس و
            // اعمال خودکار تغییرهای لازم» با این خطا شکست می‌خورد و نصب کامل نمی‌شد:
            //   SqlException 15233 — Property cannot be added. Property 'MS_Description'
            //   already exists for 'dbo.InsuranceCompanies.IsContracted'.
            // ریشه: تولیدگر پیش‌فرض مهاجرت SQL Server برای AlterColumn، هرگاه
            // OldColumn.Comment با Comment برابر نباشد (مهاجرت دست‌نویس بدون پارامتر
            // oldComment)، بی‌قید EXEC sp_addextendedproperty می‌نویسد؛ اگر ستون از قبل
            // توضیح داشته باشد SQL Server آن را با ۱۵۲۳۳ رد می‌کند، تراکنش مهاجرت عقب
            // می‌گردد و «کل» مهاجرت اعمال نمی‌شود.
            // لایهٔ ۱: خودِ مهاجرت AddInsuranceContractStatus اصلاح شد (oldComment).
            // لایهٔ ۲ (این‌جا): تولیدگر اختصاصی که افزودن/حذف توضیح جدول و ستون را
            // idempotent می‌کند — IF EXISTS → sp_updateextendedproperty، وگرنه
            // sp_addextendedproperty. توضیحات ستون فقط فرادادهٔ مستندسازی‌اند؛ این
            // تغییر هیچ اثری بر داده یا اسکیمای واقعی جداول ندارد.
            options.ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator,
                Persistence.Migrations.ClinicProMigrationsSqlGenerator>();

            // ═══ چرخهٔ سی‌امین — هشدار «Savepoints are disabled because
            // Multiple Active Result Sets (MARS) is enabled» ═══
            // ریشهٔ هشدار: رشتهٔ اتصال MultipleActiveResultSets=true دارد و
            // TransactionBehavior هم همهٔ فرمان‌های نوشتنی MediatR را در تراکنشِ
            // صریح می‌پیچد؛ SQL Server در حالت MARS از Savepoint پشتیبانی
            // نمی‌کند و EF Core برای هر SaveChangesِ داخلِ تراکنش این هشدار
            // را می‌نوشت (به‌ازای هر درخواست نوشتنی یک WRN در لاگ).
            // سرکوبِ آگاهانه و مستند این هشدارِ مشخص بی‌خطر است، چون:
            // ۱) جایگزین Savepoint، عقبگردِ کاملِ تراکنش توسط خودِ
            //    TransactionBehavior است (RollbackAsync در catch) که همان
            //    رفتار مطلوبِ همه‌یابدموند (all-or-nothing) ماست؛
            // ۲) MARS حفظ می‌شود چون کارگرهای پس‌زمینه (پشتیبان‌گیری خودکار
            //    و تپ‌قلب لایسنس) و میدلور لاگ عملیات روی همین اتصال
            //    کارهای هم‌زمان انجام می‌دهند.
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.SavepointsDisabledBecauseOfMARS));
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntityInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());
        });

        // ═══ چرخهٔ ۶۴ (A-01 گام اول) — کانتکست فقط-خواندنی گزارش‌ها ═══
        // استخر کانتکست (Pool) چون گزارش‌ها فقط می‌خوانند: بدون ردیابی، بدون
        // رویداد دامنه، بدون اینترسپتور نوشتن — و NoTracking پیش‌فرض تا هر
        // AsNoTrackingِ فراموش‌شده هم امن بماند.
        services.AddDbContextPool<ReportingDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(120); // گزارش‌های سنگین چندجدولی — دو برابر زمانِ نوشتن
            });
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            // همان سرکوب آگاهانهٔ MARS/Savepoint — گزارش‌ها در تراکنش نیستند ولی
            // اگر روزی در تراکنشی خوانده شوند، هشدارِ بی‌خطر نباید لاگ را شلوغ کند
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.SqlServerEventId.SavepointsDisabledBecauseOfMARS));
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

        // ── سرویس ایمیل (SMTP) — چرخهٔ ۸۲: بازیابی رمز + ارسال بکاپ به ایمیل ──
        // پیکربندی از بخش «Email» در appsettings خوانده می‌شود؛ بدون آن
        // IsConfigured نادرست است و مسیرهای بالادست پیام فارسی روشن می‌دهند.
        services.AddScoped<Application.Interfaces.IEmailSender, Services.Email.SmtpEmailSender>();

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
    /// در تولید، ساخت/ارتقای خودکار دیتابیس پیش از سرویس‌دهی الزامی است و شکست آن راه‌اندازی را متوقف می‌کند.
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

                // ═══ گارد ارائه‌دهندهٔ دیتابیس — چرخهٔ ۸۱ ═══
                // GetPendingMigrations/MigrateAsync فقط در ارائه‌دهنده‌های رابطه‌ای
                // معنا دارند؛ روی InMemory (تست‌های یکپارچگی API) استثنا می‌دهند.
                // چون این استثنا پیش از گام «۲) اجرای بذرها» رخ می‌داد و catch آن را
                // در محیط غیرتولیدی می‌بلعید، هیچ بذری اجرا نمی‌شد: کاربر admin ساخته
                // نمی‌شد و لاگین همهٔ تست‌های API با ۴۰۰ «نام کاربری یا رمز عبور
                // نادرست است» شکست می‌خورد (در تولید SQL Server است و مسیر عوض نمی‌شود).
                if (!db.Database.IsRelational())
                {
                    logger.LogInformation(
                        "ارائه‌دهندهٔ دیتابیس رابطه‌ای نیست ({Provider}) — مهاجرت رد شد و بذرها اجرا می‌شوند",
                        db.Database.ProviderName ?? "نامشخص");
                }
                else
                {
                    var pending = db.Database.GetPendingMigrations().ToList();
                    if (pending.Count > 0)
                    {
                        logger.LogInformation(
                            "اعمال {Count} تغییر دیتابیس در راه‌اندازی اولیه/ارتقا…",
                            pending.Count);
                        // در نصب نخست، همین فراخوانی دیتابیس و تمام جدول‌های نسخهٔ فعلی
                        // را می‌سازد؛ در ارتقا فقط تغییراتِ باقی‌مانده را اعمال می‌کند.
                        // هیچ ابزار dotnet-ef یا اجرای دستی از طرف درمانگاه لازم نیست.
                        try
                        {
                            await db.Database.MigrateAsync();
                        }
                        catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number is 15233 or 15197)
                        {
                            // ═══ خطای «توضیح ستون/جدول» (MS_Description) — گزارش نصب v2.22.5 ═══
                            // 15233: افزودن توضیحی که از قبل هست؛ 15197: حذف توضیحی که نیست.
                            // هر دو نشانهٔ مهاجرتِ دست‌نویس با AlterColumn(comment:) بدون
                            // oldComment است (توضیح: ClinicProMigrationsSqlGenerator). پیام
                            // صریح داده می‌شود تا نصب‌کننده/مالک بداند مشکل از رشتهٔ اتصال یا
                            // مجوز CREATE DATABASE نیست و ریشه‌یابی ساعت‌ها طول نکشد.
                            logger.LogCritical(sqlEx,
                                "مهاجرت دیتابیس به‌دلیل «توضیح ستون/جدول» (MS_Description) شکست خورد " +
                                "(کد {Number}) — اسکیمای دیتابیس از کد عقب می‌ماند. " +
                                "افزودن/حذف توضیح در این نسخه idempotent شده است؛ اگر این پیام را " +
                                "می‌بینید یعنی مهاجرت با migrationBuilder.Sql فرمان " +
                                "sp_addextendedproperty/sp_dropextendedproperty را دستی اجرا می‌کند — " +
                                "آن را به AlterColumn(comment:, oldComment:) / AlterTable(comment:) " +
                                "تغییر دهید. متن خطای SQL Server: {Message}",
                                sqlEx.Number, sqlEx.Message);

                            throw new InvalidOperationException(
                                $"اعمال تغییر دیتابیس ناموفق بود — توضیح ستون/جدول تکراری یا ناموجود " +
                                $"(SQL {sqlEx.Number}: {sqlEx.Message})", sqlEx);
                        }
                    }

                    // سرور نباید با اسکیمای ناقص آمادهٔ سرویس‌دهی شود. شکست اتصال،
                    // مجوز ناکافی یا خطای DDL راه‌اندازی را متوقف می‌کند تا نصب‌کننده
                    // علت را رفع کند؛ ادامهٔ خاموش با خطاهای پراکندهٔ زمان اجرا خطرناک است.
                    var stillPending = db.Database.GetPendingMigrations().ToList();
                    if (stillPending.Count > 0)
                    {
                        var message = $"پس از اعمال خودکار، هنوز {stillPending.Count} تغییر دیتابیس اعمال‌نشده است: " +
                                      string.Join(", ", stillPending);
                        logger.LogCritical("{DatabaseMigrationFailure}", message);
                        throw new InvalidOperationException(message);
                    }
                }
            }

            // ۱.۵) پرکردن شناسه‌دار ردیف‌های «بدون شمارهٔ سند» — چرخهٔ ۷۰
            // گزارش مالک: «در تب دفتر روزنامه، در لیست اسناد شماره را نمی‌ناندازد
            // و به جاش خط تیره می‌اندازد». ستون EntryNumber با مقدار پیش‌فرضِ
            // دیتابیس (SeqJournalEntryNumber → JE-000NNN) پر می‌شود؛ ردیف‌هایی که
            // پیش‌تر/به هر علتی NULL شده‌اند در اینجا یک‌باره و idempotent
            // شناسه‌دار می‌شوند: هر ردیف NULL با «NEXT VALUE FOR» از همان توالی
            // شمارهٔ یکتای تازه می‌گیرد (هر بار اجرا فقط ردیف‌های NULL را می‌گیرد؛
            // اجرای مجدد بی‌اثر است و هیچ شماره‌ای دوباره مصرف نمی‌شود بی‌آنکه
            // ردیفی بگیرد). فقط روی SQL Server — بقیهٔ ارائه‌دهنده‌ها چنین توالی/
            // عبارتی ندارند و این گام برایشان بی‌اثر رد می‌شود.
            using (var scope15 = serviceProvider.CreateScope())
            {
                var db15 = scope15.ServiceProvider.GetRequiredService<Persistence.ClinicProDbContext>();
                var logger15 = scope15.ServiceProvider
                    .GetRequiredService<ILogger<Microsoft.Extensions.DependencyInjection.ServiceProvider>>();
                try
                {
                    if (db15.Database.IsSqlServer())
                    {
                        // ═══ چرخهٔ ۷۱ — رفع سه لاگِ اضافی/خطای هر استارتِ سرور ═══
                        // گزارش مالک: در هر راه‌اندازی سرور API این سه لاگ می‌آمد:
                        //  ۱) WRN «First/FirstOrDefault operator without OrderBy and filter»
                        //  ۲) ERR «InvalidCastException: Unable to cast 'System.Int32' to
                        //     'System.Int64'» در همین متد (خط ~۲۵۵)
                        //  ۳) WRN «پرکردن شمارهٔ سندهای قدیمی ناموفق بود — ادامه بدون توقف»
                        // ریشه: کوئری شمارشِ پس-از-پرکردن با SqlQueryRaw<long> نوشته شده
                        // بود اما COUNT(*) در SQL Server همیشه «int» (Int32) برمی‌گرداند
                        // و EF Core برای <long> سرراست SqlDataReader.GetInt64 صدا می‌زند →
                        // InvalidCastException؛ یعنی کوئریِ صرفاً-تشخیصیِ «آیا ردیف تهی
                        // ماند؟» روی هر استارتِ کاملاً سالم هم می‌شکست — حتی وقتی UPDATE
                        // موفق بوده و هیچ تهی‌ای نمانده. FirstOrDefaultAsync بدون Where هم
                        // هشدار EF (FirstWithoutOrderByAndFilter) را روشن می‌کرد.
                        // رفع: کوئری دوم به‌کلی حذف شد — «تعداد ردیف‌های پرشده» از خروجیِ
                        // خودِ ExecuteSqlRawAsync (تعداد سطرهای تحت‌تأثیر UPDATE) می‌آید:
                        // یک رفت‌وبرگشتِ کمتر به دیتابیس، بدون خواندن اسکالر، بدون هشدار.
                        // ضمناً «remaining» از اساس منطق مرده بود: تک‌UPDATE با WHERE IS
                        // NULL همهٔ تهی‌ها را پر می‌کند؛ اگر می‌شکست استثنا بالا می‌آمد،
                        // نه تهیِ مانده — پس شمارشِ تهیِ پس-از-UPDATE فقط هزینه بود.
                        var filledCount = await db15.Database.ExecuteSqlRawAsync(
                            "UPDATE [JournalEntries] SET [EntryNumber] = " +
                            "CONCAT('JE-', RIGHT('000000' + CAST(NEXT VALUE FOR SeqJournalEntryNumber AS varchar(10)), 6)) " +
                            "WHERE [EntryNumber] IS NULL");
                        if (filledCount > 0)
                            logger15.LogInformation(
                                "پرکردن شمارهٔ سندهای قدیمی در راه‌اندازی: {Count} سندِ بدون شماره، شمارهٔ یکتای تازه گرفت",
                                filledCount);
                    }
                }
                catch (Exception ex15)
                {
                    // بهترین تلاش — نباید راه‌اندازی را متوقف کند؛ شماره‌گذاری
                    // ردیف‌های تازه همچنان از مسیر پیش‌فرضِ INSERT کار می‌کند
                    logger15.LogWarning(ex15, "پرکردن شمارهٔ سندهای قدیمی ناموفق بود — ادامه بدون توقف");
                }
            }

            // ۲) اجرای بذرها
            await DataSeederRunner.RunAllAsync(serviceProvider);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider
                .GetRequiredService<ILogger<Microsoft.Extensions.DependencyInjection.ServiceProvider>>();
            var environment = serviceProvider.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            logger.LogError(ex, "راه‌اندازی دیتابیس/بذرها با خطا روبه‌رو شد");

            // در استقرار درمانگاه نباید API با اسکیمای ناقص آمادهٔ سرویس‌دهی شود.
            // در Development و تست، نبود SQL Server می‌تواند عمدی باشد.
            if (environment.IsProduction())
                throw;
        }
    }
}
