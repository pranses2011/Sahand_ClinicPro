using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره ویرایش اطلاعات حقوقی پرسنل — چرخه دهم
/// </summary>
public partial class EmployeeSalaryEditWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public EmployeeSalaryEditWindow(EmployeeSalaryEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}
