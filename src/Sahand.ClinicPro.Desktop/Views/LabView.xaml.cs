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
            // کاتالوگ تست‌ها — جدول اصلی و پنل بینش مخفی و کاتالوگ نمایان
            OrdersGrid.Visibility = Visibility.Collapsed;
            InsightPanel.Visibility = Visibility.Collapsed;
            _ = vm.LoadCatalogAsync();
        }
        else
        {
            OrdersGrid.Visibility = Visibility.Visible;
            InsightPanel.Visibility = Visibility.Visible;
            vm.StatusFilterIndex = status;
            _ = vm.LoadAsync();
        }
    }

    /// <summary>کارت آمار «همه» — تب همهٔ درخواست‌ها (طراحی نسخهٔ ۳)</summary>
    private void OnStatAllClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => MainTabs.SelectedIndex = 0;

    /// <summary>کارت آمار «سفارش‌شده» — تب سفارش‌شده‌ها</summary>
    private void OnStatOrderedClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => MainTabs.SelectedIndex = 3;

    /// <summary>کارت آمار «نمونه‌گیری‌شده» — فیلتر وضعیت ۲ (طرح تأییدشده: هر پنج کارت کلیک‌پذیر)</summary>
    private void OnStatSampledClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not LabViewModel vm) return;
        OrdersGrid.Visibility = Visibility.Visible;
        InsightPanel.Visibility = Visibility.Visible;
        vm.StatusFilterIndex = 2;
        _ = vm.LoadAsync();
    }

    /// <summary>کارت آمار «در حال انجام» — تب در حال انجام</summary>
    private void OnStatProgressClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => MainTabs.SelectedIndex = 1;

    /// <summary>کارت آمار «نتایج آماده» — تب نتایج آماده</summary>
    private void OnStatDoneClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => MainTabs.SelectedIndex = 2;

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
