using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت تور هدایت‌شده اولین اجرا — چرخه سیزدهم (درخواست ۸-۶)؛
/// بازنویسی چرخه هجدهم (گزارش دوازدهم مالک):
/// ماسک تیره تمام‌صفحه با سوراخ روشن دور کنترل هدف، حباب راهنما با
/// ناوبری قبلی/بعدی/رد شدن و نشانگر پیشرفت.
/// رفع‌های این چرخه — هایلایت‌های «خالی/بی‌ارتباط» گزارش‌شده:
/// ۱) خطای DPI: PointToScreen پیکسل فیزیکی برمی‌گرداند ولی مسیر ماسک
///    با واحد منطقی DIP ترسیم می‌شد؛ در نمایشگرهای با مقیاس ۱۲۵/۱۵۰٪ سوراخ
///    جابه‌جا و خالی می‌افتاد. اکنون مختصات نسبت به پنجرهٔ تور و با
///    TransformFromDevice به DIP تبدیل می‌شود.
/// ۲) گام‌های صفحات (داشبورد/بیماران/…) به جای هایلایت آیتم ریز منو،
///    ابتدا به خود صفحه ناوبری می‌کنند و «هر دو» — آیتم منو و میزکار
///    صفحه — سوراخ روشن می‌گیرند تا نوشتهٔ حباب با تصویر روی صفحه بخواند.
/// ۳) حباب خوش‌آمد (گام بدون هدف) حالا دور خودش سوراخ می‌گیرد؛ پیش‌تر
///    سوراخ ثابت مرکزی بود که با هر ابعاد صفحه جابه‌جا می‌ماند.
/// ۴) تغییر اندازهٔ پنجره در میانهٔ تور، ماسک را دوباره چیده می‌کند.
/// </summary>
public partial class GuidedTourWindow : Window
{
    /// <summary>پنجره اصلی — برای یافتن کنترل‌های هدف</summary>
    private readonly Window _main;

    /// <summary>ایستگاه جاری</summary>
    private int _index;

    /// <summary>مجموعه ایستگاه‌ها</summary>
    private readonly List<TourStep> _steps;

    /// <summary>رخداد پایان تور (با هر دو حالت تکمیل/رد شدن)</summary>
    public event EventHandler? TourClosed;

    /// <summary>سازنده با پنجره اصلی</summary>
    public GuidedTourWindow(Window main)
    {
        InitializeComponent();
        _main = main;
        _steps = TourCatalog.Steps;

        // تغییر اندازه/جابه‌جایی پنجرهٔ اصلی → چیدمان دوبارهٔ ماسک ایستگاه جاری
        _main.SizeChanged += OnMainSizeChanged;
        SizeChanged += (_, _) => ShowStep(_index);

        // ناوبری با کلید‌ها: راست = قبلی (RTL)، چپ = بعدی، Esc = رد شدن
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { OnSkip(this, e); e.Handled = true; }
        };

