using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PaymentRecoveryStoreTests
{
    private static string TempFolder() => Path.Combine(Path.GetTempPath(), "clinicpro-payment-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Store_encrypts_reserves_and_only_clears_matching_request()
    {
        var directory = TempFolder();
        try
        {
            var store = new PaymentRecoveryStore(directory);
            var payment = new PendingPayment(7, 9, 500, 2, "SECRET-TRACE-1381", "1234", Guid.NewGuid(), "https://clinic.example/api/v1/", 2);
            store.Reserve(payment);
            var contents = Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetFiles(directory).Single()));
            contents.Should().NotContain("SECRET-TRACE-1381");
            store.Load(payment.Server, 7).Should().Be(payment);
            Action duplicate = () => store.Reserve(payment with { RequestId = Guid.NewGuid() });
            duplicate.Should().Throw<IOException>();
            Action wrong = () => store.Clear(payment with { RequestId = Guid.NewGuid() });
            wrong.Should().Throw<InvalidDataException>();
            store.Load(payment.Server, 7).Should().Be(payment);
            store.Clear(payment);
            store.Load(payment.Server, 7).Should().BeNull();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task Reopened_window_replays_exact_request_and_clears_after_success()
    {
        var directory = TempFolder();
        try
        {
            var store = new PaymentRecoveryStore(directory);
            var api = new FakeApiClient { NextBoolResult = false };
            api.CannedResponses["invoices/512"] = new { data = new { id = 512, patientId = 77, remainingAmount = 100m } };
            var first = new PaymentWindowViewModel(api, 512, 100m, store) { Amount = 100m, MethodIndex = 0, ReferenceNumber = "trace" };
            await first.SaveCommand.ExecuteAsync(null);
            var request = JsonSerializer.Serialize(api.LastBody);
            first.IsPaymentOutcomeUnknown.Should().BeTrue();
            var reopened = new PaymentWindowViewModel(api, 512, 0m, store);
            reopened.IsPaymentOutcomeUnknown.Should().BeTrue();
            reopened.CanEditPayment.Should().BeFalse();
            reopened.CanRetryPayment.Should().BeTrue();
            api.NextBoolResult = true;
            var success = false;
            reopened.SaveSucceeded += (_, _) => success = true;
            await reopened.RetryPaymentCommand.ExecuteAsync(null);
            success.Should().BeTrue();
            JsonSerializer.Serialize(api.LastBody).Should().Be(request);
            store.Load("test-api", 512).Should().BeNull();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task Corrupt_recovery_file_blocks_new_payment()
    {
        var directory = TempFolder();
        try
        {
            var store = new PaymentRecoveryStore(directory);
            var record = new PendingPayment(512, 77, 100, 1, null, null, Guid.NewGuid(), "test-api", null);
            store.Reserve(record);
            File.WriteAllText(Directory.GetFiles(directory).Single(), "invalid");
            var api = new FakeApiClient();
            var vm = new PaymentWindowViewModel(api, 512, 100, store);
            vm.IsPaymentOutcomeUnknown.Should().BeTrue();
            vm.CanEditPayment.Should().BeFalse();
            vm.CanRetryPayment.Should().BeFalse();
            await vm.SaveCommand.ExecuteAsync(null);
            api.Calls.Should().NotContain(c => c.Endpoint == "payments/idempotent");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }
}
