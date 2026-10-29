using FluentAssertions;
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
}
