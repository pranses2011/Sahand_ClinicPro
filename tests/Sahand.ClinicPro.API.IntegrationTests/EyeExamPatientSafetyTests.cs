using FluentAssertions;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class EyeExamPatientSafetyTests
{
    private static SaveEyeExamCommand Command(long patientId, long? visitId) =>
        new(patientId, visitId, null, null, null, null, null, null, null, null,
            null, null, null, null, "یافته", null);

    [Fact]
    public async Task Exam_cannot_be_created_for_missing_patient_or_another_patients_visit()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var first = new Patient { MedicalRecordNo = "EYE-1", NationalCode = "0010000001", FirstName = "اول", LastName = "آزمایشی", Mobile = "09120000001" };
        var second = new Patient { MedicalRecordNo = "EYE-2", NationalCode = "0010000002", FirstName = "دوم", LastName = "آزمایشی", Mobile = "09120000002" };
        db.Patients.AddRange(first, second);
        await db.SaveChangesAsync();
        var visit = new Visit { PatientId = second.Id, DoctorId = 1, VisitDate = DateTime.UtcNow };
        db.Visits.Add(visit);
        await db.SaveChangesAsync();
        var handler = new SaveEyeExamCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(Command(999999, null), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(Command(first.Id, visit.Id), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(Command(second.Id, visit.Id), default)).IsSuccess.Should().BeTrue();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.EyeExamRecord>().Should().ContainSingle();
    }
    [Fact]
    public async Task Exam_rejects_invalid_axes_pressure_and_values_the_database_would_round()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "EYE-NUM", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new SaveEyeExamCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var exam = Command(patient.Id, null);
        (await handler.Handle(exam with { Findings = null }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightAxis = 181 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { LeftIop = -1 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightSphere = 1.234m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightCylinder = 100m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { PupilDistance = 62.55m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { PupilDistance = 0 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightSphere = -2.50m, RightAxis = 180, RightIop = 0, PupilDistance = 62.5m }, default)).IsSuccess.Should().BeTrue();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.EyeExamRecord>().Should().ContainSingle();
    }

    [Fact]
    public async Task Corrected_visual_acuity_is_stored_separately_and_length_checked()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "EYE-CORRECTED", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Add(patient);
        await db.SaveChangesAsync();
        var handler = new SaveEyeExamCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var exam = Command(patient.Id, null) with { Findings = null, RightCorrectedVisualAcuity = "20/20" };
        (await handler.Handle(exam with { RightCorrectedVisualAcuity = "12345678901" }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam, default)).IsSuccess.Should().BeTrue();
        var saved = db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.EyeExamRecord>().Single();
        saved.RightCorrectedVisualAcuity.Should().Be("20/20");
        saved.RightVisualAcuity.Should().BeNull();
        saved.LeftCorrectedVisualAcuity.Should().BeNull();
    }

    [Fact]
    public async Task History_returns_corrected_and_uncorrected_vision_separately()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "EYE-HISTORY-VA", FirstName = "بیمار", LastName = "تست", Mobile = "09120000001" };
        db.Add(patient);
        await db.SaveChangesAsync();
        var handler = new SaveEyeExamCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var command = Command(patient.Id, null) with { RightVisualAcuity = "20/40", RightCorrectedVisualAcuity = "20/20", LeftCorrectedVisualAcuity = "20/25" };
        (await handler.Handle(command, default)).IsSuccess.Should().BeTrue();
        var controller = new ParaclinicalController(Mock.Of<IMediator>(), scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            Mock.Of<IWebHostEnvironment>(), Mock.Of<IConfiguration>());
        var response = (OkObjectResult)await controller.GetEyeExams(patient.Id);
        var json = JsonSerializer.SerializeToElement(response.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var result = json.GetProperty("data")[0];
        result.GetProperty("od").GetProperty("acuity").GetString().Should().Be("20/40");
        result.GetProperty("od").GetProperty("correctedAcuity").GetString().Should().Be("20/20");
        result.GetProperty("os").GetProperty("acuity").ValueKind.Should().Be(JsonValueKind.Null);
        result.GetProperty("os").GetProperty("correctedAcuity").GetString().Should().Be("20/25");
    }

}
