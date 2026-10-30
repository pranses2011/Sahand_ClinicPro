using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Application.Features.Receptions.Commands.CreateReception;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Entities.People;
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
