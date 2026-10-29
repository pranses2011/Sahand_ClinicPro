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
}
