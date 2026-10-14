using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کد پشت پنجره اصلی — اتصال به ViewModel تزریق‌شده
/// ═══ چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) ═══
/// «برای خروج از برنامه تاییدیه بگیره و در اون تاییدیه چک باکس پشتیبان‌گیری
/// قبل از خروج هم باشه»: رویداد Closing پنجرهٔ اصلی (دکمهٔ X یا Alt+F4)
/// ابتدا لغو می‌شود؛ دیالوگ فارسی خود برنامه با چک‌باکس پشتیبان‌گیری نمایش
/// داده می‌شود (منطق در MainWindowViewModel.ConfirmExitAsync) و فقط پس از
/// تأیید کاربر، بستن مجاز می‌گردد. مسیر «خروج از حساب → صفحهٔ ورود»
/// (لاگ‌اوت/انقضای نشست) از CloseForLoginSwitch می‌گذرد و تأییدیهٔ خروج
/// نمی‌گیرد — آن مسیر خروج از برنامه نیست.
/// ═══ چرخهٔ چهل‌وچهارم (گزارش مالک) — خطای خروج از برنامه ═══
/// «Cannot set Visibility to Visible or call Show, ShowDialog, Close…
/// while a Window is closing» در همین فایل، خط ۴۹ (Close داخل هندلر
/// Closing پس از await) رخ می‌داد. ریشه: ادامهٔ await گاهی «همگام» اجرا
/// می‌شود — وقتی ConfirmExitAsync بی‌درنگ کامل شود (مثلاً پشتیبان‌گیری
/// انتخاب نشود و StopAsync از قبل کامل باشد)، ادامهٔ هندلر هنوز «درون»
/// InternalCloseِ خودِ WPF می‌ماند؛ پرچم _isClosing هنوز برافراشته است و
/// فراخوانی Close در همان لحظه با VerifyNotClosing به خطا می‌افتد.
/// رفع: بستنِ پس از تأیید «تعویق‌شده» به صف دیسپچر می‌رود تا پس از پایان
/// چرخهٔ بستنِ فعلی اجرا شود + گارد ورود مجدد (دوبار زدن X هنگام باز بودن
/// دیالوگ، دیالوگ دوم نمی‌سازد) + try/catch نهایی برای مسیرهای رقابتی
/// (خاموشی برنامه هم‌زمان با تأیید کاربر).
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>پرچم بستن مجاز — پس از تأیید کاربر یا در مسیر تعویض نشست</summary>
    private bool _closeAllowed;

    /// <summary>
    /// پنجرهٔ مستقل پیام‌های شناور — چرخهٔ ۷۵ (هفتمین گزارش بریدگی پایین).
    /// میزبانی توست‌ها از درخت بصری همین پنجره به یک پنجرهٔ ویندوزی مستقلِ
    /// بی‌قاب و شفاف منتقل شد (الگوی اعلان‌های ویندوز ۱۱) — پنجرهٔ مستقل
    /// سطح رندر خودش را دارد و هیچ لایه/حاشیه/هم‌پوشانی‌ای در پنجرهٔ اصلی
    /// نمی‌تواند لبهٔ پایین کارت را clip کند. جزئیات کامل در ToastWindow.
    /// </summary>
    private Views.ToastWindow? _toastWindow;

    /// <summary>
    /// دیالوگ تأیید خروج در حال نمایش است؟ — چرخهٔ ۴۴.
    /// دوبار فشردن X (یا Alt+F4) هنگام باز بودن دیالوگ، نباید دیالوگ دوم
    /// بسازد؛ درخواست بستنِ دوم فقط لغو می‌شود و منتظر پاسخ دیالوگ اول
    /// می‌ماند. بدون این گارد، دو زنجیرهٔ «تأیید → Close» هم‌زمان می‌شدند
    /// و دومی روی پنجرهٔ «در حال بسته‌شدن» خطا می‌داد.
    /// </summary>
    private bool _exitConfirmInProgress;

    /// <summary>سازنده با تزریق ViewModel پوسته</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // ═══ چرخهٔ ۷۵ — میزبان پیام‌های شناور در پنجرهٔ مستقل ═══
        // پس از آماده‌شدن درخت بصری (نوار بالا قابل اندازه‌گیری باشد) پنجرهٔ
        // توست ساخته می‌شود؛ فهرست Toasts همان نمونهٔ ViewModel مشترک است و
        // نمایش/پنهانی با آمدن و رفتن پیام‌ها خودکار است (ToastWindow داخلی).
        Loaded += (_, _) =>
        {
            if (_toastWindow is null && DataContext is MainWindowViewModel vm)
                _toastWindow = new Views.ToastWindow(this, vm);
        };

        // تأیید خروج از برنامه — دکمهٔ X / Alt+F4 / Close از هر جای دیگر
        Closing += async (_, ce) =>
        {
            if (_closeAllowed)
                return;

            // بستن را فعلاً لغو می‌کنیم؛ پس از تأیید کاربر دوباره (مجاز) بسته می‌شود
            ce.Cancel = true;

            // دیالوگ تأیید دیگری در جریان است — این درخواست فقط لغو می‌شود
            // (باز بودن دیالوگ، کاربر هنوز تصمیم نگرفته؛ دیالوگ دوم شلوغی است)
            if (_exitConfirmInProgress)
                return;

            if (DataContext is not MainWindowViewModel vm)
            {
                // بدون ViewModel منطق تأیید نداریم — بستن مستقیم
                _closeAllowed = true;
                BeginInvokeClose();
                return;
            }

            _exitConfirmInProgress = true;
            try
            {
                var proceed = await vm.ConfirmExitAsync();
                if (proceed)
                {
                    _closeAllowed = true;
                    // ═══ چرخهٔ ۴۴ — بستن تعویق‌شده ═══
                    // ادامهٔ await ممکن است «همگام» و هنوز درون InternalCloseِ
                    // فعلی اجرا شود (پرچم _isClosing برافراشته)؛ فراخوانی
                    // مستقیم Close در همان لحظه VerifyNotClosing را می‌شکند.
                    // BeginInvoke اجرای بستن را تا پس از پایان چرخهٔ بستنِ
                    // جاری عقب می‌اندازد — همیشه امن، حتی وقتی await واقعاً
                    // ناهمگام بوده و پرچم از قبل پایین است.
                    BeginInvokeClose();
                }
                // انصراف کاربر → پنجره باز می‌ماند (Cancel همین حالا فعال است)
            }
            finally
            {
                _exitConfirmInProgress = false;
            }
        };
    }

    /// <summary>
    /// بستنِ تعویق‌شدهٔ امن — چرخهٔ ۴۴.
    /// اجرای Close به «پس از» چرخهٔ ورودی/بستنِ جاری منتقل می‌شود و خودِ
    /// فراخوانی هم در برابر پنجرهٔ ازپیش‌درحال‌بسته‌شدن/بسته‌شده (خاموشی
    /// برنامه یا مسیر رقابتی) مقاوم است — خطای «Cannot call Close while
    /// a Window is closing/closed» دیگر در هیچ مسیری سرابی نیست.
    /// </summary>
    private void BeginInvokeClose()
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                Close();
            }
            catch (InvalidOperationException)
            {
                // پنجره هم‌زمان از مسیر دیگری (خاموشی برنامه/تعویض نشست)
                // بسته یا در حال بسته‌شدن است — کار انجام شده؛ نادیده
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// بستن بی‌تأیید برای تعویض نشست — بازگشت به صفحهٔ ورود (لاگ‌اوت یا
    /// انقضای توکن). این مسیر «خروج از برنامه» نیست؛ کاربر به صفحهٔ ورود
    /// می‌رود و تأییدیه/پشتیبان‌گیری خروج نباید پرسیده شود.
    /// </summary>
    public void CloseForLoginSwitch()
    {
        _closeAllowed = true;
        Close();
    }

    /// <summary>
    /// میزبانی لایهٔ تور آموزشی — چرخهٔ سی‌وهشتم (گزارش مالک: هایلایت‌های
    /// تور به‌خاطر RTL جابه‌جا بودند). تور دیگر پنجرهٔ همپوشان جدا نیست؛
    /// به‌صورت کنترل کاربر در «لایهٔ TourLayer» درون همین پنجره مستقر
    /// می‌شود تا اندازه‌گیری هدف و ترسیم ماسک در همان درخت بصری انجام
    /// شود و خطای آینه‌ای مختصات (رفتار شناخته‌شدهٔ PointToScreen در
    /// پنجره‌های RTL) به‌کلی منتفی گردد.
    /// </summary>
    /// <param name="tour">کنترل تور ساخته‌شده — با افزودن به لایه نمایش می‌افتد</param>
    public void ShowTourOverlay(Views.GuidedTourWindow tour)
    {
        // لایه خالی است و پس‌زمینه‌ای ندارد → وقتی تور داخلش نیست، هیچ چیزی
        // رندر نمی‌شود و کلیک‌ها هم از میانش به رابط می‌رسند (hit-test شفاف)
        TourLayer.Children.Add(tour);
    }

    /// <summary>
    /// باز کردن فرم «درباره نرم‌افزار» — چرخهٔ ۷۵ (درخواست مالک):
    /// «در یک نقطه از برنامه فرم زیبا و شکیل درباره نرم‌افزار رو تعبیه بکن
    /// و داخلش اطلاعات پشتیبانی بصورت پویا هم باشه». فرم به‌صورت مودال روی
    /// همین پنجره باز می‌شود؛ اطلاعات پشتیبانی و نام درمانگاه از سرور لایسنس
    /// به‌صورت پویا خوانده می‌شوند (هیچ تماسی هاردکد نیست).
    /// میانبر صفحه‌کلید F1 هم همین فرم را باز می‌کند — عرف رایج ویندوز.
    /// </summary>
    private void OnAboutClick(object sender, RoutedEventArgs e) => OpenAbout();

    /// <summary>نمایش فرم درباره — از دکمهٔ نوار بالا یا کلید F1</summary>
    private void OpenAbout()
    {
        var about = new Views.AboutWindow { Owner = this };
        about.ShowDialog();
    }

    /// <summary>
    /// میانبرهای صفحه‌کلید پنجرهٔ اصلی — F1 = دربارهٔ نرم‌افزار (چرخهٔ ۷۵).
    /// رویداد KeyDown پنجره چون فرم‌های مودال باز، فوکوس را از این پنجره
    /// نمی‌گیرند مگر خودشان هندل کنند؛ F1 عرف استاندارد راهنمای ویندوز است.
    /// </summary>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1)
        {
            OpenAbout();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
