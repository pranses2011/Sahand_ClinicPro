using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sahand.ClinicPro.Application.Features.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.Paraclinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class PhysiotherapyReferralSafetyTests
{
    [Fact]
    public async Task Referral_is_idempotent_and_never_creates_a_treatment_session()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "REF-1", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        var other = new Patient { MedicalRecordNo = "REF-2", FirstName = "بیمار", LastName = "دیگر", Mobile = "09120000002" };
        var doctor = new Doctor();
        db.AddRange(patient, other, doctor);
        await db.SaveChangesAsync();
        var visit = new Visit { PatientId = patient.Id, DoctorId = doctor.Id };
        db.Add(visit);
        await db.SaveChangesAsync();
        var handler = new CreatePhysiotherapyReferralCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        var key = Guid.NewGuid();
        var valid = new CreatePhysiotherapyReferralCommand(key, visit.Id, patient.Id, doctor.Id, "شرح ارجاع");
        (await handler.Handle(valid with { PatientId = other.Id, RequestId = Guid.NewGuid() }, default)).IsSuccess.Should().BeFalse();
        (await handler.Handle(valid with { Note = " " }, default)).IsSuccess.Should().BeFalse();
        var first = await handler.Handle(valid, default);
        var repeat = await handler.Handle(valid, default);
        first.IsSuccess.Should().BeTrue();
        repeat.Value.Should().Be(first.Value);
        (await handler.Handle(valid with { Note = "تغییر" }, default)).IsSuccess.Should().BeFalse();
        (await db.Set<PhysiotherapyReferral>().CountAsync()).Should().Be(1);
        (await db.Set<PhysiotherapySession>().CountAsync()).Should().Be(0);
    }
}
