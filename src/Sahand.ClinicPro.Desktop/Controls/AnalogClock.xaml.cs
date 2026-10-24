using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Desktop.Controls;

/// <summary>
/// ساعت آنالوگ نمایشگر سالن انتظار — چرخهٔ ۷۴ با قالب و رنگ قابل‌تنظیم چرخهٔ ۷۹.
/// عقربه‌ها با تایمر یک‌ثانیه‌ای زاویه می‌گیرند و عقربهٔ ثانیه با انیمیشن
/// خطیِ یک‌ثانیه‌ای بین دو tick حرکت می‌کند تا «سوپِ پیوسته» داشته باشد
/// (نه پرش ثانیه‌ای). اعداد با ارقام فارسی‌اند و همهٔ رنگ‌ها برای پس‌زمینهٔ
/// تیرهٔ کیوسک انتخاب شده‌اند (خوانا از فاصلهٔ چند متری).
/// ساعتِ نمایش بر پایهٔ وقت محلی ایستگاه (DateTime.Now) است — کیوسک همان
/// جایی است که بیمار ساعتش را با آن تطبیق می‌کند.
/// </summary>
public partial class AnalogClock : UserControl
{
    /// <summary>زمان‌سنج یک‌ثانیه‌ای — محرک همهٔ عقربه‌ها</summary>
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>زاویهٔ قبلی عقربهٔ ثانیه — برای انیمیشن پیوسته بین دو tick</summary>
    private double _lastSecondAngle;

    // ═══════════════════════════════════════════════════════════════
    //  چرخهٔ ۷۹ — دو ویژگی وابستگی تازه (گزارش مالک: «برای ساعت هم تنظیمات
    //  بزار تا هم بتونیم صفحه ساعت و هم رنگشو تغییر بدیم»)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// قالب صفحهٔ ساعت — ۰ کلاسیک (۶۰ خط + اعداد ۱۲/۳/۶/۹ · پیش‌فرض، همان
    /// چهرهٔ همیشگی) · ۱ اعداد کامل (۱۲ عدد فارسی درجای ساعت‌ها) ·
    /// ۲ مینیمال (فقط ۱۲ خط کلفت ساعت — بدون عدد و خط دقیقه).
    /// </summary>
    public static readonly DependencyProperty ClockFaceStyleProperty = DependencyProperty.Register(
        nameof(ClockFaceStyle), typeof(int), typeof(AnalogClock),
        new PropertyMetadata(0, (d, _) => ((AnalogClock)d).OnFaceChanged()));

    /// <summary>قالب صفحهٔ ساعت — برای XAML و تنظیمات نمایشگر</summary>
    public int ClockFaceStyle
    {
        get => (int)GetValue(ClockFaceStyleProperty);
        set => SetValue(ClockFaceStyleProperty, value);
    }

    /// <summary>
    /// تم رنگی ساعت — ۰ نیلی (پیش‌فرض) · ۱ زمردی · ۲ کهربایی · ۳ یاقوتی ·
    /// ۴ نقره‌ای. صفحه، حلقه، خطوط، اعداد، عقربه‌ها و پیچ مرکزی همه از همین
    /// پالت رنگ می‌گیرند.
    /// </summary>
    public static readonly DependencyProperty ClockColorThemeProperty = DependencyProperty.Register(
        nameof(ClockColorTheme), typeof(int), typeof(AnalogClock),
        new PropertyMetadata(0, (d, _) => ((AnalogClock)d).OnThemeChanged()));

    /// <summary>تم رنگی ساعت — برای XAML و تنظیمات نمایشگر</summary>
    public int ClockColorTheme
    {
        get => (int)GetValue(ClockColorThemeProperty);
        set => SetValue(ClockColorThemeProperty, value);
    }

    /// <summary>تغییر قالب صفحه — بازسازی نشانگرها/اعداد با اندازهٔ فعلی</summary>
    private void OnFaceChanged()
    {
        ApplyTheme();
        BuildFace();
    }

