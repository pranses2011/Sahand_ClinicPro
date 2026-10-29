using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PhysiotherapyViewModelTests
{
    private static QuickPatient Patient(long id) => new(id, $"بیمار {id}", "", "", null, null, null);

    [Fact]
    public void Patient_change_clears_prior_clinical_information_and_example_defaults()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        api.CannedResponses["physiotherapy/sessions?patientId=2"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.Assessments.Should().OnlyContain(x => x.BaseValue == null && x.NowValue == null);
        vm.SetZonePainCommand.Execute(7);
        vm.PhysioNotes = "یادداشت"; vm.HomeProgram = "برنامه"; vm.RedFlags[0].IsChecked = true;
        vm.TechniqueItems[0].Minutes = 33; vm.TechniqueItems[0].IsSelected = true;
        vm.Assessments[0].BaseValue = 8; vm.Assessments[0].NowValue = 4;
        vm.PhysioPatient = Patient(2);
        vm.ZonePain.Should().BeEmpty(); vm.RecordPainScore.Should().BeFalse();
        vm.PhysioNotes.Should().BeNull(); vm.HomeProgram.Should().BeNull(); vm.HasRedFlag.Should().BeFalse();
        vm.TechniqueItems[0].IsSelected.Should().BeFalse(); vm.TechniqueItems[0].Minutes.Should().Be(15);
        vm.Assessments.Should().OnlyContain(x => x.BaseValue == null && x.NowValue == null);
        vm.TotalImprovementText.Should().Be("—");
    }

    [Fact]
    public async Task Missing_pain_and_assessments_are_saved_as_null_not_zero()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("painScore").ValueKind.Should().Be(JsonValueKind.Null);
        using var note = JsonDocument.Parse(body.GetProperty("progressNote").GetString()!);
        note.RootElement.GetProperty("flexion").ValueKind.Should().Be(JsonValueKind.Null);
        note.RootElement.GetProperty("assessments")[0].GetProperty("base").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Failed_history_blocks_saving_and_recent_history_uses_maximum_session_number()
    {
        var api = new FakeApiClient();
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.IsPhysioSessionEnabled.Should().BeFalse();
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "physiotherapy/sessions");
        api.CannedResponses["physiotherapy/sessions?patientId=2"] = new { data = new[] { new { sessionNumber = 23, painScore = (byte?)null }, new { sessionNumber = 25, painScore = (byte?)null } } };
        vm.PhysioPatient = Patient(2);
        vm.SessionNumber.Should().Be(26);
        vm.PainTrendPoints.Should().BeEmpty();
        vm.IsPhysioSessionEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Session_save_locks_inputs_and_preserves_notes_on_failure()
    {
        var completion = new TaskCompletionSource<bool>();
        var api = new FakeApiClient { PendingBoolPostResult = completion.Task };
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1), PhysioNotes = "یادداشت فعلی" };
        var save = vm.SavePhysioSessionCommand.ExecuteAsync(null);
        vm.IsPhysioSessionEnabled.Should().BeFalse();
        vm.CanSelectPhysioPatient.Should().BeFalse();
        await vm.SearchPhysioPatientCommand.ExecuteAsync(null);
        vm.PhysioPatient!.Id.Should().Be(1);
        completion.SetResult(false);
        await save;
        vm.IsPhysioSessionEnabled.Should().BeTrue();
        vm.PhysioNotes.Should().Be("یادداشت فعلی");
    }
    [Fact]
    public async Task Explicit_zero_pain_on_body_map_is_kept_in_session_snapshot()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.SelectZone("زانو راست");
        vm.SetZonePainCommand.Execute(0);
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("painScore").GetInt32().Should().Be(0);
        using var note = JsonDocument.Parse(body.GetProperty("progressNote").GetString()!);
        note.RootElement.GetProperty("bodyMap").GetProperty("زانو راست").GetInt32().Should().Be(0);
        note.RootElement.GetProperty("bodyMap").TryGetProperty("زانو چپ", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_numeric_fields_and_implausible_durations_cannot_be_saved()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.HasInvalidPhysioInput = true;
        vm.CanEditPhysioSession.Should().BeTrue("کاربر باید بتواند مقدار اشتباه را اصلاح کند");
        vm.IsPhysioSessionEnabled.Should().BeFalse();
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        vm.HasInvalidPhysioInput = false;
        vm.TechniqueItems[0].IsSelected = true;
        vm.TechniqueItems[0].Minutes = int.MaxValue;
        vm.TechniqueItems[1].IsSelected = true;
        vm.TechniqueItems[1].Minutes = int.MaxValue;
        vm.TotalDuration.Should().Be(2L * int.MaxValue);
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        vm.TechniqueItems[0].Minutes = 10;
        vm.TechniqueItems[1].Minutes = 10;
        vm.Assessments[0].NowValue = -1;
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "physiotherapy/sessions");
    }

    [Fact]
    public async Task Cancelling_patient_change_preserves_physiotherapy_form()
    {
        var api = new FakeApiClient();
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar(), () => false)
        {
            PhysioPatient = new QuickPatient(1, "بیمار", "", "", null, null, null, null, 0),
            PhysioNotes = "یادداشت ذخیره‌نشده", PhysioPatientSearch = "1234"
        };
        await vm.SearchPhysioPatientCommand.ExecuteAsync(null);
        vm.PhysioPatient!.Id.Should().Be(1);
        vm.PhysioNotes.Should().Be("یادداشت ذخیره‌نشده");
        api.Calls.Should().NotContain(c => c.Endpoint.StartsWith("patients/search"));
    }

    [Fact]
    public async Task Next_session_uses_server_max_not_only_recent_rows()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { nextSessionNumber = 101, data = new[] { new { sessionNumber = 25 } } };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.SessionNumber.Should().Be(101);
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("sessionNumber").GetInt32().Should().Be(101);
    }

    [Fact]
    public async Task Previous_measurement_is_not_silently_saved_as_current_session()
    {
        var api = new FakeApiClient();
        var note1 = JsonSerializer.Serialize(new { assessments = new[] { new { name = "مقیاس درد (VAS)", @base = (double?)null, now = (double?)8 } } });
        var note2 = JsonSerializer.Serialize(new { assessments = new[] { new { name = "مقیاس درد (VAS)", @base = (double?)null, now = (double?)5 } } });
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = new[] {
            new { sessionNumber = 1, dateShamsi = "1405/01/01", progressNote = note1 },
            new { sessionNumber = 2, dateShamsi = "1405/01/02", progressNote = note2 },
            new { sessionNumber = 3, dateShamsi = "1405/01/03", progressNote = "{}" } } };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        var row = vm.Assessments[0];
        row.BaseValue.Should().Be(8);
        row.LastValue.Should().Be(5);
        row.LastDate.Should().Be("1405/01/02");
        row.NowValue.Should().BeNull();
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        using var note = JsonDocument.Parse(JsonSerializer.SerializeToElement(api.LastBody).GetProperty("progressNote").GetString()!);
        note.RootElement.GetProperty("assessments")[0].GetProperty("now").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Successful_session_does_not_reuse_pain_techniques_or_current_assessments()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        api.CannedResponses["physiotherapy/sessions"] = true;
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.SetZonePainCommand.Execute(7);
        vm.TechniqueItems[0].IsSelected = true;
        vm.RedFlags[0].IsChecked = true;
        vm.Assessments[0].NowValue = 7;
        await vm.SavePhysioSessionCommand.ExecuteAsync(null);
        vm.RecordPainScore.Should().BeFalse();
        vm.ZonePain.Should().BeEmpty();
        vm.TechniqueItems.Should().OnlyContain(x => !x.IsSelected);
        vm.RedFlags.Should().OnlyContain(x => !x.IsChecked);
        vm.Assessments.Should().OnlyContain(x => x.NowValue == null);
    }

    [Fact]
    public void Referral_report_uses_current_values_and_does_not_claim_a_history_average()
    {
        var api = new FakeApiClient();
        api.CannedResponses["physiotherapy/sessions?patientId=1"] = new { data = Array.Empty<object>() };
        var vm = new PhysiotherapyViewModel(api, new FakeSnackbar()) { PhysioPatient = Patient(1) };
        vm.Assessments[0].BaseValue = 8;
        vm.Assessments[0].NowValue = 4;
        vm.GenerateReportCommand.Execute(null);
        vm.ReferralReport.Should().Contain("50٪").And.Contain("تعداد نامشخص").And.NotContain("میانگین تغییر");
        vm.Assessments[0].NowValue = 3;
        vm.ReferralReport.Should().BeNull();
        vm.Assessments[0].NowValue = -1;
        vm.GenerateReportCommand.Execute(null);
        vm.ReferralReport.Should().BeNull();
    }

    [Fact]
    public void Nonfinite_calculated_percentage_is_not_reported_as_a_number()
    {
        var row = new PhysioAssessmentRow { BaseValue = double.Epsilon, NowValue = 1, Reverse = true };
        row.Improvement.Should().BeNull();
        row.ImprovementText.Should().Be("—");
    }

}
