using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class ReceiptPrintOutcomeTests
{
    [Theory]
    [InlineData(true, TemplatePrintOutcome.Printed)]
    [InlineData(false, TemplatePrintOutcome.NotPrinted)]
    public async Task Original_preview_does_not_trigger_fallback_when_cancelled(bool printed, TemplatePrintOutcome expected)
    {
        var api = new FakeApiClient();
        api.CannedResponses["print-templates/render/ReceptionReceipt"] = new { data = new { useOriginalReceptionReceipt = true } };
        var count = 0;
        var outcome = await TemplatedPrintService.TryPrintAsync(api, "ReceptionReceipt", new(), "test", () => { count++; return printed; });
        outcome.Should().Be(expected);
        count.Should().Be(1);
    }

    [Fact]
    public async Task Callback_exception_does_not_turn_into_fallback()
    {
        var api = new FakeApiClient();
        api.CannedResponses["print-templates/render/ReceptionReceipt"] = new { data = new { useOriginalReceptionReceipt = true } };
        var action = () => TemplatedPrintService.TryPrintAsync(api, "ReceptionReceipt", new(), "test", () => throw new InvalidOperationException("print error"));
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Item_table_encodes_untrusted_text()
    {
        var html = TemplatedPrintService.ItemsTableHtml(new[] { ("<script>alert(1)</script>", "1", "2") });
        html.Should().NotContain("<script>").And.Contain("&lt;script&gt;");
    }
}