    /// <summary>تغییر تم رنگ — پالت تازه روی همهٔ اجزای نام‌دار</summary>
    private void OnThemeChanged() => ApplyTheme();

    /// <summary>
    /// ═══ پالت‌های رنگی چرخهٔ ۷۹ ═══
    /// ترتیب اعضای هر پالت:
    ///   ۰ FaceCenter (مرکز گرادیان صفحه) · ۱ FaceMid · ۲ FaceEdge (لبهٔ صفحه)
    ///   ۳ RingFill (هالهٔ بیرونی) · ۴ RingStroke (خط حلقه)
    ///   ۵ FaceStroke (حاشیهٔ گرادیانی صفحه — دو توقف)
    ///   ۶ TickHour (خط کلفت ساعت) · ۷ TickMinute (خط نازک دقیقه)
    ///   ۸ Number (رنگ اعداد) · ۹ Hand (عقربهٔ ساعت/دقیقه — دو توقف گرادیان)
    ///   ۱۰ SecondHand (عقربهٔ ثانیه) · ۱۱ SecondTail (دُم ثانیه) · ۱۲ Center (پیچ مرکزی)
    /// </summary>
    private static readonly string[][] Palettes =
    {
        // ۰ — نیلی (پیش‌فرض همیشگی — همان رنگ‌های چرخهٔ ۷۴/۷۵)
        new[] { "#22405F", "#152A42", "#0E1E31", "#101F31", "#2C4A6E", "#5B87B5",
                "#B9CFE6", "#51708F", "#DCE9F7", "#F2F7FC", "#FF5252", "#FF8A80", "#F2F7FC" },
        // ۱ — زمردی
        new[] { "#1D4A38", "#123526", "#0B241A", "#0E2018", "#1F5C42", "#4E9E77",
                "#B7E2CC", "#3E7A5D", "#D9F2E4", "#F0FBF5", "#FF6B6B", "#FFA3A3", "#F0FBF5" },
        // ۲ — کهربایی (گرم — مناسب سالن‌های کم‌نور)
        new[] { "#4A3418", "#33240F", "#231809", "#241A0C", "#5C4320", "#9E7738",
                "#E8D5AE", "#8A6E42", "#F7ECD4", "#FDF8EC", "#FF5252", "#FF8A80", "#FDF8EC" },
        // ۳ — یاقوتی (بنفش‌سرخ — تشخیص بالا از دور)
        new[] { "#451E4F", "#2E1338", "#1E0C24", "#231029", "#4A2154", "#8A4E9E",
                "#E3C8EB", "#6E4478", "#F4E3F8", "#FBF0FD", "#FFD54F", "#FFE082", "#FBF0FD" },
        // ۴ — نقره‌ای (خنثی روشن)
        new[] { "#3A4450", "#272E37", "#1B2129", "#1F252D", "#3E4A57", "#7A8B9C",
                "#DDE5EC", "#66727E", "#F2F6FA", "#FFFFFF", "#FF5252", "#FF8A80", "#FFFFFF" },
    };

