using System.Windows;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجرهٔ فعال‌سازی لایسنس — چرخه هفدهم (گزارش یازدهم مالک).
/// پیش‌تر دکمهٔ «فعال‌سازی لایسنس» صفحهٔ ورود رویدادش هیچ شنونده‌ای نداشت و
/// وارد برنامه هم بدون لایسنس ممکن بود. این پنجره پیش از ورود (یا در میانهٔ
/// ورود، پس از دریافت پاسخ «لایسنس نامعتبر») به‌صورت مودال باز می‌شود و همان
/// نمای وضعیت لایسنس را در خود دارد — فعال‌سازی آنلاین با کلید و آفلاین با
/// فایل .lic هر دو از همین‌جا کار می‌کنند (endpointها AllowAnonymous هستند).
/// پس از فعال‌سازی موفق، بنر سبز نمایش و پنجره پس از ~۱.۵ ثانیه با نتیجهٔ
/// موفق بسته می‌شود تا فراخواننده (پنجرهٔ ورود/تغییر رمز/راه‌اندازی برنامه)
/// مسیر ورود را ادامه دهد.
/// </summary>
public partial class LicenseActivationWindow : Window
{
    private readonly ISnackbarService _snackbar;
    private readonly SnackbarMessageQueue _localQueue = new(TimeSpan.FromSeconds(3.5));
    private readonly DispatcherTimer _autoCloseTimer;

    /// <summary>
    /// آیا لایسنس در این نشستِ باز بودن پنجره با موفقیت فعال شد؟
    /// فراخواننده پس از ShowDialog این مقدار را می‌خواند.
    /// </summary>
    public bool ActivatedSuccessfully { get; private set; }

    /// <summary>سازنده با تزریق ViewModel وضعیت لایسنس و سرویس پیام شناور</summary>
    public LicenseActivationWindow(LicenseStatusViewModel viewModel, ISnackbarService snackbar)
    {
        InitializeComponent();
        _snackbar = snackbar;

        // محتوا همان نمای وضعیت لایسنس است — پیام‌های موفقیت/خطای فعال‌سازی
        // روی اسنک‌بار «محلیِ همین پنجره» نمایش می‌یابند (پنجرهٔ اصلی باز نیست)
        StatusView.DataContext = viewModel;
        LocalSnackbar.MessageQueue = _localQueue;
        _snackbar.MessagePosted += OnSnackbarMessagePosted;
        viewModel.LicenseActivated += OnLicenseActivated;

        // بستن خودکار پس از نمایش بنر موفقیت — ۱.۵ ثانیه فرصت دیدن پیام
        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            DialogResult = true;
            Close();
        };

        // بستن پنجره بدون فعال‌سازی = نتیجهٔ false — فراخواننده برمی‌گردد به ورود
        Closing += (_, _) => _snackbar.MessagePosted -= OnSnackbarMessagePosted;
    }

    /// <summary>هدایت پیام‌های سرویس شناور به صف اسنک‌بار محلی</summary>
    private void OnSnackbarMessagePosted(string message, bool success)
        => _localQueue.Enqueue(message);

    /// <summary>فعال‌سازی موفق — بنر سبز + بستن خودکار با نتیجهٔ true</summary>
    private void OnLicenseActivated(object? sender, EventArgs e)
    {
        ActivatedSuccessfully = true;
        SuccessBanner.Visibility = Visibility.Visible;
        _autoCloseTimer.Start();
    }

    /// <summary>دکمهٔ بستن — بدون فعال‌سازی، فراخواننده به صفحهٔ ورود برمی‌گردد</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// باز کردن مودال پنجرهٔ فعال‌سازی از هر نقطهٔ برنامه — helper مشترک
    /// پنجرهٔ ورود، پنجرهٔ تغییر رمز و راه‌اندازی App.
    /// </summary>
    /// <returns>true اگر لایسنس در همین پنجره فعال شد</returns>
    public static bool OpenAndActivate(Window? owner)
    {
        var win = App.Services.GetRequiredService<LicenseActivationWindow>();
        win.Owner = owner;
        win.ShowDialog();
        return win.ActivatedSuccessfully;
    }
}
