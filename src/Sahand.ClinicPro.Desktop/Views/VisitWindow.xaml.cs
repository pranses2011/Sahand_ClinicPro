using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت ویرایشگر ویزیت SOAP
/// </summary>
public partial class VisitWindow : Window
{
    /// <summary>سازنده با ViewModel ویرایشگر</summary>
    public VisitWindow(VisitEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.Completed += (_, _) => Close();
    }
}