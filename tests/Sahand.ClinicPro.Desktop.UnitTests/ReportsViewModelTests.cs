using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست رگرسیون چرخهٔ ۵۰ (BUG-03) — صفحهٔ گزارش‌ها باید کاملاً ناهمگام بماند:
/// LoadAsync دو درخواست موازی می‌زند و نتیجهٔ هر دو را با await می‌خواند.
/// پیش‌تر نتیجهٔ Taskها به‌صورت هم‌گام و مسدودکننده خوانده می‌شد که در صورت
/// خطای شبکه، استثنای درون‌پیچیده به رشتهٔ UI می‌رساند و صفحه را هنگ می‌زد.
/// گیت ۵ اسکنر (scripts/security/scan_secrets.sh) همین الگو را برای همیشه می‌بندد.
/// </summary>
public class ReportsViewModelTests
{
    // ═══════════════ ۱) بارگذاری موفق هر دو گزارش ═══════════════

    /// <summary>
    /// هر دو پاسخ آماده باید نمودار و جمع‌ها را پر کنند — خواندنِ نتیجه با
    /// await پس از Task.WhenAll سالم است و هیچ نقطهٔ هم‌گام‌ساز ندارد.
    /// </summary>
    [Fact]
    public async Task LoadAsync_ShouldPopulateChartsAndTotalsFromBothResponses()
    {
        var api = new FakeApiClient();
        api.CannedResponses["reports/daily-revenue?days=30"] = new ReportsViewModel.RevenueResponse
        {
            IsSuccess = true,
            Data = new()
            {
                new() { DateShamsi = "۱۴۰۴/۰۷/۲۰", Total = 1_000_000m, Paid = 600_000m, Remaining = 400_000m },
                new() { DateShamsi = "۱۴۰۴/۰۷/۲۱", Total = 500_000m, Paid = 500_000m, Remaining = 0m },
            }
        };
        api.CannedResponses["reports/doctor-performance?days=30"] = new ReportsViewModel.PerformanceResponse
        {
            IsSuccess = true,
            Data = new()
            {
                new() { DoctorId = 1, DoctorName = "دکتر آزمون", VisitsCount = 7, TotalRevenue = 1_500_000m, DoctorShare = 500_000m },
            }
        };

        var vm = new ReportsViewModel(api);
        await vm.LoadAsync();

        // نمودار درآمد و جمع‌های سه‌گانه از پاسخ نخست
        vm.Revenue.Should().HaveCount(2, because: "دو روز دادهٔ درآمد فرستاده شد");
        vm.TotalRevenue.Should().Be(1_500_000m);
        vm.TotalPaid.Should().Be(1_100_000m);
        vm.TotalRemaining.Should().Be(400_000m);
        // نمودار عملکرد و جمع ویزیت‌ها از پاسخ دوم
        vm.Performance.Should().HaveCount(1, because: "یک پزشک در پاسخ عملکرد هست");
        vm.TotalVisits.Should().Be(7);
        // بدون خطا — IsBusy هم در پایان پایین آمده
        vm.ErrorMessage.Should().BeEmpty();
        vm.IsBusy.Should().BeFalse();
    }

    // ═══════════════ ۲) شکست پاسخ = پیام فارسی، نه استثنا ═══════════════

    /// <summary>
    /// نبود پاسخ باید به پیام خطای فارسی روی صفحه ختم شود — نه استثنای
    /// هم‌گامِ روی رشتهٔ UI (رفع چرخهٔ ۵۰ برای همیشه).
    /// </summary>
    [Fact]
    public async Task LoadAsync_WhenRevenueFails_ShouldShowPersianErrorNotCrash()
    {
        var api = new FakeApiClient
        {
            FakeError = "خطای شبیه‌سازی‌شدهٔ شبکه",
        };
        // هیچ پاسخ آماده‌ای ثبت نشده — هر دو فراخوانی null برمی‌گردانند

        var vm = new ReportsViewModel(api);
        // عملیات — پیش از چرخهٔ ۵۰ همین مسیر در بدترین حالت استثنای هم‌گام می‌داد
        await vm.LoadAsync();

        // نتیجه: خطای فارسی روی صفحه؛ هیچ کرشی در جریان نیست
        vm.ErrorMessage.Should().NotBeNullOrEmpty(because: "نبود پاسخ باید پیام بدهد");
        vm.Revenue.Should().BeEmpty();
        vm.IsBusy.Should().BeFalse(because: "چرخهٔ بارگذاری باید پایین بیاید");
    }
}
