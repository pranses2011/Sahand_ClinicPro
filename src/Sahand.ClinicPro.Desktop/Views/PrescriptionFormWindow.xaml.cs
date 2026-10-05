using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره نسخه‌نویسی — گام U7
/// </summary>
public partial class PrescriptionFormWindow : Window
{
    /// <summary>سازنده</summary>
    public PrescriptionFormWindow(PrescriptionFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
