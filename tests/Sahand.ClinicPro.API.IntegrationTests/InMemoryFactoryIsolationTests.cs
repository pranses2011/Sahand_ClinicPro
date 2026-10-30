using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class InMemoryFactoryIsolationTests
{
    [Fact]
    public async Task Factory_stores_are_isolated_while_reporting_reads_the_same_store()
    {
        await using var first = new InMemoryApiFactory();
        await using var second = new InMemoryApiFactory();
        var marker = $"factory-isolation-{Guid.NewGuid():N}";

        using (var scope = first.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            db.Surveys.Add(new Survey { Title = marker, Questions = "[]", IsActive = true });
            await db.SaveChangesAsync();
        }
        using (var scope = first.Services.CreateScope())
        {
            var reporting = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
            (await reporting.Set<Survey>().AnyAsync(x => x.Title == marker)).Should().BeTrue();
        }
        using (var scope = second.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            (await db.Surveys.AnyAsync(x => x.Title == marker)).Should().BeFalse();
        }
    }
}
