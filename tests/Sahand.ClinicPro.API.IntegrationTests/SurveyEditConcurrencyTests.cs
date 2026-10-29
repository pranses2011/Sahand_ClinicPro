using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class SurveyEditConcurrencyTests
{
    [Fact]
    public async Task Database_concurrency_conflict_returns_409_without_persisting_mutation()
    {
        using var factory = new InMemoryApiFactory();
        long id;
        using (var seed = factory.Services.CreateScope())
        {
            var db = seed.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var survey = new Survey { Title = "عنوان اصلی", Questions = "[]", IsActive = true };
            db.Surveys.Add(survey);
            await db.SaveChangesAsync();
            id = survey.Id;
        }
        using (var scope = factory.Services.CreateScope())
        {
            var real = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var proxy = new Mock<IUnitOfWork>();
            proxy.Setup(x => x.Repository<Survey>()).Returns(real.Repository<Survey>());
            proxy.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateConcurrencyException());
            var controller = new SurveysController(proxy.Object, Mock.Of<IConfiguration>());
            var list = (OkObjectResult)await controller.GetSurveys();
            var packet = JsonSerializer.SerializeToElement(list.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var row = packet.GetProperty("data").EnumerateArray().Single(x => x.GetProperty("id").GetInt64() == id);
            var version = row.GetProperty("editVersion").GetString();
            var request = new SurveysController.CreateSurveyRequest("تغییر همزمان", null, "[]", null, null, version);
            (await controller.UpdateSurvey(id, request)).Should().BeOfType<ConflictObjectResult>();
        }
        using var verify = factory.Services.CreateScope();
        var stored = await verify.ServiceProvider.GetRequiredService<ClinicProDbContext>().Surveys.SingleAsync(x => x.Id == id);
        stored.Title.Should().Be("عنوان اصلی");
    }
}
