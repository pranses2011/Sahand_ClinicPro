using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجره گزارش تفصیلی سود و حقوق درصدی — چرخه دهم
/// </summary>
public partial class CommissionReportWindow : Window
{
    /// <summary>سازنده با ViewModel — بارگذاری اولیه خودکار</summary>
    public CommissionReportWindow(CommissionReportViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
