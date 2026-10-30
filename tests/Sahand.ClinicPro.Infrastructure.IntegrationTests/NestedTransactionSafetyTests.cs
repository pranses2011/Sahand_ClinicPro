using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>رفتار تراکنش مشترک روی پایگاه رابطه‌ای، بدون وابستگی به مدل سایر جداول.</summary>
public class NestedTransactionSafetyTests
{
    private static async Task<(ClinicProDbContext Db, SqliteConnection Connection, TransactionManager Manager)> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ClinicProDbContext>().UseSqlite(connection).Options;
        var db = new ClinicProDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE TransactionProbe (Id INTEGER NOT NULL)");
        return (db, connection, new TransactionManager(db, NullLogger<TransactionManager>.Instance));
    }

    private static async Task<long> CountAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TransactionProbe";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Inner_success_does_not_commit_before_outer_rollback()
    {
        var (db, connection, manager) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (1)");
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (2)");
            await manager.CommitAsync();
            await manager.RollbackAsync();
            (await CountAsync(connection)).Should().Be(0);
        }
    }

    [Fact]
    public async Task Inner_and_outer_success_commit_together()
    {
        var (db, connection, manager) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (1)");
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (2)");
            await manager.CommitAsync();
            await manager.CommitAsync();
            (await CountAsync(connection)).Should().Be(2);
        }
    }

    [Fact]
    public async Task Caught_inner_failure_marks_outer_transaction_for_full_rollback()
    {
        var (db, connection, manager) = await CreateAsync();
        await using (connection)
        await using (db)
        {
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (1)");
            await manager.BeginAsync();
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (2)");
            await manager.RollbackAsync();
            // حتی اگر گرداننده بیرونی خطا را گرفته و نوشتن دیگری انجام دهد،
            // هیچ‌یک از عملیات نباید پس از commit ظاهری باقی بمانند.
            await db.Database.ExecuteSqlRawAsync("INSERT INTO TransactionProbe VALUES (3)");
            var commit = () => manager.CommitAsync();
            await commit.Should().ThrowAsync<InvalidOperationException>();
            (await CountAsync(connection)).Should().Be(0);
        }
    }
}
