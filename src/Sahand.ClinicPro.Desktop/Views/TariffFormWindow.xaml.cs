using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم ثبت/ویرایش خدمت/تعرفه — چرخه بیست‌وسوم (گزارش شانزدهم مالک).
/// </summary>
public partial class TariffFormWindow : Window
{
    /// <summary>سازنده با ViewModel فرم تعرفه</summary>
    public TariffFormWindow(TariffFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
