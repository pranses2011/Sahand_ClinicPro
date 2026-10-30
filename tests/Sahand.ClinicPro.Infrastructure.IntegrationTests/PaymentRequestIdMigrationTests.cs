using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class PaymentRequestIdMigrationTests
{
    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options) { }

    [Fact]
    public async Task Migration_allows_legacy_null_keys_but_enforces_key_even_after_soft_delete()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options;
        await using var db = new TestDb(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Payments (Id INTEGER PRIMARY KEY, IsDeleted INTEGER NOT NULL)");
        var migration = new PaymentRequestId();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Payments (Id, IsDeleted) VALUES (1, 0), (2, 0)");
        var key = Guid.NewGuid().ToString();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Payments (Id, IsDeleted, RequestId) VALUES (3, 1, {key})");
        Func<Task> duplicate = () => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Payments (Id, IsDeleted, RequestId) VALUES (4, 0, {key})");
        await duplicate.Should().ThrowAsync<SqliteException>();
    }
}
