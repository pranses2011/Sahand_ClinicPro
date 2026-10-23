using FluentAssertions;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>رگرسیون‌های تبدیل/نمایش دادهٔ گزارش‌ها</summary>
public sealed class ReportBrowserViewModelTests
{
    [Fact]
    public async Task Execute_ShouldKeepRowShapeAndNotFormatCountsAsCurrency()
    {
        var api = new FakeApiClient();
        api.CannedResponses["reports/execute/visit-patterns"] = new
        {
            isSuccess = true,
            data = new
            {
                available = true,
                columns = new[] { "تاریخ", "تعداد ویزیت", "مبلغ" },
                rows = new[]
                {
                    new
                    {
                        date = "2026-10-22T00:00:00Z",
                        shamsi = "۱۴۰۵/۰۷/۳۰",
                        visits = 12_345,
                        amount = 50_000m
                    }
                }
            }
        };

        var currency = typeof(Money).GetProperty(nameof(Money.CurrencyUnit))!;
        var previousCurrency = currency.GetValue(null);
        currency.SetValue(null, "Toman");
        try
        {
            var vm = new ReportBrowserViewModel(api, new FakeSnackbar())
            {
                SelectedReport = new ReportCatalogItem(
                    "visit-patterns", "الگوی مراجعات", "", new List<string>())
            };

            await vm.ExecuteCommand.ExecuteAsync(null);

            vm.Columns.Should().HaveCount(3);
            vm.Rows.Should().ContainSingle();
            vm.Rows[0].Cells.Should().HaveCount(3,
                "هر ردیف باید دقیقاً با تعداد سرستون‌های گزارش هم‌اندازه باشد");
            vm.Rows[0].Cells[0].Text.Should().Be("۱۴۰۵/۰۷/۳۰",
                "وقتی تاریخ میلادی و شمسی هر دو موجودند، ستون تاریخ باید شمسی را نشان دهد");
            vm.Rows[0].Cells[1].Text.Should().Be("۱۲٬۳۴۵",
                "تعداد ویزیت یک شمارنده است و نباید با واحد تومان تقسیم شود");
            vm.Rows[0].Cells[2].Text.Should().Be("۵٬۰۰۰",
                "مبلغ باید مطابق واحد نمایشی تومان تبدیل شود");
        }
        finally
        {
            currency.SetValue(null, previousCurrency);
        }
    }
}
