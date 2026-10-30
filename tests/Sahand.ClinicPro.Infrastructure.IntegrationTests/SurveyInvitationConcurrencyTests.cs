using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Infrastructure.Persistence.Configurations;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

/// <summary>بررسی تراکنش و کنترل همزمانی دعوت با پایگاه‌داده رابطه‌ای.</summary>
public class SurveyInvitationConcurrencyTests
{
    private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
    {
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<SurveyInvitation> Invitations => Set<SurveyInvitation>();
        public DbSet<SurveyResponse> Responses => Set<SurveyResponse>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new SurveyInvitationConfiguration());
            modelBuilder.Entity<Survey>().Ignore(x => x.DomainEvents);
            modelBuilder.Entity<SurveyInvitation>().Ignore(x => x.DomainEvents);
            modelBuilder.Entity<SurveyResponse>().Ignore(x => x.DomainEvents);
        }
    }

    [Fact]
    public async Task Competing_consumption_rolls_back_second_response()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TestDb>().UseSqlite(connection).Options;
        long invitationId;
        await using (var seed = new TestDb(options))
        {
            await seed.Database.EnsureCreatedAsync();
            var survey = new Survey { Title = "پرسش", IsActive = true };
            seed.Surveys.Add(survey);
            await seed.SaveChangesAsync();
            var invitation = new SurveyInvitation { SurveyId = survey.Id, TokenHash = new string('A', 64), ExpiresAt = DateTime.UtcNow.AddDays(1) };
            seed.Invitations.Add(invitation);
            await seed.SaveChangesAsync();
            invitationId = invitation.Id;
        }
        await using var first = new TestDb(options);
        await using var second = new TestDb(options);
        var a = await first.Invitations.SingleAsync(x => x.Id == invitationId);
        var b = await second.Invitations.SingleAsync(x => x.Id == invitationId);
        a.UsedAt = DateTime.UtcNow;
        b.UsedAt = DateTime.UtcNow;
        first.Responses.Add(new SurveyResponse { SurveyId = a.SurveyId, Comments = "اول" });
        second.Responses.Add(new SurveyResponse { SurveyId = b.SurveyId, Comments = "دوم" });
        await first.SaveChangesAsync();
        var saveSecond = async () => await second.SaveChangesAsync();
        await saveSecond.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verify = new TestDb(options);
        var responses = await verify.Responses.AsNoTracking().ToListAsync();
        responses.Should().ContainSingle().Which.Comments.Should().Be("اول");
        (await verify.Invitations.SingleAsync()).UsedAt.Should().NotBeNull();
    }
}
