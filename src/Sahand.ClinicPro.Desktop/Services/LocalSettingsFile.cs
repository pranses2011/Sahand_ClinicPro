using System.IO;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>ثبت گروهی تنظیمات بدون جایگزینی فایل سالم با نسخه ناقص.</summary>
public static class LocalSettingsFile
{
    /// <summary>داده قبلی نامعتبر یا خطای نوشتن را به فراخواننده اعلام می‌کند.</summary>
    public static void MergeAndWrite(string path, IReadOnlyDictionary<string, string> values)
    {
        var existing = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? throw new InvalidDataException("فایل تنظیمات خالی یا نامعتبر است.")
            : new Dictionary<string, string>();
        foreach (var (key, value) in values) existing[key] = value;
        Replace(path, existing);
    }

    /// <summary>جایگزینی کامل فایل با استفاده از فایل موقت هم‌پوشه.</summary>
    public static void Replace(string path, IReadOnlyDictionary<string, string> values)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("مسیر تنظیمات معتبر نیست.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".settings-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
