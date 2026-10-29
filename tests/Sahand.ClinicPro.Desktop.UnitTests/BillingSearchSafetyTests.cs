using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class BillingSearchSafetyTests
{
    [Fact]
    public async Task Changed_search_discards_old_result_and_selection()
    {
        var api = new FakeApiClient();
        var vm = new BillingViewModel(api, new FakeSnackbar());
        var pending = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.PendingGetResponses["invoices?page=1&pageSize=15&search=12"] = pending.Task;
        vm.InvoiceSearch = "12";
        var old = vm.SearchInvoicesAsync();
        vm.InvoiceSearch = "13";
        api.CannedResponses["invoices?page=1&pageSize=15&search=13"] = new
        { data = new[] { new { id = 13L, invoiceNumber = "INV-13", patientName = "بیمار دوم", totalAmount = 100m, paidAmount = 0m } } };
        await vm.SearchInvoicesAsync();
        vm.Invoices.Should().ContainSingle().Which.Id.Should().Be(13);
        vm.SelectedInvoice = vm.Invoices[0];
        pending.SetResult(new { data = new[] { new { id = 12L, invoiceNumber = "INV-12", patientName = "بیمار اول", totalAmount = 100m, paidAmount = 0m } } });
        await old;
        vm.Invoices.Should().ContainSingle().Which.Id.Should().Be(13);
        vm.SelectedInvoice!.Id.Should().Be(13);
        vm.InvoiceSearch = "14";
        vm.SelectedInvoice.Should().BeNull();
        vm.Invoices.Should().BeEmpty();
    }
}
