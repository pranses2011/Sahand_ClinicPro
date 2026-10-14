using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Persistence.Seeders;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// گارد رگرسیون چرخهٔ ۷۲ (گزارش مالک) — «بازه‌های مرجع و حدود بحرانی هر
/// آزمایش رو بطور کامل و دقیق تکمیل کن. میخوام هنگام نصب داخل برنامه باشن
/// و در صورت نیاز بتونم اصلاحشون بکنم».
///
/// سناریوهای تحت پوشش:
///  ۱) نصب تازه — بذرِ کاتالوگ، بازه‌های ساخت‌یافتهٔ عددی را هم می‌نشاند
///     (نه فقط متن‌های نمایشی قدیمی) — همان «هنگام نصب داخل برنامه».
///  ۲) دیتابیس موجود بدون بازه (نسخهٔ قبل) — اجرای مجدد بذر، بازه‌ها را
///     بک‌فیل می‌کند؛ معادل نصب بستهٔ آپدیت + ری‌استارت API.
///  ۳) گارد مالکیت — تستی که مالک ویرایش کرده (حتی یک فیلد) هرگز بازنویسی
///     نمی‌شود؛ بذر فقط ردیف‌های «هر ۹ ستون تهی» را می‌نویسد.
///  ۴) آزمایش کیفی (کشت خون) بدون بازهٔ عددی می‌ماند — تفسیر اشتباه نمی‌کند.
/// </summary>
public class LabReferenceRangeBackfillTests
{
    /// <summary>ساخت کانتکست روی دیتابیس درون‌حافظه‌ای ایزوله</summary>
    private static ClinicProDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"LabRangesDb_{Guid.NewGuid():N}")
            .Options;
        var context = new ClinicProDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// نصب تازه — کاتالوگ بذرفشان‌شده باید بازه‌های ساخت‌یافتهٔ نمونهٔ
    /// هموگلوبین را داشته باشد: مرد ۱۳٫۵-۱۷٫۵ / زن ۱۲-۱۵٫۵ / کودک ۱۱-۱۴
    /// (تا ۱۲ سالگی) و حدود بحرانی ۷ و ۲۰ — موتور تفسیر (LabResultInterpreter)
    /// از همین ستون‌ها تغذیه می‌شود.
    /// </summary>
    [Fact]
    public async Task Fresh_install_seeds_structured_ranges_on_hemoglobin()
    {
        using var context = NewContext();
        var seeder = new ReferenceDataSeeder(context);

        await seeder.SeedAsync();

        var hgb = await context.LabTests.AsNoTracking()
            .SingleAsync(t => t.TestCode == "HGB");

        hgb.RefMinAdultMale.Should().Be(13.5m, "کف بازهٔ مردان هموگلوبین");
        hgb.RefMaxAdultMale.Should().Be(17.5m, "سقف بازهٔ مردان هموگلوبین");
        hgb.RefMinAdultFemale.Should().Be(12m, "کف بازهٔ زنان هموگلوبین");
        hgb.RefMaxAdultFemale.Should().Be(15.5m, "سقف بازهٔ زنان هموگلوبین");
        hgb.RefMinChild.Should().Be(11m, "کف بازهٔ کودک هموگلوبین");
        hgb.RefMaxChild.Should().Be(14m, "سقف بازهٔ کودک هموگلوبین");
        hgb.ChildAgeMaxYears.Should().Be(12, "سقف سن کودک هموگلوبین");
        hgb.CriticalLow.Should().Be(7m, "حد بحرانی پایین هموگلوبین");
        hgb.CriticalHigh.Should().Be(20m, "حد بحرانی بالای هموگلوبین");
    }

    /// <summary>
    /// دیتابیس موجودِ نسخهٔ قبل — ردیف‌های کاتالوگ بدون بازه (فقط متن‌های
    /// نمایشی قدیمی) با اجرای مجدد بذر، بازهٔ ساخت‌یافته می‌گیرند؛ این همان
    /// مسیر «نصب آپدیت + ری‌استارت سرور» مالک است که Seeder در هر
    /// راه‌اندازی اجرا می‌شود.
    /// </summary>
    [Fact]
    public async Task Existing_database_without_ranges_gets_backfilled_on_reseed()
    {
        using var context = NewContext();

        // شبیه‌سازی ردیفِ نسخهٔ قبل — متن نمایشی هست، ستون‌های عددی خالی
        context.LabTests.Add(new Domain.Entities.Paraclinical.LabTest
        {
            TestCode = "K",
            TestName = "پتاسیم",
            NormalRangeMale = "3.5-5.1",
            NormalRangeFemale = "3.5-5.1",
            Unit = "mEq/L",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var seeder = new ReferenceDataSeeder(context);
        await seeder.SeedAsync();

        var potassium = await context.LabTests.AsNoTracking()
            .SingleAsync(t => t.TestCode == "K");

        potassium.RefMinAdultMale.Should().Be(3.5m, "کف پتاسیم باید بک‌فیل شود");
        potassium.RefMaxAdultMale.Should().Be(5.1m, "سقف پتاسیم باید بک‌فیل شود");
        potassium.CriticalLow.Should().Be(2.5m, "حد بحرانی پایین پتاسیم");
        potassium.CriticalHigh.Should().Be(6.2m, "حد بحرانی بالای پتاسیم");
    }

    /// <summary>
    /// گارد مالکیت — ویرایش مالک محفوظ می‌ماند: ردیفی که حتی یک ستون
    /// ساخت‌یافتهٔ پرشده دارد (مثلاً مالک حد بحرانی را عوض کرده) در
    /// اجراهای بعدی بذر «دست‌نخورده» باقی می‌ماند — بذر هرگز ویرایش
    /// مالک را بازنویسی نمی‌کند.
    /// </summary>
    [Fact]
    public async Task Owner_edited_test_is_never_overwritten_by_reseed()
    {
        using var context = NewContext();

        // مالک حد بحرانی بالای پتاسیم را ۷ گذاشته و یک کف مردان هم
        context.LabTests.Add(new Domain.Entities.Paraclinical.LabTest
        {
            TestCode = "K",
            TestName = "پتاسیم",
            RefMinAdultMale = 3.6m,   // ویرایش مالک
            CriticalHigh = 7m,        // ویرایش مالک
            IsActive = true
        });
        await context.SaveChangesAsync();

        var seeder = new ReferenceDataSeeder(context);
        await seeder.SeedAsync();

        var potassium = await context.LabTests.AsNoTracking()
            .SingleAsync(t => t.TestCode == "K");

        potassium.RefMinAdultMale.Should().Be(3.6m, "ویرایش مالک نباید بازنویسی شود");
        potassium.CriticalHigh.Should().Be(7m, "ویرایش مالک نباید بازنویسی شود");
        // بقیهٔ ستون‌های همان ردیف هم دست نمی‌خورند (رفتار یکپارچه: کل ردیف مالک است)
        potassium.RefMaxAdultMale.Should().BeNull("ردیف ویرایش‌شده کلاً دست‌نخورده می‌ماند");
    }

    /// <summary>
    /// آزمایش کیفی (کشت خون) بدون بازهٔ عددی می‌ماند — بذرِ بازه‌ها فقط
    /// آزمایش‌های کمّی را پوشش می‌دهد تا موتور تفسیر نتیجهٔ «منفی/مثبت»
    /// را به‌اشتباه فلگ عددی نزند.
    /// </summary>
    [Fact]
    public async Task Qualitative_test_stays_without_numeric_range()
    {
        using var context = NewContext();
        var seeder = new ReferenceDataSeeder(context);

        await seeder.SeedAsync();

        var bloodCulture = await context.LabTests.AsNoTracking()
            .SingleAsync(t => t.TestCode == "B/C");

        bloodCulture.RefMinAdultMale.Should().BeNull("کشت خون آزمایش کیفی است");
        bloodCulture.RefMaxAdultMale.Should().BeNull();
        bloodCulture.CriticalLow.Should().BeNull();
        bloodCulture.CriticalHigh.Should().BeNull();
    }
}
