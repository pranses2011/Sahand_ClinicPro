using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// کد پشت پنجرهٔ اسپلش — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
/// «برنامه Splash با انیمیشن و نوار پیشرفت بساز که خیلی زیبا و با زمینهٔ
/// شفاف باشه» — مراحل بارگذاری با پیام فارسی و درصد فارسی نمایش داده
/// می‌شود؛ در پایان با محوشدن آرام بسته و پنجرهٔ بعدی (ورود/اصلی) باز
/// می‌گردد. ساختار طوری است که مرحله‌های واقعی راه‌اندازی (تم، توکن،
/// لایسنس) را با ReportProgress گزارش کنند.
/// </summary>
public partial class SplashWindow : Window
{
    /// <summary>نرخ فریم پیشرفت — نوار به‌صورت پیوسته حرکت می‌کند</summary>
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(24) };

    /// <summary>درصد فعلی نمایش</summary>
    private double _progress;

    /// <summary>درصد هدف — به‌آرامی به آن نزدیک می‌شویم (حرکت روان)</summary>
    private double _target;

    /// <summary>پیام‌های مرحله‌ای — با پیشرفت عوض می‌شوند</summary>
    private static readonly (int At, string Text)[] Stages =
    {
        (0,  "در حال آماده‌سازی…"),
        (15, "بارگذاری فونت‌ها و ظاهر…"),
        (35, "بررسی وضعیت لایسنس…"),
        (55, "اتصال به سرور درمانگاه…"),
        (75, "بارگذاری داده‌های پایه…"),
        (92, "تقریباً آماده است…"),
        (100, "آماده شد ✨"),
    };

    /// <summary>سازنده — شروع تایمر حرکت نوار</summary>
    public SplashWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => AnimateStep();
        _timer.Start();
    }

    /// <summary>
    /// گزارش پیشرفت — از گام‌های راه‌اندازی فراخوانده می‌شود؛
    /// درصد به‌صورت نرم به هدف نزدیک و پیام مرحله تازه می‌شود.
    /// </summary>
    public void ReportProgress(double percent)
    {
        _target = Math.Clamp(percent, 0, 100);
        var stage = Stages.LastOrDefault(s => _progress >= s.At);
        if (stage.Text is not null)
            SubtitleText.Text = stage.Text;
    }

    /// <summary>یک گام انیمیشن — نوار به هدف نزدیک و عرض/متن تازه می‌شود</summary>
    private void AnimateStep()
    {
        // حرکت روان به سمت هدف — هر فریم ۸٪ باقی‌فاصله
        if (_progress < _target)
            _progress = Math.Min(_target, _progress + Math.Max(0.6, (_target - _progress) * 0.08));

        var width = 300 * _progress / 100.0;
        ProgressFill.Width = width;
        ProgressText.Text = $"{PersianDateHelper.ToPersianDigits(((int)Math.Round(_progress)).ToString())}٪";
    }

    /// <summary>
    /// بستن با محوشدن آرام — پس از پایان مراحل فراخوانده می‌شود؛
    /// حداکثر ۴۰۰ms صبر و سپس پنجره واقعاً بسته می‌شود.
    /// </summary>
    public void CloseWithFade()
    {
        _target = 100;
        // فرصت کوتاه تا نوار کامل پر شود
        var fadeDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
        fadeDelay.Tick += (_, _) =>
        {
            fadeDelay.Stop();
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(360))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            fade.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fade);
        };
        fadeDelay.Start();
    }
}
