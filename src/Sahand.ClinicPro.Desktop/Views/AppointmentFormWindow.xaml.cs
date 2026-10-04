using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت نوبت
/// </summary>
public partial class AppointmentFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم</summary>
    public AppointmentFormWindow(AppointmentFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}