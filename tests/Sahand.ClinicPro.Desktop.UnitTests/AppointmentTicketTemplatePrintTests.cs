using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class AppointmentTicketTemplatePrintTests
{
    private static AppointmentTicketData Data(int? queue = 7) =>
        new("نام بیمار", "پزشک انتخاب‌شده", "1405/07/08", "10:30", "APT-12", queue);

    [Fact]
    public async Task Sends_actual_data_and_falls_back_only_when_no_template_available()
    {
        Dictionary<string, object?>? variables = null;
        var prints = 0;
        var result = await AppointmentTicketTemplatePrint.PrintAsync(Data(), v =>
        {
            variables = v;
            return Task.FromResult(TemplatePrintOutcome.Fallback);
        }, () => { prints++; return true; });
        variables!["patient_name"].Should().Be("نام بیمار");
        variables["doctor_name"].Should().Be("پزشک انتخاب‌شده");
        variables["appointment_date"].Should().Be("1405/07/08");
        variables["appointment_time"].Should().Be("10:30");
        variables["tracking_code"].Should().Be("APT-12");
        variables["queue_number"].Should().Be("7");
        prints.Should().Be(1);
        result.Should().Be((TemplatePrintOutcome.Printed, true));
        Data(null).Variables()["queue_number"].Should().Be("");
    }

    [Theory]
    [InlineData(TemplatePrintOutcome.HtmlWindowClosed)]
    [InlineData(TemplatePrintOutcome.NotPrinted)]
    [InlineData(TemplatePrintOutcome.Printed)]
    public async Task Selected_template_never_triggers_second_print(TemplatePrintOutcome outcome)
    {
        var prints = 0;
        var result = await AppointmentTicketTemplatePrint.PrintAsync(Data(), _ => Task.FromResult(outcome),
            () => { prints++; return true; });
        prints.Should().Be(0);
        result.Should().Be((outcome, false));
    }

    [Fact]
    public async Task Cancelled_internal_print_is_not_reported_as_success()
    {
        var result = await AppointmentTicketTemplatePrint.PrintAsync(Data(), _ => Task.FromResult(TemplatePrintOutcome.Fallback), () => false);
        result.Should().Be((TemplatePrintOutcome.NotPrinted, true));
    }
}
