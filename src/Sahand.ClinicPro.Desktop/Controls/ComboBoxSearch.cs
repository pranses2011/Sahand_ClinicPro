using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// رفتار «جستجوی تایپی» کمبوباکس — چرخهٔ چهل‌وسوم (گزارش مالک)
/// </summary>
/// <remarks>
/// «تمامی فیلدهایی که بصورت کمبوباس هستند و گزینه‌هایی دارند تا انتخاب کنیم
/// رو قابلیت تایپ کردن هم بهشون اضافه بکن تا با تایپ کردن، زودتر گزینهٔ مورد
/// نظر رو پیدا بکنیم (تایپ کردن مانند جستجو هم عمل بکنه یعنی اگه داخل گزینه‌های
/// کمبوباکس یک گزینه داریم با عنوان "تزریقات و پانسمان سرپایی"، با تایپ کردن
/// پانسمان این گزینه رو هم نشون بده و فقط پانسمان رو در اول گزینه‌ها نگرده)»
///
/// پیاده‌سازی به‌صورت «خاصیت ضمیمه» + استایل ضمنی سراسری: استایل ضمنی
/// ComboBox در App.xaml این خاصیت را برای «همهٔ» کمبوباکس‌های برنامه روشن
/// می‌کند (قانون هم‌کلاسی‌ها) و کمبوباکس‌های خاص می‌توانند با
/// ComboBoxSearch.IsEnabled="False" صریحاً خارج شوند (مثل کمبوباکس‌های
/// ویرایش‌پذیری که متنِ تازهٔ تایپ‌شده را به منطق «ساخت مقدار جدید»
/// می‌فرستند — سمت/دپارتمان فرم پرسنل — یا قالب‌های آیتمِ سفارشی که متنِ
/// نمایشی قلم‌هایشان از ToString می‌آید).
///
/// رفتار:
///   • کمبو ویرایش‌پذیر می‌شود (IsEditable) و با هر تایپ، فهرست گزینه‌ها
///     «زنده» پالایش می‌شود — تطبیق از نوع «شامل‌بودن» است نه فقط «شروع‌با»؛
///   • تطبیق با «نرمال‌سازی فارسی» انجام می‌شود: ي/ی و ك/ک عربی، ة→ه،
///     ارقام فارسی/عربی→لاتین، نیم‌فاصله و فاصله‌های اضافی نادیده — تا
///     صفحه‌کلید فارسی/عربی کاربر هرگز گزینه را پنهان نکند؛
///   • متن آیتم از DisplayMemberPath (بازتاب PropertyPath ساده) یا
///     ComboBoxItem.Content یا ToString خوانده می‌شود؛
///   • Enter = انتخاب نخستین گزینهٔ پالایش‌شده؛ کلیک روی گزینه = انتخاب
///     استاندارد WPF؛ پاک‌شدن متن = نمایش همهٔ گزینه‌ها؛
///   • در از دست دادن فوکوس، اگر متنِ تایپ‌شده دقیقاً با گزینه‌ای برابر شود
///     همان انتخاب می‌شود؛ در غیر این صورت متن کاربر دست‌نخورده می‌ماند
///     (فرم‌های «مقدار تازه» — که عمداً خارج نشده‌اند — می‌توانند همان را
///     بخوانند).
/// </remarks>
public static class ComboBoxSearch
{
    /// <summary>روشن/خاموش کردن رفتار روی یک کمبوباکس</summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(ComboBoxSearch),
            new FrameworkPropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>خواندن پرچم فعال‌بودن</summary>
    public static bool GetIsEnabled(DependencyObject obj)
        => (bool)obj.GetValue(IsEnabledProperty);

    /// <summary>تنظیم پرچم فعال‌بودن</summary>
    public static void SetIsEnabled(DependencyObject obj, bool value)
        => obj.SetValue(IsEnabledProperty, value);

