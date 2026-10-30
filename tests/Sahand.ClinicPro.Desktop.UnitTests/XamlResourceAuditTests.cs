using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// 🔍 گارد ممیزی ارجاعات منبع XAML — چرخه چهاردهم (v2.0.1)
/// ریشه مشکل: ارجاعات StaticResource در XAML هنگام «بیلد» اعتبارسنجی نمی‌شوند و
/// فقط در «زمان اجرا» فرو می‌ریزند (کرش MaterialDesignFilledButton در پنجره ورود).
/// این تست همان کلاس خطا را پیش از انتشار می‌گیرد: هر ارجاع StaticResource/DynamicResource
/// در XAML دسکتاپ باید یا در خود پروژه تعریف شده باشد (x:Key) یا در مانیفست کلیدهای
/// واقعی پکیج MaterialDesignThemes موجود باشد.
/// چرخه شانزدهم (v2.0.3 — گزارش دهم مالک): تست دوم (scope-محور) افزوده شد — کرش
/// BoolToVis داشبورد با وجود همین تست از نگاه رفت، چون این تست کلیدها را از «همهٔ
/// فایل‌ها به‌صورت سراسری» جمع می‌کند و تعریف BoolToVis در ۱۳ فایل دیگر، ارجاعِ
/// بی‌تعریفِ DashboardView را پوشانده بود؛ در حالی که StaticResource در زمان اجرا
/// فقط از منابع «همان فایل» یا App.xaml حل می‌شود.
/// </summary>
public class XamlResourceAuditTests
{
    private static readonly Regex RefRegex = new(
        @"(?:Static|Dynamic)Resource\s+([A-Za-z_][A-Za-z0-9_.\-]*)",
        RegexOptions.Compiled);

    /// <summary>فقط ارجاعات StaticResource — این‌ها باید در parse-time فایل حل شوند.</summary>
    private static readonly Regex StaticRefRegex = new(
        @"StaticResource\s+([A-Za-z_][A-Za-z0-9_.\-]*)",
        RegexOptions.Compiled);

    private static readonly Regex KeyDefRegex = new(
        @"x:Key=""([^""]+)""",
        RegexOptions.Compiled);

    /// <summary>کلیدهای سیستمی WPF که تعریف آن‌ها در قالب‌های پایه است.</summary>
    private static readonly HashSet<string> SystemKeys = new() { "FocusVisual" };

    [Fact]
    public void همه_ارجاعات_منبع_در_فایل‌های_اکسام_ال_تعریف_یا_موجود_هستند()
    {
        // ۱) یافتن ریشه مخزن (نشانه: Directory.Build.props)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent!;
        }

        dir.Should().NotBeNull("ریشه مخزن با Directory.Build.props یافت نشد — تست باید از درون مخزن اجرا شود");

