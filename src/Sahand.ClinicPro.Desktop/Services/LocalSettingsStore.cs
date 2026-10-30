using System.IO;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// ذخیره‌سازی تنظیمات محلی کلاینت — فایل JSON در %AppData%
/// الگوی مشابه TokenManager — بدون رمز عبور (فقط نام کاربری و تنظیمات ظاهری)
/// </summary>
public static class LocalSettingsStore
{
    /// <summary>مسیر فایل تنظیمات — %AppData%\ClinicPro\settings.json</summary>
    private static string SettingsPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClinicPro");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    /// <summary>همهٔ کلیدهای ذخیره‌شده — برای فهرست‌ها (مثل پریست‌های تم چرخه ۲۴)</summary>
    public static List<string> Keys()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new List<string>();
            var json = File.ReadAllText(SettingsPath);
            var doc = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return doc?.Keys.ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>حذف یک کلید — برای حذف پریست تم (چرخه ۲۴)</summary>
    public static void Remove(string key)
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;
            var json = File.ReadAllText(SettingsPath);
            var doc = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (doc is null || !doc.Remove(key))
                return;
            File.WriteAllText(SettingsPath,
                System.Text.Json.JsonSerializer.Serialize(doc,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // حذف ناموفق هرگز نباید جریان را متوقف کند
        }
    }

    /// <summary>خواندن یک مقدار رشته‌ای — تهی در نبود</summary>
    public static string Get(string key)
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return string.Empty;
            var json = File.ReadAllText(SettingsPath);
            var doc = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return doc is not null && doc.TryGetValue(key, out var value) ? value : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static readonly object WriteLock = new();

    /// <summary>ثبت گروهی با اعلان خطا؛ تغییرات فرایند جاری با Set هم‌زمان نمی‌شوند.</summary>
    public static void SetManyRequired(IReadOnlyDictionary<string, string> values)
    {
        lock (WriteLock) LocalSettingsFile.MergeAndWrite(SettingsPath, values);
    }

    /// <summary>نوشتن یک مقدار رشته‌ای — فایل کامل بازنویسی می‌شود</summary>
    public static void Set(string key, string value)
    {
        try
        {
            SetManyRequired(new Dictionary<string, string> { [key] = value });
        }
        catch
        {
            // ذخیره تنظیمات محلی نباید جریان برنامه را متوقف کند
        }
    }
}
