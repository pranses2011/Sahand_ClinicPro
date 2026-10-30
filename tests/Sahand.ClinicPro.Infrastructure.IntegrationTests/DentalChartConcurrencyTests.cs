using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>آزمون رابطه‌ای الگوی اتمیک ثبت طرح و تغییر نشانگر بیمار؛ نه جایگزین آزمون SQL Server.</summary>
public class DentalChartConcurrencyTests
{
    private sealed class Patient
    {
        public long Id { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] Version { get; set; } = Array.Empty<byte>();
    }

    private sealed class Plan
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Plan> Plans => Set<Plan>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // SQLite rowversion خودکار SQL Server ندارد؛ افزایش آن در آزمون شبیه‌سازی می‌شود.
            builder.Entity<Patient>().Property(x => x.Version).IsConcurrencyToken().ValueGeneratedNever();
        }
    }

    [Fact]
    public async Task Competing_plan_insert_rolls_back_when_patient_version_changes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options;
        await using (var seed = new TestDb(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Patients.Add(new Patient { Id = 1, Version = new byte[] { 1 } });
            await seed.SaveChangesAsync();
        }
        await using var first = new TestDb(options);
        await using var second = new TestDb(options);
        var firstPatient = await first.Patients.SingleAsync();
        var secondPatient = await second.Patients.SingleAsync();
        firstPatient.Version = new byte[] { 2 };
        secondPatient.Version = new byte[] { 3 };
        firstPatient.UpdatedAt = secondPatient.UpdatedAt = DateTime.UtcNow;
        first.Entry(firstPatient).Property(p => p.UpdatedAt).IsModified = true;
        second.Entry(secondPatient).Property(p => p.UpdatedAt).IsModified = true;
        first.Plans.Add(new Plan { PatientId = 1, Details = "طرح اول" });
        second.Plans.Add(new Plan { PatientId = 1, Details = "طرح دوم" });
        await first.SaveChangesAsync();
        await FluentActions.Invoking(() => second.SaveChangesAsync()).Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verify = new TestDb(options);
        var plans = await verify.Plans.ToListAsync();
        plans.Should().ContainSingle().Which.Details.Should().Be("طرح اول");
    }
}
