using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت تور هدایت‌شده اولین اجرا — چرخه سیزدهم (درخواست ۸-۶):
/// ماسک تیره تمام‌صفحه با سوراخ روشن (CombinedGeometry) دور کنترل هدف،
/// حباب راهنما با ناوبری قبلی/بعدی/رد شدن و نشانگر پیشرفت.
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
        ShowStep(0);
    }

    /// <summary>نمایش ایستگاه فعلی — ماسک و حباب تنظیم می‌شوند</summary>
    private void ShowStep(int index)
    {
        if (index < 0 || index >= _steps.Count) return;
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

    /// <summary>ساخت ماسک تیره با سوراخ روشن دور کنترل هدف</summary>
    private void UpdateMask(string? targetName)
    {
        const double pad = 10;
        Rect hole;

        var target = FindTarget(targetName);
        if (target is not null && target.IsVisible)
        {
            // مختصات کنترل نسبت به دسکتاپ
            var topLeft = target.PointToScreen(new Point(0, 0));
            var size = target.RenderSize;
            hole = new Rect(topLeft.X - pad, topLeft.Y - pad, size.Width + 2 * pad, size.Height + 2 * pad);
        }
        else
        {
            // هدف تهی یا یافت‌نشده — سوراخ مرکزی دور حباب
            hole = new Rect(
                SystemParameters.WorkArea.Width / 2 - 240,
                SystemParameters.WorkArea.Height / 2 - 200,
                480, 400);
        }

        // ماسک: کل صفحه منهای مستطیل سوراخ (خروج از تقارن گرد)
        var full = new RectangleGeometry(new Rect(0, 0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height));
        var cut = new RectangleGeometry(hole, 14, 14);
        var combined = new CombinedGeometry(GeometryCombineMode.Exclude, full, cut);
        MaskPath.Data = combined;

        // جای‌گذاری حباب — زیر سوراخ (یا بالای آن اگر جا نبود)
        var top = hole.Bottom + 18;
        if (top + 320 > SystemParameters.WorkArea.Height)
            top = Math.Max(70, hole.Top - 330);
        Bubble.Margin = new Thickness(0, top, 0, 0);
        Bubble.HorizontalAlignment = SystemParameters.WorkArea.Width - hole.Left < 520
            ? HorizontalAlignment.Left
            : SystemParameters.WorkArea.Width - hole.Right < 520 ? HorizontalAlignment.Right : HorizontalAlignment.Center;
    }

    /// <summary>یافتن کنترل هدف در درخت بصری پنجره اصلی — با نام، Uid یا متن آیتم منو</summary>
    private FrameworkElement? FindTarget(string? targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        // ۱) جستجو با نام ثبت‌شده (NavList/TopBar)
        var named = _main.FindName(targetName) as FrameworkElement;
        if (named is not null) return named;

        // ۲) جستجوی آیتم‌های منوی ناوبری با کلید صفحه
        if (_main is MainWindow mw && mw.DataContext is ViewModels.MainWindowViewModel)
        {
            var listBox = _main.FindName("NavList") as ItemsControl;
            if (listBox is not null)
            {
                var container = ListBoxItems(listBox)
                    .FirstOrDefault(item => item.DataContext is ViewModels.NavItem nav && nav.PageKey == targetName);
                if (container is not null) return container;
            }
        }
        return null;
    }

    /// <summary>پیمایش آیتم‌های تولیدشده لیست‌باکس</summary>
    private static IEnumerable<ListBoxItem> ListBoxItems(ItemsControl control)
    {
        foreach (var item in control.Items.OfType<ViewModels.NavItem>())
        {
            if (control.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)
                yield return container;
        }
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
