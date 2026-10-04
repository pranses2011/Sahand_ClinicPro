using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم درخواست آزمایش
/// </summary>
public partial class LabOrderFormWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public LabOrderFormWindow(LabOrderFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}