        Loaded += (_, _) => ShowStep(0);
    }

    /// <summary>نمایش ایستگاه فعلی — ماسک و حباب تنظیم می‌شوند</summary>
    private void ShowStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return;
        if (!IsLoaded && index != 0) return;
        _index = index;
        var step = _steps[index];

        StepTitle.Text = step.Title;
        StepMessage.Text = step.Message;
        StepCounter.Text = $"{ToPersian(index + 1)} از {ToPersian(_steps.Count)}";
        TourProgress.Value = (index + 1) * 100.0 / _steps.Count;
        BackButton.Visibility = index > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content = index == _steps.Count - 1 ? "شروع کار 🚀" : "بعدی ▶";

        UpdateMask(step.TargetName);
    }

    /// <summary>
    /// ساخت ماسک تیره با سوراخ‌های روشن — هماهنگ با DPI و نسبت به همین پنجره.
    /// اگر هدف یک کلید صفحه (PageKey) باشد، ابتدا پنجرهٔ اصلی به آن صفحه
    /// می‌رود، سپس «آیتم منو + میزکار صفحه» هر دو سوراخ می‌گیرند.
    /// </summary>
    private void UpdateMask(string? targetName)
    {
        const double pad = 10;

        // گام بدون هدف (خوش‌آمدید): حباب در میانهٔ صفحه + سوراخ روشن دور خودش
        if (string.IsNullOrEmpty(targetName))
        {
            ApplyCenteredMask();
            return;
        }

        var holes = new List<Rect>();

        // ۱) هدف مستقیم با نام ثبت‌شده (NavList / TopBar / ...)
        var named = _main.FindName(targetName) as FrameworkElement;
        if (named is not null && named.IsVisible)
        {
            holes.Add(TargetRect(named, pad));
        }
        else
        {
            // ۲) کلید صفحه: ناوبری به صفحه + هایلایت دوگانهٔ «آیتم منو + میزکار»
            var navigated = NavigateToPage(targetName);
            if (navigated)
            {
                // آیتم منو — با اسکرول برای تحقق یافتن ظرف مجازی‌سازی‌شده
                if (_main.FindName("NavList") is ListBox navList)
                {
                    var item = navList.Items.OfType<ViewModels.NavItem>()
                        .FirstOrDefault(n => n.PageKey == targetName);
                    if (item is not null)
                    {
                        navList.ScrollIntoView(item);
                        navList.UpdateLayout();
                        if (navList.ItemContainerGenerator.ContainerFromItem(item)
                                is ListBoxItem container && container.IsVisible)
                            holes.Add(TargetRect(container, pad));
                    }
                }

                // میزکار صفحه — بدنهٔ اصلی که نوشتهٔ حباب آن را توضیح می‌دهد
                if (_main.FindName("PageHost") is FrameworkElement host && host.IsVisible)
                    holes.Add(TargetRect(host, pad));
            }

            if (holes.Count == 0)
            {
                // هدف ناموفق — حباب میانه + سوراخ دور خودش (بدون هایلایت گمراه‌کننده)
                ApplyCenteredMask();
                return;
            }
        }

        ApplyMask(holes.Select(ClampToWindow).ToList(), placeBubble: true);
    }

    /// <summary>حباب میانهٔ صفحه با سوراخ روشن دور خودش — گام بدون هدف</summary>
    private void ApplyCenteredMask()
    {
        var width = ActualWidth > 0 ? ActualWidth : SystemParameters.WorkArea.Width;
        var height = ActualHeight > 0 ? ActualHeight : SystemParameters.WorkArea.Height;

        var top = Math.Max(40, (height - 440) / 2);
        Bubble.HorizontalAlignment = HorizontalAlignment.Center;
        Bubble.Margin = new Thickness(0, top + 16, 0, 0);
        ApplyMask(new List<Rect> { ClampToWindow(new Rect(width / 2 - 245, top, 490, 430)) },
            placeBubble: false);
    }

    /// <summary>اعمال ماسک چندسوراخه + جای‌گذاری حباب زیر پایینی‌ترین سوراخ</summary>
    private void ApplyMask(List<Rect> holes, bool placeBubble)
    {
        var width = ActualWidth > 0 ? ActualWidth : SystemParameters.WorkArea.Width;
        var height = ActualHeight > 0 ? ActualHeight : SystemParameters.WorkArea.Height;

        var full = new RectangleGeometry(new Rect(0, 0, width, height));
        var cuts = new GeometryGroup();
        foreach (var hole in holes)
            cuts.Children.Add(new RectangleGeometry(hole, 14, 14));
        var combined = new CombinedGeometry(GeometryCombineMode.Exclude, full, cuts);
        MaskPath.Data = combined;

        if (!placeBubble) return;

        var anchor = holes.OrderByDescending(h => h.Bottom).First();
        var top = anchor.Bottom + 18;
        if (top + 330 > height)
            top = Math.Max(70, anchor.Top - 340);
        Bubble.Margin = new Thickness(0, top, 0, 0);
        Bubble.HorizontalAlignment = width - anchor.Left < 520
            ? HorizontalAlignment.Left
            : width - anchor.Right < 520 ? HorizontalAlignment.Right : HorizontalAlignment.Center;
    }

    /// <summary>
    /// مستطیل هدف در فضای منطقی همین پنجره — DPI-امن:
    /// PointToScreen پیکسل فیزیکی می‌دهد؛ با مبدأ پنجرهٔ تور و
    /// TransformFromDevice به DIP تبدیل می‌شود.
    /// </summary>
    private Rect TargetRect(FrameworkElement target, double pad)
    {
        var source = PresentationSource.FromVisual(this);
        var toLogical = source?.CompositionTarget?.TransformFromDevice
                        ?? Matrix.Identity;
        var origin = PointToScreen(new Point(0, 0));       // پیکسل فیزیکی مبدأ تور
        var targetTopLeft = target.PointToScreen(new Point(0, 0)); // پیکسل فیزیکی هدف

        var rel = toLogical.Transform(new Point(
            targetTopLeft.X - origin.X, targetTopLeft.Y - origin.Y));
        var size = target.RenderSize;                       // از قبل منطقی است

        return new Rect(rel.X - pad, rel.Y - pad,
            size.Width + 2 * pad, size.Height + 2 * pad);
    }

    /// <summary>بریدن سوراخ به محدودهٔ پنجره — سوراخ بیرون‌زدیده دیده نمی‌شود</summary>
    private Rect ClampToWindow(Rect rect)
    {
        var width = ActualWidth > 0 ? ActualWidth : SystemParameters.WorkArea.Width;
        var height = ActualHeight > 0 ? ActualHeight : SystemParameters.WorkArea.Height;
        var x = Math.Max(0, Math.Min(rect.X, width - 8));
        var y = Math.Max(0, Math.Min(rect.Y, height - 8));
        return new Rect(x, y,
            Math.Max(8, Math.Min(rect.Width, width - x)),
            Math.Max(8, Math.Min(rect.Height, height - y)));
    }

    /// <summary>
    /// ناوبری پنجرهٔ اصلی به صفحهٔ با کلید داده‌شده — برای گام‌های توصیف‌کنندهٔ
    /// محتوای صفحه، تا هایلایت با نوشتهٔ حباب هم‌خوان باشد.
    /// </summary>
    private bool NavigateToPage(string pageKey)
    {
        if (_main.DataContext is not ViewModels.MainWindowViewModel shell) return false;
        var item = shell.NavItems.FirstOrDefault(n => n.PageKey == pageKey);
        if (item is null || Equals(shell.SelectedNav, item)) return item is not null;

        shell.SelectedNav = item;
        // فرصت رندر صفحهٔ جدید پیش از اندازه‌گیری ماسک
        Dispatcher.InvokeAsync(() => ShowStep(_index), DispatcherPriority.Loaded);
        return true;
    }

    /// <summary>گام بعدی — در پایان، تور بسته و «تکمیل» اعلام می‌شود</summary>
    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_index >= _steps.Count - 1)
        {
            Finish(completed: true);
            return;
        }
        ShowStep(_index + 1);
    }

    /// <summary>گام قبلی</summary>
    private void OnBack(object sender, RoutedEventArgs e) => ShowStep(_index - 1);

    /// <summary>رد شدن از تور</summary>
    private void OnSkip(object sender, RoutedEventArgs e) => Finish(completed: false);

    /// <summary>پایان تور — علامت‌گذاری فایل مارکر محلی برای عدم نمایش خودکار مجدد</summary>
    private void Finish(bool completed)
    {
        try
        {
            _main.SizeChanged -= OnMainSizeChanged;
        }
        catch
        {
            // شنونده اختیاری است
        }

        try
        {
            var user = TourMarker.CurrentUser ?? "user";
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SahandClinicPro");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"tour-done-{user}.marker");
            if (!File.Exists(file))
                File.WriteAllText(file, DateTime.Now.ToString("O"));
        }
        catch
        {
            // مارکر محلی اختیاری است — خطای فایل نباید تور را بشکند
        }

        TourClosed?.Invoke(this, EventArgs.Empty);
        Close();
    }

    /// <summary>شنوندهٔ جدا برای جداسازی مطمئن (قابل حذف با -=)</summary>
    private void OnMainSizeChanged(object sender, SizeChangedEventArgs e)
        => Dispatcher.InvokeAsync(() => ShowStep(_index), DispatcherPriority.Background);

    /// <summary>تبدیل عدد به ارقام فارسی</summary>
    private static string ToPersian(int value) =>
        value.ToString().Replace('0', '۰').Replace('1', '۱').Replace('2', '۲').Replace('3', '۳')
            .Replace('4', '۴').Replace('5', '۵').Replace('6', '۶').Replace('7', '۷')
            .Replace('8', '۸').Replace('9', '۹');
}

/// <summary>کلاس کمکی نگهداری نام کاربر فعلی برای مارکر تور — بدون وابستگی به UI</summary>
public static class TourMarker
{
    /// <summary>نام کاربر فعلی — از TokenManager در ورود تنظیم می‌شود</summary>
    public static string? CurrentUser { get; set; }
}
