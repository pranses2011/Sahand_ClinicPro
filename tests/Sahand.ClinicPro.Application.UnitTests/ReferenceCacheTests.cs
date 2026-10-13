using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Sahand.ClinicPro.Application.Common.Caching;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های واحد کش مرجع L1 — چرخهٔ ۶۰ نقشهٔ راه (P-06).
/// پوشش: بازگشت مقدار کش‌شده، اجرای یک‌بار کارخانه (ضد stampede)،
/// باطل‌سازی تک‌کلید و پیشوندی، و رفتار TTL.
/// </summary>
public class ReferenceCacheTests
{
    /// <summary>ساخت کش واقعی حافظه‌ای — همان پیاده‌شی که در تولید ثبت می‌شود</summary>
    private static MemoryReferenceCache NewCache() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    /// <summary>
    /// فراخوانی دوم باید از کش برگردد — کارخانه فقط یک‌بار اجرا می‌شود
    /// (مهم‌ترین ادعای چرخهٔ ۶۰: کاهش پرس‌وجوی دادهٔ مرجع).
    /// </summary>
    [Fact]
    public async Task Second_Call_Returns_Cached_Value_Without_Factory()
    {
        var cache = NewCache();
        var calls = 0;

        var first = await cache.GetOrSetAsync("ref:test:one",
            async _ => { calls++; return await Task.FromResult(new List<int> { 1, 2 }); },
            TimeSpan.FromMinutes(1), CancellationToken.None);
        var second = await cache.GetOrSetAsync("ref:test:one",
            async _ => { calls++; return await Task.FromResult(new List<int> { 9 }); },
            TimeSpan.FromMinutes(1), CancellationToken.None);

        calls.Should().Be(1, because: "کارخانه فقط در نخستین فراخوانی اجرا می‌شود");
        second.Should().BeSameAs(first, because: "نمونهٔ کش‌شده همان مرجع قبلی است");
        second.Should().ContainInOrder(new List<int> { 1, 2 }, because: "مقدار از کش است نه کارخانهٔ دوم");
    }

    /// <summary>
    /// دو فراخوانی موازی برای کلید سرد — کارخانه باید فقط یک‌بار اجرا شود
    /// (قفل دانه‌ای per-key + دوبار-بررسی؛ جلوگیری از «انفجار کش» در بارگذاری اولیه).
    /// سناریو: نخست فراخوانی قفل را می‌گیرد و کارخانهٔ کند (۱۰۰ms) را اجرا می‌کند؛
    /// دوم در صف قفل می‌ماند؛ پس از نوشته‌شدن کش، دوم با دوبار-بررسی از کش
    /// برمی‌گردد و کارخانه هرگز دوبار اجرا نمی‌شود.
    /// </summary>
    [Fact]
    public async Task Parallel_Cold_Calls_Execute_Factory_Once()
    {
        var cache = NewCache();
        var calls = 0;

        var task1 = cache.GetOrSetAsync("ref:test:stampede",
            async _ =>
            {
                Interlocked.Increment(ref calls);
                await Task.Delay(100); // کارخانهٔ کند — پنجرهٔ ورود فراخوانی دوم به صف
                return await Task.FromResult("value");
            },
            TimeSpan.FromMinutes(1), CancellationToken.None);
        var task2 = cache.GetOrSetAsync("ref:test:stampede",
            async _ =>
            {
                Interlocked.Increment(ref calls);
                return await Task.FromResult("should-never-be-built");
            },
            TimeSpan.FromMinutes(1), CancellationToken.None);

        await Task.WhenAll(task1, task2);

        calls.Should().Be(1, because: "فراخوانی دوم با دوبار-بررسی از کش برمی‌گردد نه کارخانه");
        task1.Result.Should().BeSameAs(task2.Result, because: "هر دو فراخوانی یک مقدار کش‌شده می‌گیرند");
    }

    /// <summary>باطل‌سازی تک‌کلید — فراخوانی بعدی دوباره کارخانه را اجرا می‌کند</summary>
    [Fact]
    public async Task Evict_Forces_Next_Call_To_Rebuild()
    {
        var cache = NewCache();
        var calls = 0;

        await cache.GetOrSetAsync("ref:test:evict",
            async _ => { calls++; return await Task.FromResult("v1"); },
            TimeSpan.FromMinutes(5), CancellationToken.None);
        cache.Evict("ref:test:evict");
        var after = await cache.GetOrSetAsync("ref:test:evict",
            async _ => { calls++; return await Task.FromResult("v2"); },
            TimeSpan.FromMinutes(5), CancellationToken.None);

        calls.Should().Be(2, because: "پس از باطل‌سازی دوباره ساخته می‌شود");
        after.Should().Be("v2", because: "مقدار تازهٔ کارخانهٔ دوم برمی‌گردد");
    }

