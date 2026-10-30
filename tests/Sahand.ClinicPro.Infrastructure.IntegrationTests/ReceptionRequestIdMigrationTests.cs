using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class ReceptionRequestIdMigrationTests
{
    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options) { }

    [Fact]
    public async Task Migration_keeps_legacy_nulls_and_prevents_reuse_after_soft_delete()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new TestDb(new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Receptions (Id INTEGER PRIMARY KEY, IsDeleted INTEGER NOT NULL)");
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(new ReceptionRequestId().UpOperations))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Receptions (Id, IsDeleted) VALUES (1, 0), (2, 0)");
        var key = Guid.NewGuid().ToString();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Receptions (Id, IsDeleted, RequestId) VALUES (3, 1, {key})");
        Func<Task> duplicate = () => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Receptions (Id, IsDeleted, RequestId) VALUES (4, 0, {key})");
        await duplicate.Should().ThrowAsync<SqliteException>();
    }
}