    /// <summary>
    /// اعمال پالت رنگی روی همهٔ اجزای نام‌دار ساعت — چرخهٔ ۷۹.
    /// مقادیر خارج از بازه به «نیلی» (پیش‌فرض) فرومی‌روند تا تنظیمات کهنه
    /// هرگز ساعتِ بی‌رنگ نسازد.
    /// </summary>
    private void ApplyTheme()
    {
        var p = Palettes[Math.Clamp(ClockColorTheme, 0, Palettes.Length - 1)];

        // هالهٔ بیرونی + خط حلقه
        OuterRing.Fill = Frozen(p[3]);
        OuterRing.Stroke = Frozen(p[4]);

        // صفحهٔ اصلی — گرادیان شعاعی شیشه‌ای با همان الگوی چرخهٔ ۷۴
        var face = new RadialGradientBrush(ToColor(p[0]), ToColor(p[2]))
        {
            GradientOrigin = new Point(0.35, 0.3),
            Center = new Point(0.5, 0.45),
        };
        face.GradientStops.Add(new GradientStop(ToColor(p[0]), 0));
        face.GradientStops.Add(new GradientStop(ToColor(p[1]), 0.65));
        face.GradientStops.Add(new GradientStop(ToColor(p[2]), 1));
        FaceEllipse.Fill = face;
        var faceStroke = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        faceStroke.GradientStops.Add(new GradientStop(ToColor(p[5]), 0));
        faceStroke.GradientStops.Add(new GradientStop(ToColor(p[4]), 0.5));
        faceStroke.GradientStops.Add(new GradientStop(ToColor(p[5]), 1));
        FaceEllipse.Stroke = faceStroke;

        // عقربه‌های ساعت/دقیقه — گرادیان عمودی هم‌الگوی چرخهٔ ۷۴
        var handBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        handBrush.GradientStops.Add(new GradientStop(ToColor(p[9]), 0));
        handBrush.GradientStops.Add(new GradientStop(ToColor(p[8]), 1));
        HourRect.Fill = handBrush;
        var minuteBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
        };
        minuteBrush.GradientStops.Add(new GradientStop(ToColor(p[9]), 0));
        minuteBrush.GradientStops.Add(new GradientStop(ToColor(p[8]), 1));
        MinuteRect.Fill = minuteBrush;

        // عقربهٔ ثانیه (نوک تند + دُم کم‌رنگ‌تر) + حلقهٔ لغزش
        SecondTipRect.Fill = Frozen(p[10]);
        SecondTailRect.Fill = Frozen(p[11]);
        SecondBalanceRing.Stroke = Frozen(p[10]);
        SecondBalanceRing.Fill = Frozen(p[1]);

        // پیچ مرکزی
        CenterOuter.Fill = Frozen(p[12]);
        CenterInner.Fill = Frozen(p[10]);

