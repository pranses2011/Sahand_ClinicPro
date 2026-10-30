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
    [InlineData(null, true)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Close_preview_defaults_to_previous_behavior_and_can_be_disabled(string? value, bool expected)
        => PrintFlow.ResolveClosePreview(value).Should().Be(expected);

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(15, 9)]
    public void Copy_count_is_limited_to_supported_range(int count, int expected)
        => PrintFlow.NormalizeCopies(count).Should().Be(expected);

    [Fact]
    public void Printer_override_and_inheritance_remain_distinct()
    {
        var legacy = new[] { "", "چاپگر قدیمی", "چاپگر بعدی" };
        PrintJobSettings.ResolvePrinter("چاپگر اختصاصی", legacy).Should().Be("چاپگر اختصاصی");
        PrintJobSettings.ResolvePrinter("", legacy).Should().Be("چاپگر قدیمی");
        PrintJobSettings.ResolvePrinter(null, Array.Empty<string>()).Should().BeNull();
    }

    [Theory]
    [InlineData("ReceptionReceipt", true, true)]
    [InlineData("AppointmentTicket", true, true)]
    [InlineData("PatientCard", true, true)]
    [InlineData("Invoice", false, false)]
    [InlineData("ImagingReport", true, true)]
    [InlineData("PhysioHomeProgram", true, true)]
    [InlineData("PhysioReferralReport", true, true)]
    [InlineData("DentalChart", true, true)]
    [InlineData("DentalTreatmentPlan", true, true)]
    [InlineData("GlassesPrescription", true, true)]
    [InlineData("Payslip", false, false)]
    public void Settings_only_enable_options_connected_to_printing(string key, bool printer, bool receiptOptions)
    {
        var row = new PrintJobRow { Key = key };
        row.SupportsPrinter.Should().Be(printer);
        row.SupportsFlowOptions.Should().Be(receiptOptions);
        row.Preview.Should().BeNull();
    }
}
