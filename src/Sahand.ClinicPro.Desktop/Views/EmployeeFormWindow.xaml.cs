using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت پرسنل
/// </summary>
public partial class EmployeeFormWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public EmployeeFormWindow(EmployeeFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}