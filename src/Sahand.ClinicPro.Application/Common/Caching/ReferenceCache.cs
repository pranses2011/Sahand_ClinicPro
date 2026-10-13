using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Sahand.ClinicPro.Application.Common.Caching;

/// <summary>
/// کلیدهای کش مرجع — چرخهٔ ۶۰ نقشهٔ راه (P-06).
/// همهٔ کلیدهای دادهٔ مرجع با پیشوند «ref:» شروع می‌شوند تا پاک‌سازیِ
/// پیشوندی («EvictByPrefix») بتواند همهٔ داده‌های مرجع را یک‌جا باطل کند.
/// نام‌گذاری: «ref:{دسته}:{جزئیات}» — مثال «ref:settings:Printing».
/// </summary>
public static class ReferenceCacheKeys
{
    /// <summary>پیشوند مشترک همهٔ کلیدهای مرجع</summary>
    public const string Prefix = "ref:";

    /// <summary>شرکت‌های بیمهٔ فعال — خوانده‌شده با هر بار باز شدن فرم پذیرش/بیمار/فاکتور</summary>
    public const string InsuranceCompanies = "ref:lookups:insurance-companies";

    /// <summary>آزمایش‌های فعال — صفحهٔ آزمایشگاه (بدون جستجو)</summary>
    public const string LabTests = "ref:lookups:lab-tests";

    /// <summary>داروها و اقلام انبار (بدون جستجو) — نسخه‌نویسی/داروخانه</summary>
    public const string Medications = "ref:lookups:medications";

    /// <summary>تشخیص‌های ICD-10 (بدون جستجو) — ویرایشگر SOAP</summary>
    public const string Diagnoses = "ref:lookups:diagnoses";

    /// <summary>قالب کلید تنظیمات: «ref:settings:{دسته}» — دستهٔ تهی یعنی همهٔ تنظیمات</summary>
    public static string Settings(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "ref:settings:__all__" : $"ref:settings:{category.Trim()}";

    /// <summary>پیشوند همهٔ کلیدهای تنظیمات — برای باطل‌سازی یکجای ذخیرهٔ تنظیم</summary>
    public const string SettingsPrefix = "ref:settings:";
}

/// <summary>
/// قرارداد کش مرجع لایهٔ کاربرد — چرخهٔ ۶۰ (P-06 نقشهٔ راه: «کش L1 برای
/// داده‌های مرجع: ICD-10، داروها، تخصص‌ها، تنظیمات»).
/// </summary>
/// <remarks>
/// چرا اینترفیس در لایهٔ Application تعریف می‌شود و پیاده‌سازی حافظه‌ای:
/// ۱) گرداننده‌های MediatR فقط باید به انتزاع وابسته باشند (قاعدهٔ معماری)؛
/// ۲) تست‌ها می‌توانند پیاده‌سازی بی‌اثر (Noop) تزریق کنند؛
/// ۳) چرخهٔ ۶۱ (P-05) لایهٔ دوم توزیع‌شده را با همین قرارداد می‌تواند
///    بازنویسی کند بدون دست‌زدن به گرداننده‌ها.
/// مقادیر ذخیره‌شده باید تغییرناپذیر (record/فهرست فقط‌خواندنی) باشند —
/// کش بین درخواست‌ها مشترک است و هیچ فراخوانی نباید نمونهٔ کش‌شده را
/// تغییر دهد.
/// </remarks>
public interface IReferenceCache
{
    /// <summary>
    /// خواندن از کش؛ در نبود، ساخت با کارخانه و ذخیره با اعتبار عمر مشخص.
    /// هم‌زمانی: دو فراخوانی موازی برای کلید یکسان فقط یک‌بار کارخانه را
    /// اجرا می‌کنند (قفل دانه‌ای per-key) تا «انفجار کش» در بار اول رخ ندهد.
    /// </summary>
    /// <typeparam name="T">نوع مقدار — باید تغییرناپذیر باشد</typeparam>
    /// <param name="key">کلید یکتا (از ReferenceCacheKeys)</param>
    /// <param name="factory">سازندهٔ مقدار در صورت نبود در کش</param>
    /// <param name="ttl">اعتبار عمر مقالهٔ کش</param>
    /// <param name="cancellationToken">توکن لغو</param>
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl, CancellationToken cancellationToken) where T : class;

    /// <summary>باطل‌کردن یک کلید — پس از هر نوشتن روی دادهٔ مرجع متناظر</summary>
    void Evict(string key);

    /// <summary>باطل‌کردن همهٔ کلیدهای یک پیشوند — مثلاً همهٔ تنظیمات</summary>
    void EvictByPrefix(string prefix);
}

