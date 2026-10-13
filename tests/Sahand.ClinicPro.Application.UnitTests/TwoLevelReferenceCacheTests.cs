using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Sahand.ClinicPro.Application.Common.Caching;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

/// <summary>
/// تست‌های واحد کش دومرحله‌ای — چرخهٔ ۶۱ (P-05).
/// پوشش: فروکش به L1 خالص بدون L2، سهم L2 در پرکردن L1، باطل‌سازی
/// بین‌نمونه‌ای با شمارندهٔ نسل، و ناسازگاری JSON.
/// </summary>
public class TwoLevelReferenceCacheTests
{
    /// <summary>کش L1 تازه — هر تست ایزوله</summary>
    private static IMemoryCache NewL1() => new MemoryCache(new MemoryCacheOptions());

    /// <summary>L2 درون‌حافظه‌ای تازه — شبیه‌ساز توزیع‌شدهٔ مشترک بین «نمونه‌ها»</summary>
    private static IDistributedCache NewL2() => new MemoryDistributedCache(
        Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));

    /// <summary>
    /// بدون L2 — رفتار باید با L1 خالص هم‌ارز باشد (استقرار تک‌نمونه‌ای
    /// پیش‌فرض؛ کش لایهٔ دوم اختیاری است و نباید چیزی را خراب کند).
    /// </summary>
    [Fact]
    public async Task Without_L2_Behaves_Like_Pure_L1()
    {
        var cache = new TwoLevelReferenceCache(NewL1(), TimeSpan.FromMinutes(2));
        var calls = 0;

        var v1 = await cache.GetOrSetAsync(ReferenceCacheKeys.InsuranceCompanies,
            async _ => { calls++; return await Task.FromResult(new List<string> { "a" }); },
            TimeSpan.FromMinutes(2), CancellationToken.None);
        var v2 = await cache.GetOrSetAsync(ReferenceCacheKeys.InsuranceCompanies,
            async _ => { calls++; return await Task.FromResult(new List<string> { "b" }); },
            TimeSpan.FromMinutes(2), CancellationToken.None);

        calls.Should().Be(1, because: "بدون L2 همان رفتار L1 چرخهٔ ۶۰ است");
        v2.Should().BeSameAs(v1);
    }

    /// <summary>
    /// پرشدن L1 از L2 — «نمونهٔ دوم» (L1 تازه، L2 مشترک) نباید کارخانه
    /// (دیتابیس) را صدا بزند؛ مقدار از L2 می‌آید و در L1 خودش می‌نشیند.
    /// </summary>
    [Fact]
    public async Task Second_Instance_Fills_From_L2_Without_Factory()
    {
        var l2 = NewL2();
        var instance1 = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var instance2 = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var dbCalls = 0;

        // نمونهٔ اول — دیتابیس
        await instance1.GetOrSetAsync(ReferenceCacheKeys.Medications,
            async _ => { dbCalls++; return await Task.FromResult(new List<string> { "استامینوفن" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);

        // نمونهٔ دوم — باید از L2 بگیرد نه دیتابیس
        var fromL2 = await instance2.GetOrSetAsync(ReferenceCacheKeys.Medications,
            async _ => { dbCalls++; return await Task.FromResult(new List<string> { "نباید ساخته شود" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);

        dbCalls.Should().Be(1, because: "نمونهٔ دوم از L2 خواند و کارخانه اجرا نشد");
        fromL2.Should().ContainSingle(s => s == "استامینوفن",
            because: "مقدار از سریال JSON لایهٔ دوم بازگشایی شد");
    }

    /// <summary>
    /// باطل‌سازی پیشوندی بین نمونه‌ها — نمونهٔ اول پیشوند تنظیمات را باطل
    /// می‌کند؛ نمونهٔ دوم نباید مقالهٔ کهنهٔ L1 خود را ببیند و باید دوباره
    /// از کارخانه/دیتابیس بسازد (رویداد باطل‌سازی از راه شمارندهٔ نسل).
    /// </summary>
    [Fact]
    public async Task Prefix_Invalidation_Propagates_To_Other_Instance()
    {
        var l2 = NewL2();
        var instance1 = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var instance2 = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var dbCalls = 0;

        // هر دو نمونه مقدار را دارند: نمونهٔ اول از دیتابیس، نمونهٔ دوم از L2
        await instance1.GetOrSetAsync(ReferenceCacheKeys.Settings(null),
            async _ => { dbCalls++; return await Task.FromResult(new List<string> { "v1" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);
        await instance2.GetOrSetAsync(ReferenceCacheKeys.Settings(null),
            async _ => { dbCalls++; return await Task.FromResult(new List<string> { "v1" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);
        dbCalls.Should().Be(1, because: "نمونهٔ دوم از L2 خواند نه دیتابیس");

        // نمونهٔ اول ذخیرهٔ تنظیم را باطل می‌کند (مثل SaveSettingCommand)
        instance1.EvictByPrefix(ReferenceCacheKeys.SettingsPrefix);

        // نمونهٔ دوم نباید مقالهٔ کهتهٔ L1 خودش را ببیند — نسل کهنه است
        var after = await instance2.GetOrSetAsync(ReferenceCacheKeys.Settings(null),
            async _ => { dbCalls++; return await Task.FromResult(new List<string> { "v2" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);

        dbCalls.Should().Be(2, because: "مقالهٔ کهنهٔ نمونهٔ دوم با نسل باطل شد و دوباره ساخته شد");
        after.Should().ContainSingle(s => s == "v2", because: "مقدار تازه پس از باطل‌سازی");
    }

    /// <summary>
    /// Evict تک‌کلید — باید هر دو لایه را پاک کند؛ فراخوانی بعدی از کارخانه.
    /// </summary>
    [Fact]
    public async Task Single_Evict_Clears_Both_Levels()
    {
        var l2 = NewL2();
        var cache = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var calls = 0;

        await cache.GetOrSetAsync(ReferenceCacheKeys.LabTests,
            async _ => { calls++; return await Task.FromResult(new List<string> { "CBC" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);
        cache.Evict(ReferenceCacheKeys.LabTests);
        var after = await cache.GetOrSetAsync(ReferenceCacheKeys.LabTests,
            async _ => { calls++; return await Task.FromResult(new List<string> { "FBS" }); },
            TimeSpan.FromSeconds(30), CancellationToken.None);

        calls.Should().Be(2, because: "هر دو لایه پاک شدند و دوباره ساخته شد");
        after.Should().ContainSingle(s => s == "FBS");
    }

    /// <summary>
    /// مقالهٔ خراب L2 (JSON نامعتبر) — باید مثل نبودن رفتار کند؛ کارخانه
    /// اجرا می‌شود و جریان اصلی نمی‌شکند (قاعدهٔ کش: هرگز عامل خطا نباشد).
    /// </summary>
    [Fact]
    public async Task Corrupted_L2_Entry_Falls_Back_To_Factory()
    {
        var l2 = NewL2();
        // نوشتن مستقیم مقدار خراب در L2 — کلید همان قالب Prefixed است
        l2.SetString(DistributedCacheKeys.Prefix + ReferenceCacheKeys.Diagnoses,
            "{{corrupted json", new DistributedCacheEntryOptions());

        var cache = new TwoLevelReferenceCache(NewL1(), l2, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
        var value = await cache.GetOrSetAsync(ReferenceCacheKeys.Diagnoses,
            async _ => await Task.FromResult(new List<string> { "J06.9" }),
            TimeSpan.FromSeconds(30), CancellationToken.None);

        value.Should().ContainSingle(s => s == "J06.9",
            because: "مقالهٔ خراب نادیده گرفته شد و کارخانه مقدار درست ساخت");
    }
}
