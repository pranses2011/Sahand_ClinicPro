using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پشت‌صحنهٔ پنجرهٔ مدیریت اتاق‌ها — چرخه بیست‌ویکم (گزارش پانزدهم مالک)
/// فقط InitializeComponent؛ همهٔ منطق در RoomsManagerViewModel است.
/// </summary>
public partial class RoomsManagerWindow : Window
{
    /// <summary>سازنده با ViewModel تزریق‌شده</summary>
    public RoomsManagerWindow(RoomsManagerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
