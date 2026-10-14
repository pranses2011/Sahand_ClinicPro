using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ویرایش کاتالوگ آزمایش — چرخهٔ ۷۲
/// (بازه‌های مرجع و حدود بحرانی — قابل اصلاح توسط مالک)
/// </summary>
public partial class LabTestEditWindow : Window
{
    /// <summary>سازنده با ViewModel ویرایش</summary>
    public LabTestEditWindow(LabTestEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
        viewModel.SaveSucceeded += (_, _) => Close();
    }
}
