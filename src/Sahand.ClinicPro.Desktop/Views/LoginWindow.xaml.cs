using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره ورود — گام U1: انتقال امن رمز + کشیدن پنجره بدون حاشیه +
/// دکمه نمایش/مخفی رمز + ثبت نام کاربری در تنظیمات محلی (RememberMe)
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    /// <summary>سازنده با تزریق ViewModel ورود</summary>
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // PasswordBox به‌صورت امن با VM همگام می‌شود (بدون دوطرفه‌سازی XAML)
        PasswordBox.PasswordChanged += (_, _) => viewModel.Password = PasswordBox.Password;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        viewModel.PasswordChangeRequired += OnPasswordChangeRequired;

        // چرخه هفدهم (گزارش یازدهم مالک): دکمهٔ «فعال‌سازی لایسنس» پیش‌تر
        // رویدادش را به هیچ شنونده‌ای نمی‌داد (کاملاً بی‌اثر بود) و ورود موفق هم
        // بدون لایسنس مستقیم پوستهٔ اصلی را باز می‌کرد — هر دو مسیر اکنون به
        // پنجرهٔ فعال‌سازی مستقل وصل‌اند
        viewModel.LicenseActivationRequested += OnLicenseActivationRequested;
        viewModel.LicenseActivationRequired += OnLicenseActivationRequired;

        // بازیابی نام کاربری ذخیره‌شده (مرا به خاطر بسپار) — فایل JSON محلی
        Loaded += (_, _) =>
        {
            UsernameBox.Focus();
            var saved = Services.LocalSettingsStore.Get("SavedUsername");
            if (!string.IsNullOrWhiteSpace(saved))
            {
                UsernameBox.Text = saved;
                _viewModel.RememberMe = true;
                PasswordBox.Focus();
            }
        };
    }

    /// <summary>
    /// کشیدن فرم از سطح خالی کارت. رویداد روی کارت قرار دارد، اما ورودی‌ها و
    /// دکمه‌های داخل آن نباید با شروع DragMove از کار بیفتند.
    /// </summary>
    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed || IsInteractiveSource(e.OriginalSource as DependencyObject))
            return;

        try
        {
            DragMove();
            e.Handled = true;
        }
        catch (InvalidOperationException)
        {
            // اگر سیستم پنجره در همان لحظه در حال تغییر حالت بود، کلیک عادی بماند.
        }
    }

    private static bool IsInteractiveSource(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ButtonBase or TextBoxBase or System.Windows.Controls.PasswordBox or ComboBox)
                return true;

            element = element switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(element),
                ContentElement content => ContentOperations.GetParent(content) ?? LogicalTreeHelper.GetParent(content),
                _ => LogicalTreeHelper.GetParent(element)
            };
        }

        return false;
    }

    /// <summary>دکمه بستن پنجره</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>کلید Enter داخل رمز عبور → فرمان ورود</summary>
    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            e.Handled = true;
            if (_viewModel.LoginCommand.CanExecute(null))
                _viewModel.LoginCommand.Execute(null);
        }
    }

    /// <summary>
    /// نمایش یا مخفی کردن رمز عبور — جابه‌جایی PasswordBox و TextBox.
    /// چرخه هفدهم (گزارش یازدهم): هنگام مخفی‌شدن، کادر ماسک با آخرین مقدار ViewModel
    /// همگام می‌شود (ممکن است کاربر در کادر آشکار ویرایش کرده باشد)؛ پس از
    /// مقداردهی برنامه‌ای SelectAll تا تایپِ بعدی «جایگزین همه» شود نه درج در ابتدا
    /// </summary>
    private void OnEyeToggled(object sender, RoutedEventArgs e)
    {
        var show = EyeToggle.IsChecked == true;
        PasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        if (show)
        {
            PasswordRevealBox.CaretIndex = PasswordRevealBox.Text.Length;
        }
        else if (PasswordBox.Password != _viewModel.Password)
        {
            PasswordBox.Password = _viewModel.Password;
            PasswordBox.SelectAll();
        }
    }

    /// <summary>
    /// دکمهٔ «فعال‌سازی لایسنس» — چرخه هفدهم (گزارش یازدهم): پنجرهٔ فعال‌سازی
    /// مستقل به‌صورت مودال باز می‌شود؛ endpointهای فعال‌سازی AllowAnonymous هستند
    /// و پیش از ورود هم کار می‌کنند. پس از بستن، کاربر همچنان باید وارد شود.
    /// </summary>
    private void OnLicenseActivationRequested(object? sender, EventArgs e)
        => LicenseActivationWindow.OpenAndActivate(this);

    /// <summary>
    /// ورود موفق اما لایسنس نامعتبر — چرخه هفدهم (گزارش یازدهم): تا فعال‌سازی
    /// پوستهٔ اصلی باز نمی‌شود؛ اگر فعال شد ورود ادامه می‌یابد و اگر پنجره بدون
    /// فعال‌سازی بسته شد، توکن همین ورود پاک می‌شود تا اجرای بعدی هم از همین
    /// گیت بگذرد و پیام راهنما روی همین صفحهٔ ورود نمایش می‌یابد
    /// </summary>
    private void OnLicenseActivationRequired(object? sender, EventArgs e)
    {
        if (LicenseActivationWindow.OpenAndActivate(this))
        {
            OnLoginSucceeded(sender, e);
        }
        else
        {
            App.Services.GetRequiredService<ITokenManager>().Clear();
            _viewModel.Password = string.Empty;
            PasswordBox.Password = string.Empty;
            _viewModel.ShowLoginError("برای ورود به برنامه ابتدا لایسنس را فعال کنید");
            PasswordBox.Focus();
        }
    }

    /// <summary>
    /// پس از ورود موفق — ذخیره نام کاربری در صورت درخواست و باز کردن پنجره اصلی
    /// چرخهٔ چهل‌ودوم: نمایش پنجرهٔ اصلی از App.ShowMainWindow می‌گذرد — نخست
    /// نمونهٔ زندهٔ موجود می‌یابد و فقط در نبودش نمونهٔ نو می‌سازد؛ پس از
    /// «خروج از حساب» و ورود مجدد، MainWindow قبلی (بسته‌شده با CloseForLoginSwitch)
    /// دیگر Show نمی‌شود و خطای پنجرهٔ بسته رخ نمی‌دهد.
    /// </summary>
    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        // فقط نام کاربری ذخیره می‌شود — هرگز رمز عبور
        Services.LocalSettingsStore.Set("SavedUsername",
            _viewModel.RememberMe ? _viewModel.Username : string.Empty);

        App.ShowMainWindow();
        Close();
    }

    /// <summary>
    /// سرور تغییر رمز را الزامی کرده — چرخه چهاردهم (گزارش پنجم مالک):
    /// پنجره تغییر رمز با رمز فعلیِ همان لحظه باز می‌شود؛ پنجره اصلی هنوز باز
    /// نمی‌شود تا رمز تازه ثبت شود (قول «دورزدنی‌ناپذیر» قدم ۷ راهنمای SQL)
    /// </summary>
    private void OnPasswordChangeRequired(object? sender, EventArgs e)
    {
        // فقط نام کاربری ذخیره می‌شود — هرگز رمز عبور
        Services.LocalSettingsStore.Set("SavedUsername",
            _viewModel.RememberMe ? _viewModel.Username : string.Empty);

        var change = App.Services.GetRequiredService<ChangePasswordWindow>();
        change.Initialize(_viewModel.Username.Trim(), _viewModel.Password);

        System.Windows.Application.Current.MainWindow = change;
        change.Show();
        Close();
    }
}
