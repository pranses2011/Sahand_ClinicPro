using FluentAssertions;
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
        (await handler.Handle(exam with { RightAxis = 181 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { LeftIop = -1 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightSphere = 1.234m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightCylinder = 100m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { PupilDistance = 62.55m }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { PupilDistance = 0 }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(exam with { RightSphere = -2.50m, RightAxis = 180, RightIop = 0, PupilDistance = 62.5m }, default)).IsSuccess.Should().BeTrue();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.EyeExamRecord>().Should().ContainSingle();
    }

}
