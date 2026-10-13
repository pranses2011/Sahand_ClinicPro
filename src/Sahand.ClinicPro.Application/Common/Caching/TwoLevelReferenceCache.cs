using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Sahand.ClinicPro.Application.Common.Caching;

/// <summary>
/// کلیدهای مشترک لایهٔ دوم — چرخهٔ ۶۱ (P-05).
/// </summary>
public static class DistributedCacheKeys
{
    /// <summary>پیشوند همهٔ مقاله‌های L2 — مقاله‌های L1 هم با همین کلید ذخیره می‌شوند</summary>
    public const string Prefix = "scpro:ref:";

    /// <summary>قالب کلید «نسل» یک پیشوند — با هر باطل‌سازیِ پیشوندی، نسل یک بالا می‌رود</summary>
    public static string Generation(string prefix) => $"scpro:gen:{prefix}";
}

/// <summary>
/// کش مرجع دومرحله‌ای (L1 حافظه‌ای + L2 توزیع‌شده) — چرخهٔ ۶۱ (P-05).
/// </summary>
/// <remarks>
/// معماری برای استقرارهای چندنمونه‌ای (چند نمونهٔ API پشت یک بارگذار، یا
/// میزبان‌های .NET جدا که دادهٔ مرجع مشترک دارند):
///
///  خواندن:  L1 (کوتاه‌عمر) → L2 (بلندعمر) → کارخانه (دیتابیس)
///  نوشتن:  کارخانه → L2 (JSON) → L1
///
/// باطل‌سازی پیشوندی بین نمونه‌ها با «شمارندهٔ نسل» انجام می‌شود:
/// هر پیشوند یک شمارندهٔ نسل در L2 دارد. هر مقالهٔ L1 و L2 همراه با
/// نسلی که هنگام نوشتن معتبر بوده ذخیره می‌شود؛ در هر خواندن، نسلِ
/// فعلیِ پیشوند از L2 خوانده می‌شود و مقاله‌ای که نسلِ قدیمی‌تری دارد
/// «کهنه» شمرده می‌شود — در نتیجه با هر EvictByPrefix (بالابردن نسل)،
/// همهٔ نمونه‌ها و هر دو لایه به‌صورت خودکار مقاله‌های پیشوند را دور
/// می‌ریزند و از کارخانه دوباره می‌سازند.
///
/// L2 «اختیاری» است (تصمیم نقشهٔ راه): وقتی میزبان IDistributedCache
/// ثبت نکرده باشد، این کش به L1 خالص فرو می‌کاهد و رفتار آن با
/// MemoryReferenceCache هم‌ارز می‌شود؛ استقرار تک‌سرور درمانگاه هیچ
/// وابستگی جدیدی نمی‌گیرد.
///
/// نکتهٔ کارایی: خواندن نسل پیشوند یک رفت‌وبرگشت اضافهٔ L2 در هر خواندنِ
/// مقاله است؛ برای پشتیبان حافظه‌ای (پیش‌فرض) ناچیز است و برای Redis آینده
/// با کش‌شدگی مقاله‌ها در L1 عملاً کم‌تر از ۱٪ درخواست‌ها به آن می‌رسد.
/// </remarks>
public sealed class TwoLevelReferenceCache : IReferenceCache
{
    /// <summary>جداسازی قالب JSON مقاله‌های L2 — نام‌گذاری پیش‌فرض، بدون تبدیل اضافه</summary>
    private static readonly JsonSerializerOptions L2Json = new()
    {
        IncludeFields = true,
    };

    /// <summary>کش L1 — همان حافظهٔ درون‌فرایندی</summary>
    private readonly IMemoryCache _l1;

    /// <summary>کش L2 — اختیاری؛ تهی یعنی استقرار تک‌نمونه‌ای (فقط L1)</summary>
    private readonly IDistributedCache? _l2;

    /// <summary>عمر مقالهٔ L1 — کوتاه (چرخهٔ ۶۰: بین ۹۰ ثانیه تا ۵ دقیقه)</summary>
    private readonly TimeSpan _l1Ttl;

    /// <summary>عمر مقالهٔ L2 — بلند (پیش‌فرض ۱۰ دقیقه)</summary>
    private readonly TimeSpan _l2Ttl;

    /// <summary>قفل‌های دانه‌ای per-key — ضد انفجار کش</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    /// <summary>عنصر داخلی L1 — مقدار + نسلِ پیشوندی که با آن نوشته شده</summary>
    private sealed record L1Envelope(object? Value, long Generation);

