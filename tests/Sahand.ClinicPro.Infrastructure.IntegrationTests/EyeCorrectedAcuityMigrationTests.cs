using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>آزمون مهاجرت ستون‌ها روی SQLite؛ جایگزین اجرای واقعی SQL Server نیست.</summary>
public class EyeCorrectedAcuityMigrationTests
{
    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options) { }

    [Fact]
    public async Task Migration_keeps_existing_acuity_and_leaves_new_values_unknown()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options;
        await using var db = new TestDb(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE EyeExamRecords (Id INTEGER PRIMARY KEY, RightVisualAcuity TEXT NULL, LeftVisualAcuity TEXT NULL)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO EyeExamRecords (Id, RightVisualAcuity, LeftVisualAcuity) VALUES (1, '20/40', '20/50')");
        var migration = new EyeCorrectedVisualAcuity();
        var sql = db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations);
        foreach (var command in sql) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        using var query = connection.CreateCommand();
        query.CommandText = "SELECT RightVisualAcuity, LeftVisualAcuity, RightCorrectedVisualAcuity, LeftCorrectedVisualAcuity FROM EyeExamRecords WHERE Id = 1";
        await using var reader = await query.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetString(0).Should().Be("20/40");
        reader.GetString(1).Should().Be("20/50");
        reader.IsDBNull(2).Should().BeTrue();
        reader.IsDBNull(3).Should().BeTrue();
    }
}
