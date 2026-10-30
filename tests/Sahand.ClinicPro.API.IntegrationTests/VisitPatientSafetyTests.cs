using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;
using Sahand.ClinicPro.Domain.Entities.Clinical;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class VisitPatientSafetyTests
{
    [Fact]
    public async Task Visit_identity_is_checked_and_new_vitals_reference_the_visit()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var owner = new Patient { MedicalRecordNo = "VISIT-OWNER", FirstName = "اول", LastName = "بیمار", Mobile = "09120000001" };
        var other = new Patient { MedicalRecordNo = "VISIT-OTHER", FirstName = "دوم", LastName = "بیمار", Mobile = "09120000002" };
        var doctor = new Doctor();
        db.AddRange(owner, other, doctor);
        await db.SaveChangesAsync();
        var reception = new Reception { PatientId = owner.Id, DoctorId = doctor.Id, Status = ReceptionStatus.Admitted };
        db.Add(reception);
        await db.SaveChangesAsync();
        var handler = new SaveVisitCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<SaveVisitCommandHandler>.Instance);
        (await handler.Handle(new(new SaveVisitDto(null, other.Id, doctor.Id, reception.Id)), default)).IsSuccess.Should().BeFalse();
        var saved = await handler.Handle(new(new SaveVisitDto(null, owner.Id, doctor.Id, reception.Id,
            VitalSigns: new VitalSignsInputDto(HeartRate: 75))), default);
        saved.IsSuccess.Should().BeTrue();
        var vital = await db.Set<VitalSign>().SingleAsync();
        vital.VisitId.Should().Be(saved.Value.Id);
        vital.PatientId.Should().Be(owner.Id);
        (await handler.Handle(new(new SaveVisitDto(saved.Value.Id, other.Id, doctor.Id, null,
            ChiefComplaint: "داده نامرتبط")), default)).IsSuccess.Should().BeFalse();
        (await db.Set<Visit>().SingleAsync()).ChiefComplaint.Should().BeNull();
    }

    [Fact]
    public async Task Completed_visit_cannot_be_reopened_or_overwritten_by_draft()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "VISIT-COMPLETE", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000003" };
        var doctor = new Doctor();
        db.AddRange(patient, doctor);
        await db.SaveChangesAsync();
        var visit = new Visit { PatientId = patient.Id, DoctorId = doctor.Id, Status = VisitStatus.Completed, ChiefComplaint = "اصلی" };
        db.Add(visit);
        await db.SaveChangesAsync();
        var handler = new SaveVisitCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<SaveVisitCommandHandler>.Instance);
        var result = await handler.Handle(new(new SaveVisitDto(visit.Id, patient.Id, doctor.Id, null, ChiefComplaint: "ویرایش")), default);
        result.IsSuccess.Should().BeFalse();
        visit.Status.Should().Be(VisitStatus.Completed);
        visit.ChiefComplaint.Should().Be("اصلی");
    }
    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public void Invalid_oxygen_saturation_is_not_accepted(int value)
    {
        Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit.VisitVitalsSafety
            .IsValid(new VitalSignsInputDto(OxygenSaturation: value)).Should().BeFalse();
    }

    [Fact]
    public async Task Stale_or_missing_visit_version_cannot_overwrite_clinical_data()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "VISIT-VERSION", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000004" };
        var doctor = new Doctor();
        db.AddRange(patient, doctor);
        await db.SaveChangesAsync();
        var handler = new SaveVisitCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), NullLogger<SaveVisitCommandHandler>.Instance);
        var first = await handler.Handle(new(new SaveVisitDto(null, patient.Id, doctor.Id, null, ChiefComplaint: "اول")), default);
        first.IsSuccess.Should().BeTrue();
        var missing = await handler.Handle(new(new SaveVisitDto(first.Value.Id, patient.Id, doctor.Id, null, ChiefComplaint: "گمشده")), default);
        missing.IsSuccess.Should().BeFalse();
        var second = await handler.Handle(new(new SaveVisitDto(first.Value.Id, patient.Id, doctor.Id, null,
            ChiefComplaint: "دوم", EditVersion: first.Value.EditVersion)), default);
        second.IsSuccess.Should().BeTrue();
        var stale = await handler.Handle(new(new SaveVisitDto(first.Value.Id, patient.Id, doctor.Id, null,
            ChiefComplaint: "قدیمی", EditVersion: first.Value.EditVersion)), default);
        stale.IsSuccess.Should().BeFalse();
        (await db.Set<Visit>().SingleAsync()).ChiefComplaint.Should().Be("دوم");
    }

}
