using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class ImagingOrderSafetyTests
{
    private static QuickPatient Patient(long id) => new(id, $"بیمار {id}", "", "", null, null, null);

    [Theory]
    [InlineData(2, 4)] // MRI
    [InlineData(3, 3)] // CT
    public async Task Requested_modality_and_custom_body_part_are_sent_exactly(int selectedIndex, int expectedModality)
    {
        var api = new FakeApiClient();
        var vm = new ImagingViewModel(api, new FakeSnackbar())
        { ImagingPatient = Patient(4), ImagingTypeIndex = selectedIndex, ImagingRegion = "  مچ دست چپ  " };
        await vm.SaveImagingOrderCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("modality").GetInt32().Should().Be(expectedModality);
        body.GetProperty("bodyPart").GetString().Should().Be("مچ دست چپ");
    }

    [Fact]
    public async Task Empty_region_and_unsupported_modality_are_not_sent()
    {
        var api = new FakeApiClient();
        var vm = new ImagingViewModel(api, new FakeSnackbar()) { ImagingPatient = Patient(4), ImagingRegion = " " };
        await vm.SaveImagingOrderCommand.ExecuteAsync(null);
        vm.ImagingRegion = "زانو"; vm.ImagingTypeIndex = 5;
        await vm.SaveImagingOrderCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "imaging/orders");
    }

    [Fact]
    public async Task Save_locks_form_and_preserves_snapshot_on_failed_request()
    {
        var pending = new TaskCompletionSource<bool>();
        var api = new FakeApiClient { PendingBoolPostResult = pending.Task };
        var vm = new ImagingViewModel(api, new FakeSnackbar())
        { ImagingPatient = Patient(4), ImagingTypeIndex = 2, ImagingRegion = "زانو", ImagingClinicalInfo = "درد" };
        var save = vm.SaveImagingOrderCommand.ExecuteAsync(null);
        vm.CanEditImagingOrder.Should().BeFalse();
        await vm.SearchImagingPatientCommand.ExecuteAsync(null);
        vm.ImagingPatient!.Id.Should().Be(4);
        pending.SetResult(false);
        await save;
        vm.CanEditImagingOrder.Should().BeTrue();
        vm.ImagingClinicalInfo.Should().Be("درد");
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("bodyPart").GetString().Should().Be("زانو");
    }

    [Fact]
    public async Task Failed_or_ambiguous_search_clears_old_patient()
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] {
            new { id = 1, firstName = "بیمار", lastName = "الف" }, new { id = 2, firstName = "بیمار", lastName = "ب" }
        } };
        var vm = new ImagingViewModel(api, new FakeSnackbar()) { ImagingPatient = Patient(4), ImagingPatientSearch = "1234" };
        await vm.SearchImagingPatientCommand.ExecuteAsync(null);
        vm.ImagingPatient.Should().BeNull();
    }
}
