using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sahand.ClinicPro.Application.Features.Appointments.Commands.ChangeAppointmentStatus;
using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;
using Xunit;

namespace Sahand.ClinicPro.Application.UnitTests;

public class WaitlistCancellationSafetyTests
{
    [Fact]
    public async Task Failed_nested_result_is_not_reported_as_no_candidate()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<NotifyWaitlistForFreedSlotCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<long>.Failure(new Error("WAITLIST.FAIL", "خطا")));
        var notifier = new WaitlistNotifier(sender.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => notifier.NotifyFreedSlotAsync(
            1, 2, DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0)));
    }

    [Fact]
    public async Task Cancellation_does_not_swallow_queue_failure_or_save_success()
    {
        var appointment = new Appointment { Id = 1, PatientId = 3, DoctorId = 2,
            AppointmentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), AppointmentTime = new TimeOnly(10, 0),
            Status = AppointmentStatus.Booked };
        var appointments = new Mock<IRepository<Appointment>>();
        appointments.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var patients = new Mock<IRepository<Patient>>();
        patients.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Appointment>()).Returns(appointments.Object);
        uow.Setup(u => u.Repository<Patient>()).Returns(patients.Object);
        var notifier = new Mock<IWaitlistNotifier>();
        notifier.Setup(n => n.NotifyFreedSlotAsync(1, 2, appointment.AppointmentDate, appointment.AppointmentTime,
                It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("queue failure"));
        var handler = new ChangeAppointmentStatusCommandHandler(uow.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled, "تست"), CancellationToken.None));
        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
