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
    [Fact]
    public async Task Existing_visit_preserves_physical_exam_soap_and_legacy_secondary_diagnoses()
    {
        var api = new FakeApiClient();
        api.CannedResponses["visits/42"] = new { data = new { id = 42L, patientId = 7L, doctorId = 8L, receptionId = 9L,
            status = 4, physicalExamination = "یافته اصلی", soapSubjective = "شرح مستقل", soapObjective = "معاینه مستقل",
            soapAssessment = "ارزیابی مستقل", soapPlan = "برنامه مستقل", secondaryDiagnoses = "قالب قدیمی" } };
        api.CannedResponses["visits"] = new { data = 42L };
        var vm = new VisitEditViewModel(api, new FakeSnackbar(), 42);
        vm.PhysicalExamText.Should().Be("یافته اصلی");
        vm.HeadNeckNormal.Should().BeFalse();
        vm.ChiefComplaint = "شرح جدید";
        await vm.SaveDraftCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("receptionId").GetInt64().Should().Be(9);
        body.GetProperty("physicalExamination").GetString().Should().Be("یافته اصلی");
        body.GetProperty("soapSubjective").GetString().Should().Be("شرح مستقل");
        body.GetProperty("soapObjective").GetString().Should().Be("معاینه مستقل");
        body.GetProperty("secondaryDiagnoses").GetString().Should().Be("قالب قدیمی");
        vm.StopTimers();
    }

    [Fact]
    public async Task Completed_visit_is_not_saved_or_reopened()
    {
        var api = new FakeApiClient();
        api.CannedResponses["visits/42"] = new { data = new { id = 42L, patientId = 7L, doctorId = 8L, status = 3 } };
        var vm = new VisitEditViewModel(api, new FakeSnackbar(), 42);
        await vm.SaveDraftCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "visits");
        vm.StopTimers();
    }

    [Fact]
    public async Task Vital_numbers_are_validated_and_persian_digits_are_preserved()
    {
        var api = new FakeApiClient();
        api.CannedResponses["visits"] = new { data = 42L };
        var vm = new VisitEditViewModel(api, new FakeSnackbar(), 7, 8);
        vm.BpText = "۱۲۰/۸۰";
        vm.HrText = "۷۲";
        vm.TempText = "۳۷٫۵";
        vm.Spo2Text = "۹۸";
        vm.HeightText = "۱۷۰";
        await vm.SaveDraftCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }).GetProperty("vitalSigns");
        body.GetProperty("bloodPressureSystolic").GetInt32().Should().Be(120);
        body.GetProperty("heartRate").GetInt32().Should().Be(72);
        body.GetProperty("temperature").GetDecimal().Should().Be(37.5m);
        body.GetProperty("height").GetDecimal().Should().Be(170m);
        vm.Spo2Text = "۱۰۱";
        await vm.SaveDraftCommand.ExecuteAsync(null);
        api.Calls.Count(c => c.Method == "POST" && c.Endpoint == "visits").Should().Be(1);
        vm.StopTimers();
    }

    [Fact]
    public void Vital_cards_do_not_claim_clinical_normality_or_divide_by_tiny_height()
    {
        var vm = new VisitEditViewModel(new FakeApiClient(), new FakeSnackbar(), 7, 8);
        vm.HrText = "۷۲";
        vm.WeightText = "۷۰";
        vm.HeightText = "۰٫۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۱";
        vm.VitalCards.Single(x => x.Title == "HR").StatusText.Should().Contain("نیازمند ارزیابی");
        vm.BmiDisplay.Should().BeNull();
        vm.StopTimers();
    }

}