    /// <summary>عنصر داخلی L2 — همان الگو با نسل؛ در JSON سریال می‌شود</summary>
    private sealed record L2Envelope(long Generation, object? Value);

    /// <summary>
    /// سازندهٔ استقرار تک‌نمونه‌ای — بدون L2؛ رفتار دقیقاً L1 خالص است.
    /// </summary>
    public TwoLevelReferenceCache(IMemoryCache l1, TimeSpan l1Ttl)
        : this(l1, null, l1Ttl, TimeSpan.FromMinutes(10))
    {
    }

    /// <summary>
    /// سازندهٔ کامل — با L2 توزیع‌شده.
    /// </summary>
    /// <param name="l1">کش حافظه‌ای درون‌فرایندی</param>
    /// <param name="l2">کش توزیع‌شده (Redis یا Memory) — تهی یعنی خاموش</param>
    /// <param name="l1Ttl">عمر مقاله‌های L1</param>
    /// <param name="l2Ttl">عمر مقاله‌های L2</param>
    public TwoLevelReferenceCache(IMemoryCache l1, IDistributedCache? l2,
        TimeSpan l1Ttl, TimeSpan l2Ttl)
    {
        _l1 = l1;
        _l2 = l2;
        _l1Ttl = l1Ttl;
        _l2Ttl = l2Ttl;
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl, CancellationToken cancellationToken) where T : class
    {
        // مسیر گرم — L1
        if (TryGetL1<T>(key, out var warm) && warm is not null)
            return warm;

        var gate = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // دوبار-بررسی L1 پس از قفل
            if (TryGetL1<T>(key, out var second) && second is not null)
                return second;

            // L2 (اگر در کار است) — بازگشایی JSON؛ مقالهٔ کهنه مثل نبودن است
            if (_l2 is not null && TryGetL2<T>(key, out var fromL2) && fromL2 is not null)
            {
                StoreL1(key, fromL2, ttl);
                return fromL2;
            }

            // کارخانه (دیتابیس) — سپس نوشتن در L2 و L1
            var value = await factory(cancellationToken);
            if (value is null)
                return value!;

            StoreL2(key, value);
            StoreL1(key, value, ttl);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public void Evict(string key)
    {
        _l1.Remove(key);
        _l2?.Remove(Prefixed(key));
    }

    /// <inheritdoc />
    public void EvictByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;

        // نمونهٔ جاری بی‌درنگ کلیدهای شناخته‌شدهٔ L1 را دور می‌ریزد
        foreach (var known in KnownKeysFor(prefix))
            _l1.Remove(known);

        if (_l2 is null)
            return; // تک‌نمونه‌ای — همین کافی است

        // چندنمونه‌ای — نسل پیشوند در L2 بالا می‌رود؛ همهٔ نمونه‌ها و هر دو لایه
        // مقاله‌های این پیشوند را کهنه خواهند دید (مقاله‌های L2 قدیمی با مقایسهٔ
        // نسلِ درونشان در نخستین خواندن دور ریخته می‌شوند)
        var genKey = DistributedCacheKeys.Generation(prefix);
        var current = ReadGeneration(genKey);
        _l2.SetString(genKey, (current + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _l2Ttl * 6 });
    }

    /// <summary>
    /// خواندن L1 با کنترل نسل — مقالهٔ با نسلِ قدیمی‌تر از نسلِ فعلی پیشوند،
    /// کهنه است و دور ریخته می‌شود (مثل نبودن).
    /// </summary>
    private bool TryGetL1<T>(string key, out T? value)
    {
        if (_l1.TryGetValue(key, out L1Envelope? envelope) && envelope?.Value is not null)
        {
            var prefix = Level2PrefixOf(key);
            if (prefix is not null && _l2 is not null)
            {
                var remote = ReadGeneration(DistributedCacheKeys.Generation(prefix));
                if (remote != envelope.Generation)
                {
                    _l1.Remove(key); // کهنه — دور ریختن
                    value = default;
                    return false;
                }
            }

            if (envelope.Value is T typed)
            {
                value = typed;
                return true;
            }
        }
        value = default;
        return false;
    }

    /// <summary>نوشتن L1 همراه با نسل فعلی پیشوندِ متناظر</summary>
    private void StoreL1<T>(string key, T value, TimeSpan ttl) where T : class
    {
        var generation = CurrentGenerationOf(key);
        _l1.Set(key, new L1Envelope(value, generation), ttl);
    }

