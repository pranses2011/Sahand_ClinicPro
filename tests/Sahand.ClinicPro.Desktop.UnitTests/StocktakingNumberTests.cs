using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class StocktakingNumberTests
{
    [Fact]
    public async Task Quantities_preserve_fractions_and_uncounted_values_are_not_zero()
    {
        var api = new FakeApiClient();
        api.CannedResponses["stocktakings/1"] = new { data = new { id = 1, totalShortage = 0.25m, totalSurplus = 0m, items = new[] {
            new { itemId = 5, medicationName = "دارو", systemQuantity = 12.5m, countedQuantity = 12.25m, difference = -0.25m, isCounted = true },
            new { itemId = 6, medicationName = "دیگر", systemQuantity = 3m, countedQuantity = 0m, difference = -3m, isCounted = false }
        } } };
        var vm = new StocktakingViewModel(api, new FakeSnackbar());
        vm.SelectedSession = new StockSessionRow(1, "1", "", "در حال شمارش", "", "", "", null);
        vm.Items[0].SystemQty.Should().Be("۱۲.۵");
        vm.Items[0].CountedQty.Should().Be("۱۲.۲۵");
        vm.Items[0].DiffText.Should().Be("-۰.۲۵");
        vm.Items[1].CountedQty.Should().Be("—");
        vm.SummaryText.Should().Contain("۰.۲۵");
        vm.SelectedItem = vm.Items[0];
        vm.CountInput = "۱۲٫۷۵";
        api.CannedResponses["stocktakings/1/items/5/count"] = new { isSuccess = true };
        await vm.SaveCountCommand.ExecuteAsync(null);
        JsonSerializer.SerializeToElement(api.LastBody).GetProperty("countedQuantity").GetDecimal().Should().Be(12.75m);
    }
}
