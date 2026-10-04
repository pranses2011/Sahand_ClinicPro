using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره ورود — اتصال ViewModel و انتقال رمز عبور امن
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

        Loaded += (_, _) => UsernameBox.Focus();
    }

    /// <summary>پس از ورود موفق — باز کردن پنجره اصلی</summary>
    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var main = App.Services.GetRequiredService(typeof(MainWindow)) as MainWindow;
        if (main is null) return;

        System.Windows.Application.Current.MainWindow = main;
        main.Show();
        Close();
    }
}