        // رنگ‌های نشانگرها/اعداد به‌صورت فیلد نگه داشته می‌شوند (BuildFace می‌خواند)
        _tickHourHex = p[6];
        _tickMinuteHex = p[7];
        _numberHex = p[8];
    }

    /// <summary>رنگ خطوط ساعت از پالت فعال — BuildFace می‌خواند</summary>
    private string _tickHourHex = "#B9CFE6";

    /// <summary>رنگ خطوط دقیقه از پالت فعال</summary>
    private string _tickMinuteHex = "#51708F";

    /// <summary>رنگ اعداد از پالت فعال</summary>
    private string _numberHex = "#DCE9F7";

    /// <summary>
    /// ساخت صفحهٔ ساعت (خطوط + اعداد) بسته به قالب — چرخهٔ ۷۹:
    ///   ۰ کلاسیک: ۱۲ خط کلفت ساعت + ۴۸ خط نازک دقیقه + اعداد ۱۲/۳/۶/۹
    ///     (همان چهرهٔ چرخهٔ ۷۵ — پیش‌فرض)
    ///   ۱ اعداد کامل: ۱۲ عدد فارسی درجای ساعت‌ها + خط نازک دقیقه
    ///   ۲ مینیمال: فقط ۱۲ خط کلفت ساعت — بدون عدد و خط دقیقه
    /// با SizeChanged دوباره ساخته می‌شود تا نسبت‌ها در هر اندازه درست بمانند.
    /// </summary>
    private void BuildFace()
    {
        var canvas = TickCanvas;
        canvas.Children.Clear();

        // بوم طراحی ثابت ۲۸۰ است و Viewbox آن را مقیاس می‌کند — شعاع همیشه
        // ۱۲۸ می‌ماند تا عقربه‌ها و خطوط در هر اندازه دقیقاً هم‌نسبت باشند.
        const double size = 280;
        var radius = size / 2 - 12;
        var center = size / 2;
        canvas.Width = size;
        canvas.Height = size;

        var style = Math.Clamp(ClockFaceStyle, 0, 2);

        // خطوط دقیقه — در کلاسیک و اعداد کامل؛ در مینیمال حذف
        if (style is 0 or 1)
        {
            for (var i = 0; i < 60; i++)
            {
                // در قالب «اعداد کامل» جای هر ساعتِ پنج‌تایی، عدد می‌نشیند؛
                // خط دقیقه فقط برای خانه‌های غیرعددی رسم می‌شود
                if (style == 1 && i % 5 == 0) continue;
                AddTick(canvas, center, radius, i * 6,
                    length: 7, width: 1.2, brushHex: _tickMinuteHex);
            }
        }

        // اعداد فارسی — ۱۲/۳/۶/۹ در کلاسیک؛ همهٔ ۱۲ عدد در «اعداد کامل»
        if (style is 0 or 1)
        {
            var hourNumbers = style == 0
                ? new[] { 12, 3, 6, 9 }
                : new[] { 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            foreach (var hour in hourNumbers)
            {
                var angle = hour * 30 * Math.PI / 180.0;
                // فاصلهٔ عدد از مرکز — کمی داخل‌تر از خطوط تا روی‌شان ننشیند
                var r = style == 0 ? radius - 33 : radius - 22;
                var tb = new TextBlock
                {
                    Text = PersianDateHelper.ToPersianDigits(hour.ToString()),
                    FontSize = style == 0 ? 21 : 16.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Frozen(_numberHex),
                };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tb, center + r * Math.Sin(angle) - tb.DesiredSize.Width / 2);
                Canvas.SetTop(tb, center - r * Math.Cos(angle) - tb.DesiredSize.Height / 2);
                canvas.Children.Add(tb);
            }
        }

        // خطوط کلفت ساعت — در کلاسیک و مینیمال (در «اعداد کامل» عدد جایگزین خط است)
        if (style is 0 or 2)
        {
            for (var h = 0; h < 12; h++)
                AddTick(canvas, center, radius, h * 30,
                    length: style == 2 ? 19 : 15, width: style == 2 ? 3.4 : 3, brushHex: _tickHourHex);
        }
    }

    /// <summary>افزودن یک خط نشانگر در زاویهٔ مشخص (۰ = بالا، ساعت‌گرد)</summary>
    private static void AddTick(Canvas canvas, double center, double radius, int angleDeg,
        double length, double width, string brushHex)
    {
        var angle = angleDeg * Math.PI / 180.0;
        var x1 = center + radius * Math.Sin(angle);
        var y1 = center - radius * Math.Cos(angle);
        var x2 = center + (radius - length) * Math.Sin(angle);
        var y2 = center - (radius - length) * Math.Cos(angle);

        var line = new System.Windows.Shapes.Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            StrokeThickness = width,
            Stroke = Frozen(brushHex),
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false,
        };
        canvas.Children.Add(line);
    }

    /// <summary>سازنده — راه‌اندازی زمان‌سنج و نخستین ترسیم</summary>
    public AnalogClock()
    {
        InitializeComponent();

        _timer.Tick += (_, _) => UpdateHands();
        Loaded += (_, _) =>
        {
            ApplyTheme();
            BuildFace();
            UpdateHands(animate: false);
            _timer.Start();
        };
        // تغییر اندازهٔ کنترل (پیش‌نمایش کوچک / تلویزیون بزرگ) — نشانگرها
        // با شعاع تازه بازسازی می‌شوند تا نسبت‌ها در هر اندازه درست بمانند
        SizeChanged += (_, _) => BuildFace();
        Unloaded += (_, _) => _timer.Stop();
    }

    /// <summary>ساخت قلم فریزشده — بدون تخصیص در هر فریم رندر</summary>
    private static SolidColorBrush Frozen(string hex)
    {
        if (new BrushConverter().ConvertFromString(hex) is not SolidColorBrush brush)
            return Brushes.White; // کد رنگ ناشناخته — قلم امن (هرگز رخ نمی‌دهد)
        brush.Freeze();
        return brush;
    }

    /// <summary>تبدیل هگز به رنگ — برای گرادیان‌های ساخته‌شده در کد</summary>
    private static Color ToColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);

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
