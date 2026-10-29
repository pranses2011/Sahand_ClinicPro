using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class ComplementarySurveyDesignerTests
{
    [Fact]
    public async Task Legacy_designer_sends_version_and_preserves_unchanged_question_json()
    {
        const string questions = "[{ \"q\": \"پرسش؟\", \"type\": \"text\" }]";
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new ComplementaryViewModel(api, new FakeSnackbar());
        var row = new SurveyRow { Id = 8, Title = "عنوان اصلی", QuestionsJson = questions, EditVersion = "version-8" };
        vm.OpenSurveyDesignerCommand.Execute(row);
        vm.IsSurveyDesignerOpen.Should().BeTrue();
        vm.DesignerTitle = "عنوان جدید";
        await vm.SaveSurveyDesignerCommand.ExecuteAsync(null);
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("editVersion").GetString().Should().Be("version-8");
        body.GetProperty("questions").GetString().Should().Be(questions);
    }

    [Theory]
    [InlineData("{broken", "version")]
    [InlineData("[]", null)]
    public void Legacy_designer_blocks_unsupported_or_unversioned_data(string questions, string? version)
    {
        var api = new FakeApiClient();
        api.CannedResponses["surveys"] = new { data = Array.Empty<object>() };
        var vm = new ComplementaryViewModel(api, new FakeSnackbar());
        vm.OpenSurveyDesignerCommand.Execute(new SurveyRow { Id = 8, Title = "عنوان", QuestionsJson = questions, EditVersion = version });
        vm.IsSurveyDesignerOpen.Should().BeFalse();
    }
}
