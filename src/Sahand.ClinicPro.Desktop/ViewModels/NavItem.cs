using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// آیتم ناوبری ستون کنار — آیکون و عنوان و کلید صفحه.
/// چرخه بیست‌ویکم (گزارش پانزدهم مالک): «گزینه‌های لیست کناری مرتب نیستند و
/// شلوغ است؛ بر اساس عملکرد به‌صورت زیر منو قرار بده» — پشتیبانی از
/// زیرمنو با مجموعهٔ Children و وضعیت باز/بسته (IsExpanded) و هایلایت
/// صفحهٔ فعال (IsCurrent).
/// </summary>
public partial class NavItem : ObservableObject
{
    /// <summary>عنوان فارسی صفحه</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>کلید صفحه برای ناوبری — تهی برای آیتم‌های فقط-گروه</summary>
    public string PageKey { get; init; } = string.Empty;

    /// <summary>نام آیکون Material Design</summary>
    public string Icon { get; init; } = string.Empty;

    /// <summary>آیا این صفحه فعال است (در گام‌های بعدی ماژول فعال می‌شود)</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// زیرمنو — اگر پر باشد آیتم به‌صورت گروه بازشو دیده می‌شود؛
    /// کلیک روی آیتم گروه فقط باز/بسته می‌کند و ناوبری از فرزندان انجام می‌شود.
    /// </summary>
    public ObservableCollection<NavItem> Children { get; } = new();

    /// <summary>آیا زیرمنو دارد (برای انتخاب قالب آیتم)</summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>باز بودن گروه — برای آیتم‌های والد</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// این آیتم صفحهٔ فعال فعلی است — هایلایت منو؛ چرخه بیست‌ویکم:
    /// برای فرزندان زیرمنو لازم است چون در ListBox تودرتوی جدا از والدند.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrent;
}