    /// <summary>
    /// باطل‌سازی پیشوندی «ref:settings:» — هر دو کلید دستهٔ تنظیمات و «همه»
    /// باید باطل شوند؛ این همان مسیر ذخیرهٔ تنظیم است (SaveSettingCommand).
    /// </summary>
    [Fact]
    public async Task EvictByPrefix_Invalidates_All_Category_Keys()
    {
        var cache = NewCache();
        var calls = 0;

        string Factory(string v) => v;
        await cache.GetOrSetAsync(ReferenceCacheKeys.Settings(null),
            async _ => { calls++; return await Task.FromResult(Factory("all")); },
            TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.GetOrSetAsync(ReferenceCacheKeys.Settings("Printing"),
            async _ => { calls++; return await Task.FromResult(Factory("printing")); },
            TimeSpan.FromMinutes(5), CancellationToken.None);

        cache.EvictByPrefix(ReferenceCacheKeys.SettingsPrefix);

        await cache.GetOrSetAsync(ReferenceCacheKeys.Settings(null),
            async _ => { calls++; return await Task.FromResult(Factory("all2")); },
            TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.GetOrSetAsync(ReferenceCacheKeys.Settings("Printing"),
            async _ => { calls++; return await Task.FromResult(Factory("printing2")); },
            TimeSpan.FromMinutes(5), CancellationToken.None);

        calls.Should().Be(4, because: "پس از باطل‌سازی پیشوندی هر دو کلید دوباره ساخته شدند");
    }

    /// <summary>انقضای TTL — پس از عمر مقاله، فراخوانی بعدی دوباره می‌سازد</summary>
    [Fact]
    public async Task Expired_Entry_Rebuilds()
    {
        var cache = NewCache();
        var calls = 0;

        await cache.GetOrSetAsync("ref:test:ttl",
            async _ => { calls++; return await Task.FromResult("a"); },
            TimeSpan.FromMilliseconds(50), CancellationToken.None);
        await Task.Delay(120); // انقضا

        var value = await cache.GetOrSetAsync("ref:test:ttl",
            async _ => { calls++; return await Task.FromResult("b"); },
            TimeSpan.FromMilliseconds(50), CancellationToken.None);

        calls.Should().Be(2, because: "مقالهٔ منقضی دوباره ساخته می‌شود");
        value.Should().Be("b");
    }

    /// <summary>
    /// کش بی‌اثر (تست‌ها/میزبان‌های بدون کش) — هر فراخوانی مستقیم به کارخانه
    /// می‌رود؛ قرارداد رفتاری را حفظ می‌کند تا جایگزینی بی‌دردسر باشد.
    /// </summary>
    [Fact]
    public async Task Noop_Cache_Always_Calls_Factory()
    {
        IReferenceCache cache = new NoopReferenceCache();
        var calls = 0;

        await cache.GetOrSetAsync("ref:test:noop",
            async _ => { calls++; return await Task.FromResult("x"); },
            TimeSpan.FromMinutes(5), CancellationToken.None);
        await cache.GetOrSetAsync("ref:test:noop",
            async _ => { calls++; return await Task.FromResult("x"); },
            TimeSpan.FromMinutes(5), CancellationToken.None);

        calls.Should().Be(2, because: "کش بی‌اثر هرگز مقدار نگه نمی‌دارد");
        cache.Evict("ref:test:noop");   // نباید بیندازد
        cache.EvictByPrefix("ref:");     // نباید بیندازد
    }

    /// <summary>کلیدهای مرجع استاندارد — پیشوندها باید با الگوی مستند هم‌خوان باشند</summary>
    [Fact]
    public void Standard_Keys_Follow_Prefix_Convention()
    {
        ReferenceCacheKeys.InsuranceCompanies.Should().StartWith(ReferenceCacheKeys.Prefix);
        ReferenceCacheKeys.LabTests.Should().StartWith(ReferenceCacheKeys.Prefix);
        ReferenceCacheKeys.Medications.Should().StartWith(ReferenceCacheKeys.Prefix);
        ReferenceCacheKeys.Diagnoses.Should().StartWith(ReferenceCacheKeys.Prefix);
        ReferenceCacheKeys.Settings(null).Should().StartWith(ReferenceCacheKeys.SettingsPrefix);
        ReferenceCacheKeys.Settings("Printing").Should().StartWith(ReferenceCacheKeys.SettingsPrefix);
    }
}
