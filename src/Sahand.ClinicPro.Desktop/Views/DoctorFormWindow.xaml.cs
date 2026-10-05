using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره فرم پزشک ۵ تب — گام U3
/// </summary>
public partial class DoctorFormWindow : Window
{
    /// <summary>سازنده با ViewModel و اتصال رویداد بستن</summary>
    public DoctorFormWindow(DoctorFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
