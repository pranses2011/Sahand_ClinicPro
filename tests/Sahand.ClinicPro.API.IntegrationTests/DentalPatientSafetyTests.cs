using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class DentalPatientSafetyTests
{
    [Fact]
    public async Task Plan_requires_existing_patient_and_nonnegative_values()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "DENTAL-1", NationalCode = "0010000021", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new CreateDentalPlanCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(999999, null, 0, 0, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, -1, 0, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 0, -1, "{}"), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(patient.Id, null, 0, 0, "{}"), default)).IsSuccess.Should().BeTrue();
        db.Set<Sahand.ClinicPro.Domain.Entities.Paraclinical.DentalTreatmentPlan>().Should().ContainSingle();
    }
}
