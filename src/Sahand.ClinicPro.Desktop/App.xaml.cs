using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کلاس برنامه کلاینت دسکتاپ درمانگاه سهند — راه‌اندازی DI و انتخاب پنجره آغازین
/// اگر توکن معتبر ذخیره‌شده باشد مستقیم پنجره اصلی و در غیر این صورت پنجره ورود باز می‌شود.
/// </summary>
/// <remarks>
/// نکته فنی: نام پایه به‌صورت کامل «System.Windows.Application» نوشته شده است زیرا
/// فضای‌نام Sahand.ClinicPro.Application (لایه کاربرد) با نام کلاس Application در WPF تداخل دارد.
/// </remarks>
public partial class App : System.Windows.Application
{
    /// <summary>ارائه‌دهنده سرویس سراسری برنامه دسکتاپ</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>رویداد راه‌اندازی — ساخت کانتینر DI و انتخاب پنجره آغازین</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // پیکربندی — appsettings.json کنار برنامه یا پوشه خروجی
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        Services = services.BuildServiceProvider();

        // انتخاب پنجره آغازین بر اساس وضعیت توکن
        var tokens = Services.GetRequiredService<ITokenManager>();
        if (tokens.HasValidToken)
        {
            // چرخه هفدهم (گزارش یازدهم): توکن معتبر ذخیره شده اما اگر سرور صریحاً
            // لایسنس را نامعتبر اعلام کند، پیش از پوستهٔ اصلی فعال‌سازی مطالبه
            // می‌شود — پیش‌تر این مسیر هیچ گاردی نداشت و با توکنِ به‌یادمانده
            // بدون لایسنس هم مستقیم وارد برنامه می‌شدند؛ وضعیت «نامعلوم»
            // (قطعی شبکه) مانع کار درمانگاه نمی‌شود (fail-open فقط برای آن)
            _ = StartupWithLicenseGateAsync();
        }
        else
        {
            var login = Services.GetRequiredService<LoginWindow>();
            MainWindow = login;
            login.Show();
        }
    }

    /// <summary>
    /// راه‌اندازی پوستهٔ اصلی پشت گیت لایسنس — چرخه هفدهم (گزارش یازدهم).
    /// پس از بررسی وضعیت لایسنس: معتبر/نامعلوم ⇒ پنجرهٔ اصلی؛ نامعتبر ⇒ پنجرهٔ
    /// فعال‌سازی مودال و در صورت انجام‌نشدن آن، توکن پاک و صفحهٔ ورود نشان
    /// می‌شود تا هیچ مسیری بدون لایسنس به پوسته نرسد. هر خطای غیرمنتظره،
    /// مطابق سیاست fail-open، پوسته را باز می‌گذارد (قفل‌شدن درمانگاه ممنوع).
    /// </summary>
    private async Task StartupWithLicenseGateAsync()
    {
        try
        {
            var api = Services.GetRequiredService<IApiClient>();
            // مهلت کوتاه بررسی — در شبکهٔ معلق، پس از ~۸ ثانیه وضعیت «نامعلوم»
            // فرض شده و پوسته (fail-open) باز می‌شود تا کار درمانگاه نایستد
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
            var verdict = await LicenseGate.CheckAsync(api, cts.Token);

            if (verdict == LicenseGate.Verdict.Invalid
                && !Views.LicenseActivationWindow.OpenAndActivate(null))
            {
                // فعال‌سازی انجام نشد — توکن همین اجرا پاک می‌شود و ورود مجدد
                // از همان گیت لایسنس می‌گذرد
                Services.GetRequiredService<ITokenManager>().Clear();
                var login = Services.GetRequiredService<LoginWindow>();
                MainWindow = login;
                login.Show();
                return;
            }

            var main = Services.GetRequiredService<MainWindow>();
            MainWindow = main;
            main.Show();
        }
        catch
        {
            // fail-open — در خطای غیرمنتظره پوستهٔ اصلی باز می‌شود
            if (MainWindow is null)
            {
                var main = Services.GetRequiredService<MainWindow>();
                MainWindow = main;
                main.Show();
            }
        }
    }

    /// <summary>ثبت سرویس‌ها و پنجره‌ها در کانتینر</summary>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // پیکربندی
        services.AddSingleton<IConfiguration>(configuration);

        // سرویس‌های زیرساخت کلاینت
        services.AddSingleton<ITokenManager, TokenManager>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();

        // HttpClient سفارشی کلاینت API
        services.AddHttpClient<ApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
            {
                // گواهی خودامضای محیط توسعه
                ServerCertificateCustomValidationCallback =
                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        services.AddSingleton<IApiClient>(sp =>
            sp.GetRequiredService<ApiClient>());

        // اتصال بلادرنگ SignalR — Hubهای صف و اعلان (گام R1)
        services.AddSingleton<IRealtimeClient, RealtimeClientService>();

        // پنجره‌ها و ViewModelها
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<LoginViewModel>();
        // جریان تغییر رمز اجباری — چرخه چهاردهم (گزارش پنجم مالک)
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<ChangePasswordWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<PatientsViewModel>();
        services.AddTransient<DoctorsViewModel>();
        services.AddTransient<AppointmentsViewModel>();
        services.AddTransient<ReceptionsViewModel>();
        services.AddTransient<VisitsViewModel>();
        services.AddTransient<PrescriptionsViewModel>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<AccountingViewModel>();
        services.AddTransient<InsuranceViewModel>();
        services.AddTransient<ParaclinicalViewModel>();
        services.AddTransient<LabViewModel>();
        services.AddTransient<PharmacyViewModel>();
        services.AddTransient<StocktakingViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportBrowserViewModel>();
        services.AddTransient<AdminViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<UserFormViewModel>();
        services.AddTransient<UserFormWindow>();
        services.AddTransient<RolesViewModel>();
        services.AddTransient<SmsViewModel>();
        services.AddTransient<SurveyViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<BranchesViewModel>();
        services.AddTransient<SepasViewModel>();
        services.AddTransient<EPrescriptionViewModel>();
        services.AddTransient<LicenseStatusViewModel>();
        services.AddTransient<ComplementaryViewModel>();
        services.AddTransient<AdvancedEmrViewModel>();
        services.AddTransient<PayrollViewModel>();
        services.AddTransient<AttendanceViewModel>();
        // گزارش‌ساز رایگانه — چرخه سیزدهم (درخواست ۴-۳)
        services.AddTransient<AdhocReportViewModel>();
        services.AddTransient<AdhocReportWindow>();
        // مرکز آموزش و تور هدایت‌شده — چرخه سیزدهم (درخواست ۸-۶)
        services.AddTransient<TrainingCenterViewModel>();

        // پنجرهٔ فعال‌سازی مستقل لایسنس — چرخه هفدهم (گزارش یازدهم):
        // پیش از ورود/در میانهٔ ورود به‌صورت مودال باز می‌شود
        services.AddTransient<LicenseActivationWindow>();
    }
}
