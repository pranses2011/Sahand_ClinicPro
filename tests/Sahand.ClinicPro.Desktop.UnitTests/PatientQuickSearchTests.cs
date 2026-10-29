using FluentAssertions;
using Sahand.ClinicPro.Desktop.Helpers;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class PatientQuickSearchTests
{
    [Fact]
    public async Task Multiple_matching_patients_are_not_silently_selected()
    {
        var api = new FakeApiClient();
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] {
            new { id = 1, firstName = "علی", lastName = "احمدی" },
            new { id = 2, firstName = "علی", lastName = "رضایی" }
        } };
        var snackbar = new FakeSnackbar();
        var patient = await PatientQuickSearch.FindAsync(api, snackbar, "1234");
        patient.Should().BeNull();
        snackbar.Messages.Should().Contain(x => x.Message.Contains("مبهم"));
    }

    [Fact]
    public async Task Single_valid_match_is_returned_and_failed_request_is_not_reported_as_no_patient()
    {
        var api = new FakeApiClient { FakeError = "خطای ارتباط" };
        api.CannedResponses["patients/search?term=1234"] = new { data = new[] {
            new { id = 7, firstName = "علی", lastName = "احمدی", nationalCode = "1234", mobile = "" }
        } };
        var snackbar = new FakeSnackbar();
        (await PatientQuickSearch.FindAsync(api, snackbar, "1234"))!.Id.Should().Be(7);
        (await PatientQuickSearch.FindAsync(api, snackbar, "9999")).Should().BeNull();
        snackbar.Messages.Should().Contain(x => x.Message == "خطای ارتباط");
    }
}
