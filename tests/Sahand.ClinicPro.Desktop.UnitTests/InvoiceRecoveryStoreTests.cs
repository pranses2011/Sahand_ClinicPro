using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Application.DTOs.Billing;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Sahand.ClinicPro.Domain.Enums;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class InvoiceRecoveryStoreTests
{
    private static string Folder() => Path.Combine(Path.GetTempPath(), "clinicpro-invoice-tests", Guid.NewGuid().ToString("N"));
    private static PendingInvoice Sample() => new(
        new CreateInvoiceDto(7, null, null, InvoiceType.Visit, null, 0,
            new() { new InvoiceItemDto(1, "SECRET-SERVICE", 1, 100, 0, null, 0) }, ExpectedQuote: "q1", RequestId: Guid.NewGuid()),
        new InvoicePreviewDto(100, 0, 0, 0, 100, null, "q1"), "test-api", null);

    [Fact]
    public void Store_encrypts_reserves_and_only_clears_same_request()
    {
        var folder = Folder();
        try
        {
            var store = new InvoiceRecoveryStore(folder);
            var pending = Sample();
            store.Reserve(pending);
            Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetFiles(folder).Single())).Should().NotContain("SECRET-SERVICE");
            store.Load("test-api", 7)!.Request.RequestId.Should().Be(pending.Request.RequestId);
            Action duplicate = () => store.Reserve(pending);
            duplicate.Should().Throw<IOException>();
            Action wrong = () => store.Clear(pending with { Request = pending.Request with { RequestId = Guid.NewGuid() } });
            wrong.Should().Throw<InvalidDataException>();
            store.Clear(pending);
            store.Load("test-api", 7).Should().BeNull();
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public async Task Reopening_patient_recovers_same_invoice_without_automatic_payment()
    {
        var folder = Folder();
        try
        {
            var store = new InvoiceRecoveryStore(folder);
            var api = new FakeApiClient();
            var pending = Sample();
            store.Reserve(pending);
            api.CannedResponses["invoices/idempotent"] = new { data = 42L };
            var vm = new InvoiceFormViewModel(api, new FakeSnackbar(), store)
            { Patient = new QuickPatient(7, "بیمار", "", "", null, null, null) };
            vm.CanEditInvoice.Should().BeFalse();
            vm.CanRetryInvoice.Should().BeTrue();
            vm.InvoiceRecoveryInfo.Should().Contain("SECRET-SERVICE");
            await vm.RetryInvoiceCommand.ExecuteAsync(null);
            var body = JsonSerializer.SerializeToElement(api.LastBody);
            body.GetProperty("RequestId").GetGuid().Should().Be(pending.Request.RequestId!.Value);
            vm.ErrorMessage.Should().Contain("پرداخت خودکار انجام نشد");
            api.Calls.Should().NotContain(c => c.Endpoint == "payments/idempotent");
            store.Load("test-api", 7).Should().BeNull();
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public async Task Corrupt_pending_invoice_blocks_new_submission()
    {
        var folder = Folder();
        try
        {
            var store = new InvoiceRecoveryStore(folder);
            store.Reserve(Sample());
            File.WriteAllText(Directory.GetFiles(folder).Single(), "corrupt");
            var api = new FakeApiClient();
            var vm = new InvoiceFormViewModel(api, new FakeSnackbar(), store)
            { Patient = new QuickPatient(7, "بیمار", "", "", null, null, null) };
            vm.CanEditInvoice.Should().BeFalse();
            vm.CanRetryInvoice.Should().BeFalse();
            await vm.SaveAndPrintCommand.ExecuteAsync(null);
            api.Calls.Should().NotContain(c => c.Endpoint == "invoices/idempotent");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Fact]
    public async Task Simultaneous_reservations_for_same_patient_do_not_overwrite_each_other()
    {
        var folder = Folder();
        try
        {
            var store = new InvoiceRecoveryStore(folder);
            var first = Sample();
            var second = first with { Request = first.Request with { RequestId = Guid.NewGuid() } };
            using var gate = new ManualResetEventSlim(false);
            async Task<bool> Attempt(PendingInvoice pending) => await Task.Run(() =>
            {
                gate.Wait();
                try { store.Reserve(pending); return true; }
                catch (IOException) { return false; }
            });
            var a = Attempt(first);
            var b = Attempt(second);
            gate.Set();
            var results = await Task.WhenAll(a, b);
            results.Count(x => x).Should().Be(1);
            var key = store.Load("test-api", 7)!.Request.RequestId;
            (key == first.Request.RequestId || key == second.Request.RequestId).Should().BeTrue();
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
