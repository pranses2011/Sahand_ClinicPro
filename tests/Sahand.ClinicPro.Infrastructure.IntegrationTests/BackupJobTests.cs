using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Entities.Infrastructure;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Sahand.ClinicPro.Infrastructure.Services.Backup;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>
/// چرخه دوازدهم (A2): پشتیبان‌گیری خودکار شبانه — ثبت رکورد + پاک‌سازی
/// نسخه‌های فراتر از سهمیه نگهداشت (Backup:RetentionCount)
/// </summary>
public class BackupJobTests
{
    private static (BackupJob Job, ClinicProDbContext Ctx) MakeJob(int retention)
    {
        var options = new DbContextOptionsBuilder<ClinicProDbContext>()
            .UseInMemoryDatabase($"backup-test-{Guid.NewGuid():N}")
            .Options;
        var ctx = new ClinicProDbContext(options);

        var repoMock = new Mock<IRepository<BackupHistory>>();
        repoMock.Setup(r => r.QueryableTracking).Returns(ctx.Set<BackupHistory>());
        repoMock.Setup(r => r.QueryableNoTracking).Returns(ctx.Set<BackupHistory>().AsNoTracking());
        repoMock.Setup(r => r.AddAsync(It.IsAny<BackupHistory>(), It.IsAny<CancellationToken>()))
            .Returns<BackupHistory, CancellationToken>((e, _) =>
            {
                ctx.Set<BackupHistory>().Add(e);
                return Task.FromResult(e);
            });

        var uowMock = new Mock<IUnitOfWork>();
        uowMock.Setup(u => u.Repository<BackupHistory>()).Returns(repoMock.Object);
        uowMock.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async _ => await ctx.SaveChangesAsync(_) > 0);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Backup:RetentionCount"] = retention.ToString(),
                ["Backup:Directory"] = Path.GetTempPath(),
            })
            .Build();

        return (new BackupJob(uowMock.Object, config, NullLogger<BackupJob>.Instance), ctx);
    }

    private static BackupHistory Hist(DateTime started, bool automatic = true) => new()
    {
        BackupType = BackupType.Full,
        FilePath = null, // بدون فایل فیزیکی — حذف رکورد هم باید کار کند
        StartedAt = started,
        Status = BackupStatus.Success,
        IsAutomatic = automatic,
        StorageType = StorageType.Local,
    };

    [Fact]
    public async Task RunAsync_ShouldRegisterAutomaticBackupAndEnforceRetention()
    {
        var (job, ctx) = MakeJob(retention: 3);

        // ۱۰ نسخه خودکار قبلی + ۲ نسخه دستی (نباید پاک‌سازی شوند)
        var baseTime = DateTime.UtcNow.Date.AddDays(-30); // قطعاً در گذشته نسبت به امروز
        for (var i = 0; i < 10; i++)
            ctx.BackupHistory.Add(Hist(baseTime.AddDays(i)));
        ctx.BackupHistory.Add(Hist(baseTime.AddDays(20), automatic: false));
        ctx.BackupHistory.Add(Hist(baseTime.AddDays(21), automatic: false));
        await ctx.SaveChangesAsync();

        await job.RunAsync(CancellationToken.None);

        // پس از اجرا: ۱۰ قدیمی + ۱ تازه خودکار + ۲ دستی = کل رکوردها
        var all = ctx.BackupHistory.IgnoreQueryFilters().ToList();
        all.Should().HaveCount(13);

        // حذف‌نرم‌شده‌ها: ۱۰ + ۱ - ۳ نگه‌داشته = ۸ نسخه خودکار منقضی
        var softDeleted = all.Count(b => b.IsDeleted);
        softDeleted.Should().Be(8);

        // نسخه‌های زنده: ۳ خودکار (تازه‌ترین‌ها) + ۲ دستی
        var alive = all.Where(b => !b.IsDeleted).ToList();
        alive.Count(b => b.IsAutomatic).Should().Be(3);
        alive.Count(b => !b.IsAutomatic).Should().Be(2);

        // تازه‌ترین رکورد باید خودکارِ همین امشب باشد
        var newest = alive.Where(b => b.IsAutomatic).OrderByDescending(b => b.StartedAt).First();
        newest.IsAutomatic.Should().BeTrue();
        newest.StartedAt!.Value.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithNoOldBackups_ShouldRemoveNothing()
    {
        var (job, ctx) = MakeJob(retention: 14);

        // فقط ۲ نسخه — زیر سهمیه
        ctx.BackupHistory.Add(Hist(DateTime.UtcNow.AddDays(-1)));
        ctx.BackupHistory.Add(Hist(DateTime.UtcNow.AddDays(-2)));
        await ctx.SaveChangesAsync();

        var removed = await job.CleanupExpiredAsync(CancellationToken.None);

        removed.Should().Be(0);
        ctx.BackupHistory.Count(b => !b.IsDeleted).Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WithSingleExistingBackup_ShouldKeepBoth()
    {
        var (job, ctx) = MakeJob(retention: 14);

        ctx.BackupHistory.Add(Hist(DateTime.UtcNow.AddDays(-1)));
        await ctx.SaveChangesAsync();

        await job.RunAsync(CancellationToken.None);

        ctx.BackupHistory.Count(b => !b.IsDeleted).Should().Be(2);
        ctx.BackupHistory.IgnoreQueryFilters().Count(b => b.IsDeleted).Should().Be(0);
    }
}
