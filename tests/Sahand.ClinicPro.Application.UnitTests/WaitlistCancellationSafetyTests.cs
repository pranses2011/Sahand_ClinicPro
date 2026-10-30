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
        var clock = Mock.Of<IDateTimeService>(x => x.NowIran == new DateTime(2000, 1, 1));
        var handler = new ChangeAppointmentStatusCommandHandler(uow.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object, clock);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled, "تست"), CancellationToken.None));
        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Repeated_cancellation_does_not_offer_the_same_slot_or_overwrite_original_reason()
    {
        var appointment = new Appointment { Id = 1, PatientId = 3, DoctorId = 2,
            AppointmentDate = new DateOnly(2026, 5, 10), AppointmentTime = new TimeOnly(15, 0),
            Status = AppointmentStatus.Cancelled, CancellationReason = "دلیل اول" };
        var appointments = new Mock<IRepository<Appointment>>();
        appointments.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Appointment>()).Returns(appointments.Object);
        var notifier = new Mock<IWaitlistNotifier>(MockBehavior.Strict);
        var clock = Mock.Of<IDateTimeService>(x => x.NowIran == new DateTime(2026, 5, 10, 10, 0, 0));
        var handler = new ChangeAppointmentStatusCommandHandler(uow.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object, clock);

        var result = await handler.Handle(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled, "دلیل دوم"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("دلیل اول", appointment.CancellationReason);
        notifier.VerifyNoOtherCalls();
        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelled_appointment_cannot_be_reactivated_after_its_slot_may_have_been_offered()
    {
        var appointment = new Appointment { Id = 1, Status = AppointmentStatus.Cancelled };
        var appointments = new Mock<IRepository<Appointment>>();
        appointments.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Appointment>()).Returns(appointments.Object);
        var notifier = new Mock<IWaitlistNotifier>(MockBehavior.Strict);
        var handler = new ChangeAppointmentStatusCommandHandler(uow.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object, Mock.Of<IDateTimeService>());

        var result = await handler.Handle(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Confirmed), default);
        Assert.True(result.IsFailure);
        Assert.Equal("ALREADY_CANCELLED", result.Error.Code);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        notifier.VerifyNoOtherCalls();
        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelling_a_slot_already_past_today_does_not_offer_it_to_the_waitlist()
    {
        var appointment = new Appointment { Id = 1, PatientId = 3, DoctorId = 2,
            AppointmentDate = new DateOnly(2026, 5, 10), AppointmentTime = new TimeOnly(9, 0),
            Status = AppointmentStatus.Booked };
        var appointments = new Mock<IRepository<Appointment>>();
        appointments.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var patients = new Mock<IRepository<Patient>>();
        patients.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Repository<Appointment>()).Returns(appointments.Object);
        uow.Setup(u => u.Repository<Patient>()).Returns(patients.Object);
        uow.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var notifier = new Mock<IWaitlistNotifier>(MockBehavior.Strict);
        var clock = Mock.Of<IDateTimeService>(x => x.NowIran == new DateTime(2026, 5, 10, 10, 0, 0));
        var handler = new ChangeAppointmentStatusCommandHandler(uow.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<ChangeAppointmentStatusCommandHandler>.Instance, notifier.Object, clock);

        var result = await handler.Handle(new ChangeAppointmentStatusCommand(1, AppointmentStatus.Cancelled, "لغو"), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        notifier.VerifyNoOtherCalls();
        uow.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
