using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.Features.Appointments.Waitlist;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;
using Xunit;

namespace Sahand.ClinicPro.API.IntegrationTests;

public class WaitlistOfferControllerTests
{
    [Theory]
    [InlineData(0, null, "10:00")]
    [InlineData(5, "not-a-date", "10:00")]
    [InlineData(5, null, "invalid-time")]
    public async Task Invalid_parameters_do_not_silently_select_a_different_slot(long doctorId, string? date, string time)
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var clock = Mock.Of<IDateTimeService>();
        var controller = new AppointmentsController(mediator.Object, clock);
        Assert.IsType<BadRequestObjectResult>(await controller.OfferFreedSlot(doctorId, date, time));
        mediator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Default_date_uses_Iran_clock_and_success_only_claims_queued()
    {
        var today = new DateOnly(2026, 5, 10);
        var clock = Mock.Of<IDateTimeService>(x => x.TodayIran == today);
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(m => m.Send(It.IsAny<NotifyWaitlistForFreedSlotCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<long>.Success(42));
        var controller = new AppointmentsController(mediator.Object, clock);

        var response = Assert.IsType<OkObjectResult>(await controller.OfferFreedSlot(5, null, "۱۳:۱۵"));
        var payload = Assert.IsType<ApiResponse<long>>(response.Value);
        Assert.True(payload.IsSuccess);
        Assert.Equal(42L, payload.Data);
        Assert.Contains("صف", payload.Message);
        Assert.DoesNotContain("ارسال شد", payload.Message);
        mediator.Verify(m => m.Send(It.Is<NotifyWaitlistForFreedSlotCommand>(c => c.DoctorId == 5
            && c.CancelledAppointmentId == 0 && c.AppointmentDate == today
            && c.AppointmentTime == new TimeOnly(13, 15)), It.IsAny<CancellationToken>()), Times.Once);
    }
}
