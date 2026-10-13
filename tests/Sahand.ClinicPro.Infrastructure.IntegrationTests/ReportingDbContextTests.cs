using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// تست‌های گارد کانتکست فقط-خواندنی گزارش‌ها — چرخهٔ ۶۴ (A-01 گام اول).
/// گزاره‌ها: الف) هر مسیر SaveChanges استثنا می‌دهد؛ ب) مدل گزارش با مدل
/// اصلی هم‌شکل است (همان موجودیت‌ها) تا Set&lt;T&gt; گزارش‌ها همه جا در دسترس باشد.
/// </summary>
public class ReportingDbContextTests
{
    /// <summary>ساخت کانتکست گزارش روی InMemory — همان الگوی کارخانهٔ تست</summary>
    private static ReportingDbContext NewReporting()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase("ReportingGuardTests")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;
        return new ReportingDbContext(options);
    }

    /// <summary>ساخت کانتکست اصلی روی InMemory با همان نام فروشگاه</summary>
    private static ClinicProDbContext NewMain()
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase("ReportingGuardTests")
            .Options;
        return new ClinicProDbContext(options);
    }

    /// <summary>هر چهار مسیر ذخیره‌سازی باید بلافاصله استثنا دهند</summary>
    [Fact]
    public async Task All_SaveChanges_Paths_Throw()
    {
        await using var reporting = NewReporting();
        reporting.Add(new Domain.Entities.People.Branch { BranchName = "شعبه تست" });

        var sync = Record.Exception(() => reporting.SaveChanges());
        var syncFlag = Record.Exception(() => reporting.SaveChanges(acceptAllChangesOnSuccess: true));
        var asyncPlain = await Record.ExceptionAsync(() => reporting.SaveChangesAsync());
        var asyncFlag = await Record.ExceptionAsync(() =>
            reporting.SaveChangesAsync(acceptAllChangesOnSuccess: true, CancellationToken.None));

        sync.Should().BeOfType<InvalidOperationException>(
            because: "کانتکست گزارش فقط-خواندنی است — SaveChanges همیشه رد می‌شود");
        syncFlag.Should().BeOfType<InvalidOperationException>();
        asyncPlain.Should().BeOfType<InvalidOperationException>();
        asyncFlag.Should().BeOfType<InvalidOperationException>();
        sync!.Message.Should().Contain("فقط-خواندنی",
            because: "پیام فارسی در لاگ و خطای API بلافاصله قابل فهم است");
    }

    /// <summary>
    /// هم‌شکلی مدل گزارش با مدل اصلی — پیش‌شرط Set&lt;T&gt; در کنترلر؛ اگر
    /// موجودیتی فقط در مدل اصلی باشد، گزارش آن را نمی‌بیند و این تست قرمز می‌شود.
    /// </summary>
    [Fact]
    public void Reporting_Model_Covers_All_Main_Entities()
    {
        using var reporting = NewReporting();
        using var main = NewMain();

        var mainEntities = main.Model.GetEntityTypes()
            .Select(t => t.ClrType).ToHashSet();
        var reportingEntities = reporting.Model.GetEntityTypes()
            .Select(t => t.ClrType).ToHashSet();

        // هر موجودیتِ مدل اصلی باید در مدل گزارش باشد (فروشگاه مشترک — همان دیتابیس واقعی)
        var missing = mainEntities.Except(reportingEntities).ToList();
        missing.Should().BeEmpty(
            because: "گزارش‌ها از همان دیتابیس می‌خوانند؛ موجودیت گم‌شده یعنی Set<T> در کنترلر خراب می‌شود");
    }

    /// <summary>بدون AsNoTracking صریح هم پرس‌وجو دنبال نمی‌شود (NoTracking پیش‌فرض DI)</summary>
    [Fact]
    public void Default_Tracking_Behavior_Is_NoTracking_When_Configured()
    {
        // همان الگوی ثبت DI تولید؛ این تست مستقیماً گزینه را می‌سازد
        using var reporting = NewReporting();
        // InMemory + NoTracking — ردیابی از پیکربندی می‌آید؛ در DI تولید همان گزینه ثبت می‌شود
        reporting.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking,
            because: "هیچ پرس‌وجوی گزارشی نباید ChangeTracker را بار کند");
    }
}
