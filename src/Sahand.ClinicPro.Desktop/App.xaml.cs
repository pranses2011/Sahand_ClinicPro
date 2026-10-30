using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Threading;
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

        // ═══ چرخهٔ ۷۹ (ریشه‌کنی «گزارش‌های همه-صفر» و «سال ۰۷۸۴ لیست انتظار») ═══
        // فرهنگ جاری رشته‌ها فارسی می‌شود — پارس ورودی‌های عددی ارقام فارسی را
        // می‌پذیرد و قالب‌بندی پیش‌فرض اعداد ارقام فارسی می‌گیرد.
        // ⚠️ درس حیاتی این چرخه: تقویمِ پیش‌فرضِ فرهنگ fa-IR در NET 8
        // «PersianCalendar» است؛ در نتیجه هر «ToString("yyyy-MM-dd")» اجزای
        // «جلالی» تولید می‌کرد (مثلاً 1405-07-22 به‌جای 2026-10-14) و سرور آن
        // رشته را سال ۱۴۰۵ «میلادی» می‌پنداشت → بازهٔ تهی → همهٔ گزارش‌ها صفر
        // («در این بازه ردیفی ثبت نشده است») و بازهٔ دلخواه لیست انتظار با سال
        // ۰۷۸۴ ثبت می‌شد (۱۴۰۵−۶۲۱=۰۷۸۴ در نمایش مجدد شمسی).
        // راه‌حل استاندارد مایکروسافتی: فرهنگ fa-IR «نوشتنی» ساخته می‌شود و فقط
        // «تقویم قالب تاریخ» آن به میلادی برمی‌گردد — ارقام فارسی، جداکننده‌ها و
        // پارس اعداد فارسی دست‌نخورده می‌مانند؛ اما قالب‌بندی پیش‌فرض تاریخها
        // همیشه میلادی/ISO است. نمایش شمسی در سراسر برنامه از ابتدا از طریق
        // PersianDateHelper (تبدیل صریح) انجام می‌شود و بی‌تغییر می‌ماند.
        var fa = new System.Globalization.CultureInfo("fa-IR");
        fa.DateTimeFormat.Calendar = new System.Globalization.GregorianCalendar();
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = fa;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = fa;

        // ═══ چرخهٔ ۷۴ (گزارش مالک) — هدایت چرخ موس به والد قابل‌اسکرول ═══
        // «روی بعضی از عناصر که اسکرول ندارند، wheel عمل نمی‌کند و صفحه اسکرول
        // نمی‌شود مثل اینکه قفل شده باشد» — عناصر دارای ScrollView داخلی
        // (جدول‌ها/لیست‌ها) رویداد چرخ را حتی در ناتوانی می‌خورند؛ این هدایت‌گر
        // سراسری در سطح پنجره، چرخش را به نخستین والدِ توانمند می‌رساند.
        Helpers.WheelScrollForwarder.Register();
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
        // کرده و تغییرات ذخیره‌نشده دارد، هنگام ESC ابتدا تأیید فارسی خود
        // برنامه پرسیده می‌شود — با دیالوگ برنامه (قانون مالک) نه کادر پیام
        // ویندوز. (چرخهٔ ۷۸: این بررسی فقط روی مسیر ESC می‌ماند.)
        if (window is not Sahand.ClinicPro.Desktop.MainWindow
            && window is not Views.LoginWindow)
        {
            // ═══ چرخهٔ ۷۵ (گزارش مالک) — اسنپ‌شات خودکار تغییرات فرم ═══
            // «قانون بررسی تغییر شدن فرم که برای کلید ESC بود عمل نمیکنه —
            // چک نمیکنه ببینه که اطلاعات فرم تغییر کرده یا نه» — ردیاب
            // IHasUnsavedChanges فقط در دو فرم نشسته بود؛ اکنون پوسته برای
            // «هر» پنجرهٔ فرم، در نخستین لحظهٔ رندر کامل یک اسنپ‌شات از
            // DataContext می‌گیرد (FormSnapshot) و هنگام ESC مقایسه
            // می‌کند: متفاوت = تأیید فارسی. تأخیر اولیه (Background) یعنی
            // پس از پایان بارگذاری‌های اولیه؛ فرم‌های دارای قرارداد صریح همچنان
            // رفتار غنی‌تر خود را حفظ می‌کنند (اولویت اول).
            // ═══ چرخهٔ ۷۸ (بند مالک: «با بیرون رفتن از فرم‌ها بدون زدن کلید ESC
            // هم خطا رو نشون میده») — دو اصلاح ریشه‌ای ═══
            // ۱) دوباره‌ثبتی مبنا: بارگذاری‌های async فرم (لیست پزشکان/خدمات و…)
            //     پیش‌تر «بعد از» اسنپ‌شات اولیه می‌رسیدند و فرمِ دست‌نخورده را
            //     «کثیف» جلوه می‌دادند؛ حالا ~۲.۵ ثانیه بعد — پس از نشستن
            //     داده‌های async — مبنا یک‌بار دیگر تازه می‌شود؛ اگر کاربر در این
            //     مدت شروع به تایپ کرده باشد تازه‌سازی لغو می‌شود تا ورودی او
            //     بلعیده نشود.
            // ۲) دامنهٔ قانون فقط ESC: بستن با دکمهٔ X یا دکمه‌های انصراف عمدی
            //     است و بی‌سؤال بسته می‌شود (درخواست صریح مالک — رویداد
            //     Closing دیگر بررسی نمی‌کند).
            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                window.SetValue(FormBaselineProperty, ViewModels.FormSnapshot.Capture(window.DataContext));
            }), System.Windows.Threading.DispatcherPriority.Background);

            // تازه‌سازی دوم مبنا — پس از نشستن بارگذاری‌های async فرم
            var userTyped = false; // اولین تایپِ واقعی کاربر، تازه‌سازی را لغو می‌کند
            window.PreviewTextInput += (_, _) => userTyped = true;
            var baselineRefresher = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.5)
            };
            baselineRefresher.Tick += (_, _) =>
            {
                baselineRefresher.Stop();
                if (!userTyped && window.IsLoaded)
                    window.SetValue(FormBaselineProperty,
                        ViewModels.FormSnapshot.Capture(window.DataContext));
            };
            baselineRefresher.Start();

            window.PreviewKeyDown += (_, ke) =>
            {
                if (ke.Key == System.Windows.Input.Key.Escape)
                {
                    // ═══ چرخهٔ ۷۴ (گزارش مالک) — لایه‌بندی ESC ═══
                    // «وقتی کنترل انتخابگر تاریخ و یا انتخابگر ساعت رو باز
                    // میکنم و میخوام ببندمش، با زدن کلید ESC به جای اینکه اول
                    // کنترل باز شده رو ببنده و بعدش اگه دوباره زدم فرم رو
                    // ببنده، از همون اول فرم رو میبنده (برای کومبوها هم صدق می‌کند)».
                    // ریشه: PreviewKeyDown از ریشه (پنجره) «تونل» می‌خورد — هندلر
                    // پنجره همیشه پیش از کنترل‌های درونی اجرا می‌شد و فرم را
                    // می‌بست. رفع: اگر در درخت بصری پنجره «سطح درونی بازی»
                    // (پاپ‌آپ تقویم/ساعت، دراپ‌داون کومبو، منوی باز) باشد، این‌بار
                    // پنجره کاری نمی‌کند و رویداد به کنترل درونی تونل می‌خورد تا
                    // خودش ESC را بخورد و سطح خودش را ببندد؛ ESC بعدی فرم را
                    // می‌بندد — دقیقاً رفتار خواسته‌شده.
                    if (HasOpenOverlaySurface(window))
                        return;

                    // تغییرات ذخیره‌نشده؟ — ابتدا تأیید کاربر
                    if (!ConfirmCloseIfDirty(window))
                        return;
                    window.Close();
                    ke.Handled = true;
                }
            };

            // ═══ چرخهٔ ۷۸ — دامنهٔ قانون «تغییرات ذخیره‌نشده» فقط ESC است ═══
            // درخواست صریح مالک: «قانون بررسی تغییر کردن فرم که برای کلید ESC
            // بود درست عمل نمیکنه — با بیرون رفتن از فرم‌ها بدون زدن کلید ESC
            // هم خطا رو نشون میده». بستن با دکمهٔ X یا دکمه‌های انصراف، اقدامی
            // عمدی و آگاهانه است و بی‌سؤال بسته می‌شود؛ تنها ESC — میان‌برِ
            // سریعِ بستن — است که پیش از بستن، تأیید «تغییرات ذخیره‌نشده»
            // می‌گیرد (رویداد Closing قبلاً همین بررسی را تکرار می‌کرد و
            // حذف شد — پرچم CloseAllowed هم بی‌استفاده و حذف شد).
        }
        // مقیاس زوم به PageHost اعمال می‌شود (نه به ریشهٔ پنجره): اعمال
        // LayoutTransform روی کل ریشه، لایه‌های هم‌پوشان مانند ToastLayer را
        // همراه محتوای صفحه تغییر اندازه می‌دهد و می‌تواند آن‌ها را در لبهٔ
        // پنجره ببرد. MainWindow فقط محتوای صفحه را مقیاس می‌کند؛ نوارها و
        // اعلان‌ها در فضای واقعی پنجره باقی می‌مانند.
    }

    /// <summary>پرچم جلوگیری از وصل‌شدن دوبارهٔ فونت به همان پنجره</summary>
    private static readonly DependencyProperty FontHookedProperty =
        DependencyProperty.RegisterAttached("FontHooked", typeof(bool), typeof(App), new PropertyMetadata(false));

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — اسنپ‌شات مبنا DataContext هر پنجرهٔ فرم ═══
    /// در نخستین لحظهٔ باز شدن گرفته می‌شود و هنگام ESC/بستن با وضعیت فعلی
    /// مقایسه می‌شود (ConfirmCloseIfDirty — مسیر ۲). تهی یعنی «بدون پایش».
    /// </summary>
    private static readonly DependencyProperty FormBaselineProperty =
        DependencyProperty.RegisterAttached("FormBaseline", typeof(string), typeof(App), new PropertyMetadata(null));

    /// <summary>
    /// آیا «سطح درونی بازی» در پنجره وجود دارد که ESC باید نخست آن را ببندد؟ — چرخهٔ ۷۴.
    /// سطوح جست‌وجو‌شده در درخت بصری پنجره:
    ///   ۱) پاپ‌آپ باز (تقویم انتخابگر تاریخ، صفحهٔ انتخابگر ساعت و هر Popup
    ///      دیگری که IsOpen=true دارد — مثل پاپ‌آپ‌های پیش‌نمایش/انتخاب)؛
    ///   ۲) کومبوباکس با دراپ‌داون باز (ComboBox استاندارد و متریال خودش ESC را
    ///      می‌خورد و لیست را می‌بندد)؛
    ///   ۳) منوی زمینه/منوی باز (ContextMenu.IsOpen).
    /// توجه: پاپ‌آپ جست‌وجو از طریق منطق بازشدن کار می‌کند — «باز» یعنی همین
    /// لحظه در پنجره فعال است و ESC به‌طور طبیعی باید نخست آن را ببندد.
    /// </summary>
    /// <param name="window">پنجرهٔ فعال</param>
    /// <returns>true یعنی سطح درونی بازی هست؛ ESC نباید فرم را ببندد</returns>
    private static bool HasOpenOverlaySurface(Window window)
    {
        try
        {
            // اگر تمرکز صفحه‌کلید در پنجرهٔ دیگری است (مثلاً پاپ‌آپِ پنجرهٔ دیگر)،
            // پنجرهٔ جاری سطح بازی ندارد
            if (!ReferenceEquals(System.Windows.Input.Keyboard.FocusedElement, null))
            {
                // عنصر متمرکز باید داخل همین پنجره باشد وگرنه سطح بازی متعلق به
                // پنجرهٔ دیگری است که هندلر خودش آن را می‌بندد
                var focused = System.Windows.Input.Keyboard.FocusedElement as DependencyObject;
                var owner = FindAncestorWindow(focused);
                if (owner is not null && !ReferenceEquals(owner, window))
                    return false;
            }

            // پیمایش درخت بصری پنجره — یافتن پاپ‌آپ باز / کومبوی باز / منوی باز
            var found = false;
            VisitVisualTree(window, node =>
            {
                if (found) return;
                switch (node)
                {
                    case System.Windows.Controls.Primitives.Popup
                        { IsOpen: true } p when p.Child is not System.Windows.Controls.ToolTip:
                        // پاپ‌آپ باز — تقویم/ساعت/پیش‌نمایش؛ پاپ‌آپِ tooltip مستثناست
                        // چون خودش با حرکت موس بسته می‌شود و نباید ESC را نگه دارد
                        found = true;
                        break;
                    case System.Windows.Controls.ComboBox { IsDropDownOpen: true }:
                        found = true;
                        break;
                    case System.Windows.Controls.ContextMenu { IsOpen: true }:
                        found = true;
                        break;
                }
            });
            return found;
        }
        catch
        {
            // هر خطای پیمایش (درخت در حال تغییر) — رفتار امن: فرم بسته نشود این‌بار
            return true;
        }
    }

    /// <summary>پیمایش عمقی درخت بصری — بازدید همهٔ فرزندان بصری عنصر</summary>
    private static void VisitVisualTree(DependencyObject root, Action<DependencyObject> visit)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            visit(child);
            VisitVisualTree(child, visit);
        }
    }

    /// <summary>یافتن پنجرهٔ مالک عنصر — برای اطمینان از تعلق تمرکز به همین پنجره</summary>
    private static Window? FindAncestorWindow(DependencyObject? node)
    {
        while (node is not null)
        {
            if (node is Window w)
                return w;
            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }
        return null;
    }

    /// <summary>
    /// بررسی تغییرات ذخیره‌نشده پیش از بستن پنجره — چرخهٔ بیست‌وششم:
    /// اگر ViewModel فرم قرارداد IHasUnsavedChanges را پیاده کرده و کثیف
    /// است، تأیید دوگزینه‌ای فارسی نمایش داده می‌شود؛ انصراف کاربر یعنی
    /// «فرم باز بماند». فرم‌های بدون قرارداد (نمایشی/مرورگر) بی‌تأیید
    /// بسته می‌شوند — رفتار پیشین.
    /// </summary>
    private static bool ConfirmCloseIfDirty(Window window)
    {
        // ۱) قرارداد صریح فرم (چرخهٔ ۲۶) — دقیق‌ترین منبع؛ نام فرم را هم می‌داند
        if (window.DataContext is ViewModels.IHasUnsavedChanges { HasUnsavedChanges: true } dirty)
        {
            // دیالوگ فارسی خود برنامه — نه کادر پیام ویندوز (قانون مالک)
            var answer = Views.AppDialog.Confirm(
                "تغییرات ذخیره‌نشده",
                $"در «{dirty.UnsavedChangesTitle}» تغییراتی وارد کرده‌اید که ذخیره نشده است.\n" +
                "فرم بدون ذخیره بسته شود؟",
                Views.AppDialogIcon.Warning);
            return answer == true;
        }

        // ۲) ═══ چرخهٔ ۷۵ — اسنپ‌شات عمومی ═══
        // فرم‌های بدون قرارداد هم پایش می‌شوند: اسنپ‌شات مبنا (لحظهٔ باز شدن)
        // با وضعیت فعلی DataContext مقایسه می‌شود؛ تفاوت = ورودی کاربر = تأیید.
        // پنجره‌های بدون DataContext/فقط‌خواندنی (لیست‌ها) اسنپ‌شاتشان هرگز
        // تغییر نمی‌کند و بی‌صدا رد می‌شوند.
        var baseline = window.GetValue(FormBaselineProperty) as string;
        if (baseline is null)
            return true; // اسنپ‌شات نیست (مثلاً پنجرهٔ فقط‌خواندنی) — بستن بی‌تأیید
        var current = ViewModels.FormSnapshot.Capture(window.DataContext);
        if (ViewModels.FormSnapshot.IsSame(baseline, current))
            return true; // هیچ چیز عوض نشده — بستن بی‌تأیید

        var formTitle = window.Title;
        var answer2 = Views.AppDialog.Confirm(
            "تغییرات ذخیره‌نشده",
            $"در «{formTitle}» تغییراتی وارد کرده‌اید که احتمالاً ذخیره نشده است.\n" +
            "فرم بدون ذخیره بسته شود؟",
            Views.AppDialogIcon.Warning);
        return answer2 == true;
    }

    /// <summary>ثبت خطای گرفته‌شده در فایل crash.log کنار تنظیمات کاربر</summary>
    private static void LogCrash(string source, Exception? exception)
    {
        try
        {
            var dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SahandClinicPro");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "crash.log"),
                $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] ({source}) {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* ثبت خطا نباید خودش خطا بسازد */ }
    }


    /// <summary>رندر واقعی صفحهٔ تصویربرداری با دادهٔ نمونه و ذخیرهٔ اسکرین‌شات PNG</summary>
    private async Task RenderImagingScreenshotAsync(string path)
    {
        try
        {
            var vm = Services.GetRequiredService<ImagingViewModel>();
            var today = DateTime.Today;
            ImagingViewModel.ImagingOrderRow Row(long id, string name, string modality, string part,
                int status, int priority, double hour) =>
                new(id, 1000 + id, name, modality, part, status, priority,
                    "درد و محدودیت حرکت", null, null, null, null, null,
                    today.AddHours(hour), status >= 3 ? today.AddHours(hour + 1) : null,
                    status == 4 ? today.AddHours(hour + 2) : null, "زن", "۱۳۷۰/۰۵/۱۲");
            vm.ImagingOrders.Add(Row(1, "زهرا محمدی", "CT", "مغز بدون تزریق", 1, 1, 8.1));
            vm.ImagingOrders.Add(Row(2, "علی رضایی", "MRI", "ستون فقرات کمری", 1, 1, 8.4));
            vm.ImagingOrders.Add(Row(3, "مریم کریمی", "Ultrasound", "شکم و لگن", 2, 3, 9.0));
            vm.ImagingOrders.Add(Row(4, "حسن احمدی", "XRay", "قفسه سینه", 2, 3, 9.3));
            vm.ImagingOrders.Add(Row(5, "سارا حسینی", "MRI", "زانوی راست", 1, 3, 10.0));
            vm.ImagingOrders.Add(Row(6, "رضا صادقی", "CT", "سینوس‌ها", 2, 3, 10.5));
            vm.ImagingOrders.Add(Row(7, "نگار عزیزی", "Mammography", "غربالگری دوطرفه", 1, 3, 11.0));
            vm.ImagingOrders.Add(Row(8, "امیر جلالی", "CT", "شکم با تزریق", 3, 3, 8.2));
            vm.ImagingOrders.Add(Row(9, "لیلا مرادی", "Ultrasound", "تیروئید", 3, 3, 9.1));
            vm.ImagingOrders.Add(Row(10, "جواد نادری", "XRay", "مچ دست چپ", 3, 3, 10.2));
            vm.ImagingOrders.Add(Row(11, "هانیه رستمی", "MRI", "مغز با تزریق", 3, 3, 11.2));
            vm.ImagingOrders.Add(Row(12, "مهدی کاظمی", "CT", "قفسه سینه", 4, 3, 7.5));
            vm.ImagingOrders.Add(Row(13, "فاطمه نجفی", "Ultrasound", "بارداری", 4, 3, 8.0));
            vm.ImagingOrders.Add(Row(14, "داوود شریفی", "XRay", "شانه راست", 4, 3, 8.6));
            vm.ImagingOrders.Add(Row(15, "الهه موسوی", "MRI", "شانه چپ", 4, 3, 9.6));
            vm.ImagingOrders.Add(Row(16, "کاوه بهرامی", "CT", "گردن با تزریق", 4, 3, 10.6));
            vm.ImagingOrders.Add(Row(17, "رؤیا صالحی", "Ultrasound", "کبد و کیسه صفرا", 5, 3, 7.8));
            vm.ImagingOrders.Add(Row(18, "پارسا یوسفی", "XRay", "لگن", 5, 3, 8.8));
            vm.WorkFilter = "wait";
            vm.WorkFilter = "all";

            Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-IR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fa-IR");
            var view = new ImagingView
            {
                DataContext = vm,
                Language = XmlLanguage.GetLanguage("fa-IR"),
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF7)),
                FontFamily = new FontFamily("Tahoma"),
                FlowDirection = FlowDirection.RightToLeft
            };
            var window = new Window
            {
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF7)),
                Content = view,
                Width = 1020,
                Height = 745,
                Topmost = true,
                WindowStyle = WindowStyle.None,
                // حاشیهٔ تغییر اندازهٔ پیشفرض WPF (چند پیکسل از کاغذدیواری
                // ویندوز را در لبهٔ چپ عکس نشان میداد) — حذف کامل.
                ResizeMode = ResizeMode.NoResize,
                BorderThickness = new Thickness(0),
                ShowInTaskbar = false,
                Left = 0,
                Top = 0,
                Title = "اسکرین‌شات راستی‌آزمایی تصویربرداری",
                FlowDirection = FlowDirection.RightToLeft
            };
            window.Show();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(700);
            var width = Math.Max(1, (int)Math.Ceiling(view.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(view.ActualHeight));
            File.WriteAllText(path + ".txt",
                $"Flow={view.FlowDirection} Lang={view.Language} Actual={view.ActualWidth}x{view.ActualHeight} Render={view.RenderSize.Width}x{view.RenderSize.Height} Desired={view.DesiredSize.Width}x{view.DesiredSize.Height}");
            // اسکرین‌شات واقعی از روی صفحه (همان خروجی‌ای که کاربر روی مانیتور می‌بیند،
            // با آینه‌ای‌شدن راست‌به‌چپِ سطح پنجره و شکل‌دهی حروف) — RenderTargetBitmap
            // این دو را ندارد و خروجی‌اش با اجرای واقعی فرق می‌کرد.
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var left = (int)window.Left; var top = (int)window.Top;
            using var screen = new System.Drawing.Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(screen))
                g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
            screen.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            window.Close();
            Shutdown(0);
        }
        catch (Exception ex)
        {
            LogCrash("اسکرین‌شات تصویربرداری", ex);
            try { File.WriteAllText(path + ".error.txt", ex.ToString()); } catch { }
            Shutdown(1);
        }
    }

    /// <summary>کلیدهای صفحاتی که حالت راستیآزمایی تصویری میتواند رندر کند.</summary>
    internal static readonly string[] ScreenshotPageKeys =
    {
        "dentistry", "ophthalmology", "physiotherapy", "survey", "survey-results",
        "print-template", "print-settings", "print-designer", "imaging"
    };

    /// <summary>
    /// نشاندن همان پنج عدد نمونهای طرح تأییدی روی کارتهای آماری صفحه،
    /// تا اسکرینشات WPF و رندر HTML طرح دقیقاً یکسان دیده شوند.
    /// </summary>
    private static void SetDesignKpis(ViewModels.DesignKpis k,
        string v1, string v2, string v3, string v4, string v5)
    {
        k.V1 = v1; k.V2 = v2; k.V3 = v3; k.V4 = v4; k.V5 = v5;
    }

    /// <summary>
    /// رندر **واقعی** یکی از صفحات برنامه (همان درخت بصری WPF که کاربر میبیند)
    /// و ذخیرهٔ اسکرینشات PNG — ویژهٔ چرخهٔ مقایسه با طرح تأییدی مالک.
    /// فقط با آرگومان صریح <c>--screenshot &lt;کلید&gt; &lt;مسیر&gt;</c> فعال
    /// میشود؛ بدون ورود/سرور بالا میآید و دادهٔ نمونهٔ فارسی روی فرم مینشاند.
    /// </summary>
    private async Task RenderPageScreenshotAsync(string page, string path)
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fa-IR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fa-IR");

            UserControl view;
            object viewModel;
            var winWidth = 1180;
            var winHeight = 800;

            switch (page.ToLowerInvariant())
            {
                case "dentistry":
                {
                    var m = Services.GetRequiredService<DentistryViewModel>();
                    SetDesignKpis(m.Kpis, "۱۲۴", "۱۸", "۷", "۵", "۴۸٫۵M");
                    // دادهٔ نمونهٔ چارت — رنگها همان وضعیتهای واقعی برنامهد
                    for (var i = 0; i < m.UpperTeeth.Count; i++)
                        m.UpperTeeth[i].Condition = i switch { 2 => 1, 5 => 2, 9 => 3, 13 => 4, _ => 0 };
                    for (var i = 0; i < m.LowerTeeth.Count; i++)
                        m.LowerTeeth[i].Condition = i switch { 1 => 2, 4 => 1, 11 => 5, _ => 0 };
                    m.SelectedTooth = m.UpperTeeth.Count > 2 ? m.UpperTeeth[2] : null;
                    m.DentalPlan.Add(new DentalPlanRow(16, "درمان ریشه (۴ کانال)", 4500000m, "در انتظار", "#D97706", "O", "۱۴۰۵/۰۸/۱۲"));
                    m.DentalPlan.Add(new DentalPlanRow(14, "ترمیم کامپوزیت", 1200000m, "انجامشده", "#059669", "M", "۱۴۰۵/۰۷/۲۸"));
                    m.DentalPlan.Add(new DentalPlanRow(36, "روکش سرامیکی", 8000000m, "در انتظار", "#D97706", "—", "بدون نوبت"));
                    viewModel = m;
                    view = new DentistryView { DataContext = m };
                    break;
                }
                case "ophthalmology":
                {
                    var m = Services.GetRequiredService<OphthalmologyViewModel>();
                    SetDesignKpis(m.Kpis, "۸", "۲", "۶", "۴", "۹۶");
                    m.OdSphere = "-2.50"; m.OdCylinder = "-0.75"; m.OdAxis = "180"; m.OdAdd = "+1.50"; m.OdIop = "۱۴";
                    m.OsSphere = "-3.00"; m.OsCylinder = "-1.00"; m.OsAxis = "175"; m.OsAdd = "+1.50"; m.OsIop = "۲۳";
                    m.Pd = "۶۲";
                    m.EyeFindings = "حدت بینایی دو چشم با عینک کامل؛ فشار چشم چپ بالاتر از حد طبیعی — پیگیری گلوکوم.";
                    m.SlitOd = "قرنیه شفاف، اتاق قدامی عمیق، عدسی شفاف.";
                    m.SlitOs = "قرنیه شفاف، کاپ دیسک ۰٫۶ — نیازمند OCT.";
                    m.PachymetryOd = "۵۴۲"; m.PachymetryOs = "۵۳۸";
                    m.VisualFieldText = "میدان بینایی طبیعی";
                    m.DiagnosisText = "آستیگماتیسم مرکب دوطرفه · مشکوک به گلوکوم چشم چپ";
                    m.CarePlanText = "OCT عصب بینایی + تونومتری مجدد طی دو هفته";
                    m.DrugPrescriptionText = "قطرهٔ تیمولول ۰٫۵٪ — یک قطره صبحگاهی چشم چپ";
                    m.FollowUpText = "دو هفتهٔ دیگر";
                    m.EyeHistory.Add(new EyeExamRow("۱۴۰۵/۰۸/۰۳", "OD: -2.50 / -0.75 × 180", "OS: -3.00 / -1.00 × 175", "IOP چپ ۲۳"));
                    m.EyeHistory.Add(new EyeExamRow("۱۴۰۵/۰۵/۱۸", "OD: -2.25 / -0.50 × 180", "OS: -2.75 / -0.75 × 175", "طبیعی"));
                    m.EyeHistory.Add(new EyeExamRow("۱۴۰۴/۱۱/۲۲", "OD: -2.00 / -0.50 × 180", "OS: -2.50 / -0.75 × 175", "طبیعی"));
                    viewModel = m;
                    view = new OphthalmologyView { DataContext = m };
                    break;
                }
                case "physiotherapy":
                {
                    var m = Services.GetRequiredService<PhysiotherapyViewModel>();
                    SetDesignKpis(m.Kpis, "۹", "۲", "۲۴", "۴۱", "۶۸٪");
                    m.TotalSessions = 12;
                    m.ActiveProtocolName = "کمردرد مزمن — پروتکل ۱۲ جلسهای";
                    m.ProtocolGoal = "کاهش درد به زیر ۳ و بازگشت دامنهٔ حرکتی کمر";
                    m.SelectedZone = "کمر (لومبار)";
                    m.PainScore = 4;
                    m.PhysioNotes = "بهبود قابل توجه در خمشدن به جلو؛ ادامهٔ تمرینات ثباتدهندهٔ مرکزی.";
                    m.HomeProgram = "پلانک ۳×۳۰ ثانیه · کشش همسترینگ ۳×۳۰ ثانیه · پیادهروی ۲۰ دقیقه";
                    m.History.Add(new PhysioSessionDto { Id = 5, DateShamsi = "۱۴۰۵/۰۸/۰۵", SessionNumber = 5, PainScore = 4, Duration = 45, Treatments = "TENS · اولتراسوند · تمرینات درمانی" });
                    m.History.Add(new PhysioSessionDto { Id = 4, DateShamsi = "۱۴۰۵/۰۸/۰۱", SessionNumber = 4, PainScore = 5, Duration = 45, Treatments = "TENS · تمرینات درمانی" });
                    m.History.Add(new PhysioSessionDto { Id = 3, DateShamsi = "۱۴۰۵/۰۷/۲۷", SessionNumber = 3, PainScore = 6, Duration = 40, Treatments = "لیزر درمانی · ماساژ" });
                    m.History.Add(new PhysioSessionDto { Id = 2, DateShamsi = "۱۴۰۵/۰۷/۲۳", SessionNumber = 2, PainScore = 7, Duration = 40, Treatments = "اولتراسوند · تمرینات کششی" });
                    m.History.Add(new PhysioSessionDto { Id = 1, DateShamsi = "۱۴۰۵/۰۷/۱۹", SessionNumber = 1, PainScore = 8, Duration = 35, Treatments = "ارزیابی اولیه · TENS" });
                    viewModel = m;
                    view = new PhysiotherapyView { DataContext = m };
                    break;
                }
                case "survey":
                case "survey-results":
                {
                    var m = Services.GetRequiredService<SurveyViewModel>();
                    SetDesignKpis(m.Kpis, "۴۲٪", "۴٫۲/۵", "۳۱۲", "۲", "۱۸");
                    m.Surveys.Add(new SurveyListRow(1, "رضایت از ویزیت پزشک", "ارزیابی کیفیت ویزیت و برخورد پزشک", "رضایت بیماران", true, "فعال", 312, "۴٫۲/۵", null));
                    m.Surveys.Add(new SurveyListRow(2, "کیفیت پذیرش و برخورد", "نظرسنجی کوتاه پس از پذیرش", "کیفیت خدمات", true, "فعال", 194, "۴٫۵/۵", null));
                    m.Surveys.Add(new SurveyListRow(3, "نظرسنجی فصلی پاییز", "ویژهٔ بیماران مزمن", "ارزیابی پزشک", false, "پایان یافته", 428, "۴٫۳/۵", null));
                    m.TotalResponsesText = "۳۱۲";
                    m.AverageText = "۴٫۲/۵";
                    m.Last30Text = "۴۸";
                    m.Stars.Add(new StarBar(5, 148, 100, "★★★★★"));
                    m.Stars.Add(new StarBar(4, 96, 65, "★★★★"));
                    m.Stars.Add(new StarBar(3, 41, 28, "★★★"));
                    m.Stars.Add(new StarBar(2, 17, 12, "★★"));
                    m.Stars.Add(new StarBar(1, 10, 7, "★"));
                    m.DoctorRatings.Add(new DoctorRatingRow(1, "دکتر رضایی", 96, "۴٫۸", 96));
                    m.DoctorRatings.Add(new DoctorRatingRow(2, "دکتر کریمی", 64, "۴٫۶", 92));
                    m.DoctorRatings.Add(new DoctorRatingRow(3, "دکتر بهادری", 81, "۴٫۳", 86));
                    m.Comments.Add(new SurveyCommentRow(1, "زهرا محمدی", "دکتر رضایی", "۵ ستاره", "برخورد بسیار خوب و توضیحات کامل بود. ممنونم.", "۱۴۰۵/۰۸/۰۳"));
                    m.Comments.Add(new SurveyCommentRow(2, "علی احمدی", "دکتر کریمی", "۴ ستاره", "زمان انتظار کمی طولانی بود ولی ویزیت عالی بود.", "۱۴۰۵/۰۸/۰۲"));
                    m.Comments.Add(new SurveyCommentRow(3, "مریم حسینی", "دکتر بهادری", "۵ ستاره", "محیط تمیز و پرسنل مؤدب.", "۱۴۰۵/۰۷/۲۹"));
                    m.TabIndex = page.Equals("survey-results", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    viewModel = m;
                    view = new SurveyView { DataContext = m };
                    winHeight = 830;
                    break;
                }
                case "print-template":
                {
                    var m = Services.GetRequiredService<ComplementaryViewModel>();
                    SetDesignKpis(m.Kpis, "۳", "۶", "۲", "۹", "۱۴۰۵/۰۸/۰۳");
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 1, Key = "Invoice", Title = "فاکتور — سربرگ A5", TypeFa = "فاکتور و قبض پرداخت", PaperSize = "A5", IsActive = true, IsDefault = true, IsLoaded = true, IsDirty = true, Body = "<div class=\"header\">درمانگاه سهند</div>" });
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 2, Key = "Prescription", Title = "نسخه — سربرگ پزشک", TypeFa = "نسخه پزشکی", PaperSize = "A5", IsActive = false, Body = "" });
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 3, Key = "ReceptionReceipt", Title = "رسید پذیرش کوتاه", TypeFa = "رسید پذیرش", PaperSize = "A6", IsActive = false, Body = "" });
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 4, Key = "InsuranceForm", Title = "صورت حساب بیمه", TypeFa = "صورت حساب بیمه", PaperSize = "A4", IsActive = false, Body = "" });
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 5, Key = "LabResult", Title = "نتیجهٔ آزمایش", TypeFa = "آزمایشگاه", PaperSize = "A4", IsActive = true, Body = "" });
                    m.PrintTemplates.Add(new PrintTemplateRow { Id = 6, Key = "ImagingReport", Title = "گزارش تصویربرداری", TypeFa = "تصویربرداری", PaperSize = "A4", IsActive = true, Body = "" });
                    m.VisibleTemplates.Clear();
                    foreach (var r in m.PrintTemplates) m.VisibleTemplates.Add(r);
                    m.SelectedTemplate = m.PrintTemplates.First();
                    m.TabIndex = 0;
                    viewModel = m;
                    view = new ComplementaryView { DataContext = m };
                    winWidth = 1240;
                    break;
                }
                case "print-designer":
                {
                    var vars = new[] {
                        new ComplementaryViewModel.TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                        new ComplementaryViewModel.TemplateVariable("سند", "📄", "شماره فاکتور", "invoice_code"),
                        new ComplementaryViewModel.TemplateVariable("مالی", "🧾", "اقلام (جدول)", "items_html")
                    };
                    var sample = new FreeTemplateDocument { Width = 600, Height = 780, Elements = new() {
                        new() { Kind = "text", Text = "فاکتور نمونه — فاقد اعتبار", X = 80, Y = 35, Width = 440, Height = 45, FontSize = 20, Bold = true, Alignment = "center" },
                        new() { Kind = "line", X = 30, Y = 100, Width = 540, Height = 8 },
                        new() { Kind = "variable", Key = "patient_name", Text = "نام بیمار:", X = 300, Y = 130, Width = 250, Height = 40 },
                        new() { Kind = "variable", Key = "invoice_code", Text = "شماره:", X = 40, Y = 130, Width = 230, Height = 40 },
                        new() { Kind = "variable", Key = "items_html", Text = "اقلام:", X = 30, Y = 210, Width = 540, Height = 350 }
                    } };
                    var designer = new FreeTemplateEditorWindow(sample, vars, "A4", "Portrait");
                    var content = designer.Content;
                    designer.Content = null;
                    view = new UserControl { Content = content };
                    viewModel = designer;
                    break;
                }
                case "print-settings":
                {
                    var m = Services.GetRequiredService<SettingsViewModel>();
                    SetDesignKpis(m.Kpis, "۱۲", "۴", "۳", "۹", "۳۷");
                    if (m.PrintJobs.Count == 0)
                    {
                        // همان ۱۲ سند مرکز چاپ — بدون نیاز به سرور
                        foreach (var (key, title, desc) in new[]
                        {
                            ("ReceptionReceipt", "قبض پذیرش", "رسید پذیرش بیمار"),
                            ("AppointmentTicket", "قبض نوبت", "رسید نوبتدهی"),
                            ("Invoice", "فاکتور", "فاکتور خدمات"),
                            ("Prescription", "نسخه", "نسخهٔ پزشک"),
                            ("LabResult", "نتیجه آزمایش", "جواب آزمایش"),
                            ("LabLabel", "برچسب نمونه", "لیبل ظرف نمونه"),
                            ("ImagingReport", "گزارش تصویربرداری", "گزارش رادیولوژی"),
                            ("ShiftReceipt", "رسید شیفت", "گزارش صندوق"),
                            ("Payslip", "فیش حقوقی", "فیش پرسنل"),
                            ("Certificate", "گواهی پزشکی", "گواهی و استعلاجی"),
                            ("PatientCard", "کارت بیمار", "کارت شناسایی"),
                            ("Reports", "گزارشها", "چاپ گزارشها"),
                        })
                        {
                            m.PrintJobs.Add(new PrintJobRow
                            {
                                Key = key, Title = title, Description = desc, Icon = "🖨",
                                Printer = string.Empty, Paper = "A5", CopiesInput = "1",
                                Preview = true, AutoPrint = false
                            });
                        }
                    }
                    foreach (var n in new[] { "پیشفرض ویندوز", "HP LaserJet P1102", "EPSON LX-310", "چاپگر حرارتی ۸۰mm" })
                        if (!m.PrinterNames.Contains(n)) m.PrinterNames.Add(n);
                    m.ClinicName = "درمانگاه شبانهروزی سهند";
                    m.SectionIndex = 3; // بخش «چاپگر و چاپ»
                    viewModel = m;
                    view = new SettingsView { DataContext = m };
                    winWidth = 1240;
                    winHeight = 850;
                    break;
                }
                case "imaging":
                {
                    // همان مسیر قدیمی، با دادهٔ نمونهٔ کامل صف کار
                    await RenderImagingScreenshotAsync(path);
                    return;
                }
                default:
                    throw new ArgumentException($"کلید ناشناختهٔ صفحه: {page}. کلیدهای مجاز: {string.Join(", ", ScreenshotPageKeys)}");
            }

            // به اندازه فضای واقعاً قابل نمایش رانر محدود می‌کنیم؛ اضافه‌کردن
            // پیکسل سفید بیرون صفحه، دکمه‌های پایین را به‌اشتباه پنهان می‌کرد.
            // ابعاد خروجی در فایل همراه ثبت می‌شود و الزاماً برابر طرح نیست.
            var workArea = SystemParameters.WorkArea;
            winWidth = Math.Max(1, Math.Min(1030, (int)workArea.Width));
            winHeight = Math.Max(1, Math.Min(774, (int)workArea.Height));

            view.Language = XmlLanguage.GetLanguage("fa-IR");
            view.FlowDirection = FlowDirection.RightToLeft;
            view.Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF7));
            // همان قلمی که طرحهای HTML با آن رندر شدند (وزیرمتن جاسازی‌شده
            // داخل exe) — تا مقایسهٔ پیکسلی با رندر Edge معنادار باشد.
            view.FontFamily = (TryFindResource("App.FontFamily") as FontFamily)
                              ?? new FontFamily("Tahoma");
            // تولتیپها هنگام CopyFromScreen ممکن است زیر نشانگر ماوس باز بمانند
            // و یک کادر تیرهٔ اضافی وسط عکس بیندازند؛ مالک طرح را بدون آنها میبیند.
            ToolTipService.SetIsEnabled(view, false);

            var window = new Window
            {
                Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF7)),
                Content = view,
                Width = winWidth,
                Height = winHeight,
                Topmost = true,
                WindowStyle = WindowStyle.None,
                // حذف حاشیهٔ تغییر اندازهٔ WPF تا نوار باریک کاغذدیواری
                // ویندوز در لبهٔ چپ اسکرینشات نیفتد (همان رفع مسیر تصویربرداری).
                ResizeMode = ResizeMode.NoResize,
                BorderThickness = new Thickness(0),
                ShowInTaskbar = false,
                Left = workArea.Left,
                Top = workArea.Top,
                Title = $"اسکرینشات راستیآزمایی — {page}",
                FlowDirection = FlowDirection.RightToLeft
            };
            window.Show();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            // فرصت به بارگذاریهای آغازین (که در نبود سرور شکست میخورند) تا
            // نشانگر «در حال بارگذاری» فروبنشیند و فرم روی وضعیت پایدار رندر شود.
            await Task.Delay(1400);
            if (viewModel is ViewModelBase busyVm) busyVm.IsBusy = false;

            var width = Math.Max(1, (int)Math.Ceiling(view.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(view.ActualHeight));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path + ".txt",
                $"Page={page} Flow={view.FlowDirection} Lang={view.Language} Actual={view.ActualWidth}x{view.ActualHeight} " +
                $"Render={view.RenderSize.Width}x{view.RenderSize.Height} Desired={view.DesiredSize.Width}x{view.DesiredSize.Height}");

            // هشدار فنی: PointToScreen(0,0) روی درخت بصری RTL، نقطهٔ لبهٔ
            // **راست** ویو را برمیگرداند (WPF آینهٔ افقی روی ماتریس ریشه
            // میگذارد) و CopyFromScreen از بیرون پنجره عکس سفید میگرفت.
            // مبدأ درست همان گوشهٔ پنجره است — بهشرطی که حاشیهٔ تغییر اندازه
            // (ResizeMode=NoResize) حذف شده باشد تا چیزی از دسکتاپ نماند.
            var left = (int)window.Left;
            var top = (int)window.Top;
            // فقط ناحیه قابل‌رؤیت رانر گرفته می‌شود. اندازه واقعی در فایل
            // همراه ذخیره است و با ابعاد طرح تأییدی یکسان فرض نمی‌شود.
            var workW = Math.Max(1, (int)SystemParameters.WorkArea.Width);
            var workH = Math.Max(1, (int)SystemParameters.WorkArea.Height);
            var capW = Math.Min(width, workW);
            var capH = Math.Min(height, workH);
            using var raw = new System.Drawing.Bitmap(capW, capH);
            using (var g = System.Drawing.Graphics.FromImage(raw))
                g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(capW, capH));
            using var screen = new System.Drawing.Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(screen))
            {
                g.Clear(System.Drawing.Color.FromArgb(0xEE, 0xF3, 0xF8));
                g.DrawImageUnscaled(raw, 0, 0);
            }
            screen.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            if (page.Equals("print-designer", StringComparison.OrdinalIgnoreCase))
            {
                // رندر چیدمان در ابعاد مرجع، مستقل از محدوده تصویر مانیتور.
                // این تصویر جایگزین اسکرین‌شات روی صفحه نیست: تبدیل RTL و
                // جزئیات رندر می‌توانند با خروجی مانیتور متفاوت باشند.
                const int layoutWidth = 1568, layoutHeight = 885;
                // چیدمان را مستقیماً و بدون اجرای دوباره layout والد انجام
                // می‌دهیم؛ بازچینی پنجره کوچک می‌تواند رندر را برش دهد.
                view.Width = layoutWidth; view.Height = layoutHeight;
                if (viewModel is FreeTemplateEditorWindow editor && editor.FindName("DesignerWorkspace") is FrameworkElement workspace)
                    workspace.Margin = new Thickness(63, 12, 78, 12);
                view.Measure(new Size(layoutWidth, layoutHeight));
                view.Arrange(new Rect(0, 0, layoutWidth, layoutHeight));
                if (Math.Abs(view.RenderSize.Width - layoutWidth) > 0.1 || Math.Abs(view.RenderSize.Height - layoutHeight) > 0.1)
                    throw new InvalidOperationException("رندر طراح به ابعاد مرجع نرسید.");
                var layout = new System.Windows.Media.Imaging.RenderTargetBitmap(layoutWidth, layoutHeight, 96, 96, PixelFormats.Pbgra32);
                layout.Render(view);
                var pixels = new byte[layoutWidth * layoutHeight * 4];
                layout.CopyPixels(pixels, layoutWidth * 4, 0);
                for (var i = 3; i < pixels.Length; i += 4)
                    if (pixels[i] < 250) throw new InvalidOperationException("رندر هم‌اندازه ناقص یا شفاف است.");
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                // ریشه RTL در RenderTargetBitmap نسبت به خروجی پنجره آینه‌ای
                // است؛ برگرداندن افقی در خروجی، همان جهت پنجره را بازمی‌گرداند.
                var oriented = new System.Windows.Media.Imaging.TransformedBitmap(layout, new ScaleTransform(-1, 1));
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(oriented));
                var layoutPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "print-designer-layout.png");
                using (var stream = File.Create(layoutPath)) encoder.Save(stream);
                File.WriteAllText(layoutPath + ".txt", "Offscreen WPF layout, 1568x885 at 96 DPI; horizontally corrected for RTL. Not an on-screen screenshot; rendering can differ.");
            }
            window.Close();
            Shutdown(0);
        }
        catch (Exception ex)
        {
            LogCrash($"اسکرینشات {page}", ex);
            try { File.WriteAllText(path + ".error.txt", ex.ToString()); } catch { }
            Shutdown(1);
        }
    }

    /// <summary>رویداد راه‌اندازی — ساخت کانتینر DI و انتخاب پنجره آغازین</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Sahand.ClinicPro.Desktop.Services.SaveShortcut.Register();

        // ═══ سپر ضدبسته‌شدن برنامه (گزارش مالک: «تصویربرداری را باز می‌کنم
        // برنامه بسته می‌شود») ═══ هیچ خطای یک صفحه نباید کل برنامه را ببندد:
        // خطاهای رشتهٔ رابط کاربری این‌جا گرفته، در فایل crash.log کنار
        // تنظیمات کاربر ثبت و با پیام فارسی به کاربر نشان داده می‌شوند و
        // برنامه زنده می‌ماند؛ خطاهای تسک‌های پس‌زمینه هم فقط ثبت می‌شوند.
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash("رابط کاربری", args.Exception);
            args.Handled = true;
            try
            {
                Views.AppDialog.Error("خطای غیرمنتظره",
                    "در این بخش خطایی رخ داد و جلوی آن گرفته شد تا برنامه بسته نشود. " +
                    "جزئیات در فایل crash.log (کنار تنظیمات برنامه) ثبت شد. " +
                    "اگر خطا تکرار شد، همان بخش و کاری که می‌کردید را گزارش کنید.");
            }
            catch { /* خود دیالوگ نباید مسیر نجات را خراب کند */ }
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("تسک پس‌زمینه", args.Exception);
            args.SetObserved();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("دامنهٔ برنامه", args.ExceptionObject as Exception);

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

        // ═══ حالت اسکرین‌شات واقعی (ابزار راستی‌آزمایی طراحی) ═══
        // با آرگومان --imaging-screenshot <مسیر> برنامه بدون ورود/سرور بالا می‌آید،
        // صفحهٔ واقعی تصویربرداری را با دادهٔ نمونه روی پنجرهٔ ویندوزی رندر و از
        // خودِ درخت بصری WPF عکس PNG می‌گیرد و خارج می‌شود؛ در CI ویندوزی برای
        // مقایسهٔ عینیِ پیاده‌سازی با طرح تأییدی استفاده می‌شود و در اجرای عادی
        // کاربران هرگز فعال نیست.
        // ═══ حالت عمومی اسکرینشات واقعی (چرخهٔ مقایسه با طرح تأییدی مالک) ═══
        // --screenshot <کلیدصفحه> <مسیر> — هر یک از شش صفحهٔ هدف (و تصویربرداری)
        // را همانطور که کاربر میبیند رندر و PNG میکند؛ توضیحات کامل بالای متد.
        var pageShotIndex = Array.IndexOf(e.Args, "--screenshot");
        if (pageShotIndex >= 0)
        {
            var key = pageShotIndex + 1 < e.Args.Length ? e.Args[pageShotIndex + 1] : "dentistry";
            var outPath = pageShotIndex + 2 < e.Args.Length ? e.Args[pageShotIndex + 2] : $"{key}-screenshot.png";
            _ = RenderPageScreenshotAsync(key, outPath);
            return;
        }

        var shotIndex = Array.IndexOf(e.Args, "--imaging-screenshot");
        if (shotIndex >= 0)
        {
            var shotPath = shotIndex + 1 < e.Args.Length ? e.Args[shotIndex + 1] : "imaging-screenshot.png";
            _ = RenderImagingScreenshotAsync(shotPath);
            return;
        }


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
        // چرخهٔ ۷۸ — صف مستقل خدمات درمانی (گزارش مالک: جدا شدن از صف ویزیت)
        services.AddTransient<ServicesViewModel>();
        services.AddTransient<ServiceCatalogViewModel>();
        // چرخهٔ بیست‌وهفتم — میزکار پزشک (صفحهٔ ویزیت پزشک — گزارش هجدهم مالک)
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
        services.AddTransient<AdminHubViewModel>();
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
        // چرخهٔ ۴۷ — صفحهٔ مستقل مدیریت شیفت‌ها (گزارش مالک)
        services.AddTransient<ShiftsViewModel>();
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
