using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.Views;

/// <summary>
/// ═══ فرم «درباره نرم‌افزار» — چرخهٔ ۷۵ (درخواست مالک) ═══
/// «در یک نقطه از برنامه فرم زیبا و شکیل درباره نرم‌افزار رو تعبیه بکن و
/// داخلش اطلاعات پشتیبانی بصورت پویا هم باشه».
/// این پنجره هنگام باز شدن سه منبع پویا را می‌خواند و هیچ مقداری هاردکد
/// نیست (به‌جز هویت محصول و امضای فناوری):
///   ۱) وضعیت لایسنس (license/status) — کلید، اعتبار، انقضا؛
///   ۲) نام درمانگاه از سرور لایسنس — همگام‌شده در تنظیمات ClinicInfo
///      (تنها مرجع تغییر نام همان پنل لایسنس است — چرخهٔ ۲۷)؛
///   ۳) اطلاعات پشتیبانی (license/support) — نام/شرکت/تلفن/موبایل/ایمیل/
///      وب‌سایت؛ تلفن و ایمیل و سایت کلیک‌پذیرند (tel:/mailto:/مرورگر).
/// دکمهٔ «کپی اطلاعات برای پشتیبانی» همهٔ مشخصات را یک‌جا در کلیپ‌بورد
/// می‌گذارد تا کاربر هنگام تماس با پشتیبانی همه‌چیز را یک‌جا داشته باشد.
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>ساخت قلم رنگ از کد هگز — برای رنگ‌های وضعیت/لینک در کد پشت</summary>
    private static SolidColorBrush BrushFromHex(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze(); // قلم ایستا — کارایی رندر بهتر
        return brush;
    }

    /// <summary>کلاینت API تزریق‌شده — از کانتینر سراسری برنامه</summary>
    private readonly IApiClient _api;

    /// <summary>آخرین وضعیت لایسنس دریافت‌شده — برای دکمهٔ کپی</summary>
    private string _licenseKey = "—";

    /// <summary>آخرین وضعیت اعتبار — برای دکمهٔ کپی</summary>
    private string _licenseStatus = "—";

    /// <summary>آخرین انقضای لایسنس — برای دکمهٔ کپی</summary>
    private string _licenseExpiry = "—";

    /// <summary>آخرین نام درمانگاه — برای دکمهٔ کپی</summary>
    private string _clinicName = "—";

    /// <summary>سازنده — بارگذاری پویا پس از آماده‌شدن پنجره</summary>
    public AboutWindow()
    {
        InitializeComponent();
        _api = App.Services.GetRequiredService<IApiClient>();
        Loaded += (_, _) => _ = LoadAllAsync();
        // چرخهٔ ۷۸ — پنجرهٔ بدون قاب: بستن سریع با ESC (همان حسی که فرم‌های
        // معمولی با دکمهٔ ضربدر دارند اینجا هم باید باشد)
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    /// <summary>
    /// چرخهٔ ۷۸ (پنجرهٔ شفاف بدون قاب): جابه‌جایی پنجره با درگ روی شیشه —
    /// چون دکمهٔ نوار عنوان حذف شده، خود کارت نقش نوار عنوان را بازی می‌کند.
    /// </summary>
    private void OnGlassMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            try { DragMove(); }
            catch { /* درگ در لحظهٔ رهاکردن موس — بی‌خطر */ }
    }

    /// <summary>
    /// بارگذاری هر سه منبع پویا — هرکدام مستقل و مقاوم به خطا؛ شکست یکی
    /// بقیه را نمی‌اندازد (مثلاً قطعی سرور لایسنس، کارت درمانگاه را که از
    /// تنظیمات محلی می‌آید خراب نمی‌کند).
    /// </summary>
    private async Task LoadAllAsync()
    {
        await LoadLicenseAsync();
        await LoadClinicNameAsync();
        await LoadSupportAsync();
    }

    /// <summary>وضعیت لایسنس — کلید، اعتبار و انقضا با تاریخ شمسی</summary>
    private async Task LoadLicenseAsync()
    {
        try
        {
            var response = await _api.GetAnonymousAsync<StatusResponse>("license/status");
            var s = response?.Data;
            if (s is null) return;

            _licenseKey = s.LicenseKey ?? "—";
            LicenseKeyText.Text = s.LicenseKey is { Length: > 0 } k ? k : "—";

            _licenseStatus = s.IsValid ? "فعال" : "نامعتبر";
            // رنگ‌های روشن — متن روی شیشهٔ نیلی تیرهٔ فرم (چرخهٔ ۷۸)
            LicenseStatusText.Text = _licenseStatus;
            LicenseStatusText.Foreground = BrushFromHex(s.IsValid ? "#7EE0A8" : "#FF8A80");

            if (s.ExpiryDate is { } expiry)
            {
                _licenseExpiry = PersianDateHelper.ToShamsiFa(expiry);
                LicenseExpiryText.Text = _licenseExpiry;
            }
        }
        catch
        {
            LicenseStatusText.Text = "در حال حاضر قابل بررسی نیست";
        }
    }

    /// <summary>
    /// نام درمانگاه — از تنظیمات ClinicInfo (همگام‌شدهٔ خودکار از پنل لایسنس).
    /// این همان «نام واقعی درمانگاه از سرور لایسنس» است که مالک خواسته بود
    /// در نمایشگر سالن هم بنشیند؛ اینجا هم منبع واحد استفاده می‌شود.
    /// </summary>
    private async Task LoadClinicNameAsync()
    {
        try
        {
            var response = await _api.GetAsync<SettingsResponse>("settings?category=ClinicInfo");
            var name = response?.Data?.FirstOrDefault(x => x.Key == "ClinicName")?.Value;
            _clinicName = string.IsNullOrWhiteSpace(name) ? "درمانگاه" : name.Trim();
            ClinicNameText.Text = _clinicName;
        }
        catch
        {
            ClinicNameText.Text = "درمانگاه";
        }
    }

    /// <summary>
    /// اطلاعات پشتیبانی — کاملاً پویا؛ هر ردیف فقط وقتی مقدار دارد ساخته
    /// می‌شود. تلفن/موبایل با tel:، ایمیل با mailto: و وب‌سایت با مرورگر
    /// پیش‌فرض باز می‌شوند (هایپرلینک واقعی داخل کارت).
    /// </summary>
    private async Task LoadSupportAsync()
    {
        try
        {
            var response = await _api.GetAnonymousAsync<SupportResponse>("license/support");
            var sup = response?.Data;
            if (sup is null)
            {
                SupportEmptyText.Visibility = Visibility.Visible;
                return;
            }

            // نشان منبع — پنل | ذخیره‌شده | پیش‌فرض (شفافیت برای مالک)
            if (!string.IsNullOrWhiteSpace(sup.Source) && sup.Source != "default")
            {
                SupportSourceBadge.Visibility = Visibility.Visible;
                SupportSourceText.Text = sup.Source == "panel"
                    ? "به‌روزشده از پنل لایسنس"
                    : "ذخیره‌شده از فعال‌سازی";
            }

            var rows = new List<(string Icon, string Label, string? Value, string? Link)>
            {
                ("AccountTie", "پشتیبان", sup.Name, null),
                ("Domain", "شرکت", sup.Company, null),
                ("Phone", "تلفن", sup.Phone, sup.Phone is { } p ? $"tel:{p.Replace(" ", "")}" : null),
                ("Cellphone", "موبایل", sup.Mobile, sup.Mobile is { } m ? $"tel:{m.Replace(" ", "")}" : null),
                ("EmailOutline", "ایمیل", sup.Email, sup.Email is { } e ? $"mailto:{e}" : null),
                ("Web", "وب‌سایت", sup.Website, sup.Website),
            };

            var hasAny = false;
            foreach (var (icon, label, value, link) in rows)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                hasAny = true;
                SupportRows.Children.Add(BuildSupportRow(icon, label, value, link));
            }

            if (!hasAny)
                SupportEmptyText.Visibility = Visibility.Visible;
        }
        catch
        {
            SupportEmptyText.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// ساخت یک ردیف تماس پشتیبانی — آیکون + برچسب کم‌رنگ + مقدار؛ اگر لینک
    /// داشته باشد مقدار هایپرلینک آبیِ کلیک‌پذیر است (باز کردن tel/mailto/سایت).
    /// </summary>
    private static Border BuildSupportRow(string iconKind, string label, string value, string? link)
    {
        // چرخهٔ ۷۸ — روی شیشهٔ نیلی تیره، آبیِ تیره خوانا نیست؛ آبی‌روشنِ
        // هم‌خانوادهٔ اسپلش جایگزین شد (آیکون + هایپرلینک)
        var brush = BrushFromHex("#8FD4FF");

        // مقدار — هایپرلینک اگر لینک هست، متن ساده وگرنه
        FrameworkElement valueElement;
        if (link is not null)
        {
            var hyperlink = new Hyperlink(new Run(value))
            {
                NavigateUri = new Uri(link, UriKind.Absolute),
                Foreground = brush,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextDecorations = null,
            };
            // tel:/mailto: را خود Process.Start باز می‌کند؛ سایت را مرورگر
            hyperlink.RequestNavigate += (_, e) =>
            {
                try
                {
                    if (e.Uri.Scheme is "http" or "https")
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.AbsoluteUri,
                            UseShellExecute = true
                        });
                    else
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.AbsoluteUri.Replace(" ", ""),
                            UseShellExecute = true
                        });
                }
                catch
                {
                    // باز کردن پروتکل روی این ایستگاه ممکن نیست — بی‌صدا رد می‌شود؛
                    // خود مقدار روی صفحه هست و کاربر دستی شماره می‌گیرد
                }
            };
            valueElement = new TextBlock { Inlines = { hyperlink } };
        }
        else
        {
            valueElement = new TextBlock
            {
                Text = value,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
            };
        }

        var row = new Border
        {
            Padding = new Thickness(0, 5, 0, 5),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new MaterialDesignThemes.Wpf.PackIcon
                    {
                        Kind = (MaterialDesignThemes.Wpf.PackIconKind)Enum.Parse(
                            typeof(MaterialDesignThemes.Wpf.PackIconKind), iconKind),
                        Width = 15, Height = 15,
                        Foreground = brush,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 11,
                        Foreground = BrushFromHex("#B9CDE8"),
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new ContentControl
                    {
                        Content = valueElement,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                }
            }
        };
        return row;
    }

    /// <summary>
    /// کپی همهٔ مشخصات برای پشتیبانی — یک متن مرتب که کاربر هنگام تماس یا
    /// ارسال تیکت یک‌جا می‌فرستد: محصول، نسخه، بیلد (حیاتی برای تشخیص
    /// «کدام بیلد روی ایستگاه است»)، درمانگاه، لایسنس و وضعیت.
    /// </summary>
    private void OnCopyInfoClick(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, new[]
        {
            "سامانه جامع مدیریت درمانگاه سهند",
            $"نسخه: {AppInfo.Version}",
            $"بیلد ایستگاه: {AppInfo.BuildStamp}",
            $"نام درمانگاه: {_clinicName}",
            $"شماره لایسنس: {_licenseKey}",
            $"وضعیت لایسنس: {_licenseStatus}",
            $"تاریخ انقضا: {_licenseExpiry}",
        });

        try
        {
            Clipboard.SetText(text);
            ((Button)sender).Content = "✓ کپی شد";
            var btn = (Button)sender;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.6)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                btn.Content = "کپی اطلاعات برای پشتیبانی";
            };
            timer.Start();
        }
        catch
        {
            // کلیپ‌بورد در دسترس نیست (قفل ایستگاه) — بی‌صدا رد می‌شود
        }
    }

    /// <summary>دکمهٔ بستن</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    /* ─── قرارداد پاسخ‌های API — همان شکلی که سرور برمی‌گرداند ─── */

    private sealed class StatusResponse
    {
        public bool IsSuccess { get; set; }
        public StatusData? Data { get; set; }
    }

    private sealed class StatusData
    {
        public bool IsValid { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    private sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    private sealed class SettingDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    private sealed class SupportResponse
    {
        public bool IsSuccess { get; set; }
        public SupportData? Data { get; set; }
    }

    private sealed class SupportData
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Source { get; set; }
    }
}
