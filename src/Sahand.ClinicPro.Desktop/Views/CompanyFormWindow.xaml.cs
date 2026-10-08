using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت/ویرایش شرکت بیمه — چرخه بیست‌وسوم (گزارش شانزدهم مالک).
/// </summary>
public partial class CompanyFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم بیمه</summary>
    public CompanyFormWindow(CompanyFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
