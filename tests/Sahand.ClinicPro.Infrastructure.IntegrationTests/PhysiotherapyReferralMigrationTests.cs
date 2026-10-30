using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sahand.ClinicPro.Infrastructure.Persistence.Migrations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class PhysiotherapyReferralMigrationTests
{
    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options) { }

    [Fact]
    public async Task Migration_creates_unique_request_key_without_creating_a_treatment_session()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options;
        await using var db = new TestDb(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Visits (Id INTEGER PRIMARY KEY)");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Patients (Id INTEGER PRIMARY KEY)");
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE Doctors (Id INTEGER PRIMARY KEY)");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Visits VALUES (1); INSERT INTO Patients VALUES (1); INSERT INTO Doctors VALUES (1);");
        var migration = new PhysiotherapyReferrals();
        var sql = db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations);
        foreach (var command in sql) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var key = Guid.NewGuid().ToString();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PhysiotherapyReferrals (Id, RequestId, VisitId, PatientId, DoctorId, Note, CreatedAt) VALUES (1, {key}, 1, 1, 1, 'ارجاع', '2026-10-31')");
        Func<Task> duplicate = () => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PhysiotherapyReferrals (Id, RequestId, VisitId, PatientId, DoctorId, Note, CreatedAt) VALUES (2, {key}, 1, 1, 1, 'ارجاع', '2026-10-31')");
        await duplicate.Should().ThrowAsync<SqliteException>();
    }
}
