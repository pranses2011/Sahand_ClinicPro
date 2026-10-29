using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class InvoiceFormSafetyTests
{
    private static InvoiceFormViewModel Create(FakeApiClient api)
    {
        api.CannedResponses["invoices/preview"] = new { data = new { subTotal = 100m, discountAmount = 0m,
            insuranceShare = 0m, tax = 0m, patientPayable = 100m, appliedInsuranceId = (long?)null, quoteVersion = "TEST-QUOTE" } };
        var vm = new InvoiceFormViewModel(api, new FakeSnackbar())
        {
            Patient = new QuickPatient(7, "بیمار", "", "", null, null, null),
            ReceivedAmount = 100,
            PaymentMethodIndex = 2
        };
        vm.Items.Add(new InvoiceItemVM { ServiceId = 1, Name = "خدمت", UnitPrice = 100, Quantity = 1 });
        return vm;
    }

    [Fact]
    public async Task Wrong_amount_from_server_blocks_payment_and_repeat_invoice()
    {
        var api = new FakeApiClient();
        api.CannedResponses["invoices/idempotent"] = new { data = 42L };
        api.CannedResponses["invoices/42"] = new { data = new { id = 42L, patientId = 7L, remainingAmount = 90m } };
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        vm.ErrorMessage.Should().Contain("منطبق نیست");
        vm.CanEditInvoice.Should().BeFalse();
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        api.Calls.Count(x => x.Endpoint == "invoices/idempotent").Should().Be(1);
        api.Calls.Should().NotContain(x => x.Endpoint == "payments/idempotent");
    }

    [Fact]
    public async Task Bank_transfer_uses_bank_method_and_confirmed_amount()
    {
        var api = new FakeApiClient();
        api.CannedResponses["invoices/idempotent"] = new { data = 42L };
        api.CannedResponses["invoices/42"] = new { data = new { id = 42L, patientId = 7L, remainingAmount = 100m } };
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        var body = System.Text.Json.JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("Method").GetInt32().Should().Be(5);
        body.GetProperty("Amount").GetDecimal().Should().Be(100);
    }

    [Fact]
    public async Task Unknown_invoice_response_and_unquoted_changes_are_not_retried()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        vm.DiscountPercent = 10;
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(x => x.Endpoint == "invoices/idempotent");
        vm.DiscountPercent = 0;
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        api.Calls.Count(x => x.Endpoint == "invoices/idempotent").Should().Be(1);
        vm.ErrorMessage.Should().Contain("نامعلوم");
    }
    [Fact]
    public async Task Invalid_number_binding_blocks_invoice_creation()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        vm.HasInvalidInvoiceInput = true;
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        vm.ErrorMessage.Should().Contain("نامعتبر");
        api.Calls.Should().NotContain(x => x.Endpoint == "invoices/idempotent");
    }

    [Fact]
    public async Task Editing_an_item_after_quote_blocks_issuance_even_for_programmatic_change()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        vm.Items[0].Quantity = 2;
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(x => x.Endpoint == "invoices/idempotent");
        vm.ErrorMessage.Should().Contain("محاسبه");
    }

    [Fact]
    public async Task Invoice_request_includes_the_reviewed_quote_version()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        var body = System.Text.Json.JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("ExpectedQuote").GetString().Should().Be("TEST-QUOTE");
    }

    [Fact]
    public async Task Too_long_payment_reference_blocks_invoice_before_creation()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        vm.ReferenceNumber = new string('x', 51);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        vm.ErrorMessage.Should().Contain("نامعتبر");
        api.Calls.Should().NotContain(x => x.Endpoint == "invoices/idempotent");
    }

    [Fact]
    public void Reception_payment_rejects_reference_longer_than_server_limit()
    {
        var vm = new ReceptionPayViewModel("100") { MethodIndex = 1, ReferenceNumber = new string('x', 51) };
        vm.ConfirmCommand.Execute(null);
        vm.Result.Should().BeNull();
        vm.ErrorMessage.Should().Contain("۵۰");
    }
    [Fact]
    public async Task Retry_invoice_reuses_same_key_and_never_sends_payment_automatically()
    {
        var api = new FakeApiClient();
        var vm = Create(api);
        await vm.CalculateQuoteCommand.ExecuteAsync(null);
        await vm.SaveAndPrintCommand.ExecuteAsync(null);
        var original = System.Text.Json.JsonSerializer.Serialize(api.LastBody);
        vm.CanRetryInvoice.Should().BeTrue();
        api.CannedResponses["invoices/idempotent"] = new { data = 42L };
        await vm.RetryInvoiceCommand.ExecuteAsync(null);
        System.Text.Json.JsonSerializer.Serialize(api.LastBody).Should().Be(original);
        vm.CanRetryInvoice.Should().BeFalse();
        vm.ErrorMessage.Should().Contain("پرداخت خودکار انجام نشد");
        api.Calls.Should().NotContain(c => c.Endpoint == "payments/idempotent");
    }
}
