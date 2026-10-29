using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class SurveyViewModelTests
{
    private static SurveyListRow Row(long id, bool active = false, string? questions = null) =>
        new(id, $"نظرسنجی {id}", null, "کیفیت خدمات", active, active ? "فعال" : "غیرفعال", 0, "—", questions, "VERSION-1");

    [Fact]
    public async Task Editing_inactive_survey_preserves_status_and_type()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { SelectedSurvey = Row(4, questions: "[]") };
        vm.OpenEditFormCommand.Execute(null);
        vm.FormType.Should().Be("کیفیت خدمات");
        await vm.SaveFormCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("isActive").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("surveyType").GetInt32().Should().Be(2);
        body.GetProperty("editVersion").GetString().Should().Be("VERSION-1");
        body.GetProperty("questions").GetString().Should().Be("[]");
    }

    [Theory]
    [InlineData("{not json}")]
    [InlineData("[{\"text\":\"سؤال\",\"type\":\"unsupported\"}]")]
    [InlineData("[{\"text\":\"سؤال\",\"type\":\"rating\",\"required\":true}]")]
    [InlineData("[{\"text\":\"اول\",\"text\":\"دوم\",\"type\":\"text\"}]")]
    public void Unsupported_questions_do_not_open_edit_form(string questions)
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { SelectedSurvey = Row(1, questions: questions) };
        vm.OpenEditFormCommand.Execute(null);
        vm.IsFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Empty_results_do_not_throw_and_old_results_clear_on_selection_change()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        api.CannedResponses["surveys/1/results"] = new { data = new { summary = new { totalResponses = 3, averageRating = 4, responsesLast30Days = 2 }, starDistribution = Array.Empty<object>(), doctorRatings = Array.Empty<object>(), recentComments = Array.Empty<object>() } };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { ResultsSurvey = Row(1) };
        await vm.LoadResultsAsync();
        vm.TotalResponsesText.Should().NotBe("—");
        vm.DoctorRatings.Should().BeEmpty();
        vm.ResultsSurvey = Row(2);
        vm.TotalResponsesText.Should().Be("—");
        vm.AverageText.Should().Be("—");
        vm.ResultsMessage.Should().Contain("هنوز دریافت نشده");
    }
    [Fact]
    public async Task Internal_response_submits_snapshot_and_answers_anonymously()
    {
        const string questions = "[{\"text\":\"امتیاز؟\",\"type\":\"rating\"},{\"text\":\"شرح؟\",\"type\":\"text\"}]";
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { SelectedSurvey = Row(9, active: true, questions: questions) };
        vm.OpenResponseFormCommand.Execute(null);
        vm.IsResponseFormOpen.Should().BeTrue();
        vm.ResponseAnswers[0].Answer = "5";
        vm.ResponseAnswers[1].Answer = "خوب";
        await vm.SubmitSurveyResponseCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("patientId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("questionsSnapshot").GetString().Should().Be(questions);
        body.GetProperty("answers")[1].GetString().Should().Be("خوب");
        vm.IsResponseFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Internal_response_rejects_missing_answer()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { SelectedSurvey = Row(9, active: true, questions: "[{\"text\":\"امتیاز؟\",\"type\":\"rating\"}]") };
        vm.OpenResponseFormCommand.Execute(null);
        await vm.SubmitSurveyResponseCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "surveys/9/responses");
    }


    [Fact]
    public void List_search_status_and_real_summary_are_consistent()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = new[] {
            new { id = 1, title = "کیفیت پذیرش", description = "برخورد", type = "Service", isActive = true, responseCount = 7, averageRating = 4.0, questions = "[]" },
            new { id = 2, title = "ارزیابی پزشک", description = "خدمات", type = "Doctor", isActive = false, responseCount = 12, averageRating = 4.0, questions = "[{\"text\":\"پرسش\",\"type\":\"text\"}]" }
        } };
        var vm = new SurveyViewModel(api, new FakeSnackbar());
        vm.SurveyCountText.Should().Be("۲");
        vm.ActiveSurveyCountText.Should().Be("۱");
        vm.SurveyResponseCountText.Should().Be("۱۹");
        vm.VisibleSurveys.Should().HaveCount(2);
        vm.VisibleSurveys[1].QuestionCountText.Should().Be("۱");
        vm.SelectedSurvey = vm.VisibleSurveys[0];
        vm.SurveyStatusIndex = 2;
        vm.VisibleSurveys.Should().ContainSingle(s => s.Id == 2);
        vm.SelectedSurvey.Should().BeNull();
        vm.SurveySearch = "برخورد";
        vm.VisibleSurveys.Should().BeEmpty();
        vm.SurveyStatusIndex = 0;
        vm.VisibleSurveys.Should().ContainSingle(s => s.Id == 1);
        // فیلتر، دادهٔ اصلی یا آمار کل را تغییر نمی‌دهد.
        vm.Surveys.Should().HaveCount(2);
        vm.SurveyResponseCountText.Should().Be("۱۹");
    }

    [Fact]
    public async Task Survey_editor_is_locked_while_save_is_pending()
    {
        var completion = new TaskCompletionSource<bool>();
        var api = new FakeApiClient { PendingPutResult = completion.Task };
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var snackbar = new FakeSnackbar();
        var vm = new SurveyViewModel(api, snackbar) { SelectedSurvey = Row(4, questions: "[]") };
        vm.OpenEditFormCommand.Execute(null);
        vm.FormTitle = "عنوان ویرایش‌شده";
        var save = vm.SaveFormCommand.ExecuteAsync(null);
        vm.CanEditSurvey.Should().BeFalse();
        vm.OpenCreateFormCommand.Execute(null);
        vm.FormTitle.Should().Be("عنوان ویرایش‌شده");
        completion.SetResult(false);
        await save;
        vm.CanEditSurvey.Should().BeTrue();
        vm.IsFormOpen.Should().BeTrue();
        vm.FormTitle.Should().Be("عنوان ویرایش‌شده");
    }
    [Fact]
    public async Task Survey_editor_limits_questions_and_does_not_submit_oversized_text()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar());
        vm.OpenCreateFormCommand.Execute(null);
        vm.FormTitle = "نظرسنجی";
        vm.Questions[0].Text = new string('x', 2001);
        await vm.SaveFormCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST-bool" && c.Endpoint == "surveys");
        vm.IsFormOpen.Should().BeTrue();
        while (vm.Questions.Count < 100) vm.AddQuestionCommand.Execute(null);
        vm.AddQuestionCommand.Execute(null);
        vm.Questions.Should().HaveCount(100);
    }

    [Fact]
    public void Editing_without_server_version_is_blocked()
    {
        var vm = new SurveyViewModel(new FakeApiClient(), new FakeSnackbar())
        { SelectedSurvey = Row(4, questions: "[]") with { EditVersion = null } };
        vm.OpenEditFormCommand.Execute(null);
        vm.IsFormOpen.Should().BeFalse();
    }

    [Fact]
    public async Task Title_only_edit_preserves_exact_legacy_question_json()
    {
        const string original = "[{ \"q\": \"انتخاب؟\", \"type\": \"choice\", \"options\": \"الف|ب\" }]";
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new SurveyViewModel(api, new FakeSnackbar()) { SelectedSurvey = Row(4, questions: original) };
        vm.OpenEditFormCommand.Execute(null);
        vm.FormTitle = "عنوان جدید";
        await vm.SaveFormCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("questions").GetString().Should().Be(original);
    }

    [Fact]
    public async Task Late_survey_list_response_cannot_replace_newer_data()
    {
        var first = new TaskCompletionSource<object?>();
        var second = new TaskCompletionSource<object?>();
        var api = new FakeApiClient();
        api.PendingGetResponses["surveys"] = first.Task;
        var vm = new SurveyViewModel(api, new FakeSnackbar());
        api.PendingGetResponses["surveys"] = second.Task;
        var refresh = vm.LoadSurveysAsync();
        second.SetResult(new { data = new[] { new { id = 2, title = "فهرست جدید", type = "Service", editVersion = "NEW" } } });
        await refresh;
        first.SetResult(new { data = new[] { new { id = 1, title = "فهرست قدیمی", type = "Service", editVersion = "OLD" } } });
        await Task.Yield();
        vm.Surveys.Should().ContainSingle().Which.Title.Should().Be("فهرست جدید");
        vm.SurveyListMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task Failed_list_refresh_marks_old_data_as_stale_and_counts_unknown()
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = new[] { new { id = 1, title = "قدیمی", type = "Service" } } };
        var vm = new SurveyViewModel(api, new FakeSnackbar());
        api.CannedResponses.Remove("surveys");
        await vm.LoadSurveysAsync();
        vm.SurveyCountText.Should().Be("—");
        vm.SurveyListMessage.Should().Contain("قدیمی");
        vm.Surveys.Should().ContainSingle();
    }

}
