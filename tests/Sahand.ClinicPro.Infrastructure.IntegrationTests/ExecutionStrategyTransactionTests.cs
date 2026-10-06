using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// گارد رگرسیون چرخه چهاردهم (گزارش ششم مالک — خطای ۵۰۰ لاگین)
/// </summary>
/// <remarks>
/// ریشه: ثبت DbContext با استراتژی تلاش‌مجدد (SqlServerRetryingExecutionStrategy)
/// + تراکنش دستی <c>Database.BeginTransaction</c> — که TransactionBehavior روی همه
/// فرمان‌های MediatR اجرا می‌کند — ناسازگار است؛ EF Core نخستین عملیات داخل تراکنش
/// را با InvalidOperationException («...does not support user-initiated transactions...»)
/// می‌شکند و نتیجه‌اش ۵۰۰ «خطای غیرمنتظره در سرور رخ داد» روی هر endpoint نوشتنی
/// است. نخستین فرمان واقعی تاریخ پروژه روی SQL Server، لاگین مالک بود.
///
/// چرا تست‌های یکپارچگی API این را ندیدند؟ InMemory تراکنش را نادیده می‌گیرد و
/// اصلاً چک استراتژی را فعال نمی‌کند (با هارنس scripts/repro_login500 اثبات شد)؛
/// به همین دلیل این گارد باید روی provider رابطه‌ای واقعی (SQLite) اجرا شود.
/// </remarks>
public class ExecutionStrategyTransactionTests : IDisposable
{
    private readonly GuardDb _db = NewSqliteDb();

    /// <summary>کانتکست مینیمال SQLite روی فایل موقت — فقط برای مکانیزم چک تراکنش</summary>
    private static GuardDb NewSqliteDb()
    {
        var db = new GuardDb(Path.Combine(Path.GetTempPath(), $"guard-{Guid.NewGuid():N}.db"));
        db.Database.EnsureCreated();
        db.GuardRows.Add(new GuardRow { Title = "سلام" });
        db.SaveChanges();
        return db;
    }

    /// <summary>
    /// بازتولید مکانیزم: با استراتژی تلاش‌مجدد فعال، پرس‌وجوی داخل تراکنش دستی
    /// باید همان استثنای ۵۰۰ساز لاگین را بدهد
    /// </summary>
    [Fact]
    public async Task Retrying_Strategy_With_Manual_Transaction_Throws_On_Real_Provider()
    {
        var strategy = new GuardRetryingStrategy(_db);

        // عین جریان سرور: TransactionBehavior تراکنش را باز می‌کند، سپس گرداننده پرس‌وجو می‌زند
        await using var tx = await _db.Database.BeginTransactionAsync();

        var act = async () =>
        {
            _ = await strategy.ExecuteAsync(async () =>
                await _db.GuardRows.CountAsync());
        };

        // همان پیامی که در کنسول سرور مالک دیده می‌شد
        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("user-initiated transactions"),
                "EF Core ترکیب استراتژی تلاش‌مجدد با تراکنش دستی را مجاز نمی‌داند — " +
                "نخستین عملیات داخل تراکنش می‌شکند (خطای ۵۰۰ لاگین چرخه چهاردهم)");
    }

    /// <summary>
    /// وضعیت پس از رفع: بدون استراتژی تلاش‌مجدد، همان الگوی TransactionBehavior
    /// (بازکردن تراکنش ← عملیات ← تثبیت) باید بی‌نقص کار کند
    /// </summary>
    [Fact]
    public async Task Without_Retrying_Strategy_Manual_Transaction_Works()
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var count = await _db.GuardRows.CountAsync();
        await tx.CommitAsync();

        count.Should().Be(1, "یک ردیف بذر شد و تراکنش دستی بدون استراتژی تلاش‌مجدد سالم است");
    }

    /// <summary>
    /// گارد نقطهٔ ثبت: ثبت DbContext در DependencyInjection زیرساخت هرگز نباید
    /// استراتژی تلاش‌مجدد را فعال کند — چون معماری پروژه (TransactionBehavior)
    /// به تراکنش دستی وابسته است. الگوی اسکن سورس مطابق XamlResourceAuditTests.
    /// </summary>
    [Fact]
    public void Infrastructure_Registration_Must_Not_Enable_Retrying_Strategy()
    {
        // ۱) یافتن ریشه مخزن — بالا رفتن تا Directory.Build.props (الگوی تست ممیزی XAML)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Directory.Build.props")))
            dir = dir.Parent!;

        dir.Should().NotBeNull("ریشه مخزن یافت نشد — تست باید از درون مخزن اجرا شود");

        // ۲) فایل ثبت سرویس‌های زیرساخت — تنها نقطه ساخت DbContext برنامه
        var registration = Path.Combine(dir!.FullName,
            "src", "Sahand.ClinicPro.Infrastructure", "DependencyInjection.cs");
        File.Exists(registration).Should().BeTrue(
            $"فایل ثبت زیرساخت یافت نشد: {registration}");

        // ۳) فراخوانی کدی (با پرانتز) ممنوع — ذکر توضیحی در کامنت‌ها مجاز است
        File.ReadAllText(registration).Should().NotContain("EnableRetryOnFailure(",
            "ترکیب استراتژی تلاش‌مجدد با تراکنش دستیِ TransactionBehavior روی SQL Server " +
            "واقعی همه فرمان‌های MediatR را با InvalidOperationException می‌شکند " +
            "(خطای ۵۰۰ لاگین چرخه چهاردهم). اگر تاب‌آوری خطاهای گذرا لازم شد، " +
            "باید کل واحد کار در CreateExecutionStrategy().ExecuteAsync پیچیده شود.");
    }

    public void Dispose()
    {
        _db.Dispose();
        try { File.Delete(_db.Path); } catch { /* بی‌اهمیت — فایل موقت */ }
    }
}

// ─────────────────────── اجزای مینیمال تست ───────────────────────

/// <summary>ردیف نمونه برای کانتکست گارد</summary>
public class GuardRow
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }

    /// <summary>عنوان</summary>
    public string Title { get; set; } = "";
}

/// <summary>کانتکست مینیمال SQLite — مستقل از مدل ۸۷ موجودیتی برنامه</summary>
public class GuardDb : DbContext
{
    /// <summary>مسیر فایل موقت</summary>
    public string Path { get; }

    /// <summary>ساخته با مسیر فایل</summary>
    public GuardDb(string path) => Path = path;

    /// <summary>مجموعه ردیف‌ها</summary>
    public DbSet<GuardRow> GuardRows => Set<GuardRow>();

    /// <summary>پیکربندی SQLite</summary>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={Path}");
}

/// <summary>
/// استراتژی تلاش‌مجدد با RetriesOnFailure=true — همان قرارداد SqlServerRetryingExecutionStrategy؛
/// ShouldRetryOn هرگز true نمی‌دهد چون هدف فعال‌کردن مکانیزم چک تراکنش دستی است، نه تلاش واقعی.
/// </summary>
public class GuardRetryingStrategy(DbContext context)
    : ExecutionStrategy(context, maxRetryCount: 3, maxRetryDelay: TimeSpan.FromMilliseconds(10))
{
    /// <summary>سیاست تلاش مجدد — برای این گارد همیشه منفی</summary>
    protected override bool ShouldRetryOn(Exception exception) => false;
}
