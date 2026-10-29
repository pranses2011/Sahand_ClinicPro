using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class SurveyViewModelTests
{
    private static SurveyListRow Row(long id, bool active = false, string? questions = null) =>
        new(id, $"نظرسنجی {id}", null, "کیفیت خدمات", active, active ? "فعال" : "غیرفعال", 0, "—", questions);

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
        body.GetProperty("questions").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("{not json}")]
    [InlineData("[{\"text\":\"سؤال\",\"type\":\"unsupported\"}]")]
    [InlineData("[{\"text\":\"سؤال\",\"type\":\"rating\",\"required\":true}]")]
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

}
