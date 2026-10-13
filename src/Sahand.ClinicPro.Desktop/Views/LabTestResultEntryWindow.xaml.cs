using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فرم اختصاصی ثبت نتیجهٔ «یک آزمایش» — چرخهٔ ۶۹
/// (گزارش مالک: دابل‌کلیک روی آزمایش ← فرم مختص همان آزمایش).
/// </summary>
public partial class LabTestResultEntryWindow : Window
{
    /// <summary>سازنده با ViewModel — بستن از طریق رویداد ViewModel</summary>
    public LabTestResultEntryWindow(LabTestResultEntryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();
    }
}
