using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پرونده بیمار — فقط اتصال ViewModel
/// </summary>
public partial class PatientDetailWindow : Window
{
    /// <summary>سازنده با ViewModel پرونده</summary>
    public PatientDetailWindow(PatientDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
