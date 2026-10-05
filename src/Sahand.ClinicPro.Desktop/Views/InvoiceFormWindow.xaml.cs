using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره صدور فاکتور — گام U8
/// </summary>
public partial class InvoiceFormWindow : Window
{
    /// <summary>سازنده</summary>
    public InvoiceFormWindow(InvoiceFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
