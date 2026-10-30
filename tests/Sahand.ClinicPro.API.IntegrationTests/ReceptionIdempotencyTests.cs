using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class ReceptionIdempotencyTests
{
    [Fact]
    public async Task Repeated_request_returns_existing_reception_after_appointment_is_arrived()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "RECEPTION-IDEM", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000001" };
        var doctor = new Doctor { IsActive = true };
        db.AddRange(patient, doctor); await db.SaveChangesAsync();
        var appointment = new Appointment { PatientId = patient.Id, DoctorId = doctor.Id, Status = AppointmentStatus.Booked };
        db.Add(appointment); await db.SaveChangesAsync();
        var realtime = new Mock<IRealtimeNotifier>();
        realtime.Setup(x => x.QueueChangedAsync(It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new CreateReceptionCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            NullLogger<CreateReceptionCommandHandler>.Instance, realtime.Object);
        var dto = new CreateReceptionDto(patient.Id, doctor.Id, appointment.Id, 0, null, null, RequestId: Guid.NewGuid());
        var first = await handler.Handle(new CreateReceptionCommand(dto), default);
        first.IsSuccess.Should().BeTrue();
        appointment.Status.Should().Be(AppointmentStatus.Arrived);
        var repeat = await handler.Handle(new CreateReceptionCommand(dto), default);
        repeat.IsSuccess.Should().BeTrue(); repeat.Value.Should().Be(first.Value);
        var conflict = await handler.Handle(new CreateReceptionCommand(dto with { Notes = "متفاوت" }), default);
        conflict.Error.Code.Should().Be("RECEPTION.REQUEST_CONFLICT");
        (await db.Set<Reception>().CountAsync()).Should().Be(1);
        realtime.Verify(x => x.QueueChangedAsync(It.IsAny<long?>(), "enqueue", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task New_reception_rejects_stale_targets_insurance_and_performer_but_replay_survives_later_deactivation()
    {
        using var factory = new InMemoryApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicProDbContext>();
        var patient = new Patient { MedicalRecordNo = "RECEPTION-VALID", FirstName = "بیمار", LastName = "آزمون", Mobile = "09120000002" };
        var doctor = new Doctor { IsActive = true };
        var service = new Service { ServiceName = "خدمت", IsActive = true };
        var insurance = new InsuranceCompany { Name = "بیمه", IsActive = true, IsContracted = true };
        var employee = new Employee { FirstName = "پرسنل", LastName = "آزمون", IsActive = true };
        db.AddRange(patient, doctor, service, insurance, employee);
        await db.SaveChangesAsync();
        var realtime = new Mock<IRealtimeNotifier>();
        realtime.Setup(x => x.QueueChangedAsync(It.IsAny<long?>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new CreateReceptionCommandHandler(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(),
            NullLogger<CreateReceptionCommandHandler>.Instance, realtime.Object);
        var dto = new CreateReceptionDto(patient.Id, null, null, insurance.Id, null, null,
            ServiceId: service.Id, PerformedByEmployeeId: employee.Id, RequestId: Guid.NewGuid());
        var validator = new CreateReceptionCommandValidator();
        (await validator.ValidateAsync(new CreateReceptionCommand(dto))).IsValid.Should().BeTrue();
        (await handler.Handle(new CreateReceptionCommand(dto with { DoctorId = doctor.Id }), default)).Error.Code.Should().Be("RECEPTION.TARGET");
        (await handler.Handle(new CreateReceptionCommand(dto with { InsuranceCompanyId = -1 }), default)).Error.Code.Should().Be("RECEPTION.INSURANCE_INVALID");
        (await handler.Handle(new CreateReceptionCommand(dto with { ReferralCode = new string('x', 51) }), default)).Error.Code.Should().Be("RECEPTION.TEXT_TOO_LONG");
        (await handler.Handle(new CreateReceptionCommand(dto with { ServiceName = new string('x', 201) }), default)).Error.Code.Should().Be("RECEPTION.TEXT_TOO_LONG");
        service.IsActive = false; await db.SaveChangesAsync();
        (await handler.Handle(new CreateReceptionCommand(dto), default)).IsFailure.Should().BeTrue();
        service.IsActive = true; employee.IsActive = false; await db.SaveChangesAsync();
        (await handler.Handle(new CreateReceptionCommand(dto), default)).Error.Code.Should().Be("RECEPTION.PERFORMER_INVALID");
        employee.IsActive = true; insurance.IsContracted = false; await db.SaveChangesAsync();
        (await handler.Handle(new CreateReceptionCommand(dto), default)).Error.Code.Should().Be("RECEPTION.INSURANCE_INVALID");
        insurance.IsContracted = true; doctor.IsActive = false; await db.SaveChangesAsync();
        (await handler.Handle(new CreateReceptionCommand(dto with { DoctorId = doctor.Id, ServiceId = null }), default)).IsFailure.Should().BeTrue();
        (await db.Set<Reception>().CountAsync()).Should().Be(0);
        var first = await handler.Handle(new CreateReceptionCommand(dto), default);
        first.IsSuccess.Should().BeTrue();
        service.IsActive = false; employee.IsActive = false; insurance.IsActive = false; await db.SaveChangesAsync();
        var replay = await handler.Handle(new CreateReceptionCommand(dto), default);
        replay.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(first.Value);
        (await db.Set<Reception>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Idempotent_endpoint_requires_key_before_dispatch()
    {
        var mediator = new Mock<MediatR.IMediator>(MockBehavior.Strict);
        var controller = new Sahand.ClinicPro.API.Controllers.ReceptionsController(mediator.Object, Mock.Of<ICurrentUserService>());
        var dto = new CreateReceptionDto(1, 1, null, null, null, null);
        (await controller.CreateIdempotent(dto)).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        (await controller.CreateIdempotent(dto with { RequestId = Guid.Empty })).Should().BeOfType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>();
        mediator.VerifyNoOtherCalls();
    }
}
