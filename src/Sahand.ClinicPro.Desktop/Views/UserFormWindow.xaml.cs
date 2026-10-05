using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت/ویرایش کاربر — گام U13
/// رویدادهای PasswordBox (بدون اتصال مستقیم به دلایل امنیتی) و بررسی یکتایی نام کاربری
/// </summary>
public partial class UserFormWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public UserFormWindow(UserFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }

    /// <summary>دسترسی عمومی به ViewModel — برای ویرایش</summary>
    public UserFormViewModel ViewModel { get; }

    /// <summary>انتقال رمز به ViewModel با هر تغییر</summary>
    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        => ViewModel.Password = PasswordBox.Password;

    /// <summary>انتقال تکرار رمز به ViewModel با هر تغییر</summary>
    private void ConfirmBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        => ViewModel.ConfirmPassword = ConfirmBox.Password;

    /// <summary>بررسی یکتایی نام کاربری هنگام ترک فیلد</summary>
    private void Username_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CheckUsernameCommand.CanExecute(null))
            ViewModel.CheckUsernameCommand.Execute(null);
    }
}
