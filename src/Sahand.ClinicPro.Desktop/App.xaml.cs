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

    // ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک): فونت سراسری واقعاً اعمال شود ═══
    // «فونت و اندازهٔ فونت اعمال نمی‌شود — نه فونت‌های خود برنامه نه ویندوز»
    // ریشهٔ یافت‌شده: استایل ضمنی <Style TargetType="Window"> در App.xaml فقط
    // روی پنجره‌هایی با «نوعِ دقیقِ Window» اثر می‌کند؛ در WPF جست‌وجوی استایل
    // ضمنی با کلیدِ نوعِ دقیقِ عنصر انجام می‌شود و پنجره‌های مشتق‌شده
    // (MainWindow، فرم‌های مودال و…) آن را نمی‌گیرند — یعنی FontFamily و
    // FontSize هرگز از منابع App.FontFamily/App.BaseFontSize به هیچ پنجره‌ای
    // نمی‌رسید و تعویض فونت/سایز هیچ اثری نداشت.
    // رفع قطعی: یک «کلاس‌هندلر» روی رویداد Loaded نوع پایهٔ Window ثبت می‌شود
    // که برای همهٔ پنجره‌های برنامه (مشتق‌شده هم) اجرا می‌شود و فونت/اندازه را
    // با SetResourceReference (معادل DynamicResource در کد) وصل می‌کند —
    // تغییر منبع در ThemeService بی‌درنگ روی همهٔ پنجره‌ها اعمال می‌شود.
    static App()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            Window.LoadedEvent,
            new RoutedEventHandler(OnAnyWindowLoadedForFont));

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — ارقام فارسی «بدون استثنا» ═══
        // «همهٔ اعداد داخل برنامه فارسی باشند ولی هنوز در برخی فرم‌ها و تاریخ‌ها
        // انگلیسی‌اند» — ریشه: ارقام در WPF با زبان عنصر (پیش‌فرض en-US) شکل
        // می‌گیرند و فقط جاهایی که صریحاً مبدل PersianDigits بودند فارسی می‌شدند.
        // راه‌حل قطعی و استاندارد: مقدار پیش‌فرض Language همهٔ پنجره‌ها fa-IR
        // می‌شود (با OverrideMetadata + Inherits) تا موتور رندر WPF (جایگذاری
        // ارقام NumberSubstitution) همهٔ ارقام را به‌صورت فارسی ۰-۹ رسم کند و
        // قالب‌بندی‌های عددی StringFormat هم جداکنندهٔ فارسی بگیرند. این قانون
        // هم‌زیست بی‌خطر با مبدل‌های موجود PersianDigits دارد (بدون تغییر).
        System.Windows.FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(Window),
            new System.Windows.FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage("fa-IR"),
                System.Windows.FrameworkPropertyMetadataOptions.Inherits));

        // فرهنگ جاری رشته‌ها هم فارسی شود — پارس ورودی‌های عددی ارقام فارسی را
        // می‌پذیرد و قالب‌بندی پیش‌فرض اعداد/تاریخ‌ها شمسی می‌شود
        var fa = new System.Globalization.CultureInfo("fa-IR");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = fa;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = fa;
    }

    /// <summary>
    /// اتصال فونت/اندازهٔ پویا به هر پنجره هنگام باز شدن — چرخه بیست‌وسوم.
    /// SetResourceReference مثل {DynamicResource} رفتار می‌کند: با بازنویسی
    /// منابع App.FontFamily/App.BaseFontSize در ThemeService، همهٔ پنجره‌های
    /// باز بی‌درنگ تازه می‌شوند (بدون ری‌استارت).
    /// </summary>
    private static void OnAnyWindowLoadedForFont(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;
        // فقط یک‌بار وصل شود (پرچم روی خود پنجره)
        if (window.GetValue(FontHookedProperty) is true) return;
        window.SetValue(FontHookedProperty, true);
        window.SetResourceReference(Window.FontFamilyProperty, "App.FontFamily");
        window.SetResourceReference(Window.FontSizeProperty, "App.BaseFontSize");

        // ═══ چرخه بیست‌وچهارم — کلید Escape برای بستن فرم‌ها ═══
        // «در تمامی فرم‌هایی که باز می‌شوند کلید کنسل یا ESC را هم تنظیم کن
        // برای بستن کادرها و فرم‌ها» — برای همهٔ پنجره‌های فرعی (دیالوگ/فرم)
        // ثبت می‌شود؛ پنجرهٔ اصلی و ورود استثنا هستند (بستنشان خروج از برنامه
        // است). PreviewKeyDown پیش از پردازش عناصر داخلی (کمبو/تقویم) می‌گیرد
        // تا ESC همیشه فرم را ببندد.
        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ «باید قبل از بستن بررسی
        // کنه که تغییری داده نشده باشه و اگه تغییری باشه، با تایید کاربر فرم
        // رو ببنده»: اگر ViewModel فرم قرارداد IHasUnsavedChanges را پیاده
        // کرده و تغییرات ذخیره‌نشده دارد، ابتدا تأیید فارسی خود برنامه پرسیده
        // می‌شود — با دیالوگ برنامه (قانون مالک) نه کادر پیام ویندوز. همان
        // بررسی روی دکمهٔ بستن (X) پنجره هم از رویداد Closing رد می‌شود.
        if (window is not Sahand.ClinicPro.Desktop.MainWindow
            && window is not Views.LoginWindow)
        {
            window.PreviewKeyDown += (_, ke) =>
            {
                if (ke.Key == System.Windows.Input.Key.Escape)
                {
                    // تغییرات ذخیره‌نشده؟ — ابتدا تأیید کاربر
                    if (!ConfirmCloseIfDirty(window))
                        return;
                    AllowClose(window);
                    window.Close();
                    ke.Handled = true;
                }
            };

            // دکمهٔ بستن (X) هم همان تأیید را می‌گیرد — قانون «قبل از بستن بررسی شود»
            // چرخهٔ بیست‌وهفتم: رفع هشدار CA2013 — ReferenceEquals روی bool شکسته‌شده
            // همیشه false برمی‌گرداند (boxing) و پرچم مجوز بستن هرگز خوانده نمی‌شد؛
            // الگوی استاندارد «is not true» جایگزین شد (همان الگوی FontHookedProperty بالا)
            window.Closing += (_, ce) =>
            {
                if (window.GetValue(CloseAllowedProperty) is not true
                    && !ConfirmCloseIfDirty(window))
                    ce.Cancel = true;
                else
                    AllowClose(window);
            };
        }
        // ═══ چرخه بیست‌وچهارم — مقیاس سراسری رابط ═══ «اندازهٔ فونت فقط صفحهٔ تم
        // را تغییر می‌داد»: پنجرهٔ اصلی، محتوایش به تبدیل مقیاس پویا وصل می‌شود
        // تا تغییر اندازهٔ فونت، همهٔ عناصر برنامه را هماهنگ بزرگ/کوچک کند (زوم
        // پیوسته مثل مرورگر). پنجره‌های دیالوگ اندازهٔ ثابت دارند و مقیاس متنشان
        // از همان FontSize ارثی تأمین می‌شود تا فرم‌ها بریده نشوند.
        if (window is Sahand.ClinicPro.Desktop.MainWindow && window.Content is FrameworkElement rootContent)
            rootContent.SetResourceReference(
                FrameworkElement.LayoutTransformProperty, "App.FontScaleTransform");
    }

    /// <summary>پرچم جلوگیری از وصل‌شدن دوبارهٔ فونت به همان پنجره</summary>
    private static readonly DependencyProperty FontHookedProperty =
        DependencyProperty.RegisterAttached("FontHooked", typeof(bool), typeof(App), new PropertyMetadata(false));

    /// <summary>
    /// پرچم «بستن مجاز» — پس از تأیید کاربر (یا فرم تمیز) روی پنجره گذاشته
    /// می‌شود تا رویداد Closing دوباره تأیید نخواهد؛ چرخهٔ بیست‌وششم
    /// </summary>
    private static readonly DependencyProperty CloseAllowedProperty =
        DependencyProperty.RegisterAttached("CloseAllowed", typeof(bool), typeof(App), new PropertyMetadata(false));

    /// <summary>علامت‌گذاری پنجره به‌عنوان مجاز به بستن — پرهیز از تأیید تکراری</summary>
    private static void AllowClose(Window window) => window.SetValue(CloseAllowedProperty, true);

    /// <summary>
    /// بررسی تغییرات ذخیره‌نشده پیش از بستن پنجره — چرخهٔ بیست‌وششم:
    /// اگر ViewModel فرم قرارداد IHasUnsavedChanges را پیاده کرده و کثیف
    /// است، تأیید دوگزینه‌ای فارسی نمایش داده می‌شود؛ انصراف کاربر یعنی
    /// «فرم باز بماند». فرم‌های بدون قرارداد (نمایشی/مرورگر) بی‌تأیید
    /// بسته می‌شوند — رفتار پیشین.
    /// </summary>
    private static bool ConfirmCloseIfDirty(Window window)
    {
        if (window.DataContext is not ViewModels.IHasUnsavedChanges { HasUnsavedChanges: true } dirty)
            return true; // فرم تمیز است یا قرارداد ندارد — بستن بی‌تأیید

        // دیالوگ فارسی خود برنامه — نه کادر پیام ویندوز (قانون مالک)
        var answer = Views.AppDialog.Confirm(
            "تغییرات ذخیره‌نشده",
            $"در «{dirty.UnsavedChangesTitle}» تغییراتی وارد کرده‌اید که ذخیره نشده است.\n" +
            "فرم بدون ذخیره بسته شود؟",
            Views.AppDialogIcon.Warning);
        return answer == true;
    }

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

        // چرخه نوزدهم (گزارش سیزدهم مالک): تم رنگی انتخاب‌شدهٔ کاربر از اجرای
        // پیشین — پیش از باز شدن هر پنجره‌ای روی پوسته اعمال می‌شود تا برنامه
        // از همان لحظهٔ اول با پوستهٔ دلخواه بالا بیاید (پیش‌فرض: آبی/روشن)
        var savedThemeKey = LocalSettingsStore.Get("ThemeKey");
        var savedThemeDark = string.Equals(LocalSettingsStore.Get("ThemeIsDark"), "true",
            StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(savedThemeKey))
            Services.GetRequiredService<IThemeService>().ApplyTheme(savedThemeKey, savedThemeDark);

        // چرخه بیستم (گزارش چهاردهم مالک): تنظیمات ظاهری ذخیره‌شده — فونت و
        // اندازهٔ پایه و واحد پول نمایشی — پیش‌تر فقط ذخیره می‌شدند و اثری
        // نداشتند؛ اکنون در همان راه‌اندازی روی منابع پویا اعمال می‌شوند تا
        // انتخاب فونت/سایز/واحد پول از اجرای قبل، از لحظهٔ اول برقرار باشد
        Services.GetRequiredService<IThemeService>()
            .ApplyAppearance(ThemeService.LoadSavedAppearance());

        // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — اسپلش انیمیشنی ═══
        // «برنامه Splash با انیمیشن و نوار پیشرفت بساز که خیلی زیبا و با
        // زمینهٔ شفاف باشه» — پیش از پنجرهٔ ورود/اصلی، اسپلش با لوگوی بزرگ
        // شفاف و نوار پیشرفت درصدی نمایش داده می‌شود و مراحل راه‌اندازی
        // (تم/توکن/لایسنس) پیشرفت را جلو می‌برند.
        var tokens = Services.GetRequiredService<ITokenManager>();
        var splash = new Views.SplashWindow();
        splash.Show();
        splash.ReportProgress(20);

        if (tokens.HasValidToken)
        {
            // چرخه هفدهم (گزارش یازدهم): توکن معتبر ذخیره شده اما اگر سرور صریحاً
            // لایسنس را نامعتبر اعلام کند، پیش از پوستهٔ اصلی فعال‌سازی مطالبه
            // می‌شود — پیش‌تر این مسیر هیچ گاردی نداشت و با توکنِ به‌یادمانده
            // بدون لایسنس هم مستقیم وارد برنامه می‌شدند؛ وضعیت «نامعلوم»
            // (قطعی شبکه) مانع کار درمانگاه نمی‌شود (fail-open فقط برای آن)
            splash.ReportProgress(55);
            _ = StartupWithLicenseGateAsync(splash);
        }
        else
        {
            // چرخهٔ چهل‌ودوم: ساخت/نمایش ورود از متد کمکی می‌گذرد تا هرگز پنجرهٔ
            // بسته‌شده Show نشود؛ MainWindow هم در همان متد ست می‌شود تا با
            // بسته‌شدن اسپلش (آخرین پنجرهٔ باز) برنامه تعطیل نشود
            splash.ReportProgress(80);

            // اسپلش تا آماده‌شدن ورود بماند و با محو آرام کنار برود
            var openDelay = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };
            openDelay.Tick += (_, _) =>
            {
                openDelay.Stop();
                splash.ReportProgress(100);
                ShowLoginWindow();
                splash.CloseWithFade();
            };
            openDelay.Start();
        }
    }

    /// <summary>
    /// راه‌اندازی پوستهٔ اصلی پشت گیت لایسنس — چرخه هفدهم (گزارش یازدهم).
    /// پس از بررسی وضعیت لایسنس: معتبر/نامعلوم ⇒ پنجرهٔ اصلی؛ نامعتبر ⇒ پنجرهٔ
    /// فعال‌سازی مودال و در صورت انجام‌نشدن آن، توکن پاک و صفحهٔ ورود نشان
    /// می‌شود تا هیچ مسیری بدون لایسنس به پوسته نرسد. هر خطای غیرمنتظره،
    /// مطابق سیاست fail-open، پوسته را باز می‌گذارد (قفل‌شدن درمانگاه ممنوع).
    /// </summary>
    private async Task StartupWithLicenseGateAsync(Views.SplashWindow? splash = null)
    {
        try
        {
            var api = Services.GetRequiredService<IApiClient>();
            // مهلت کوتاه بررسی — در شبکهٔ معلق، پس از ~۸ ثانیه وضعیت «نامعلوم»
            // فرض شده و پوسته (fail-open) باز می‌شود تا کار درمانگاه نایستد
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
            var verdict = await LicenseGate.CheckAsync(api, cts.Token);
            splash?.ReportProgress(85);

            if (verdict == LicenseGate.Verdict.Invalid
                && !Views.LicenseActivationWindow.OpenAndActivate(null))
            {
                // فعال‌سازی انجام نشد — توکن همین اجرا پاک می‌شود و ورود مجدد
                // از همان گیت لایسنس می‌گذرد (چرخهٔ ۴۲: نمایش ورود از متد کمکی)
                Services.GetRequiredService<ITokenManager>().Clear();
                ShowLoginWindow();
                splash?.CloseWithFade();
                return;
            }

            // ═══ چرخهٔ ۴۶ (گزارش مالک: «برنامه پس از اسپلش بدون دلیل خارج
            // می‌شود و صفحه لاگین/اصلی نمی‌آید») — ریشه‌یابی بحرانی ═══
            // گارد قبلی چرخهٔ ۴۲ «if (MainWindow is null)» هرگز true نمی‌شد:
            // WPF خاصیت Application.MainWindow را خودکار روی «اولین پنجره‌ای
            // که Show می‌شود» ست می‌کند — یعنی همان SplashWindow. در نتیجه:
            //   ۱) ShowMainWindow() هرگز اجرا نمی‌شد و پوستهٔ اصلی باز نمی‌شد
            //   ۲) CloseWithFade اسپلش را می‌بست؛ و چون ShutdownMode برنامه
            //      «OnMainWindowClose» است و MainWindowِ برنامه همان اسپلشِ
            //      تازه‌بسته‌شده بود، کل برنامه بی‌صدا Shutdown می‌شد.
            // فیکس: گارد حذف شد؛ ShowMainWindow خودش idempotent است — نخست
            // پنجرهٔ زندهٔ موجود را از Current.Windows برمی‌دارد (فقط Activate)
            // و در نبودش نمونهٔ نو می‌سازد. اینجا همچنین MainWindow برنامه
            // روی پنجرهٔ واقعی ست می‌شود تا بستن اسپلش هرگز خروج معنا نگیرد.
            ShowMainWindow();
            splash?.ReportProgress(100);
            splash?.CloseWithFade();
        }
        catch
        {
            // fail-open — در خطای غیرمنتظره پوستهٔ اصلی باز می‌شود.
            // چرخهٔ ۴۶: همان گارد معیوب بالا اینجا هم بود — MainWindow هرگز
            // null نبود (روی اسپلش ست شده بود) پس در استثنا هیچ پنجره‌ای باز
            // نمی‌شد و برنامه روی اسپلش هنگ می‌کرد. اکنون بی‌قید‌وشرط پوسته
            // باز می‌شود (ShowMainWindow روی پنجرهٔ از قبل باز فقط Activate است)
            // و اسپلش هم با محو بسته می‌شود تا مسیر خطا هم به بن‌بست نخورد.
            ShowMainWindow();
            splash?.CloseWithFade();
        }
    }

    // ═══ چرخهٔ چهل‌ودوم (گزارش مالک) — کارخانهٔ نمایش پنجره‌های نشست ═══
    // ریشهٔ خطای «خروج از حساب» (Cannot call Show after a Window has closed):
    // پنجره‌های WPF پس از Close دیگر قابل نمایش نیستند؛ DI تک‌نمونه نمونهٔ
    // بسته را دوباره تحویل می‌داد. این دو متد الگوی صحیح را تثبیت می‌کنند:
    // نخست پنجرهٔ «زندهٔ» هم‌نوع از مجموعهٔ پنجره‌های باز برنامه برداشته
    // می‌شود (تعویض نشست دوباره‌کاری نمی‌سازد و فوکوس به همان می‌آید)؛ فقط
    // در نبودِ آن، نمونهٔ تازه از DI (اکنون Transient) ساخته می‌شود. همیشه
    // MainWindow هم روی پنجرهٔ تازه ست می‌شود تا ShutdownMode=OnLastWindowClose
    // در گذارهای نشست، برنامه را نبندد.

    /// <summary>
    /// نمایش پنجرهٔ ورود — نمونهٔ زنده اگر باشد وگرنه ساخت نمونهٔ نو.
    /// </summary>
    /// <returns>پنجرهٔ ورودِ هم‌اکنون قابل مشاهده</returns>
    public static Views.LoginWindow ShowLoginWindow()
    {
        // پنجرهٔ بسته از Current.Windows حذف می‌شود؛ پس یافتهٔ اینجا همیشه
        // قابل Show است (Show روی پنجرهٔ از قبل باز هم بی‌ضرر و بی‌اثر است)
        var login = Current.Windows.OfType<Views.LoginWindow>().FirstOrDefault()
                    ?? Services.GetRequiredService<Views.LoginWindow>();
        Current.MainWindow = login;
        login.Show();
        login.Activate(); // اگر از قبل باز بود، فقط فوکوس می‌گیرد
        return login;
    }

    /// <summary>
    /// نمایش پنجرهٔ اصلی — نمونهٔ زنده اگر باشد وگرنه ساخت نمونهٔ نو.
    /// ═══ چرخهٔ چهل‌وچهارم — شروع/نوسازی نشست با هر نمایش ═══
    /// گزارش مالک: «خروج از حساب و ورود با کاربر دیگر، همچنان کاربر قبلی
    /// را نشان می‌دهد». MainWindowViewModel تک‌نمونه است و پیش‌تر کل
    /// راه‌اندازی نشست در سازنده‌اش بود که فقط یک‌بار (برای نخستین کاربر)
    /// اجرا می‌شد. اکنون هر نمایش پنجرهٔ اصلی، StartSessionAsync همان
    /// ViewModel را صدا می‌زند: نام/عکس کاربر، بازسازی منو با پالایش
    /// دسترسی کاربر تازه، میانبرهای داشبورد او، اتصال بلادرنگ و وضعیت
    /// لایسنس — نخستین ورود و هر ورود پس از خروج از حساب، یکسان تازه.
    /// </summary>
    /// <returns>پنجرهٔ اصلیِ هم‌اکنون قابل مشاهده</returns>
    public static MainWindow ShowMainWindow()
    {
        var main = Current.Windows.OfType<MainWindow>().FirstOrDefault()
                   ?? Services.GetRequiredService<MainWindow>();
        Current.MainWindow = main;
        main.Show();
        main.Activate();

        // شروع/نوسازی نشست — fire-and-forget؛ خطایش پوسته را نمی‌شکند
        // (همهٔ بارگذاری‌های درونش بهترین‌تلاش‌اند و خودشان خطا را می‌بلعند)
        if (main.DataContext is ViewModels.MainWindowViewModel vm)
            _ = vm.StartSessionAsync();
        return main;
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
        // ═══ چرخهٔ چهل‌ودوم (گزارش مالک) — پنجره‌ها دیگر Singleton نیستند ═══
        // گزارش مالک: خطای «خروج از حساب» — InvalidOperationException با پیام
        // «Cannot set Visibility or call Show… after a Window has closed».
        // ریشه: LoginWindow/MainWindow در DI تک‌نمونه (Singleton) بودند؛ پس از
        // لاگین موفق، LoginWindow واقعاً Close می‌شود و «خروج از حساب» همان
        // نمونهٔ بسته را از DI می‌گیرد — WPF روی پنجرهٔ بسته Show را ممنوع
        // می‌کند. MainWindow هم در لاگین دوم دچار همین وضع می‌شد (CloseForLoginSwitch
        // آن را می‌بندد). ثبت Transient + متدهای کمکی App.ShowLoginWindow/
        // ShowMainWindow (که نخست پنجرهٔ زندهٔ موجود را می‌یابند) هر دو عارضه
        // را برای همیشه می‌بندد؛ ViewModelها تک‌نمونه ماندند تا وضعیت پوسته
        // (تابع‌ها/اشتراک‌ها) بین تعویض پنجره حفظ شود.
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
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
        // چرخهٔ ۴۱ — صفحهٔ مستقل لوازم مصرفی درمانگاه (بدون نیاز به ماژول
        // داروخانه؛ اندپوینت‌های api/v1/consumables خارج از گیت لایسنس)
        services.AddTransient<ConsumablesViewModel>();
        services.AddTransient<VisitsViewModel>();
        // چرخهٔ بیست‌وهفتم — میزکار پزشک (صفحهٔ ویزیت پزشک — گزارش هجدهم مالک)
        services.AddTransient<DoctorVisitViewModel>();
        services.AddTransient<PrescriptionsViewModel>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<AccountingViewModel>();
        services.AddTransient<InsuranceViewModel>();
        // ═══ چرخهٔ ۴۳ — تفکیک پاراکلینیک به چهار صفحهٔ مستقل (گزارش مالک) ═══
        services.AddTransient<DentistryViewModel>();
        services.AddTransient<OphthalmologyViewModel>();
        services.AddTransient<PhysiotherapyViewModel>();
        services.AddTransient<ImagingViewModel>();
        services.AddTransient<LabViewModel>();
        services.AddTransient<PharmacyViewModel>();
        services.AddTransient<StocktakingViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportBrowserViewModel>();
        services.AddTransient<AdminViewModel>();
        // چرخه بیستم (گزارش چهاردهم مالک): صفحهٔ مستقل «مدیریت پرسنل» —
        // آیتم منوی پرسنل پیش‌تر به صفحهٔ تنظیمات می‌رفت
        services.AddTransient<PersonnelViewModel>();
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
        // چرخهٔ سی‌ونه‌م (گزارش مالک): صفحهٔ منوی کناری «مدیریت اتاق‌ها» — همان
        // ویومدل پنجرهٔ فرم پزشک، این‌بار به‌صورت صفحهٔ اصلی (RoomsView)
        services.AddTransient<RoomsManagerViewModel>();
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
