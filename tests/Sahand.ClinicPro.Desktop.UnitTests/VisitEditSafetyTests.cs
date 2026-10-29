using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class VisitEditSafetyTests
{
    [Fact]
    public async Task Saving_new_visit_twice_reuses_created_id()
    {
        var api = new FakeApiClient();
        api.CannedResponses["visits"] = new { data = 42L };
        var vm = new VisitEditViewModel(api, new FakeSnackbar(), 7, 8);
        await vm.SaveDraftCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("id").ValueKind.Should().Be(JsonValueKind.Null);
        await vm.SaveDraftCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("id").GetInt64().Should().Be(42);
        vm.StopTimers();
    }

    [Fact]
    public async Task Next_patient_does_not_close_when_new_visit_result_is_unknown()
    {
        var api = new FakeApiClient();
        var vm = new VisitEditViewModel(api, new FakeSnackbar(), 7, 8);
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;
        await vm.NextPatientCommand.ExecuteAsync(null);
        closed.Should().BeFalse();
        await vm.SaveDraftCommand.ExecuteAsync(null);
        api.Calls.Count(c => c.Method == "POST" && c.Endpoint == "visits").Should().Be(1);
        vm.StopTimers();
    }
}
