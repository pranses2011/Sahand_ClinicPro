using System.Windows.Controls;
using System.Windows.Input;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت فهرست بیماران — دابل‌کلیک روی ردیف = مشاهده جزئیات (طبق قوانین کلی UI)
/// </summary>
public partial class PatientsView : UserControl
{
    /// <summary>سازنده پیش‌فرض</summary>
    public PatientsView()
    {
        InitializeComponent();
    }

    /// <summary>دابل‌کلیک روی ردیف — باز شدن پرونده بیمار</summary>
    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PatientsViewModel vm && vm.SelectedPatient is not null)
        {
            vm.ViewPatientCommand.Execute(null);
        }
    }
}
