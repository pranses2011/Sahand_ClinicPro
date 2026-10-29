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
    [Fact]
    public async Task Failed_queue_refresh_preserves_prior_rows_and_reports_stale_data()
    {
        var api = new FakeApiClient();
        api.CannedResponses["imaging/orders"] = new { data = new[] {
            new { id = 1, patientId = 4, patientName = "بیمار", modality = "MRI", bodyPart = "زانو", status = 1, priority = 3, createdAt = DateTime.UtcNow }
        } };
        var vm = new ImagingViewModel(api, new FakeSnackbar());
        await vm.LoadImagingOrdersCommand.ExecuteAsync(null);
        vm.ImagingOrders.Should().ContainSingle();
        vm.WorkSearch = "مورد ناموجود";
        vm.QueueEmptyMessage.Should().Contain("فیلتر");
        api.CannedResponses.Remove("imaging/orders");
        await vm.LoadImagingOrdersCommand.ExecuteAsync(null);
        vm.ImagingOrders.Should().ContainSingle();
        vm.QueueStatus.Should().Contain("ناموفق").And.Contain("قدیمی");
        vm.QueueEmptyMessage.Should().Contain("ناموفق");
    }

    private static ImagingViewModel.ImagingOrderRow Order(long id, string modality) =>
        new(id, id, $"بیمار {id}", modality, "زانو", 1, 3, null, null, null, null, null, null,
            DateTime.UtcNow, null, null, null, null);

    [Fact]
    public async Task Changing_study_clears_old_safety_and_ignores_delayed_file_list()
    {
        var old = new TaskCompletionSource<object?>();
        var api = new FakeApiClient();
        api.PendingGetResponses["imaging/files?orderId=1"] = old.Task;
        api.CannedResponses["imaging/files?orderId=2"] = new { data = new[] {
            new { id = 22, fileName = "دوم.dcm", isDicom = true }
        } };
        var vm = new ImagingViewModel(api, new FakeSnackbar());
        vm.SelectedImagingOrder = Order(1, "MRI");
        vm.SafetyItems[0].IsChecked = true;
        vm.SelectedImagingOrder = Order(2, "CT");
        vm.SafetyItems.Should().OnlyContain(x => !x.IsChecked);
        vm.ImagingFiles.Should().ContainSingle(x => x.Id == 22);
        old.SetResult(new { data = new[] { new { id = 11, fileName = "اول.dcm", isDicom = true } } });
        await old.Task;
        await Task.Yield();
        vm.ImagingFiles.Should().ContainSingle(x => x.Id == 22);
        vm.SelectedImagingFile!.Id.Should().Be(22);
    }

    [Fact]
    public async Task Delayed_image_from_previous_selection_cannot_replace_current_image()
    {
        var old = new TaskCompletionSource<byte[]?>();
        var next = new TaskCompletionSource<byte[]?>();
        var api = new FakeApiClient();
        api.PendingDownloads["imaging/files/11/content"] = old.Task;
        api.PendingDownloads["imaging/files/22/content"] = next.Task;
        var vm = new ImagingViewModel(api, new FakeSnackbar());
        vm.SelectedImagingFile = new ImagingViewModel.ImagingFileRow(11, "اول.png", false, 1, null);
        vm.SelectedImagingFile = new ImagingViewModel.ImagingFileRow(22, "دوم.png", false, 1, null);
        next.SetResult(new byte[] { 2 });
        await next.Task;
        await Task.Yield();
        old.SetResult(new byte[] { 1 });
        await old.Task;
        await Task.Yield();
        vm.CurrentImageName.Should().Be("دوم.png");
        vm.CurrentImageBytes.Should().Equal(2);
    }

}
