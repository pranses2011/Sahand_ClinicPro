using System.Windows;
using System.Windows.Controls;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت آزمایشگاه — گام U10: تب‌های وضعیت + کاتالوگ
/// </summary>
public partial class LabView : UserControl
{
    /// <summary>سازنده</summary>
    public LabView() => InitializeComponent();

    /// <summary>تغییر تب وضعیت → فیلتر خودکار لیست درخواست‌ها</summary>
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0 || DataContext is not LabViewModel vm) return;
        if (e.AddedItems[0] is not TabItem tab || tab.Tag is not string tag) return;
        if (!int.TryParse(tag, out var status)) return;

        if (status < 0)
        {
            // کاتالوگ تست‌ها — جدول اصلی مخفی و کاتالوگ نمایان
            OrdersGrid.Visibility = Visibility.Collapsed;
            _ = vm.LoadCatalogAsync();
        }
        else
        {
            OrdersGrid.Visibility = Visibility.Visible;
            vm.StatusFilterIndex = status;
            _ = vm.LoadAsync();
        }
    }

    /// <summary>
    /// چرخهٔ ۷۹ — دکمهٔ مستقل «مدیریت بازه‌های مرجع» در نوار ابزار:
    /// فرم ویرایش بازه‌ها (کاتالوگ تست‌ها) از تب پنجم پنهان بود و مالک آن را
    /// پیدا نمی‌کرد («فرمی هم برای اصلاحشون نیست»)؛ این دکمه مستقیماً همان
    /// تب را انتخاب می‌کند و OnTabChanged بارگذاری کاتالوگ را انجام می‌دهد.
    /// </summary>
    private void OnOpenCatalogClick(object sender, RoutedEventArgs e)
        => MainTabs.SelectedIndex = 4; // تب پنجم: 📖 کاتالوگ تست‌ها

    /// <summary>دابل‌کلیک → ثبت نتیجه</summary>
    private void OnOrderDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is LabViewModel vm && vm.SelectedOrder is not null)
            vm.RecordResultCommand.Execute(null);
    }
}
