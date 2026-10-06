using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره تغییر رمز عبور الزامی — چرخه چهاردهم (گزارش پنجم مالک).
/// این پنجره دورزدنی‌ناپذیر است: بستن بدون ثبت موفق یعنی خروج از برنامه؛
/// توکن ورود نیز پاک می‌شود تا با اجرای دوباره، دوباره ورود و دوباره الزام
/// تغییر رمز مطالبه شود (راهنمای SQL قدم ۷ همین را قول داده بود).
/// </summary>
public partial class ChangePasswordWindow : Window
{
    private readonly ITokenManager _tokens;
    private bool _changeCompleted;

    /// <summary>ViewModel تزریق‌شده — برای مقداردهی اولیه از سمت پنجره ورود</summary>
    public ChangePasswordViewModel ViewModel { get; }

    /// <summary>
    /// سازنده با تزریق ViewModel و مدیریت توکن
    /// </summary>
    public ChangePasswordWindow(ChangePasswordViewModel viewModel, ITokenManager tokens)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _tokens = tokens;
        DataContext = viewModel;
        viewModel.ChangeSucceeded += OnChangeSucceeded;

        // رمز فعلی از قبل پر است — فوکوس روی رمز جدید
        Loaded += (_, _) => NewPasswordBox.Focus();
    }

    /// <summary>
    /// مقداردهی اولیه از پنجره ورود — نام کاربری برای سربرگ و رمز فعلیِ همان لحظه
    /// </summary>
    public void Initialize(string username, string currentPassword)
    {
        ViewModel.Username = username;
        ViewModel.CurrentPassword = currentPassword;
        NewPasswordBox.Focus();
    }

    /// <summary>کشیدن پنجره بدون نوار عنوان — دکمه ماوس روی پس‌زمینه</summary>
    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    /// <summary>
    /// دکمه بستن — چون تغییر رمز الزامی است، بستن یعنی خروج از برنامه
    /// </summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// نمایش یا مخفی‌کردن رمز فعلی/جدید/تکرار — جابه‌جایی PasswordBox و TextBox بر اساس Tag.
    /// گزارش هفتم مالک: چشمِ «رمز فعلی» نیز اضافه شد — هر سه کادر همین الگو را دارند
    /// </summary>
    private void OnEyeToggled(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Primitives.ToggleButton toggle) return;
        var show = toggle.IsChecked == true;

        if ((string?)toggle.Tag == "Current")
        {
            CurrentPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            CurrentPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                CurrentPasswordRevealBox.CaretIndex = CurrentPasswordRevealBox.Text.Length;
                CurrentPasswordRevealBox.Focus();
            }
        }
        else if ((string?)toggle.Tag == "New")
        {
            NewPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            NewPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                NewPasswordRevealBox.CaretIndex = NewPasswordRevealBox.Text.Length;
                NewPasswordRevealBox.Focus();
            }
        }
        else if ((string?)toggle.Tag == "Confirm")
        {
            ConfirmPasswordRevealBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (show)
            {
                ConfirmPasswordRevealBox.CaretIndex = ConfirmPasswordRevealBox.Text.Length;
                ConfirmPasswordRevealBox.Focus();
            }
        }
    }

    /// <summary>
    /// پس از تغییر موفق — باز شدن پنجره اصلی و بستن این پنجره
    /// </summary>
    private void OnChangeSucceeded(object? sender, EventArgs e)
    {
        _changeCompleted = true;

        var main = App.Services.GetRequiredService(typeof(MainWindow)) as MainWindow;
        if (main is null) return;

        System.Windows.Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    /// <summary>
    /// بسته‌شدن پنجره — اگر رمز عوض نشده باشد توکن پاک و برنامه بسته می‌شود
    /// تا الزامِ تغییر رمز با اجرای بعدی دوباره مطالبه شود (دورزدنی‌ناپذیر)
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        if (!_changeCompleted)
        {
            _tokens.Clear();
            System.Windows.Application.Current.Shutdown();
        }
        base.OnClosed(e);
    }
}
