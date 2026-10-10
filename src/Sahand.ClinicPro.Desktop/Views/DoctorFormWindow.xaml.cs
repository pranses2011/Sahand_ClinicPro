using System.Windows;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// پنجره فرم پزشک ۵ تب — گام U3
/// </summary>
public partial class DoctorFormWindow : Window
{
    /// <summary>سازنده با ViewModel و اتصال رویداد بستن</summary>
    public DoctorFormWindow(DoctorFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();

        // ═══ چرخهٔ سی‌وهفتم (گزارش مالک) ═══
        // «کلید عملیاتی برنامهٔ کاری در لیست پزشکان باید فرم را مستقیم روی تب
        // برنامهٔ کاری باز کند» — نمایهٔ تب اولیه از ViewModel خوانده می‌شود؛
        // فقط وقتی خارج از بازه باشد به تب نخست برمی‌گردیم تا هرگز استثنایی
        // رخ ندهد (مثلاً اگر روزی شمار تب‌ها تغییر کند). گزارهٔ dialog-safe
        // است: پیش از ShowDialog انجام می‌شود پس کاربر تبِ درست را از همان
        // لحظهٔ باز شدن پنجره می‌بیند.
        if (viewModel.InitialTabIndex is > 0 and < 5)
            FormTabs.SelectedIndex = viewModel.InitialTabIndex;
    }
}
