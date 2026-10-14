using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Sahand.ClinicPro.Desktop.ViewModels;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// ═══ پنجرهٔ مستقل پیام‌های شناور — چرخهٔ ۷۵ (هفتمین گزارش بریدگی پایین) ═══
/// پس از شش اصلاحِ درون‌پنجره‌ای (حاشیه، جایگاه میزبان، پایهٔ TranslateTransform،
/// حذف DropShadowEffect، MinHeight، انتقال به بالای میزکار) که هرکدام در محیط
/// آزمایشی درست بودند ولی مالک همچنان «قسمت پایین توست بریده است» را گزارش
/// می‌کرد، معماری میزبانی عوض شد: کپسول‌ها در یک «پنجرهٔ ویندوزی مستقلِ»
/// بی‌قاب و شفاف نمایش می‌یابند — همان الگوی اعلان‌های خود ویندوز ۱۱.
/// یک پنجرهٔ مستقل سطح رندر مخصوص خودش دارد؛ هیچ عنصر والدی در پنجرهٔ
/// اصلی (لایه، حاشیهٔ چیدمان، هم‌پوشانی نوار وضعیت، گردکردن پیکسلی DPI
/// کسری یا رفتار درایور گرافیک) نمی‌تواند آن را clip کند — بریدگی لبهٔ
/// پایین در هر محیطی سازوکاراً منتفی است.
/// </summary>
public partial class ToastWindow : Window
{
    /// <summary>پنجرهٔ اصلی مالک — برای هم‌گامی جایگاه و چرخهٔ عمر</summary>
    private readonly Window _owner;

    /// <summary>ViewModel مشترک با پنجرهٔ اصلی — فهرست Toasts و فرمان بستن</summary>
    private readonly MainWindowViewModel _viewModel;

    // ═══ پرچم‌های ویندوز برای رفتار «غیرفعال‌گیر» — اعلانِ بی‌مزاحمت ═══
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x0800_0000; // هرگز با نمایشش فوکوس نمی‌گیرد
    private const int WsExToolWindow = 0x0000_0080; // در تسک‌بار/Alt-Tab دیده نمی‌شود

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    /// <summary>
    /// سازنده — پنجرهٔ مستقل توست با مالکیت پنجرهٔ اصلی.
    /// فهرست پیام‌ها همان ObservableCollection مشترک ViewModel است؛ افزودن/
    /// حذف توست از هر جای برنامه بی‌درنگ اینجا منعکس می‌شود.
    /// </summary>
    /// <param name="owner">پنجرهٔ اصلی برنامه — جایگاه توست نسبت به آن محاسبه می‌شود</param>
    /// <param name="viewModel">ViewModel مشترک پنجرهٔ اصلی (فهرست Toasts)</param>
    public ToastWindow(Window owner, MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _owner = owner;
        _viewModel = viewModel;
        Owner = owner;
        DataContext = viewModel;

        // نمایش/پنهانی خودکار با آمدن و رفتن پیام‌ها — پنجرهٔ خالی هرگز دیده نمی‌شود
        _viewModel.Toasts.CollectionChanged += (_, _) => SyncVisibility();

        // هم‌گامی جایگاه با جابه‌جایی/بازاندازه/بازگشت پنجرهٔ اصلی — توست
        // همیشه میانهٔ افقی و درست زیر نوار بالا می‌ماند حتی وقتی کاربر
        // پنجره را می‌کشد، تمام‌صفحه می‌کند یا مینیمایم می‌کند.
        _owner.LocationChanged += (_, _) => Reposition();
        _owner.SizeChanged += (_, _) => Reposition();
        _owner.StateChanged += (_, _) =>
        {
            // پنجرهٔ اصلی مینیمایم شد → توست هم پنهان (اعلان شناور روی دسکتاپ
            // خالی بی‌معناست)؛ بازگشت → اگر پیامی مانده دوباره نمایش.
            if (_owner.WindowState == WindowState.Minimized)
                Hide();
            else
                SyncVisibility();
        };

        // بسته‌شدن پنجرهٔ اصلی = بستن این پنجره (مالکیت ویندوزی هم همین را
        // تضمین می‌کند؛ این گارد فقط برای اطمینان دوگانه است)
        _owner.Closed += (_, _) => Close();

        // جایگاه نخستین پیش از هر نمایش
        Reposition();
    }

