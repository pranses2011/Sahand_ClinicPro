using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم صدور نسخه
/// </summary>
public partial class PrescriptionFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم</summary>
    public PrescriptionFormWindow(PrescriptionFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}