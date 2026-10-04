using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت ثبت نتیجه آزمایش
/// </summary>
public partial class LabResultWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public LabResultWindow(LabResultViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}