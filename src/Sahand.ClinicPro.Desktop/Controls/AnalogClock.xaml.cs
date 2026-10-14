using System.Windows;
using System.Windows.Controls;
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
            UpdateHands(animate: false);
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
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
