using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ قلم جدید انبار — چرخهٔ سی‌ونه‌م (گزارش مالک):
/// «خرید و انبار و قیمت‌های این لوازم هم باید باشه». فقط اتصال ViewModel و
/// بستن پنجره؛ همهٔ منطق در PharmacyItemFormViewModel است.
/// </summary>
public partial class PharmacyItemFormWindow : Window
{
    /// <summary>سازنده با ViewModel</summary>
    public PharmacyItemFormWindow(PharmacyItemFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        // بستن پنجره پس از ثبت موفق یا انصراف — با رویداد مشترک ویومدل
        viewModel.RequestClose += () => Close();
    }
}
