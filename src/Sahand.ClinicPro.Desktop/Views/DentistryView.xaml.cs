using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مستقل «دندانپزشکی» — چرخهٔ چهل‌وسوم: تفکیک از پاراکلینیکِ تب‌دار
/// به صفحهٔ اختصاصی (گزارش مالک: «برای هر کدوم یک صفحه مستقل طراحی بکن»).
/// </summary>
public partial class DentistryView : UserControl
{
    /// <summary>سازنده — مقداردهی اولیهٔ اجزا</summary>
    public DentistryView()
    {
        InitializeComponent();
    }
    private void PrintDentalChart_Click(object sender, RoutedEventArgs e) => PrintDental(false);
    private void PrintDentalPlan_Click(object sender, RoutedEventArgs e) => PrintDental(true);

    private void PrintDental(bool plan)
    {
        if (DataContext is not DentistryViewModel vm || vm.CreatePrintSnapshot() is not { } snapshot) return;
        if (plan && snapshot.Plan.Count == 0)
        {
            AppDialog.Warn("چاپ طرح درمان", "ابتدا یک قلم به طرح درمان اضافه کنید.");
            return;
        }
        var key = plan ? "DentalTreatmentPlan" : "DentalChart";
        var title = plan ? "پیش‌نویس طرح درمان دندان" : "پیش‌نویس چارت دندان";
        var document = plan ? DentalPrintService.BuildPlan(snapshot, IranClock.Now) : DentalPrintService.BuildChart(snapshot, IranClock.Now);
        PrintFlow.Print(document, title, PrintJobSettings.PrinterFor(key), jobKey: key);
    }

}
