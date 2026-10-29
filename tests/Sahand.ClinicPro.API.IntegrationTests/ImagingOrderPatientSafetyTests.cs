using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class ImagingOrderPatientSafetyTests
{
    [Fact]
    public async Task Order_requires_existing_patient_supported_modality_and_correct_body_part()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "IMG-SAFETY-01", FirstName = "بیمار", LastName = "آزمایشی", Mobile = "09120000001" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var handler = new CreateImagingOrderCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(new(999999, null, ImagingModality.MRI, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, (ImagingModality)6, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, ImagingModality.MRI, "  ")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, 999999, ImagingModality.MRI, "زانو")), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new(patient.Id, null, ImagingModality.MRI, " مچ دست چپ ")), default)).IsSuccess.Should().BeTrue();
        var saved = db.Set<ImagingOrder>().Single();
        saved.PatientId.Should().Be(patient.Id);
        saved.Modality.Should().Be(ImagingModality.MRI);
        saved.BodyPart.Should().Be("مچ دست چپ");
    }
}
