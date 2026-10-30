using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class LabResultTemplatePrintTests
{
    private static LabResultViewModel.LabOrderDetail Detail() => new()
    {
        PatientName = "بیمار نمونه", DoctorName = "پزشک", OrderNumber = "LAB-12", DateShamsi = "1405/01/02",
        Items = new()
        {
            new() { TestName = "Hb <script>", ResultNumeric = 12.55m, Unit = "g/dL", NormalRange = "12-16", Comment = "<img>" },
            new() { TestName = "آزمایش دوم", Result = "مثبت", IsAbnormal = true },
            new() { TestName = "آزمایش سوم" }
        }
    };

    [Fact]
    public void Variables_use_actual_values_escape_table_and_do_not_invent_verification()
    {
        var vars = LabResultTemplatePrint.Variables(Detail(), 5);
        vars["patient_name"].Should().Be("بیمار نمونه");
        vars["order_number"].Should().Be("LAB-12");
        vars["result_date"].Should().Be("");
        vars["verified_by"].Should().Be("");
        var table = (string)vars["results_html"]!;
        table.Should().Contain("۱۲.۵۵").And.Contain("g/dL").And.Contain("12-16").And.Contain("مثبت")
            .And.Contain("غیرعادی").And.Contain("در انتظار").And.Contain("&lt;script&gt;")
            .And.NotContain("<script>").And.NotContain("<img>");
    }

    [Theory]
    [InlineData(TemplatePrintOutcome.NotPrinted)]
    [InlineData(TemplatePrintOutcome.HtmlWindowClosed)]
    [InlineData(TemplatePrintOutcome.Printed)]
    public async Task Existing_template_does_not_trigger_second_print(TemplatePrintOutcome result)
    {
        var count = 0;
        var actual = await LabResultTemplatePrint.PrintAsync(Detail(), 5, _ => Task.FromResult(result), () => { count++; return true; });
        actual.Should().Be((result, false));
        count.Should().Be(0);
    }

    [Fact]
    public async Task Fallback_uses_internal_once_and_preserves_cancel()
    {
        var count = 0;
        var result = await LabResultTemplatePrint.PrintAsync(Detail(), 5, _ => Task.FromResult(TemplatePrintOutcome.Fallback),
            () => { count++; return false; });
        result.Should().Be((TemplatePrintOutcome.NotPrinted, true));
        count.Should().Be(1);
    }
}