    /// <summary>
    /// خواندن L2 — بازگشایی JSON پوشش‌دار (نسل + مقدار)؛ مقالهٔ با نسلِ قدیمی
    /// مثل نبودن است (خوانده نمی‌شود تا کارخانه نسخهٔ تازه بسازد).
    /// </summary>
    private bool TryGetL2<T>(string key, out T? value)
    {
        try
        {
            var raw = _l2!.GetString(Prefixed(key));
            if (string.IsNullOrEmpty(raw))
            {
                value = default;
                return false;
            }

            var envelope = JsonSerializer.Deserialize<L2Envelope>(raw, L2Json);
            if (envelope?.Value is null)
            {
                value = default;
                return false;
            }

            // نسل مقاله باید با نسل فعلی پیشوند یکی باشد
            var current = CurrentGenerationOf(key);
            if (envelope.Generation != current)
            {
                value = default;
                return false; // کهنه — کارخانه نسخهٔ تازه می‌سازد
            }

            if (envelope.Value is JsonElement element)
            {
                // مقدار به‌صورت JSON خام ذخیره شده — بازگشایی به نوع هدف
                var typed = element.Deserialize<T>(L2Json);
                if (typed is null)
                {
                    value = default;
                    return false;
                }
                value = typed;
                return true;
            }

            if (envelope.Value is T direct)
            {
                value = direct;
                return true;
            }

            value = default;
            return false;
        }
        catch (JsonException)
        {
            // مقالهٔ خراب — مثل نبودن
            value = default;
            return false;
        }
    }

    /// <summary>نوشتن L2 — پوشش نسل + مقدار با عمر بلند</summary>
    private void StoreL2<T>(string key, T value) where T : class
    {
        if (_l2 is null) return; // L2 خاموش — فقط L1
        try
        {
            var generation = CurrentGenerationOf(key);
            _l2.SetString(Prefixed(key),
                JsonSerializer.Serialize(new L2Envelope(generation, value), L2Json),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _l2Ttl });
        }
        catch (JsonException)
        {
            // نوع سریال‌نشدنی — فقط L1 می‌ماند؛ کش هیچ‌وقت عامل خطا نیست
        }
    }

    /// <summary>نسل فعلی پیشوندِ متناظر با کلید — بدون L2 همیشه صفر (نسل بی‌اثر)</summary>
    private long CurrentGenerationOf(string key)
    {
        if (_l2 is null) return 0L;
        var prefix = Level2PrefixOf(key);
        if (prefix is null) return 0L;
        return ReadGeneration(DistributedCacheKeys.Generation(prefix));
    }

    /// <summary>خواندن شمارندهٔ نسل یک پیشوند از L2 — تهی یعنی صفر</summary>
    private long ReadGeneration(string genKey)
    {
        var raw = _l2?.GetString(genKey);
        return long.TryParse(raw, out var gen) ? gen : 0L;
    }

    /// <summary>فهرست کلیدهای معلوم برای یک پیشوند — کاتالوگ ثابت برنامه</summary>
    private static IEnumerable<string> KnownKeysFor(string prefix) => prefix switch
    {
        // پیشوند ریشه — همهٔ داده‌های مرجع
        DistributedCacheKeys.Prefix => new[]
        {
            ReferenceCacheKeys.InsuranceCompanies, ReferenceCacheKeys.LabTests,
            ReferenceCacheKeys.Medications, ReferenceCacheKeys.Diagnoses,
            ReferenceCacheKeys.Settings(null), ReferenceCacheKeys.Settings("Printing"),
            ReferenceCacheKeys.Settings("License"), ReferenceCacheKeys.Settings("General"),
        },
        // پیشوند تنظیمات — همهٔ دسته‌ها + «همه»
        ReferenceCacheKeys.SettingsPrefix => new[]
        {
            ReferenceCacheKeys.Settings(null), ReferenceCacheKeys.Settings("Printing"),
            ReferenceCacheKeys.Settings("License"), ReferenceCacheKeys.Settings("General"),
        },
        _ => Array.Empty<string>(),
    };

    /// <summary>پیشوند سطح‌دوی یک کلید — «ref:{دسته}:» یا خودِ «ref:»</summary>
    private static string? Level2PrefixOf(string key)
    {
        if (!key.StartsWith(ReferenceCacheKeys.Prefix, StringComparison.Ordinal))
            return null;
        var rest = key.AsSpan(ReferenceCacheKeys.Prefix.Length);
        var sep = rest.IndexOf(':');
        return sep > 0
            ? $"{ReferenceCacheKeys.Prefix}{rest.Slice(0, sep).ToString()}:"
            : ReferenceCacheKeys.Prefix;
    }

    /// <summary>کلید کامل L2 — با پیشوند مشترک سرویس</summary>
    private static string Prefixed(string key) => DistributedCacheKeys.Prefix + key;
}
