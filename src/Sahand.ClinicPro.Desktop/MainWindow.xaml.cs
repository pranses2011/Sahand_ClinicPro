using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop;

/// <summary>
/// کد پشت پنجره اصلی — فقط اتصال به ViewModel تزریق‌شده
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>سازنده با تزریق ViewModel پوسته</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
