using System.IO;
using System.Text.Json;

namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>ردیف کش محلی تداخل — منطبق بر DrugInteractionCatalogDto سرور</summary>
public sealed class CachedInteraction
{
    /// <summary>نام ژنریک داروی اول</summary>
    public string M1 { get; set; } = string.Empty;

    /// <summary>نام فارسی داروی اول</summary>
    public string M1Fa { get; set; } = string.Empty;

    /// <summary>نام ژنریک داروی دوم</summary>
    public string M2 { get; set; } = string.Empty;

    /// <summary>نام فارسی داروی دوم</summary>
    public string M2Fa { get; set; } = string.Empty;

    /// <summary>شدت ۱ تا ۴</summary>
    public int Severity { get; set; }

    /// <summary>متن شدت</summary>
    public string SevText { get; set; } = string.Empty;

    /// <summary>رنگ شدت</summary>
    public string SevColor { get; set; } = "#ECEFF1";

    /// <summary>مکانیسم تداخل</summary>
    public string? Mech { get; set; }

    /// <summary>توصیهٔ بالینی</summary>
    public string? Rec { get; set; }
}

/// <summary>
/// کش محلی کاتالوگ تداخل‌های دارویی — چرخه بیستم (گزارش چهاردهم مالک):
/// «در نسخه‌نویسی تمامی تداخل‌ها داخل برنامه باشد و دکمهٔ بروزرسانی هم داشته
/// باشد که با زدنش تداخل‌های جدید اضافه بشن».
/// کارکرد:
///   • بارگذاری اولیه از فایل کش %AppData%\ClinicPro\interactions.json
///   • UpdateAsync — دانلود کل کاتالوگ از سرور و بازنویسی کش محلی
///   • Check — تطبیق جفت‌های ژنریکِ داروهای نسخه با کش، بدون نیاز به شبکه
/// بررسی نسخه‌نویسی این منبع را «همراه با» پاسخ سرور ترکیب می‌کند تا هیچ
/// هشداری از قلم نیفتد؛ در قطعی شبکه فقطِ همین منبعِ محلی پاسخ می‌دهد.
/// </summary>
public static class DrugInteractionCache
{
    /// <summary>مسیر کش محلی — %AppData%\ClinicPro\interactions.json</summary>
    private static string CachePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClinicPro");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "interactions.json");
        }
    }

    /// <summary>ردیف‌های بارگذاری‌شدهٔ کش — تنبل؛ بار اول فایل خوانده می‌شود</summary>
    private static List<CachedInteraction>? _rows;

    /// <summary>نشانگر بارگذاری انجام‌شده</summary>
    private static bool _loaded;

    /// <summary>همگام‌سازی داخلی برای نخ‌ها</summary>
    private static readonly object Gate = new();

    /// <summary>همهٔ ردیف‌های کش — در نبود فایل، فهرست خالی</summary>
    public static List<CachedInteraction> Rows
    {
        get
        {
            lock (Gate)
            {
                if (_loaded)
                    return _rows!;
                _rows = LoadFromDisk();
                _loaded = true;
                return _rows;
            }
        }
    }

    /// <summary>شمار ردیف‌های کش — برای نمایش وضعیت در UI</summary>
    public static int Count => Rows.Count;

    /// <summary>تاریخ آخرین بروزرسانی کش — null یعنی هنوز کشی نیست</summary>
    public static DateTime? LastUpdated =>
        File.Exists(CachePath)
            ? File.GetLastWriteTime(CachePath)
            : null;

    /// <summary>
    /// بروزرسانی کش از سرور — کل کاتالوگ دانلود و روی دیسک ذخیره می‌شود؛
    /// ردیف‌های حذف‌شده از سرور هم از کش می‌افتند (جایگزینی کامل).
    /// </summary>
    /// <returns>شمار ردیف‌های جدید کش؛ null یعنی دانلود ناموفق</returns>
    public static async Task<int?> UpdateAsync(IApiClient api)
    {
        var envelope = await api.GetAsync<InteractionsEnvelope>("prescriptions/interactions");
        var data = envelope?.Data;
        if (data is null)
            return null;

        var rows = data.Select(d => new CachedInteraction
        {
            M1 = d.Medication1Generic,
            M1Fa = d.Medication1NameFa,
            M2 = d.Medication2Generic,
            M2Fa = d.Medication2NameFa,
            Severity = d.Severity,
            SevText = d.SeverityText,
            SevColor = string.IsNullOrWhiteSpace(d.SeverityColor) ? "#ECEFF1" : d.SeverityColor,
            Mech = d.Mechanism,
            Rec = d.Recommendation,
        }).ToList();

        lock (Gate)
        {
            _rows = rows;
            _loaded = true;
        }

        File.WriteAllText(CachePath,
            JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        return rows.Count;
    }

    /// <summary>
    /// تطبیق محلی — همهٔ جفت‌های تداخلی موجود بین نام‌های ژنریک داده‌شده.
    /// تطبیق به‌صورت نادیده‌گرفتن بزرگی حروف و حساس به‌فاصله انجام می‌شود؛
    /// نام فارسی هم پشتیبان است تا اگر کلاینت نام فارسی داشت تطبیق شود.
    /// </summary>
    /// <param name="genericNames">نام‌های ژنریک (یا فارسی) داروهای نسخه</param>
    public static List<CachedInteraction> Check(IEnumerable<string> genericNames)
    {
        var names = genericNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (names.Count < 2)
            return new List<CachedInteraction>();

        var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Rows
            .Where(r => nameSet.Contains(r.M1) || nameSet.Contains(r.M1Fa))
            .Where(r => nameSet.Contains(r.M2) || nameSet.Contains(r.M2Fa))
            .ToList();
    }

    /// <summary>خواندن کش از دیسک — در هر خطا فهرست خالی</summary>
    private static List<CachedInteraction> LoadFromDisk()
    {
        try
        {
            if (!File.Exists(CachePath))
                return new List<CachedInteraction>();
            return JsonSerializer.Deserialize<List<CachedInteraction>>(
                       File.ReadAllText(CachePath)) ?? new List<CachedInteraction>();
        }
        catch
        {
            return new List<CachedInteraction>();
        }
    }

    /// <summary>پاکت پاسخ دانلود کاتالوگ</summary>
    private sealed class InteractionsEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<CatalogRow>? Data { get; set; }
    }

    /// <summary>ردیف کاتالوگ سرور</summary>
    private sealed class CatalogRow
    {
        public string Medication1Generic { get; set; } = string.Empty;
        public string Medication1NameFa { get; set; } = string.Empty;
        public string Medication2Generic { get; set; } = string.Empty;
        public string Medication2NameFa { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string SeverityText { get; set; } = string.Empty;
        public string SeverityColor { get; set; } = "#ECEFF1";
        public string? Mechanism { get; set; }
        public string? Recommendation { get; set; }
    }
}
