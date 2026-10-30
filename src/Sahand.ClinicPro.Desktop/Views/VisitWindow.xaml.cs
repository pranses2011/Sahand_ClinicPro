using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره ویزیت با سایدبار و ۸ تب — گام U6
/// </summary>
public partial class VisitWindow : Window
{
    /// <summary>سازنده با ViewModel و اتصال بستن</summary>
    public VisitWindow(VisitEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        Closed += (_, _) => viewModel.StopTimers();
    }
}
