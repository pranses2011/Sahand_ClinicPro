using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Communication;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class PublicSurveyApiFactory : InMemoryApiFactory
{
    protected override string DatabaseName => "PublicSurveyFlowDb";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Surveys:EnableBuiltInPublic"] = "true",
            ["Surveys:PublicBaseUrl"] = "https://survey.example.test"
        }));
    }
}

public class PublicSurveyTests : IClassFixture<PublicSurveyApiFactory>
{
    private readonly PublicSurveyApiFactory _factory;
    public PublicSurveyTests(PublicSurveyApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Issued_link_can_be_used_once_without_recording_identity()
    {
        const string questions = "[{\"text\":\"کیفیت؟\",\"type\":\"rating\"}]";
        long surveyId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var patient = new Patient { FirstName = "آزمایش", LastName = "بیمار", Mobile = "09123334444", MedicalRecordNo = "SURVEY-PUBLIC-01", Gender = Gender.Female, IsActive = true };
            var doctor = new Doctor { IsActive = true };
            var survey = new Survey { Title = "آزمون عمومی", Questions = questions, IsActive = true };
            db.AddRange(patient, doctor, survey);
            await db.SaveChangesAsync();
            db.Visits.Add(new Visit { PatientId = patient.Id, DoctorId = doctor.Id, VisitDate = DateTime.UtcNow, Status = VisitStatus.Completed });
            await db.SaveChangesAsync();
            surveyId = survey.Id;
        }
        var admin = _factory.CreateClient();
        var login = await admin.PostAsJsonAsync("/api/v1/auth/login", new { usernameOrEmail = "admin", password = "Admin@123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDoc.RootElement.GetProperty("data").GetProperty("accessToken").GetString());
        var send = await admin.PostAsJsonAsync($"/api/v1/surveys/{surveyId}/send-links", new { baseUrl = "https://wrong.example.test" });
        send.StatusCode.Should().Be(HttpStatusCode.OK);
        var sendJson = await send.Content.ReadAsStringAsync();
        sendJson.Should().NotContain("wrong.example.test").And.NotContain("/s/#");
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
            var sms = await db.SmsQueue.SingleAsync(x => x.Recipient == "09123334444");
            sms.Message.Should().Contain("https://survey.example.test/s/#");
            token = sms.Message!.Split("/s/#")[1];
            token.Should().HaveLength(43);
            var invitation = await db.SurveyInvitations.SingleAsync(x => x.SurveyId == surveyId);
            invitation.TokenHash.Should().Be(Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token))));
            invitation.TokenHash.Should().NotContain(token);
            invitation.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(6));
            invitation.UsedAt.Should().BeNull();
        }
        var client = _factory.CreateClient();
        var page = await client.GetAsync("/s/");
        page.StatusCode.Should().Be(HttpStatusCode.OK);
        page.Headers.CacheControl!.NoStore.Should().BeTrue();
        page.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'none'");
        (await client.PostAsJsonAsync("/s/read", new { token = "wrong" })).StatusCode.Should().Be(HttpStatusCode.Gone);
        var read = await client.PostAsJsonAsync("/s/read", new { token });
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        (await read.Content.ReadAsStringAsync()).Should().Contain("کیفیت");
        var bad = await client.PostAsJsonAsync("/s/submit", new { token, questionsSnapshot = questions, answers = new[] { "9" } });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var stale = await client.PostAsJsonAsync("/s/submit", new { token, questionsSnapshot = "[]", answers = new[] { "5" } });
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var ok = await client.PostAsJsonAsync("/s/submit", new { token, questionsSnapshot = questions, answers = new[] { "5" }, comments = "خوب" });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/s/submit", new { token, questionsSnapshot = questions, answers = new[] { "5" } })).StatusCode.Should().Be(HttpStatusCode.Gone);
        using var verify = _factory.Services.CreateScope();
        var dbVerify = verify.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var saved = await dbVerify.SurveyResponses.SingleAsync(r => r.SurveyId == surveyId);
        saved.PatientId.Should().BeNull(); saved.DoctorId.Should().BeNull(); saved.VisitId.Should().BeNull();
        saved.Answers.Should().Contain("5");
        (await dbVerify.SurveyInvitations.SingleAsync(x => x.SurveyId == surveyId)).UsedAt.Should().NotBeNull();
    }
}
