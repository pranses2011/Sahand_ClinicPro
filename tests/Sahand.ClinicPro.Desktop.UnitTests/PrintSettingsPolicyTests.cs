using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PrintSettingsPolicyTests
{
    [Theory]
    [InlineData(null, true, true)]
    [InlineData(null, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, false)]
    public void Receipt_preview_override_or_global_is_resolved_correctly(bool? specific, bool global, bool expected)
        => PrintFlow.ResolvePreview(specific, global).Should().Be(expected);

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(15, 9)]
    public void Copy_count_is_limited_to_supported_range(int count, int expected)
        => PrintFlow.NormalizeCopies(count).Should().Be(expected);

    [Theory]
    [InlineData("ReceptionReceipt", true, true)]
    [InlineData("AppointmentTicket", true, true)]
    [InlineData("PatientCard", true, true)]
    [InlineData("Invoice", false, false)]
    [InlineData("ImagingReport", false, false)]
    [InlineData("Payslip", false, false)]
    public void Settings_only_enable_options_connected_to_printing(string key, bool printer, bool receiptOptions)
    {
        var row = new PrintJobRow { Key = key };
        row.SupportsPrinter.Should().Be(printer);
        row.SupportsFlowOptions.Should().Be(receiptOptions);
        row.Preview.Should().BeNull();
    }
}
