using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// مبدل نام دستهٔ منو به رنگ تأکیدی آن — چرخهٔ ۴۲: سرصفحهٔ گروه‌های انتخابگر
/// میانبرها فقط نام گروه را از CollectionViewGroup می‌گیرد؛ این مبدل همان نام
/// را به پالت رنگی PageCatalog.GroupColors می‌برد تا نقطهٔ سرصفحهٔ هر گروه با
/// رنگ لبه/دایرهٔ کارت‌های همان دسته هماهنگ باشد.
/// </summary>
public sealed class GroupNameToColorConverter : IValueConverter
{
    /// <summary>اجرای مبدل — نام گروه به هگز رنگ؛ ناشناخته = خاکستری</summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => new SolidColorBrush((Color)ColorConverter.ConvertFromString(
            Helpers.PageCatalog.ColorOfGroup(value as string)));

    /// <summary>تبدیل معکوس لازم نیست — یک‌طرفه است</summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// کد پشت انتخابگر میانبر داشبورد — چرخهٔ ۴۲ (گزارش مالک: «در فرم انتخاب
/// میانبرهای داشبود، ظاهر بدی داره و زیباتر و حرفه‌ای ترش بکن»).
/// بازطراحی کامل رفتار:
///   • گروه‌بندی زندهٔ صفحات بر اساس دستهٔ منو (ListCollectionView +
///     PropertyGroupDescription) — با فیلترِ جستجو، گروه‌های بی‌نتیجه خودکار
///     از صحنه حذف می‌شوند و ترتیب گروه‌ها همان ترتیب طبیعی منوی برنامه است
///   • انتخاب چندگانه: SelectionMode=Multiple روی خود ListBox؛ چک‌باکس هر
///     کارت دوسویه به IsSelected آیتم بسته است پس کلیک کارت و کلیک چک‌باکس
///     همیشه هم‌نتیجه‌اند؛ خروجی SelectedKeys فهرست کلیدهای همهٔ انتخاب‌شده‌هاست
///   • جستجوی زنده با شمار نتیجه، حالت «یافت نشد» و دکمهٔ افزودن هوشمند
///     (غیرفعال با راهنما وقتی هیچ کارتی انتخاب نشده؛ متن با شمار ارقام فارسی)
/// فهرست سبک بدون ویومدل مستقل باقی مانده — آیتم‌ها همان ShortcutTileهای
/// DashboardViewModel هستند (قانون منبع یگانهٔ PageCatalog).
/// </summary>
public partial class ShortcutPickerWindow : Window
{
    /// <summary>فهرست کامل قابل نمایش — مرجع فیلتر جستجو</summary>
    private readonly List<DashboardViewModel.ShortcutTile> _all;

    /// <summary>نمای فهرست با گروه‌بندی و فیلتر زنده — منبع ItemsList</summary>
    private readonly ListCollectionView _view;

    /// <summary>کلیدهای صفحه‌های انتخاب‌شده — تهی/خالی یعنی انصراف</summary>
    public List<string>? SelectedKeys { get; private set; }

    /// <summary>سازنده با فهرست صفحات مجازِ هنوز‌پین‌نشده</summary>
    public ShortcutPickerWindow(List<DashboardViewModel.ShortcutTile> available)
    {
        InitializeComponent();
        _all = available;

        // نمای گروه‌بندی‌شده بر نام دسته — گروه‌ها به ترتیب رخورد در فهرست
        // (جریان درمان … مدیریت سامانه) و هر گروه به ترتیب طبیعی کاتالوگ
        var source = new ListCollectionView(_all);
        source.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DashboardViewModel.ShortcutTile.Group)));
        _view = source;
        ItemsList.ItemsSource = _view;

        // شمارها و برچسب‌های آغازین + اتصال رویداد تغییر انتخاب
        CountBadge.Text = Fa($"{_all.Count} صفحه در دسترس");
        RefreshCounts();
        ItemsList.SelectionChanged += (_, _) => RefreshCounts();

        if (_all.Count == 0) Close();
    }

    /// <summary>
    /// جستجوی زنده — فیلتر روی عنوان یا نام دسته (مثلاً نوشتن «مالی» همهٔ
    /// صفحات دستهٔ مالی و بیمه را می‌آورد)؛ شمار نتیجه و حالت خالی هم‌زمان
    /// تازه می‌شوند و گروه‌های بی‌نتیجه خودکار پنهان می‌شوند.
    /// </summary>
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var term = (SearchBox.Text ?? string.Empty).Trim();
        _view.Filter = string.IsNullOrEmpty(term)
            ? null
            : obj => obj is DashboardViewModel.ShortcutTile t
                && (t.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || t.Group.Contains(term, StringComparison.OrdinalIgnoreCase));
        RefreshCounts(term);
    }

    /// <summary>
    /// تازه‌سازی شمار نتیجه، حالت خالی و متن/فعالیتیِ دکمهٔ افزودن — ارقام
    /// فارسی (قانون ارقام فارسی مالک؛ همان قالب‌بندی سراسری fa-IR).
    /// </summary>
    /// <param name="term">عبارت جستجوی جاری — تهی یعنی بدون فیلتر</param>
    private void RefreshCounts(string? term = null)
    {
        var visible = _view.OfType<object>().Count();
        ResultCount.Text = string.IsNullOrEmpty(term)
            ? Fa($"نمایش {visible} صفحه در {_view.Groups?.Count ?? 0} دسته")
            : Fa($"{visible} نتیجه برای «{term}»");

        // حالت «یافت نشد» فقط وقتی فیلتری هست و هیچ نتیجه‌ای نیست
        EmptyState.Visibility = visible == 0 && !string.IsNullOrEmpty(term)
            ? Visibility.Visible
            : Visibility.Collapsed;

        // دکمهٔ افزودن هوشمند — شمار آیتم‌های انتخاب‌شده از خود لیست‌باکس
        var selectedCount = ItemsList.SelectedItems.Count;
        AddButton.IsEnabled = selectedCount > 0;
        AddButton.Content = selectedCount > 0
            ? Fa($"افزودن ({selectedCount}) میانبر")
            : "ابتدا صفحه‌ای انتخاب کنید";
        FooterHint.Text = selectedCount > 0
            ? "با «افزودن»، کاشی‌های انتخاب‌شده به داشبورد شما پین می‌شوند"
            : "برای انتخاب چند صفحه، چند کارت را تیک بزنید";
    }

    /// <summary>دکمهٔ افزودن — ثبت کلیدهای انتخاب‌شده و بستن</summary>
    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        SelectedKeys = ItemsList.SelectedItems
            .OfType<DashboardViewModel.ShortcutTile>()
            .Select(t => t.Key)
            .ToList();
        Close();
    }

    /// <summary>دکمهٔ انصراف — بستن بدون انتخاب (SelectedKeys تهی می‌ماند)</summary>
    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    /// <summary>
    /// تبدیل ارقام لاتین متن به فارسی — همان الزام ارقام فارسی برنامه؛ برای
    /// شمار‌های درون‌صفحه‌ای که از قالب‌بندی زبان عنصر WPF بی‌بهره‌اند.
    /// </summary>
    private static string Fa(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
                chars[i] = (char)('۰' + (chars[i] - '0'));
        }
        return new string(chars);
    }
}
