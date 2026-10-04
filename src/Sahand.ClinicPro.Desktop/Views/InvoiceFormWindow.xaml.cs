using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم صدور فاکتور
/// </summary>
public partial class InvoiceFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم</summary>
    public InvoiceFormWindow(InvoiceFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}