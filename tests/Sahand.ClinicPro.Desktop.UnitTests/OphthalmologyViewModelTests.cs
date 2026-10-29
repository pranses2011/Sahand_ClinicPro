using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class OphthalmologyViewModelTests
{
    private static QuickPatient Patient(long id) => new(id, $"بیمار {id}", "", "", null, null, null, null, 0);

    private static async Task<OphthalmologyViewModel> Ready(FakeApiClient api, long id)
    {
        var term = id.ToString("D4");
        api.CannedResponses[$"patients/search?term={term}"] = new { data = new[] { new { id, firstName = "بیمار", lastName = "آزمایشی", nationalCode = term, mobile = "" } } };
        api.CannedResponses[$"eye/exams?patientId={id}"] = new { data = Array.Empty<object>() };
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar()) { EyePatientSearch = term };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        return vm;
    }

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
        var vm = await Ready(api, 3);
        vm.OdSphere = "−۲٫۵"; vm.OdIop = "۲۳"; vm.Pd = "۶۲";
        vm.OdIopWarning.Should().Contain("به‌تنهایی تشخیص نیست").And.NotContain("احتمال گلوکوم");
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
        var vm = await Ready(api, 3);
        vm.OdIop = "نامعتبر";
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
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar(), confirmCopyPrescription: () => true) { EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.EyePatient!.Id.Should().Be(7);
        vm.VisualFieldText.Should().BeNull();
        vm.OdIop.Should().BeNull(); vm.OdVisionIndex.Should().Be(-1);
        vm.EyeHistory.Should().ContainSingle().Which.VisualField.Should().Be("میدان بینایی");
        vm.CopyPreviousRefractionCommand.Execute(null);
        vm.OdAdd.Should().Be("+1.5"); vm.OsAdd.Should().Be("+2");
        vm.OdSphere.Should().Be("-2"); vm.OdIop.Should().BeNull(); vm.OdVisionIndex.Should().Be(-1);
    }


    [Fact]
    public async Task Existing_clinical_findings_round_trip_without_extra_prefix_and_history_reloads()
    {
        const string findings = "یافته‌ها: بررسی اول\nاسلیت‌لمپ/فوندوس راست: بدون ضایعه";
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] { new { id = 7, firstName = "علی", lastName = "رضایی", nationalCode = "1234", mobile = "" } } };
        api.CannedResponses["eye/exams?patientId=7"] = new { data = new[] { new { findings, dateShamsi = "1405/01/01" } } };
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar()) { EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.EyeFindings.Should().BeNull();
        vm.EyeHistory.Should().ContainSingle().Which.Findings.Should().Be(findings);
        vm.EyeFindings = "یافته جدید";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("findings").GetString().Should().Be("یافته جدید");
        vm.EyeFindings.Should().BeNull();
        vm.EyeHistory.Should().ContainSingle();
        api.Calls.Count(c => c.Method == "GET" && c.Endpoint == "eye/exams?patientId=7").Should().Be(2);
    }

    [Fact]
    public async Task Exam_save_locks_patient_switch_and_releases_after_failure()
    {
        var completion = new TaskCompletionSource<bool>();
        var api = new FakeApiClient { PendingPostResult = null };
        // این مسیر POST بولی است، پاسخ معوق جداگانه دارد.
        api.PendingBoolPostResult = completion.Task;
        var vm = await Ready(api, 7);
        vm.EyeFindings = "یافته";
        var save = vm.SaveEyeExamCommand.ExecuteAsync(null);
        vm.IsEyeExamEnabled.Should().BeFalse();
        vm.CanSelectEyePatient.Should().BeFalse();
        vm.ClearEyePatientCommand.Execute(null);
        vm.EyePatient!.Id.Should().Be(7);
        completion.SetResult(false);
        await save;
        vm.IsEyeExamEnabled.Should().BeTrue();
        vm.EyeFindings.Should().Be("یافته");
    }

    [Fact]
    public async Task Empty_exam_is_not_saved()
    {
        var api = new FakeApiClient();
        var vm = await Ready(api, 7);
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "eye/exams");
    }

    [Fact]
    public async Task Failed_history_blocks_exam_until_reloaded()
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] { new { id = 7, firstName = "علی", lastName = "رضایی", nationalCode = "1234", mobile = "" } } };
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar(), () => true) { EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.IsEyeExamEnabled.Should().BeFalse();
        vm.EyeFindings = "یافته";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "eye/exams");
        api.CannedResponses["eye/exams?patientId=7"] = new { data = Array.Empty<object>() };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.IsEyeExamEnabled.Should().BeTrue();
    }
    [Fact]
    public async Task Cancelling_patient_change_preserves_eye_form()
    {
        var api = new FakeApiClient();
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar(), () => false) { EyePatient = Patient(1), EyeFindings = "یادداشت ذخیره‌نشده", EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.ClearEyePatientCommand.Execute(null);
        vm.EyePatient!.Id.Should().Be(1);
        vm.EyeFindings.Should().Be("یادداشت ذخیره‌نشده");
        api.Calls.Should().NotContain(c => c.Endpoint.StartsWith("patients/search"));
    }

    [Fact]
    public async Task Out_of_range_or_silently_rounded_measurements_are_not_sent()
    {
        var api = new FakeApiClient();
        var vm = await Ready(api, 3);
        vm.OdAxis = "۱۸۱";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        vm.OdAxis = "۹۰";
        vm.OdSphere = "۱٫۲۳۴";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        vm.OdSphere = "۱٫۲۵";
        vm.Pd = "۶۲٫۵۵";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        vm.Pd = "۶۲";
        vm.OdAdd = "۱٫۲۳۴";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "eye/exams");
    }

    [Fact]
    public async Task Refraction_copy_can_be_cancelled_and_successful_exam_clears_form()
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] { new { id = 7, firstName = "بیمار", lastName = "تست", nationalCode = "1234", mobile = "" } } };
        api.CannedResponses["eye/exams?patientId=7"] = new { data = new[] { new { od = new { sphere = -2m, iop = 23 }, findings = "یافته قدیمی" } } };
        var vm = new OphthalmologyViewModel(api, new FakeSnackbar(), confirmCopyPrescription: () => false) { EyePatientSearch = "1234" };
        await vm.SearchEyePatientCommand.ExecuteAsync(null);
        vm.OdSphere = "-1";
        vm.CopyPreviousRefractionCommand.Execute(null);
        vm.OdSphere.Should().Be("-1");
        vm.OdIop = "15";
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        vm.OdSphere.Should().BeNull();
        vm.OdIop.Should().BeNull();
        vm.EyeHistory.Should().ContainSingle().Which.Findings.Should().Be("یافته قدیمی");
        await vm.SaveEyeExamCommand.ExecuteAsync(null);
        api.Calls.Count(c => c.Method == "POST-bool" && c.Endpoint == "eye/exams").Should().Be(1);
    }

}
