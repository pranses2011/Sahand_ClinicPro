using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.Infrastructure.IntegrationTests;

public class UnitOfWorkDisposalTests
{
    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ClinicProDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IMediator>(_ => Mock.Of<IMediator>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Synchronous_scope_can_dispose_resolved_unit_of_work()
    {
        using var provider = CreateProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        scope.Dispose();
        Assert.Throws<ObjectDisposedException>(() => context.SaveChanges());
    }

    [Fact]
    public async Task Asynchronous_scope_can_dispose_resolved_unit_of_work()
    {
        await using var provider = CreateProvider();
        var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        await scope.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => context.SaveChangesAsync());
    }
}
