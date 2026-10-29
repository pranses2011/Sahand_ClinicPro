using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class DentistryViewModelTests
{
    private static FakeApiClient ApiWithPatient(long id = 1)
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] { new { id, firstName = "علی", lastName = "رضایی", nationalCode = "1234", mobile = "" } } };
        api.CannedResponses["dental/plans"] = new { isSuccess = true, data = 42L };
        api.CannedResponses[$"dental/chart?patientId={id}"] = new { data = new { teeth = new Dictionary<string, int>(), rows = Array.Empty<object>() } };
        api.CannedResponses["lookups/services?search=&insuranceCompanyId="] = new { data = new[] { new { serviceName = "خدمت واقعی", price = 456m } } };
        return api;
    }

    [Fact]
    public async Task Empty_chart_has_no_assumed_healthy_teeth_and_loads_actual_tariffs()
    {
        var api = ApiWithPatient();
        var vm = new DentistryViewModel(api, new FakeSnackbar()) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.IsDentalChartEnabled.Should().BeTrue();
        vm.CatalogItems.Should().ContainSingle().Which.Price.Should().Be(456m);
        vm.UpperTeeth.Should().OnlyContain(t => t.Condition == -1 && t.ConditionText == "ثبت نشده");
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        using var details = JsonDocument.Parse(body.GetProperty("planDetails").GetString()!);
        details.RootElement.GetProperty("teeth").EnumerateObject().Should().BeEmpty();
    }

    [Fact]
    public async Task Switching_chart_modes_preserves_both_dentitions_and_clinical_note()
    {
        var api = ApiWithPatient();
        var vm = new DentistryViewModel(api, new FakeSnackbar()) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.UpperTeeth.First(t => t.Number == 11).Condition = 2;
        vm.IsChildChart = true;
        vm.UpperTeeth.First(t => t.Number == 51).Condition = 1;
        vm.ClinicalNote = "یادداشت بالینی";
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        using var details = JsonDocument.Parse(JsonSerializer.SerializeToElement(api.LastBody).GetProperty("planDetails").GetString()!);
        details.RootElement.GetProperty("teeth").GetProperty("11").GetInt32().Should().Be(2);
        details.RootElement.GetProperty("teeth").GetProperty("51").GetInt32().Should().Be(1);
        details.RootElement.GetProperty("extras").GetProperty("clinical").GetString().Should().Be("یادداشت بالینی");
        details.RootElement.GetProperty("extras").GetProperty("isChildChart").GetBoolean().Should().BeTrue();
        vm.IsChildChart = false;
        vm.UpperTeeth.First(t => t.Number == 11).Condition.Should().Be(2);
    }

    [Fact]
    public async Task Failed_chart_load_blocks_saving_and_patient_clear_removes_data()
    {
        var api = ApiWithPatient();
        api.CannedResponses.Remove("dental/chart?patientId=1");
        var vm = new DentistryViewModel(api, new FakeSnackbar(), () => true) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.IsDentalChartEnabled.Should().BeFalse();
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "dental/plans");
        vm.ClinicalNote = "اطلاعات قبلی";
        vm.ClearDentalPatientCommand.Execute(null);
        vm.ClinicalNote.Should().BeEmpty();
        vm.DentalPatient.Should().BeNull();
    }

    [Fact]
    public async Task Pending_save_locks_chart_and_patient_selection()
    {
        var completion = new TaskCompletionSource<object?>();
        var api = ApiWithPatient();
        api.PendingPostResult = completion.Task;
        var vm = new DentistryViewModel(api, new FakeSnackbar()) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.ClinicalNote = "یادداشت";
        var save = vm.SaveDentalChartCommand.ExecuteAsync(null);
        vm.IsDentalChartEnabled.Should().BeFalse();
        vm.CanSelectDentalPatient.Should().BeFalse();
        vm.ClearDentalPatientCommand.Execute(null);
        vm.DentalPatient!.Id.Should().Be(1);
        completion.SetResult(null);
        await save;
        vm.IsDentalChartEnabled.Should().BeTrue();
        vm.ClinicalNote.Should().Be("یادداشت");
    }
    [Fact]
    public async Task Cancelling_patient_change_preserves_dental_form()
    {
        var api = new FakeApiClient();
        var vm = new DentistryViewModel(api, new FakeSnackbar(), () => false)
        {
            DentalPatient = new QuickPatient(1, "بیمار", "", "", null, null, null, null, 0),
            ClinicalNote = "یادداشت ذخیره‌نشده", DentalPatientSearch = "1234"
        };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.ClearDentalPatientCommand.Execute(null);
        vm.DentalPatient!.Id.Should().Be(1);
        vm.ClinicalNote.Should().Be("یادداشت ذخیره‌نشده");
        api.Calls.Should().NotContain(c => c.Endpoint.StartsWith("patients/search"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Unsupported_or_old_server_chart_cannot_be_silently_replaced(bool? canEdit)
    {
        var api = ApiWithPatient();
        api.CannedResponses["dental/chart?patientId=1"] = new { data = new { planId = 8, canEdit, note = "طرح قدیمی", teeth = new Dictionary<string, int>(), rows = Array.Empty<object>() } };
        var vm = new DentistryViewModel(api, new FakeSnackbar()) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        vm.IsDentalChartEnabled.Should().BeFalse();
        vm.UnsupportedDentalDetails.Should().Be("طرح قدیمی");
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "dental/plans");
    }

    [Fact]
    public async Task Chart_save_uses_loaded_plan_id_and_updates_it_after_success()
    {
        var api = ApiWithPatient();
        api.CannedResponses["dental/chart?patientId=1"] = new { data = new { planId = 10L, canEdit = true, teeth = new Dictionary<string, int>(), rows = Array.Empty<object>() } };
        var vm = new DentistryViewModel(api, new FakeSnackbar()) { DentalPatientSearch = "1234" };
        await vm.SearchDentalPatientCommand.ExecuteAsync(null);
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("expectedPlanId").GetInt64().Should().Be(10);
        await vm.SaveDentalChartCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("expectedPlanId").GetInt64().Should().Be(42);
    }

    [Fact]
    public void Plan_total_uses_same_currency_conversion_as_individual_prices()
    {
        var vm = new DentistryViewModel(new FakeApiClient(), new FakeSnackbar());
        vm.DentalPlan.Add(new DentalPlanRow(11, "ترمیم", 12340m, "پیشنهاد", "#FFF"));
        vm.DentalPlan.Add(new DentalPlanRow(12, "معاینه", 10000m, "پیشنهاد", "#FFF"));
        vm.PlanTotal.Should().Be(22340m);
        vm.PlanTotalText.Should().Be(Sahand.ClinicPro.Desktop.Helpers.Money.Format(22340m));
    }

}
