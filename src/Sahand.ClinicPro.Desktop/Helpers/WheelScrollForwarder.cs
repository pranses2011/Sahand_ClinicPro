using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// هدایت‌گر چرخ موس به والدِ قابل‌اسکرول — چرخهٔ ۷۴ (گزارش مالک)
/// ═══════════════════════════════════════════════════════════════════
/// «وقتی wheel یا همون کلید چرخان موس رو استفاده میکنم، روی بعضی از عناصر
/// که اسکرول ندارند عمل نمیکنه و صفحه اسکرول نمیشه مثل اینکه قفل شده و یا
/// به ابتدا یا انتها رسیده باشه. ولی وقتی موس رو از روی اون عنصر کنار
/// میکشم دوباره میتونم اسکرول کنم».
///
/// ریشهٔ رفتار WPF: هر عنصرِ دارای ScrollView داخلی (DataGrid، ListBox،
/// ListView، کومبو و…) رویداد چرخ موس را «می‌خورد» حتی وقتی هیچ چیزی برای
/// اسکرول ندارد (محتوای کوتاه‌تر از درگاه، یا رسیده به ابتدا/انتها). نتیجه:
/// صفحهٔ زیرین هرگز اسکرول نمی‌شود و تجربهٔ «قفل‌شدن» ایجاد می‌کند.
///
/// راه‌حل استاندارد: پیش‌نمایشِ تونلی چرخ موس در سطح پنجره رهگیری می‌شود؛
/// اگر داخلی‌ترین اسکرول‌کنندهٔ زیر ماوس «در همان جهت» توان اسکرول ندارد،
/// نخستین والدِ توانمند دستی اسکرول می‌شود و رویداد مصرف‌شده علامت می‌خورد.
/// این رفتار با کلاس‌هندلر روی همهٔ پنجره‌های برنامه فعال می‌شود
/// (WheelScrollForwarder.Register در سازندهٔ استاتیک App) و به هیچ
/// صفحه/کنترلی نیازی به تغییر ندارد.
/// </summary>
public static class WheelScrollForwarder
{
    /// <summary>
    /// ثبت کلاس‌هندلر سراسری — یک‌بار در راه‌اندازی برنامه.
    /// handledEventsToo=true: حتی اگر عنصری رویداد را مصرف کرده باشد هم
    /// فراخوانی می‌شویم (برای رصد)، ولی با گارد Handled بی‌اثر عمل می‌کنیم.
    /// </summary>
    public static void Register() =>
        EventManager.RegisterClassHandler(
            typeof(Window),
            UIElement.PreviewMouseWheelEvent,
            new RoutedEventHandler(OnPreviewMouseWheel),
            handledEventsToo: true);

    /// <summary>
    /// منطق اصلی — پیش‌نمایش چرخ موس در سطح پنجره (پیش از همهٔ عناصر داخلی).
    /// ترتیب تصمیم:
    ///   ۱) زنجیرهٔ ScrollViewers از عنصر زیر ماوس به بالا جمع می‌شود؛
    ///   ۲) اگر داخلی‌ترین در جهت چرخش توان دارد → دست نمی‌زنیم (رفتار عادی)؛
    ///   ۳) وگرنه نخستین والدِ توانمند به‌صورت دستی اسکرول می‌شود و رویداد
    ///      مصرف‌شده علامت می‌خورد تا داخلیِ ناتوان دوباره نخوردش.
    /// </summary>
    private static void OnPreviewMouseWheel(object sender, RoutedEventArgs e)
    {
        if (e is not MouseWheelEventArgs args) return;
        if (args.Handled) return;
        if (e.OriginalSource is not DependencyObject original) return;

        // زنجیرهٔ اسکرول‌کننده‌ها: از داخلی‌ترین (زیر ماوس) به بیرونی‌ترین
        var chain = AncestorScrollViewers(original);
        if (chain.Count == 0) return;

        // داخلی‌ترین توان دارد؟ — رفتار عادی WPF کافی است
        if (CanScroll(chain[0], args)) return;

        // جابه‌جایی افقی با Shift — همان منطق با محور افقی
        var horizontal = (args.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0;

        // نخستین والدِ توانمند در همان جهت — اسکرول دستی
        for (var i = 1; i < chain.Count; i++)
        {
            if (!CanScroll(chain[i], args, horizontal)) continue;
            ScrollManually(chain[i], args.Delta, horizontal);
            // مصرف‌شده: داخلیِ ناتوان نباید پس از ما دوباره رویداد را بخورد
            args.Handled = true;
            return;
        }
        // هیچ والدِ توانمندی نبود — رویداد آزاد می‌ماند (رفتار پیش‌فرض)
    }

    /// <summary>
    /// آیا اسکرول‌کننده در جهتِ چرخشِ ماوس توانِ جابه‌جایی دارد؟
    /// «توان» = محتوای بلندتر از درگاه + نبودن در مرز همان جهت
    /// (در ابتدای لیست، چرخش به بالا توان نیست و برعکس).
    /// </summary>
    private static bool CanScroll(ScrollViewer viewer, MouseWheelEventArgs args, bool horizontal = false)
    {
        if (horizontal)
            return viewer.ScrollableWidth > 0.5 &&
                   (args.Delta > 0 ? viewer.HorizontalOffset > 0.5
                                   : viewer.HorizontalOffset < viewer.ScrollableWidth - 0.5);

        return viewer.ScrollableHeight > 0.5 &&
               (args.Delta > 0 ? viewer.VerticalOffset > 0.5
                               : viewer.VerticalOffset < viewer.ScrollableHeight - 0.5);
    }

    /// <summary>
    /// اسکرول دستی والدها — مقدار گام:
    /// اسکرول‌کنندهٔ پیکسلی (CanContentScroll=false): ~۵۴ پیکسل (≈ سه سطر متن)؛
    /// اسکرول‌کنندهٔ آیتمی (CanContentScroll=true — مثل DataGrid تو در تو):
    /// ~۳ آیتم (VerticalOffset در واحد آیتم است نه پیکسل).
    /// علامت: چرخش به پایین (Delta منفی) = رفتن به پایین = افزایش آفست.
    /// </summary>
    private static void ScrollManually(ScrollViewer viewer, double delta, bool horizontal)
    {
        var sign = delta > 0 ? -1 : 1; // بالا/راست مثبت → کاهش آفست
        if (horizontal)
        {
            var step = viewer.CanContentScroll ? 3 : 54;
            viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset + sign * step);
        }
        else
        {
            var step = viewer.CanContentScroll ? 3 : 54;
            viewer.ScrollToVerticalOffset(viewer.VerticalOffset + sign * step);
        }
    }

    /// <summary>
    /// جمع‌آوری زنجیرهٔ ScrollViewers از عنصر منبع به سمت ریشه —
    /// شامل اسکرول‌کننده‌های درون قالب کنترل‌ها (DataGrid/ListBox داخلی).
    /// </summary>
    private static List<ScrollViewer> AncestorScrollViewers(DependencyObject from)
    {
        var list = new List<ScrollViewer>();
        var node = from;
        while (node is not null)
        {
            if (node is ScrollViewer viewer)
                list.Add(viewer);
            node = node is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }
        return list;
    }
}
