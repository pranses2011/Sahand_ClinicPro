using System.Collections.ObjectModel;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// اسنک‌بار ساختگی — پیام‌ها در حافظه ضبط می‌شوند
/// چرخهٔ سی‌وهشتم: قرارداد سرویس پیام شناور ساخت‌یافته شد (ToastMessage با
/// نوع چهارگانه) — دام ساختگی هم با همان شکل نو شد تا بیلد تست سالم بماند.
/// </summary>
public sealed class FakeSnackbar : ISnackbarService
{
    /// <summary>پیام‌های ضبط‌شده: (متن، نوع)</summary>
    public List<(string Message, ToastKind Kind)> Messages { get; } = new();

    /// <summary>رویداد انتشار پیام — مدل کامل (چرخهٔ ۳۸)</summary>
    public event Action<ToastMessage>? MessagePosted;

    /// <summary>نمایش پیام موفق</summary>
    public void Success(string message) { Messages.Add((message, ToastKind.Success)); MessagePosted?.Invoke(new ToastMessage(ToastKind.Success, message)); }

    /// <summary>نمایش پیام خطا</summary>
    public void Error(string message) { Messages.Add((message, ToastKind.Error)); MessagePosted?.Invoke(new ToastMessage(ToastKind.Error, message)); }

    /// <summary>نمایش پیام هشدار — چرخهٔ ۳۸</summary>
    public void Warning(string message) { Messages.Add((message, ToastKind.Warning)); MessagePosted?.Invoke(new ToastMessage(ToastKind.Warning, message)); }

    /// <summary>نمایش پیام خبری</summary>
    public void Info(string message) { Messages.Add((message, ToastKind.Info)); MessagePosted?.Invoke(new ToastMessage(ToastKind.Info, message)); }
}

/// <summary>
/// چرخه دوازدهم (C1 — بند ۴-۱ سند تحلیل):
/// پنج تست جریان‌های پول‌ساز دسکتاپ با کلاینت API ساختگی —
/// ثبت پرداخت، ویرایش حقوقی، فیلتر گزارش کارانه و تحلیل نمودار گزارش‌ساز.
/// </summary>
public class MoneyFlowViewModelTests
{
    // ═══════════════ ۱) ثبت پرداخت روی فاکتور ═══════════════

