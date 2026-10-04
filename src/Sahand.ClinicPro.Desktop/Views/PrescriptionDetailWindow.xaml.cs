using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت جزئیات نسخه
/// </summary>
public partial class PrescriptionDetailWindow : Window
{
    /// <summary>سازنده با ViewModel جزئیات</summary>
    public PrescriptionDetailWindow(PrescriptionDetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }

    /// <summary>دکمه بستن</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}