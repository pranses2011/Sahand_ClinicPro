using System.Windows.Controls;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت صفحهٔ مدیریت اتاق‌ها — چرخهٔ سی‌ونه‌م (گزارش مالک):
/// «مدیریت اتاق‌ها رو در منوی کناری هم قرار بده». فقط InitializeComponent؛
/// همهٔ منطق در RoomsManagerViewModel مشترک با پنجرهٔ فرم پزشک است.
/// </summary>
public partial class RoomsView : UserControl
{
    /// <summary>سازندهٔ پیش‌فرض — برای طراح XAML</summary>
    public RoomsView()
    {
        InitializeComponent();
    }
}
