using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Application.DTOs.Receptions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class ReceptionRecoveryStoreTests
{
    private static string Folder() => Path.Combine(Path.GetTempPath(), "clinicpro-reception-recovery", Guid.NewGuid().ToString("N"));
    private static PendingReception Sample() => new(new CreateReceptionDto(7, 1, 9, 0, null, "SECRET-NOTE", RequestId: Guid.NewGuid()),
        "بیمار", "پزشک", "test-api", null);

    [Fact]
    public void Store_encrypts_request_and_prevents_concurrent_workflow_and_wrong_clear()
    {
        var folder = Folder();
        try
        {
            var store = new ReceptionRecoveryStore(folder);
            var pending = Sample();
            using (store.AcquireWorkflow("test-api", 7))
            {
                store.Reserve(pending);
                Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetFiles(folder, "*.dat").Single())).Should().NotContain("SECRET-NOTE");
                Action concurrent = () => store.AcquireWorkflow("test-api", 7);
                concurrent.Should().Throw<IOException>();
            }
            store.Load("test-api", 7)!.Request.RequestId.Should().Be(pending.Request.RequestId);
            Action wrong = () => store.Clear(pending with { Request = pending.Request with { RequestId = Guid.NewGuid() } });
            wrong.Should().Throw<InvalidDataException>();
            store.Clear(pending);
            store.Load("test-api", 7).Should().BeNull();
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public async Task Reopened_patient_replays_only_reception_and_keeps_other_steps_manual()
    {
        var folder = Folder();
        try
        {
            var store = new ReceptionRecoveryStore(folder);
            var pending = Sample();
            store.Reserve(pending);
            var api = new FakeApiClient();
            api.CannedResponses["receptions/idempotent"] = new { data = 42L };
            var vm = new ReceptionsViewModel(api, new FakeSnackbar(), receptionRecoveryStore: store)
            { FoundPatient = new QuickPatient(7, "بیمار", "", "", null, null, null) };
            vm.IsReceptionOutcomeUnknown.Should().BeTrue();
            vm.CanEditReception.Should().BeFalse();
            vm.CanRetryReception.Should().BeTrue();
            await vm.RetryReceptionCommand.ExecuteAsync(null);
            JsonSerializer.SerializeToElement(api.LastBody).GetProperty("RequestId").GetGuid().Should().Be(pending.Request.RequestId!.Value);
            api.Calls.Should().NotContain(x => x.Endpoint == "consumables/consume" || x.Endpoint == "invoices/idempotent" || x.Endpoint == "payments/idempotent");
            store.Load("test-api", 7).Should().BeNull();
            vm.CanEditReception.Should().BeFalse();
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public async Task Corrupt_recovery_blocks_new_reception()
    {
        var folder = Folder();
        try
        {
            var store = new ReceptionRecoveryStore(folder);
            store.Reserve(Sample());
            File.WriteAllText(Directory.GetFiles(folder, "*.dat").Single(), "corrupt");
            var api = new FakeApiClient();
            var vm = new ReceptionsViewModel(api, new FakeSnackbar(), receptionRecoveryStore: store)
            { FoundPatient = new QuickPatient(7, "بیمار", "", "", null, null, null) };
            vm.CanEditReception.Should().BeFalse();
            vm.CanRetryReception.Should().BeFalse();
            await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
            api.Calls.Should().NotContain(x => x.Endpoint == "receptions/idempotent");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
