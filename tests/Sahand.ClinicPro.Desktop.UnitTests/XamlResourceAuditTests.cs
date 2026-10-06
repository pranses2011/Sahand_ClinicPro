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
/// </summary>
public class XamlResourceAuditTests
{
    private static readonly Regex RefRegex = new(
        @"(?:Static|Dynamic)Resource\s+([A-Za-z_][A-Za-z0-9_.\-]*)",
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
}