    [Fact]
    public async Task Payment_Save_ShouldPostToPaymentsWithInvoicePatientAndAmount()
    {
        // ترتیب: ابتدا جزئیات فاکتور، سپس ثبت پرداخت
        // ═══ چرخهٔ ۷۳ — اصلاح شکل پاسخ کنسروی ═══
        // ViewModel از چرخهٔ ۴۸ به بعد جزئیات فاکتور را درون پاکت استاندارد
        // سرور {isSuccess, data:{...}} انتظار دارد (InvoiceDetailEnvelope).
        // پاسخِ مسطحِ قبلی (id/patientId/remainingAmount در ریشه) باعث می‌شد
        // Data نال شود، ذخیره با «دریافت فاکتور ناموفق بود» متوقف شود و
        // رویداد موفقیت هرگز شلیک نشود — تست از v2.15.0 شکسته بود.
        var api = new FakeApiClient();
        api.CannedResponses["invoices/512"] = new
        {
            isSuccess = true,
            data = new
            {
                id = 512,
                patientId = 77,
                remainingAmount = 250_000
            }
        };
        var vm = new PaymentWindowViewModel(api, invoiceId: 512, remainingAmount: 250_000m)
        {
            Amount = 200_000m,
            MethodIndex = 1, // کارت‌خوان
            ReferenceNumber = "TRX-8899",
        };
        var succeeded = false;
        vm.SaveSucceeded += (_, _) => succeeded = true;

        await vm.SaveCommand.ExecuteAsync(null);

        succeeded.Should().BeTrue("پرداخت معتبر باید رویداد موفقیت را شلیک کند");
        api.Calls.Should().Contain(c => c.Method == "GET" && c.Endpoint == "invoices/512");
        api.Calls.Should().Contain(c => c.Method == "POST-bool" && c.Endpoint == "payments");
        succeeded.Should().BeTrue();
        vm.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task Payment_Save_WithZeroAmount_ShouldFailFastWithoutApiCall()
    {
        var api = new FakeApiClient();
        var vm = new PaymentWindowViewModel(api, invoiceId: 10, remainingAmount: 100m)
        {
            Amount = 0,
        };

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("بزرگ‌تر از صفر");
        api.Calls.Should().BeEmpty("اعتبارسنجی محلی نباید سرور را درگیر کند");
    }

    // ═══════════════ ۲) ویرایش اطلاعات حقوقی پرسنل ═══════════════

    [Fact]
    public async Task SalaryEdit_Save_ShouldPutBaseAndPercentToHrEndpoint()
    {
        var api = new FakeApiClient { NextBoolResult = true };
        var vm = new EmployeeSalaryEditViewModel(api, employeeId: 33,
            "رضا محمدنژاد", "پرستار — بخش اورژانس",
            baseSalary: 22_000_000m, commissionPercent: 30m)
        {
            BaseSalary = "24000000",
            CommissionPercent = "35",
        };
        var saved = false;
        vm.SaveSucceeded += (_, _) => saved = true;

        await vm.SaveCommand.ExecuteAsync(null);

        saved.Should().BeTrue();
        api.Calls.Should().ContainSingle(c => c.Method == "PUT-bool")
            .Which.Endpoint.Should().Be("hr/employees/33/salary");
    }

    [Fact]
    public async Task SalaryEdit_Save_WithPercentOver100_ShouldRejectLocally()
    {
        var api = new FakeApiClient();
        var vm = new EmployeeSalaryEditViewModel(api, 33, "پرسنل", "بخش", 10m, 10m)
        {
            BaseSalary = "12000000",
            CommissionPercent = "150", // خارج از بازه
        };

        await vm.SaveCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("۰ تا ۱۰۰");
        api.Calls.Should().BeEmpty("درصد نامعتبر نباید به سرور برسد");
    }

    // ═══════════════ ۳) فیلتر گزارش کارانه — دوره/پرسنل ═══════════════

    [Fact]
    public async Task CommissionReport_Load_ShouldCallEndpointWithYearMonthAndEmployeeFilter()
    {
        var api = new FakeApiClient();
        api.CannedResponses["hr/employees"] = new { isSuccess = true, data = new[] { new { id = 5, fullName = "مهدی قلی‌زاده" } } };
        api.CannedResponses["reports/commission-detail?year=1405&month=7&employeeId=5"] = new
        {
            data = new
            {
                year = 1405, month = 7,
                periodText = "مهر ۱۴۰۵",
                generatedAtShamsi = "۱۴۰۵/۰۷/۱۳",
                clinicName = "درمانگاه سهند",
                employeeCount = 1, serviceCountTotal = 42,
                totalRevenue = 500_000_000m, totalCost = 200_000_000m,
                totalProfit = 300_000_000m, totalCommission = 75_000_000m,
                summary = new object[]
                {
                    new { employeeId = 5, personnelCode = "P-105", fullName = "مهدی قلی‌زاده",
                          departmentName = "آزمایشگاه", positionName = "کارشناس ارشد",
                          isDoctorLinked = false, doctorNames = "—",
                          serviceCount = 42, totalRevenue = 500_000_000m, totalCost = 200_000_000m,
                          totalProfit = 300_000_000m, commissionPercent = 25m,
                          commissionBase = 300_000_000m, serviceCommission = 75_000_000m,
                          salaryStatusText = "پرداخت شد" }
                },
                details = Array.Empty<object>(),
            }
        };

        var vm = new CommissionReportViewModel(api, new FakeSnackbar());
        await vm.InitializeAsync(); // فهرست پرسنل + دوره جاری

        vm.SelectedEmployeeIndex = 1; // «همه پرسنل» = 0؛ پرسنل ۵ = ۱
        await vm.LoadCommand.ExecuteAsync(null);

        api.Calls.Should().Contain(c =>
            c.Method == "GET" && c.Endpoint == "reports/commission-detail?year=1405&month=7&employeeId=5");
        vm.TotalCommission.Should().Be(75_000_000m);
        vm.TotalProfit.Should().Be(300_000_000m);
        vm.CommissionChartPoints.Should().NotBeEmpty("دونات سهم پرسنل باید پر شود");
    }

    // ═══════════════ ۴) گزارش‌ساز — تحلیل بلاک نمودار پاسخ سرور ═══════════════

    [Fact]
    public async Task ReportBrowser_Execute_WithChartBlock_ShouldParseKindAndPoints()
    {
        var api = new FakeApiClient();
        api.CannedResponses["reports/catalog"] = new
        {
            isSuccess = true,
            data = new object[]
            {
                new
                {
                    icon = "📈", category = "گزارش‌های نموداری",
                    reports = new object[]
                    {
                        new { id = "chart-top-services", title = "پردرآمدترین خدمات",
                              description = "رتبه‌بندی", paramsList = new[] { "fromDate", "toDate", "top" } }
                    }
                }
            }
        };
        api.CannedResponses["reports/execute/chart-top-services?fromDate=2026-09-01&toDate=2026-10-06&top=20"] = new
        {
            isSuccess = true,
            data = new
            {
                available = true,
                summary = new { services = 5, revenue = 480_000_000m },
                columns = new[] { "خدمت", "درآمد (ریال)" },
                rows = new object[]
                {
                    new { service = "ویزیت تخصصی", revenue = 180_000_000m },
                },
                chart = new
                {
                    type = "hbar",
                    title = "پردرآمدترین خدمات",
                    unit = "ریال",
                    points = new object[]
                    {
                        new { label = "ویزیت تخصصی", value = 180_000_000 },
                        new { label = "سونوگرافی", value = 150_000_000 },
                        new { label = "آزمایش کامل", value = 120_000_000 },
                    }
                }
            }
        };

        var vm = new ReportBrowserViewModel(api, new FakeSnackbar());
        await vm.LoadCatalogCommand.ExecuteAsync(null);

        // انتخاب گزارش نموداری و تنظیم پارامترها
        vm.SelectedCategory = vm.Categories.First(c => c.Title.Contains("نموداری"));
        vm.SelectedReport = vm.SelectedCategory!.Reports.First(r => r.Id == "chart-top-services");
        vm.FromDate = new DateTime(2026, 9, 1);
        vm.ToDate = new DateTime(2026, 10, 6);
        vm.TopCount = 20;

        await vm.ExecuteCommand.ExecuteAsync(null);

        vm.HasChart.Should().BeTrue("بلاک chart در پاسخ هست");
        vm.ChartKind.Should().Be("hbar", "نوع اعلام‌شده سرور باید همانند رندر شود");
        vm.ChartPoints.Should().HaveCount(3);
        vm.ChartPoints.Should().NotBeNull();
        vm.ChartPoints![0].Label.Should().Be("ویزیت تخصصی");
        vm.ChartPoints[0].Value.Should().Be(180_000_000);
        vm.SummaryCards.Should().NotBeEmpty();
        vm.Rows.Should().HaveCount(1);
    }
}
