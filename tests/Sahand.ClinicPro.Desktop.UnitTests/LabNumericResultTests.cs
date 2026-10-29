using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class LabNumericResultTests
{
    [Theory]
    [InlineData("۱۲٫۵", 12.5)]
    [InlineData("۱۲/۵", 12.5)]
    [InlineData("١٢.٥", 12.5)]
    [InlineData("−۰٫۰۵", -0.05)]
    public void Numeric_results_parse_without_money_conversion(string input, double expected)
    {
        LabNumericResult.TryParse(input, out var result).Should().BeTrue();
        result.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("1,234")]
    [InlineData("1٬234")]
    [InlineData("1.2.3")]
    [InlineData("abc")]
    public void Ambiguous_and_invalid_inputs_are_rejected(string input)
        => LabNumericResult.TryParse(input, out _).Should().BeFalse();

    [Fact]
    public void Printed_numeric_value_keeps_fraction_and_does_not_use_currency_formatting()
    {
        LabNumericResult.Format(12.55m).Should().Be("۱۲.۵۵");
        LabNumericResult.Format(0.01m).Should().Be("۰.۰۱");
        LabNumericResult.Format(-0.125m).Should().Be("-۰.۱۲۵");
    }

    [Fact]
    public async Task Detailed_result_form_sends_persian_decimal_as_exact_numeric_value()
    {
        var api = new FakeApiClient();
        var vm = new LabTestResultEntryViewModel(api, new LabItemRow { Id = 5, TestName = "نمونه" }, 1, "بیمار", "", "")
            { ResultNumeric = "۱۲٫۵۵" };
        await vm.SaveCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("resultNumeric").GetDecimal().Should().Be(12.55m);
    }

    [Fact]
    public void Missing_interpretation_is_not_presented_as_normal()
    {
        var item = new LabItemRow { Id = 2, TestName = "کیفی", Result = "منفی", AbnormalFlag = null };
        var vm = new LabTestResultEntryViewModel(new FakeApiClient(), item, 1, "بیمار", "", "");
        vm.SavedFlagText.Should().Contain("طبیعی بودن تأیید نشده");
        vm.SavedFlagBrush.Should().NotBe("#43A047");
    }
}