/// <summary>
/// پیاده‌سازی حافظه‌ای کش مرجع (L1) — چرخهٔ ۶۰.
/// </summary>
/// <remarks>
/// ساختار داخلی: IMemoryCache برای مقاله‌ها + نگاشت پیشوند→فهرست کلیدهای
/// زنده برای EvictByPrefix (IMemoryCache پیشوند را پشتیبانی نمی‌کند).
/// قفل دانه‌ای per-key با ConcurrentDictionary برای جلوگیری از اجرای
/// موازی کارخانهٔ یک کلید (stampede).
/// </remarks>
public sealed class MemoryReferenceCache : IReferenceCache
{
    /// <summary>کش زیربنایی مایکروسافت — تزریق‌شده از میزبان</summary>
    private readonly IMemoryCache _cache;

    /// <summary>قفل‌های دانه‌ای per-key — جلوگیری از stampede</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    /// <summary>کلیدهای زنده به تفکیک پیشوند — برای EvictByPrefix</summary>
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _prefixIndex =
        new(StringComparer.Ordinal);

    /// <summary>سازنده با تزریق کش حافظه‌ای</summary>
    public MemoryReferenceCache(IMemoryCache cache) => _cache = cache;

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl, CancellationToken cancellationToken) where T : class
    {
        // مسیر گرم — بدون قفل
        if (_cache.TryGetValue(key, out T? hit) && hit is not null)
            return hit;

        var gate = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            // دوبار-بررسی پس از قفل — فراخوانی نخست ممکن است مقدار را نوشته باشد
            if (_cache.TryGetValue(key, out T? second) && second is not null)
                return second;

            var value = await factory(cancellationToken);
            if (value is null)
                return value!; // مقدار تهی کش نمی‌شود (نگاه منفی در کار نیست — سادگی)

            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                // حذف از فهرست‌های پیشوندی هنگام انقضا — نشت فهرست را می‌بندد
                PostEvictionCallbacks =
                {
                    new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = (k, _, _, _) =>
                        {
                            if (k is string keyStr)
                                RemoveFromPrefixIndex(keyStr);
                        }
                    }
                }
            });

            IndexByPrefix(key);
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
        _cache.Remove(key);
        RemoveFromPrefixIndex(key);
    }

    /// <inheritdoc />
    public void EvictByPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix)) return;
        if (_prefixIndex.TryGetValue(prefix, out var keys))
        {
            foreach (var key in keys.Keys)
            {
                _cache.Remove(key);
            }
            _prefixIndex.TryRemove(prefix, out _);
        }
    }

    /// <summary>ثبت کلید در فهرست پیشوندش — پیشوند سطح یک (ref:) و سطح دو (ref:settings:)</summary>
    private void IndexByPrefix(string key)
    {
        if (!key.StartsWith(ReferenceCacheKeys.Prefix, StringComparison.Ordinal)) return;

        // سطح یک: «ref:» — همهٔ مرجع‌ها
        RegisterUnderPrefix(ReferenceCacheKeys.Prefix, key);

        // سطح دو: «ref:{دسته}:» — مثلاً «ref:settings:» یا «ref:lookups:»
        var rest = key.AsSpan(ReferenceCacheKeys.Prefix.Length);
        var sep = rest.IndexOf(':');
        if (sep > 0)
            RegisterUnderPrefix($"{ReferenceCacheKeys.Prefix}{rest.Slice(0, sep).ToString()}:", key);
    }

    /// <summary>افزودن کلید به فهرست یک پیشوند مشخص</summary>
    private void RegisterUnderPrefix(string prefix, string key)
    {
        var bucket = _prefixIndex.GetOrAdd(prefix, _ => new ConcurrentDictionary<string, byte>());
        bucket[key] = 1;
    }

    /// <summary>حذف کلید از همهٔ فهرست‌های پیشوندی</summary>
    private void RemoveFromPrefixIndex(string key)
    {
        foreach (var bucket in _prefixIndex)
            bucket.Value.TryRemove(key, out _);
    }
}

/// <summary>
/// کش مرجع بی‌اثر — برای تست‌ها و میزبان‌هایی که کش نمی‌خواهند.
/// هر فراخوانی مستقیم به کارخانه می‌رود؛ باطل‌سازی هیچ کاری نمی‌کند.
/// </summary>
public sealed class NoopReferenceCache : IReferenceCache
{
    /// <inheritdoc />
    public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl, CancellationToken cancellationToken) where T : class
        => factory(cancellationToken);

    /// <inheritdoc />
    public void Evict(string key) { }

    /// <inheritdoc />
    public void EvictByPrefix(string prefix) { }
}