    /// <summary>فعال‌سازی/غیرفعال‌سازی رفتار — سیم‌کشی رویدادها فقط یک‌بار</summary>
    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo) return;
        var enabled = (bool)e.NewValue;

        if (enabled)
        {
            // نصب رفتار — گارد دوباره‌کاری (استایل + مقدار صریح روی یک کنترل)
            if (GetState(combo) is not null) return;
            var state = new SearchState();
            combo.SetValue(StateProperty, state);

            // ویرایش‌پذیر + بازماندن پاپ‌آپ هنگام تایپ + خاموشی تکمیل خودکار WPF
            // (IsTextSearchEnabled پیش‌فرض فقط «شروع‌با» تطبیق می‌دهد و با پالایش
            // زندهٔ ما می‌جنگید)
            combo.IsEditable = true;
            combo.StaysOpenOnEdit = true;
            combo.IsTextSearchEnabled = false;

            combo.AddHandler(TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler(OnComboTextChanged));
            combo.AddHandler(Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnComboSelectionChanged));
            combo.PreviewKeyDown += OnComboPreviewKeyDown;
            combo.LostFocus += OnComboLostFocus;
            combo.DropDownClosed += OnComboDropDownClosed;
        }
        else if (e.OldValue is true && GetState(combo) is not null)
        {
            // خاموش‌سازی صریح — فقط رویدادها را جدا می‌کنیم؛ IsEditable را
            // دست نمی‌زنیم تا کمبوباکس‌هایی که از ابتدا ویرایش‌پذیر بودند
            // (سمت/دپارتمان پرسنل) رفتار پیشین خود را حفظ کنند
            combo.SetValue(StateProperty, null);
            combo.RemoveHandler(TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler(OnComboTextChanged));
            combo.RemoveHandler(Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnComboSelectionChanged));
            combo.PreviewKeyDown -= OnComboPreviewKeyDown;
            combo.LostFocus -= OnComboLostFocus;
            combo.DropDownClosed -= OnComboDropDownClosed;
        }
    }

    /// <summary>وضعیت داخلی هر کمبوباکس — گارد بازگشتیِ همگام‌سازی متن/انتخاب</summary>
    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached("State", typeof(SearchState),
            typeof(ComboBoxSearch), new FrameworkPropertyMetadata(null));

    /// <summary>خواندن وضعیت داخلی</summary>
    private static SearchState? GetState(DependencyObject obj)
        => (SearchState?)obj.GetValue(StateProperty);

    /// <summary>وضعیت جستجوی یک کمبوباکس</summary>
    private sealed class SearchState
    {
        /// <summary>در حال همگام‌سازی متن از انتخاب هستیم؟ (تایپ کاربر نیست)</summary>
        public bool SyncingText;

        /// <summary>متنِ پیشینِ تایپ‌شده برای تشخیص تغییر واقعی</summary>
        public string LastTerm = string.Empty;
    }

    // ═════════════════════════ رویدادها ═════════════════════════

    /// <summary>تغییر متن — اگر تایپ کاربر است، پالایش زنده انجام می‌شود</summary>
    private static void OnComboTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var state = GetState(combo);
        if (state is null || state.SyncingText) return;

        var term = combo.Text ?? string.Empty;
        if (term == state.LastTerm) return;

        // ═══ تشخیص «همگام‌سازی متن از انتخاب» در برابر «تایپ کاربر» ═══
        // متنِ کمبوی ویرایش‌پذیر با انتخاب گزینه هم یکی می‌شود (و با مقدار اولیهٔ
        // اتصال SelectedItem هنگام بارگذاری صفحه) — این تغییرها «تایپ» نیستند و
        // نباید فهرست را پالایش کنند؛ تنها وقتی پالایش می‌شود که متن از متنِ
        // گزینهٔ منتخبِ فعلی فاصله بگیرد (هم‌ارز نرمال‌شده).
        var selectedText = combo.SelectedItem is null
            ? string.Empty
            : GetItemText(combo, combo.SelectedItem);
        if (Normalize(term) == Normalize(selectedText))
        {
            state.LastTerm = term;
            return;
        }

        state.LastTerm = term;

        ApplyFilter(combo, term);

        // پاپ‌آپ هنگام پالایش باز بماند تا نتیجه‌ها همان لحظه دیده شوند؛
        // فقط وقتی گزینه‌ای مانده باشد (پاپ‌آپ خالی آزاردهنده است)
        if (!combo.IsDropDownOpen && combo.HasItems)
            combo.IsDropDownOpen = true;
    }

    /// <summary>تغییر انتخاب — فیلتر پاک و متن با گزینهٔ منتخب همگام می‌شود</summary>
    private static void OnComboSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var state = GetState(combo);
        if (state is null) return;

        ApplyFilter(combo, string.Empty);
        state.LastTerm = combo.Text ?? string.Empty;
    }

    /// <summary>Enter — انتخاب نخستین گزینهٔ پالایش‌شده و بستن پاپ‌آپ</summary>
    private static void OnComboPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        if (e.Key != Key.Enter) return;

        var view = GetItemView(combo);
        if (view is null) return;

        foreach (var item in view.OfType<object>())
        {
            // نخستین گزینهٔ قابل‌دیدن پس از پالایش — انتخاب و پایان
            SelectItem(combo, item);
            break;
        }
        combo.IsDropDownOpen = false;
        e.Handled = true;
    }

    /// <summary>از دست دادن فوکوس — تطبیق دقیق متن با گزینه‌ها</summary>
    private static void OnComboLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var state = GetState(combo);
        if (state is null) return;

        var term = Normalize(combo.Text ?? string.Empty);
        if (term.Length == 0) return;

        foreach (var item in combo.Items.OfType<object>())
        {
            if (Normalize(GetItemText(combo, item)) == term)
            {
                SelectItem(combo, item);
                return;
            }
        }
        // متنِ بدون تطبیق — دست‌نخورده می‌ماند (فرم‌های «مقدار تازه» خودشان
        // تصمیم می‌گیرند؛ فرم‌های انتخابی هم با مقدار تهی/قبلی ذخیره می‌شوند)
    }

    /// <summary>بسته‌شدن پاپ‌آپ — پالایش پس از انتخاب پاک می‌شود</summary>
    private static void OnComboDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo) return;
        var state = GetState(combo);
        if (state is null) return;
        ApplyFilter(combo, string.Empty);
        state.LastTerm = combo.Text ?? string.Empty;
    }

    // ═════════════════════════ پالایش ═════════════════════════

    /// <summary>اعمال فیلتر «شامل‌بودنِ نرمال‌شده» روی نمای آیتم‌ها</summary>
    private static void ApplyFilter(ComboBox combo, string term)
    {
        var view = GetItemView(combo);
        if (view is null) return;

        var normalized = Normalize(term);
        if (normalized.Length == 0)
        {
            if (view.Filter is not null)
            {
                view.Filter = null;
            }
            return;
        }

        view.Filter = item =>
        {
            var text = GetItemText(combo, item);
            return Normalize(text).Contains(normalized, StringComparison.Ordinal);
        };
    }

    /// <summary>نمای فهرست آیتم‌ها — از ItemsSource یا خود Items</summary>
    private static ICollectionView? GetItemView(ComboBox combo)
    {
        var source = combo.ItemsSource ?? (combo.HasItems ? combo.Items : null);
        if (source is null) return null;
        var view = CollectionViewSource.GetDefaultView(source);
        // فهرست‌های اشیاء بدون گروه/مرتب‌سازی — نمای پیش‌فرض کافی است
        return view;
    }

    /// <summary>انتخاب آیتم با گارد همگام‌سازی متن</summary>
    private static void SelectItem(ComboBox combo, object item)
    {
        var state = GetState(combo);
        if (state is not null) state.SyncingText = true;
        try
        {
            combo.SelectedItem = item;
            // متن ویرایش‌پذیر باید همان لحظه با گزینه یکی شود
            combo.Text = GetItemText(combo, item);
            if (state is not null) state.LastTerm = combo.Text;
        }
        finally
        {
            if (state is not null) state.SyncingText = false;
        }
    }

    // ═════════════════════════ متن آیتم ═════════════════════════

    /// <summary>
    /// نگاشت کش‌شدهٔ DisplayMemberPath → خوانندهٔ سریع مقدار؛ کمبوباکس‌های
    /// هم‌شکل از یک نوع آیتم، همان خوانندهٔ کامپایل‌شده را بازاستفاده می‌کنند.
    /// </summary>
    private static readonly Dictionary<string, Func<object, string?>> PathGetters = new();

    /// <summary>متن نمایشی یک آیتم — Content کمبوباکس‌آیتم یا مسیر نمایشی یا ToString</summary>
    private static string GetItemText(ComboBox combo, object? item)
    {
        switch (item)
        {
            case null:
                return string.Empty;
            case ComboBoxItem comboBoxItem:
                return comboBoxItem.Content?.ToString() ?? string.Empty;
            case string text:
                return text;
        }

        var path = combo.DisplayMemberPath;
        if (!string.IsNullOrEmpty(path))
        {
            var key = item.GetType().FullName + "|" + path;
            if (!PathGetters.TryGetValue(key, out var getter))
            {
                getter = CompilePathGetter(item.GetType(), path);
                PathGetters[key] = getter;
            }
            return getter(item) ?? string.Empty;
        }

        // قالب سفارشی بدون DisplayMemberPath — ToString بهترین تلاش است؛
        // این کمبوباکس‌ها بهتر است با IsEnabled=False خارج شوند (راهنمای کلاس)
        return item.ToString() ?? string.Empty;
    }

    /// <summary>ساخت خوانندهٔ مقدار برای مسیر نمایشی ساده («Name» یا «Item1.Value»)</summary>
    private static Func<object, string?> CompilePathGetter(Type itemType, string path)
    {
        // مسیرهای سادهٔ نقطه‌چین — پیمایش بازتابی با کش نوع/پراپرتی
        var parts = path.Split('.');
        return obj =>
        {
            var current = obj;
            foreach (var part in parts)
            {
                if (current is null) return null;
                var prop = current.GetType().GetProperty(part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                current = prop?.GetValue(current);
            }
            return current?.ToString();
        };
    }

    /// <summary>نرمال‌سازی فارسی برای تطبیق — هم‌ارزسازی نویسه‌های عربی/فارسی و ارقام</summary>
    internal static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            var c = ch switch
            {
                'ي' => 'ی',      // ي عربی → ی فارسی
                'ك' => 'ک',      // ك عربی → ک فارسی
                'ة' => 'ه',
                'أ' or 'إ' or 'آ' => 'ا',
                'ؤ' => 'و',
                'ئ' => 'ی',
                '\u200c' => ' ', // نیم‌فاصله → فاصله (برای برابری عبارت‌ها)
                >= '۰' and <= '۹' => (char)('0' + ch - '۰'), // ارقام فارسی → لاتین
                >= '٠' and <= '٩' => (char)('0' + ch - '٠'), // ارقام عربی → لاتین
                _ => ch
            };
            builder.Append(c);
        }
        return builder.ToString()
            .Trim()
            .Replace("  ", " ", StringComparison.Ordinal); // فاصله‌های دوتایی در عبارت جستجو
    }
}