    /// <summary>
    /// پس از ساخت هندل ویندوز — نصب پرچم‌های غیرفعال‌گیر:
    /// WS_EX_NOACTIVATE تضمین می‌کند نمایش پنجره هیچ‌وقت فوکوس را از فرم
    /// فعالِ برنامه نکشد (کاربر وسط تایپ، مکث ناگهانی ندارد) و
    /// WS_EX_TOOLWINDOW آن را از تسک‌بار و Alt-Tab پنهان می‌کند.
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            var style = GetWindowLong(source.Handle, GwlExStyle);
            SetWindowLong(source.Handle, GwlExStyle, style | WsExNoActivate | WsExToolWindow);
        }
    }

    /// <summary>
    /// جایگاه‌یابی — میانهٔ افقیِ پنجرهٔ اصلی، درست زیر نوار بالای آن.
    /// ارتفاع نوار بالا (TopBar) از درخت بصری اندازه‌گیری می‌شود؛ اگر هنوز
    /// آماده نیست مقدار امنِ ۶۴px (نمای معمول نوار) به‌کار می‌رود.
    /// مختصات WPF همهٔ پنجره‌ها DIP است؛ محاسبه در هر DPI و مانیتورِ
    /// چندگانه سازگار است.
    /// </summary>
    private void Reposition()
    {
        // پنجرهٔ مینیمایم جایگاه معنا ندارد — SyncVisibility پنهان‌اش کرده
        if (_owner.WindowState == WindowState.Minimized)
            return;

        // ارتفاع نوار بالا — از عنصر نام‌دار پنجرهٔ اصلی؛ در نبودش ۶۴px امن
        var topBar = _owner.FindName("TopBar") as FrameworkElement;
        var topBarHeight = topBar?.ActualHeight > 0 ? topBar.ActualHeight : 64;

        // زیرِ نوار بالا با ۱۲px فاصلهٔ تنفس؛ میانهٔ افقی نسبت به پنجرهٔ اصلی.
        // عرض این پنجره توسط SizeToContent بعد از layout در دسترس است؛ برای
        // نخستین چیدمانِ پیش از آن، تقریب امن (نصف عرض خودش) به‌کار می‌رود.
        var selfWidth = ActualWidth > 0 ? ActualWidth : Width;
        var left = _owner.Left + Math.Max(0, (_owner.ActualWidth - selfWidth) / 2);
        Left = left;
        Top = _owner.Top + topBarHeight + 12;
    }

    /// <summary>
    /// هم‌گامسازی نمایش با فهرست پیام‌ها — پیام هست → دیده شو؛ فهرست خالی
    /// → فوراً پنهان (هزینهٔ صفر). جایگاه هم پیش از هر نمایش تازه می‌شود
    /// تا بعد از جابه‌جایی پنجرهٔ اصلی، توستِ قدیمی در جای غلط ننشیند.
    /// </summary>
    private void SyncVisibility()
    {
        if (_viewModel.Toasts.Count > 0)
        {
            if (!IsVisible)
            {
                Reposition();
                // Show بدون فعال‌سازی — ShowActivated=false روی خود پنجره
                Show();
            }
            else
            {
                Reposition();
            }
        }
        else if (IsVisible)
        {
            Hide();
        }
    }

    /// <summary>
    /// بستن با Esc نباید کار کند (پنجره فوکوس نمی‌گیرد) اما برای اطمینان،
    /// هر تلاش برای بستنِ دستی فقط پنهان می‌کند — چرخهٔ عمر واقعی به
    /// پنجرهٔ اصلی گره خورده است.
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // پنهان‌سازی به‌جای بستن اگر مالک هنوز زنده است — نمونهٔ سرویس‌گونه
        if (_owner.IsLoaded)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}
