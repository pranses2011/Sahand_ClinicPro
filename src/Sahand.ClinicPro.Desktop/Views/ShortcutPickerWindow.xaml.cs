using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت انتخابگر میانبر داشبورد — چرخهٔ سی‌ونه‌م (گزارش مالک).
/// فهرست سبک (بدون ویومدل مستقل): آیتم‌ها همان ShortcutTileهایِ DashboardViewModel
/// هستند؛ جستجوی زنده با فیلتر نام و انتخاب با کلیک — ساده و سریع.
/// </summary>
public partial class ShortcutPickerWindow : Window
{
    /// <summary>فهرست کامل قابل نمایش — مرجع فیلتر جستجو</summary>
    private readonly List<DashboardViewModel.ShortcutTile> _all;

    /// <summary>کلید صفحهٔ انتخاب‌شده — تهی یعنی انصراف</summary>
    public string? SelectedKey { get; private set; }

    /// <summary>سازنده با فهرست صفحات مجازِ هنوز‌پین‌نشده</summary>
    public ShortcutPickerWindow(List<DashboardViewModel.ShortcutTile> available)
    {
        InitializeComponent();
        _all = available;
        ItemsList.ItemsSource = _all;
        if (_all.Count == 0) Close();
    }

    /// <summary>جستجوی زنده — فیلتر نام صفحه بر اساس متن</summary>
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var term = (SearchBox.Text ?? string.Empty).Trim();
        ItemsList.ItemsSource = string.IsNullOrEmpty(term)
            ? _all
            : _all.Where(t => t.Title.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>کلیک روی آیتم — انتخاب و بستن پنجره</summary>
    private void OnItemClick(object sender, MouseButtonEventArgs e)
    {
        if (ItemsList.SelectedItem is DashboardViewModel.ShortcutTile tile)
        {
            SelectedKey = tile.Key;
            Close();
        }
    }
}
