using FluentAssertions;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class InvoiceDocumentPrintTests
{
    private static InvoiceDetailDto Detail(long id = 42, InvoiceStatus status = InvoiceStatus.Issued) =>
        new(id, "INV-42", 7, "<script>بیمار</script>", "۱۴۰۵/۰۸/۰۱", InvoiceType.Visit,
            100, 10, 5, 75, 20, 55, PaymentStatus.PartiallyPaid, status,
            new() { new(1, "<img src=x>", 1, 100, 10, 70, 20, 90) });
    private static InvoiceRow Row(long id) => new(id, $"INV-{id}", "", "بیمار", 75, 20, "", "");

    [Fact]
    public void Print_document_uses_real_values_escapes_text_and_does_not_claim_receipt()
    {
        var detail = Detail();
        InvoiceDocumentPrint.Printable(detail, 42).Should().BeTrue();
        var vars = InvoiceDocumentPrint.TemplateVariables(detail);
        vars["insurance_share"].Should().Be("۲۰ ریال");
        vars["total"].Should().Be("۹۵ ریال");
        vars["patient_share"].Should().Be("۷۵ ریال");
        vars["remaining"].Should().Be("۵۵ ریال");
        vars["items_html"]!.ToString().Should().Contain("&lt;img").And.NotContain("<img");
        var html = InvoiceDocumentPrint.Fallback(detail);
        html.Should().Contain("رسید پرداخت نیست").And.Contain("پرداخت جزئی").And.Contain("۵۵ ریال");
        html.Should().Contain("&lt;script&gt;").And.NotContain("<script>بیمار</script>");
        InvoiceDocumentPrint.WithRequiredHeader("<html><body><p>قالب</p></body></html>", detail)!
            .Should().Contain("رسید پرداخت نیست").And.Contain("قالب");
        InvoiceDocumentPrint.WithRequiredHeader("<p>بدون بدنه</p>", detail).Should().BeNull();
    }

    [Fact]
    public void Cancelled_or_unmatched_invoice_is_not_printable()
    {
        InvoiceDocumentPrint.Printable(Detail(status: InvoiceStatus.Cancelled), 42).Should().BeFalse();
        InvoiceDocumentPrint.Printable(Detail(status: InvoiceStatus.Draft), 42).Should().BeFalse();
        InvoiceDocumentPrint.Printable(Detail(), 43).Should().BeFalse();
    }

    [Fact]
    public async Task Print_load_discards_late_response_after_selection_changes()
    {
        var api = new FakeApiClient();
        var vm = new BillingViewModel(api, new FakeSnackbar()) { SelectedInvoice = Row(42) };
        var pending = new TaskCompletionSource<object?>();
        api.PendingGetResponses["invoices/42"] = pending.Task;
        var load = vm.LoadInvoiceForPrintAsync(42, null, null);
        vm.SelectedInvoice = Row(43);
        pending.SetResult(new { data = Detail() });
        (await load).Should().BeNull();
        api.Calls.Should().NotContain(x => x.Method == "POST");
    }

    [Fact]
    public async Task Print_load_requires_matching_current_invoice_and_completed_status()
    {
        var api = new FakeApiClient();
        var vm = new BillingViewModel(api, new FakeSnackbar()) { SelectedInvoice = Row(42) };
        api.CannedResponses["invoices/42"] = new { data = Detail() };
        (await vm.LoadInvoiceForPrintAsync(42, null, null))!.Id.Should().Be(42);
        api.CannedResponses["invoices/42"] = new { data = Detail(43) };
        (await vm.LoadInvoiceForPrintAsync(42, null, null)).Should().BeNull();
        api.CannedResponses["invoices/42"] = new { data = Detail(status: InvoiceStatus.Cancelled) };
        (await vm.LoadInvoiceForPrintAsync(42, null, null)).Should().BeNull();
    }
}
