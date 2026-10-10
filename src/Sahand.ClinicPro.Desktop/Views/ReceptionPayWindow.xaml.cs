using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ پرداخت پذیرش — چرخهٔ سی‌ونه‌م (گزارش مالک):
/// «کارت خوان در پذیرش پیاده سازی نشده». فقط اتصال ViewModel و رویداد بستن؛
/// نتیجهٔ انتخاب کاربر (روش + مرجع + کارت) پس از بسته‌شدن از ویومدل خوانده می‌شود.
/// </summary>
public partial class ReceptionPayWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public ReceptionPayWindow(ReceptionPayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += () =>
        {
            DialogResult = viewModel.Result is not null;
            Close();
        };
    }
}
