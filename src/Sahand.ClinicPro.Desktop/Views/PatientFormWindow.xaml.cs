using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم بیمار — ثبت و ویرایش ۵ تب
/// </summary>
public partial class PatientFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم</summary>
    public PatientFormWindow(PatientFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
