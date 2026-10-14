using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// ساعت آنالوگ نمایشگر سالن انتظار — چرخهٔ ۷۴.
/// عقربه‌ها با تایمر یک‌ثانیه‌ای زاویه می‌گیرند و عقربهٔ ثانیه با انیمیشن
/// خطیِ یک‌ثانیه‌ای بین دو tick حرکت می‌کند تا «سوپِ پیوسته» داشته باشد
/// (نه پرش ثانیه‌ای). اعداد ۱۲/۳/۶/۹ با ارقام فارسی‌اند و همهٔ رنگ‌ها برای
/// پس‌زمینهٔ تیرهٔ کیوسک انتخاب شده‌اند (خوانا از فاصلهٔ چند متری).
/// ساعتِ نمایش بر پایهٔ وقت محلی ایستگاه (DateTime.Now) است — کیوسک همان
/// جایی است که بیمار ساعتش را با آن تطبیق می‌کند.
/// </summary>
public partial class AnalogClock : UserControl
{
    /// <summary>زمان‌سنج یک‌ثانیه‌ای — محرک همهٔ عقربه‌ها</summary>
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>زاویهٔ قبلی عقربهٔ ثانیه — برای انیمیشن پیوسته بین دو tick</summary>
    private double _lastSecondAngle;

    /// <summary>سازنده — راه‌اندازی زمان‌سنج و نخستین ترسیم</summary>
    public AnalogClock()
    {
        InitializeComponent();

        _timer.Tick += (_, _) => UpdateHands();
        Loaded += (_, _) =>
        {
            CreateTickMarks();
            UpdateHands(animate: false);
            _timer.Start();
        };
        // تغییر اندازهٔ کنترل (پیش‌نمایش کوچک / تلویزیون بزرگ) — نشانگرها
        // با شعاع تازه بازسازی می‌شوند تا نسبت‌ها در هر اندازه درست بماند
        SizeChanged += (_, _) => CreateTickMarks();
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — ساخت ۶۰ نشانگر ساعت/دقیقه روی صفحه ═══
    /// گزارش مالک: «فقط ۴ شمارهٔ اصلی نوشته شده؛ خطوط ساعت و دقیقه هم
    /// داشته باشه — خطوط ساعت کلفت‌تر». دو دسته خط ساخته می‌شود:
    ///   • ۱۲ خطِ ساعت — هر ۳۰ درجه، پهنای ۳px، طول ۱۶px، روشن (#B9CFE6)
    ///   • ۴۸ خطِ دقیقه — هر ۶ درجه (غیر از مضرب‌های ۳۰)، پهنای ۱٫۴px،
    ///     طول ۸px، کم‌رنگ (#51708F)
    /// خطوط از لبهٔ داخلی صفحه (شعاع ۱۱۷px در کنترل ۲۶۰pxی) به داخل
    /// کشیده می‌شوند؛ با SizeChanged دوباره ساخته می‌شوند تا در هر اندازهٔ
    /// کنترل (نمایشگر بزرگ/پیش‌نمایش کوچک) نسبت‌ها درست بمانند.
    /// </summary>
    private void CreateTickMarks()
    {
        var canvas = TickCanvas;
        canvas.Children.Clear();

        // شعاع مفثر صفحه — نصف کوچک‌ترین ضلع کنترل منهای حاشیهٔ حلقه
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) size = 260; // نخستین ترسیم پیش از layout — اندازهٔ طراحی
        var radius = size / 2 - 12;
        var center = size / 2;
        canvas.Width = size;
        canvas.Height = size;

        for (var i = 0; i < 60; i++)
        {
            var isHour = i % 5 == 0;
            var angleDeg = i * 6;
            var angle = angleDeg * Math.PI / 180.0;

            // طول و پهنا بر نوع نشانگر — ساعت‌ها کلفت و بلندتر
            var length = isHour ? 16 : 8;
            var width = isHour ? 3 : 1.4;

            // نقطهٔ بیرونی (روی حلقه) و درونی (به سمت مرکز)
            var outer = radius;
            var inner = radius - length;
            var x1 = center + outer * Math.Sin(angle);
            var y1 = center - outer * Math.Cos(angle);
            var x2 = center + inner * Math.Sin(angle);
            var y2 = center - inner * Math.Cos(angle);

            var line = new System.Windows.Shapes.Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                StrokeThickness = width,
                Stroke = isHour ? HourTickBrush : MinuteTickBrush,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };
            line.IsHitTestVisible = false;
            canvas.Children.Add(line);
        }
    }

    /// <summary>قلم خطوط ساعت — روشن و کلفت (تشخیص از دور)</summary>
    private static readonly System.Windows.Media.SolidColorBrush HourTickBrush = Frozen("#B9CFE6");

    /// <summary>قلم خطوط دقیقه — نازک و کم‌رنگ (پس‌زمینهٔ ریتم صفحه)</summary>
    private static readonly System.Windows.Media.SolidColorBrush MinuteTickBrush = Frozen("#51708F");

    /// <summary>ساخت قلم فریزشده — بدون تخصیص در هر فریم رندر</summary>
    private static SolidColorBrush Frozen(string hex)
    {
        if (new BrushConverter().ConvertFromString(hex) is not SolidColorBrush brush)
            return Brushes.White; // کد رنگ ناشناخته — قلم امن (هرگز رخ نمی‌دهد)
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// به‌روزرسانی زاویهٔ سه عقربه.
    /// ساعت/دقیقه: مقدارگذاری مستقیم (حرکتشان به‌قدری ریز است که پرش دیده نمی‌شود).
    /// ثانیه: انیمیشن خطیِ ۱ ثانیه‌ای از زاویهٔ فعلی به بعدی → حرکت پیوسته.
    /// مرز ۳۶۰→۰: زاویهٔ هدف محاسبه‌شده همیشه روبه‌جلو می‌ماند تا انیمیشن
    /// هرگز به عقب برنگردد (رفع کلاسیک «پرش معکوس در ثانیهٔ ۶۰»).
    /// </summary>
    /// <param name="animate">false یعنی نخستین ترسیم (بدون انیمیشن)</param>
    private void UpdateHands(bool animate = true)
    {
        var now = DateTime.Now;

        // عقربهٔ ساعت: هر ساعت ۳۰ درجه + سهم دقیقه/ثانیه برای پیوستگی
        HourRotation.Angle = (now.Hour % 12) * 30 + now.Minute * 0.5 + now.Second / 120.0;
        // عقربهٔ دقیقه: هر دقیقه ۶ درجه + سهم ثانیه
        MinuteRotation.Angle = now.Minute * 6 + now.Second * 0.1;

        // عقربهٔ ثانیه: هر ثانیه ۶ درجه — زاویهٔ مطلق روبه‌جلو (بدون بازگشت به صفر)
        var target = now.Minute * 360 + now.Second * 6;
        if (animate)
        {
            var animation = new DoubleAnimation
            {
                From = _lastSecondAngle,
                To = target,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                FillBehavior = FillBehavior.HoldEnd,
            };
            SecondRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
        }
        else
        {
            SecondRotation.Angle = target;
        }
        _lastSecondAngle = target;
    }

    /// <summary>متن ساعت دیجیتال همراه — «۱۴:۳۵:۰۵» با ارقام فارسی (کنار آنالوگ)</summary>
    public static string DigitalText(DateTime now)
        => PersianDateHelper.ToPersianDigits(now.ToString("HH:mm:ss"));
}
