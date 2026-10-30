using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Laboratory;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class LabOrderSafetyTests
{
    [Fact]
    public async Task Invalid_or_partial_order_does_not_create_any_lab_order()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "LAB-SAFE-1", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        var other = new Patient { MedicalRecordNo = "LAB-SAFE-2", FirstName = "بیمار", LastName = "دیگر", Mobile = "09120000002" };
        var doctor = new Doctor();
        var test = new LabTest { TestName = "CBC", IsActive = true };
        db.AddRange(patient, other, doctor, test);
        await db.SaveChangesAsync();
        var visit = new Visit { PatientId = other.Id, DoctorId = doctor.Id };
        db.Add(visit);
        await db.SaveChangesAsync();
        var handler = new CreateLabOrderCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        (await handler.Handle(new(new CreateLabOrderDto(patient.Id, doctor.Id, visit.Id, new() { test.Id })), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new CreateLabOrderDto(patient.Id, doctor.Id, null, new() { test.Id, 999999 })), default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(new(new CreateLabOrderDto(patient.Id, doctor.Id, null, new() { test.Id, test.Id })), default)).IsSuccess.Should().BeFalse();
        (await db.Set<LabOrder>().CountAsync()).Should().Be(0);
        (await handler.Handle(new(new CreateLabOrderDto(patient.Id, doctor.Id, null, new() { test.Id })), default)).IsSuccess.Should().BeTrue();
        (await db.Set<LabOrder>().CountAsync()).Should().Be(1);
    }
}
