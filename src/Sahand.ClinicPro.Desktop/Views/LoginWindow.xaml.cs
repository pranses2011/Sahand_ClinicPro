using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
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

    /// <summary>کشیدن پنجره بدون نوار عنوان — دکمه ماوس روی پس‌زمینه</summary>
    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
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

    /// <summary>نمایش یا مخفی کردن رمز عبور — جابه‌جایی PasswordBox و TextBox</summary>
    private void OnEyeToggled(object sender, RoutedEventArgs e)
    {
        var show = EyeToggle.IsChecked == true;
        PasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        if (show)
            PasswordRevealBox.CaretIndex = PasswordRevealBox.Text.Length;
    }

    /// <summary>پس از ورود موفق — ذخیره نام کاربری در صورت درخواست و باز کردن پنجره اصلی</summary>
    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        // فقط نام کاربری ذخیره می‌شود — هرگز رمز عبور
        Services.LocalSettingsStore.Set("SavedUsername",
            _viewModel.RememberMe ? _viewModel.Username : string.Empty);

        var main = App.Services.GetRequiredService(typeof(MainWindow)) as MainWindow;
        if (main is null) return;

        System.Windows.Application.Current.MainWindow = main;
        main.Show();
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
