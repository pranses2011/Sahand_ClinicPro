using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت پرداخت
/// </summary>
public partial class PaymentWindow : Window
{
    private bool _paymentSucceeded;
    /// <summary>سازنده با ViewModel پرداخت</summary>
    public PaymentWindow(PaymentWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => { _paymentSucceeded = true; Close(); };
        Closing += (_, e) => { if (viewModel.IsBusy && !_paymentSucceeded) e.Cancel = true; };
    }
}