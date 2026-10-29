using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class PhysioPatientSafetyTests
{
    [Fact]
    public async Task Session_requires_existing_patient_and_valid_numbers()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "PHYSIO-1", NationalCode = "0010000011", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new RecordPhysioSessionCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(999999, null, 1, null, 20, 5), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 0, null, 20, 5), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 1, null, 0, 5), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 1, null, 20, 11), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 1, null, 1441, 5), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 1, null, 20, 0), default)).IsSuccess.Should().BeTrue();
        (await handler.Handle(new(patient.Id, null, 1, null, 20, 0), default)).IsSuccess.Should().BeFalse();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.PhysiotherapySession>().Should().ContainSingle();
    }
    [Fact]
    public async Task History_stays_chronological_and_next_number_includes_older_sessions()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "PHYSIO-HISTORY", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        db.Add(new PhysiotherapySession { PatientId = patient.Id, SessionNumber = 100 });
        for (var i = 1; i <= 25; i++) db.Add(new PhysiotherapySession { PatientId = patient.Id, SessionNumber = i });
        await db.SaveChangesAsync();
        var controller = new ParaclinicalController(Mock.Of<IMediator>(), scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), Mock.Of<IWebHostEnvironment>(), Mock.Of<IConfiguration>());
        var response = (OkObjectResult)await controller.GetPhysioSessions(patient.Id);
        var json = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var sessions = json.GetProperty("data").EnumerateArray().ToArray();
        sessions.Should().HaveCount(20);
        sessions[^1].GetProperty("sessionNumber").GetInt32().Should().Be(25);
        json.GetProperty("nextSessionNumber").GetInt32().Should().Be(101);
    }

}