        // ۲) مانیفست کلیدهای واقعی پکیج MaterialDesignThemes
        var manifest = Path.Combine(dir!.FullName, "scripts", "materialdesign-keys-5.3.2.txt");
        File.Exists(manifest).Should().BeTrue(
            $"مانیفست کلیدهای پکیج یافت نشد: {manifest} — با scripts/extract_materialdesign_keys.py بازتولید شود");
        var pkgKeys = File.ReadAllLines(manifest)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("#"))
            .ToHashSet();

        // ۳) کلیدهای محلی تعریف‌شده در XAML پروژه دسکتاپ
        var xamlDir = Path.Combine(dir.FullName, "src", "Sahand.ClinicPro.Desktop");
        var xamlFiles = Directory.EnumerateFiles(xamlDir, "*.xaml", SearchOption.AllDirectories).ToList();
        xamlFiles.Should().NotBeEmpty("هیچ فایل XAML یافت نشد");

        var localKeys = new HashSet<string>();
        foreach (var file in xamlFiles)
        {
            var text = File.ReadAllText(file);
            foreach (Match m in KeyDefRegex.Matches(text))
            {
                localKeys.Add(m.Groups[1].Value);
            }
        }

        // ۴) بررسی همه ارجاعات — هیچ کلید بدون تعریف نباشد
        var missing = new SortedDictionary<string, List<string>>();
        foreach (var file in xamlFiles)
        {
            var rel = Path.GetRelativePath(dir.FullName, file);
            foreach (Match m in RefRegex.Matches(File.ReadAllText(file)))
            {
                var key = m.Groups[1].Value;
                if (localKeys.Contains(key) || pkgKeys.Contains(key) || SystemKeys.Contains(key))
                {
                    continue;
                }

                (missing.TryGetValue(key, out var list) ? list : missing[key] = [])
                    .Add(rel);
            }
        }

        missing.Should().BeEmpty(
            "ارجاعات منبع نامعتبر در XAML یافت شد — این ارجاعات در زمان اجرا کرش می‌کنند. " +
            "کلیدها را با اسکریپت scripts/audit_xaml_resources.py و مانیفست " +
            "scripts/materialdesign-keys-5.3.2.txt بررسی و اصلاح کنید. موارد: " +
            string.Join("، ", missing.Select(kv => $"{kv.Key} ({kv.Value.Count} فایل)")));
    }

    [Fact]
    public void ارجاعات_استاتیک_در_محدودهٔ_همان_فایل_یا_اپ_قابل_حل_هستند()
    {
        // ۱) یافتن ریشه مخزن (نشانه: Directory.Build.props)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent!;
        }

        dir.Should().NotBeNull("ریشه مخزن با Directory.Build.props یافت نشد — تست باید از درون مخزن اجرا شود");

        // ۲) مانیفست کلیدهای واقعی پکیج MaterialDesignThemes (از طریق App.xaml ادغام‌شده)
        var manifest = Path.Combine(dir!.FullName, "scripts", "materialdesign-keys-5.3.2.txt");
        File.Exists(manifest).Should().BeTrue(
            $"مانیفست کلیدهای پکیج یافت نشد: {manifest} — با scripts/extract_materialdesign_keys.py بازتولید شود");
        var pkgKeys = File.ReadAllLines(manifest)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("#"))
            .ToHashSet();

        // ۳) کلیدهای تعریف‌شده در «هر فایل» به‌تفکیک + کلیدهای سطح برنامه (App.xaml)
        var xamlDir = Path.Combine(dir.FullName, "src", "Sahand.ClinicPro.Desktop");
        var xamlFiles = Directory.EnumerateFiles(xamlDir, "*.xaml", SearchOption.AllDirectories).ToList();
        xamlFiles.Should().NotBeEmpty("هیچ فایل XAML یافت نشد");

        var keysPerFile = xamlFiles.ToDictionary(
            f => f,
            f =>
            {
                var keys = new HashSet<string>();
                foreach (Match m in KeyDefRegex.Matches(File.ReadAllText(f)))
                {
                    keys.Add(m.Groups[1].Value);
                }
                return keys;
            });

        var appKeys = keysPerFile.TryGetValue(Path.Combine(xamlDir, "App.xaml"), out var appSet)
            ? new HashSet<string>(appSet)
            : new HashSet<string>();

        // ۳ب) دیکشنریهای ادغامشده در App.xaml (مثلاً Styles/DesignSystem.xaml)
        //     WPF کلیدهای آنها را هم در سطح برنامه و همان InitializeComponent
        //     در دسترس میگذارد. ممیزی پیشتر فقط x:Keyهای «خود فایل App.xaml»
        //     را میدید و شش صفحهای را که از سیستم طراحی مشترک استفاده میکنند
        //     بهاشتباه «خارج از محدوده» اعلام میکرد؛ در حالی که رندر واقعی WPF
        //     همان صفحات روی ویندوز بدون XamlParseException انجام شده است.
        var appPath = Path.Combine(xamlDir, "App.xaml");
        if (File.Exists(appPath))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(appPath),
                         @"Source=""pack://application:,,,/([^"";]+\.xaml)""",
                         RegexOptions.IgnoreCase))
            {
                var rel = m.Groups[1].Value.TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var merged = Path.GetFullPath(Path.Combine(xamlDir, rel));
                if (!File.Exists(merged)) continue;
                foreach (Match k in KeyDefRegex.Matches(File.ReadAllText(merged)))
                    appKeys.Add(k.Groups[1].Value);
            }
        }

        // ۴) هر ارجاع StaticResource باید در محدودهٔ خود فایل + App.xaml + پکیج حل شود —
        //    تعریف کلید در «فایل‌های دیگر» برای این فایل در parse-time قابل‌دسترس نیست
        var scopeViolations = new SortedDictionary<string, List<string>>();
        foreach (var file in xamlFiles)
        {
            var rel = Path.GetRelativePath(dir.FullName, file);
            var allowed = keysPerFile[file]
                .Concat(appKeys)
                .Concat(pkgKeys)
                .Concat(SystemKeys)
                .ToHashSet();

            foreach (Match m in StaticRefRegex.Matches(File.ReadAllText(file)))
            {
                var key = m.Groups[1].Value;
                if (allowed.Contains(key))
                {
                    continue;
                }

                (scopeViolations.TryGetValue(key, out var list) ? list : scopeViolations[key] = [])
                    .Add(rel);
            }
        }

        scopeViolations.Should().BeEmpty(
            "ارجاع StaticResource خارج از محدودهٔ فایل یافت شد — این ارجاعات در parse-time " +
            "(InitializeComponent) با XamlParseException می‌ریزند حتی اگر کلید در فایل‌های " +
            "دیگر تعریف شده باشد. راه‌حل: تعریف محلی <... x:Key=\"...\"/> در همان فایل یا " +
            "انتقال به App.xaml. موارد: " +
            string.Join("، ", scopeViolations.Select(kv =>
                $"{kv.Key} در {string.Join("، ", kv.Value.Distinct())}")));
    }

    [Fact]
    public void کاشی_میانبر_و_توست_در_مقیاس_های_مختلف_به_قاب_ثابت_بریده_نمی‌شوند()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
        {
            dir = dir.Parent!;
        }

        dir.Should().NotBeNull("ریشه مخزن با Directory.Build.props یافت نشد");
        var desktop = Path.Combine(dir!.FullName, "src", "Sahand.ClinicPro.Desktop");
        var mainWindow = File.ReadAllText(Path.Combine(desktop, "MainWindow.xaml"));
        var mainWindowCode = File.ReadAllText(Path.Combine(desktop, "MainWindow.xaml.cs"));
        var appCode = File.ReadAllText(Path.Combine(desktop, "App.xaml.cs"));
        var dashboard = File.ReadAllText(Path.Combine(desktop, "Views", "DashboardView.xaml"));

        mainWindowCode.Should().Contain("PageHost.SetResourceReference(FrameworkElement.LayoutTransformProperty",
            "مقیاس رابط باید به محتوای قابل‌اسکرول محدود باشد، نه ریشهٔ پنجره و اعلان‌های رویی");
        appCode.Should().NotContain("rootContent.SetResourceReference(\n                FrameworkElement.LayoutTransformProperty",
            "LayoutTransform روی ریشهٔ پنجره می‌تواند توست پایین صفحه را از قاب بیرون ببرد");
        mainWindow.Should().Contain("UseLayoutRounding=\"True\"",
            "گرد کردن اندازه‌گیری در سطح پنجره خطای نیم‌پیکسل در DPIهای کسری را مهار می‌کند");
        mainWindow.Should().Contain("MinHeight=\"60\"",
            "کارت توست باید حداقل فضای عمودی امن برای متن و آیکون داشته باشد");
        mainWindow.Should().Contain("Margin=\"16,0,16,54\"",
            "توست باید با فاصلهٔ امن بالای نوار وضعیت قرار گیرد، نه روی لبهٔ پنجره");
        mainWindow.Should().NotContain("MaxHeight=\"",
            "ارتفاع توست نباید سقف ثابت داشته باشد؛ پیام بلند باید رشد کند");
        dashboard.Should().Contain("UseLayoutRounding=\"True\"");
        dashboard.Should().Contain("CanContentScroll=\"False\"",
            "اسکرول پیکسلی باید امکان رساندن کامل آخرین ردیف میانبرها به viewport را بدهد");
        dashboard.Should().Contain("Width=\"168\" MinHeight=\"96\"",
            "کاشی باید ارتفاع کشسان و فضای کافی برای فونت/مقیاس داشته باشد");
        dashboard.Should().Contain("TextWrapping=\"Wrap\"",
            "عنوان و زیرعنوان بلند باید به‌جای خروج از قاب در چند خط نمایش داده شوند");
    }
}
