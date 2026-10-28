using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class OphthalmologyViewModelTests
{
    private static QuickPatient Patient(long id) => new(id, $"بیمار {id}", "", "", null, null, null, null, 0);

    [Fact]
    public void Switching_patients_clears_all_previous_clinical_values_and_defaults()
    {
        var vm = new OphthalmologyViewModel(new FakeApiClient(), new FakeSnackbar()) { EyePatient = Patient(1) };
        vm.OdSphere = "-2"; vm.OdAdd = "+1"; vm.OdVisionIndex = 2;
        vm.ColorVisionIndex = 0; vm.EyeFindings = "یافته"; vm.SlitOd = "تست";
        vm.VisualFieldText = "میدان"; vm.CarePlanText = "درمان"; vm.HistoryTrendText = "روند";
        vm.EyePatient = Patient(2);
        vm.OdSphere.Should().BeNull(); vm.OdAdd.Should().BeNull(); vm.EyeFindings.Should().BeNull();
        vm.SlitOd.Should().BeNull(); vm.VisualFieldText.Should().BeNull(); vm.CarePlanText.Should().BeNull();
        vm.HistoryTrendText.Should().BeEmpty();
        vm.OdVisionIndex.Should().Be(-1); vm.OsVisionIndex.Should().Be(-1); vm.ColorVisionIndex.Should().Be(-1);
    }

    [Fact]
    public async Task Persian_numeric_input_is_sent_as_numbers_and_unselected_vision_is_not_assumed_normal()
    {
        var api = new FakeApiClient();
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar()) { EyePatient = Patient(3), OdSphere = "−۲٫۵", OdIop = "۲۳", Pd = "۶۲" };
        vm.OdIopWarning.Should().NotBeNull();
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("rightSphere").GetDecimal().Should().Be(-2.5m);
        body.GetProperty("rightIop").GetInt32().Should().Be(23);
        body.GetProperty("pupilDistance").GetDecimal().Should().Be(62m);
        body.GetProperty("rightVisualAcuity").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("colorVision").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Invalid_numeric_input_does_not_create_exam()
    {
        var api = new FakeApiClient();
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar()) { EyePatient = Patient(3), OdIop = "نامعتبر" };
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Endpoint == "eye/exams" && c.Method == "POST-bool");
    }
    [Fact]
    public async Task Search_loads_previous_field_and_saved_add_for_the_selected_patient()
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] { new { id = 7, firstName = "علی", lastName = "رضایی", nationalCode = "1234", mobile = "" } } };
        api.CannedResponses["eye/exams?patientId=7"] = new { data = new[] { new {
            od = new { sphere = -2m, cylinder = -1m, axis = 90, acuity = "20/40", iop = 19 },
            os = new { sphere = -1m, cylinder = -0.5m, axis = 80, acuity = "20/20", iop = 18 },
            visualField = "میدان بینایی", eyewearPrescription = "OD: SPH -2 · CYL -1 · AXIS 90 · ADD +1.5\nOS: SPH -1 · CYL -0.5 · AXIS 80 · ADD +2\nPD: 62 mm"
        } } };
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar()) { EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.EyePatient!.Id.Should().Be(7);
        vm.VisualFieldText.Should().Be("میدان بینایی");
        vm.OdAdd.Should().Be("+1.5"); vm.OsAdd.Should().Be("+2");
        vm.OdVisionIndex.Should().Be(Array.IndexOf(vm.VisionOptions, "20/40"));
        vm.EyeHistory.Should().ContainSingle();
    }

}
