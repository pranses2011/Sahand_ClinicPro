using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست‌های ViewModel صف پذیرش — چرخهٔ ۵۸ (پرریسک‌ترین صفحهٔ عملیاتی):
/// آمار زندهٔ صف، فهرست پذیرش‌های امروز، و دو رفع تاریخی که نباید هرگز
/// پس برنند: گزینهٔ «آزاد (بدون بیمه)» در صدر کشوی بیمه (چرخهٔ ۲۳ — بیمهٔ
/// انتخابیِ گم‌شده) و بارگذاری دوبارهٔ اقلام مصرفی با هر ورود صفحه (چرخهٔ ۴۵).
/// </summary>
public class ReceptionsViewModelTests
{
    /// <summary>پیکربندی پاسخ‌های آماده برای صف پذیرش سالم</summary>
    private static FakeApiClient MakeApi(bool withData = true)
    {
        var api = new FakeApiClient();
        if (!withData)
        {
            api.FakeError = "خطای شبیه‌سازی‌شدهٔ سرور";
            return api;
        }
        api.CannedResponses["receptions/queue-stats"] = new
        {
            isSuccess = true,
            data = new { waitingCount = 3, receivedCount = 10, inQueue = 5, todayRevenue = 1_200_000m },
        };
        api.CannedResponses["receptions/today"] = new
        {
            isSuccess = true,
            data = new object[]
            {
                new
                {
                    id = 91L, receptionNumber = "R-0091", patientName = "سارا محمدی",
                    doctorName = "دکتر احمدی", timeShamsi = "۰۹:۱۵", status = 2,
                    insuranceName = "تأمین اجتماعی", waitingTime = 12,
                    roomName = "اتاق ۳", invoiceTotal = 800_000m, paidAmount = 800_000m,
                },
                new
                {
                    id = 92L, receptionNumber = "R-0092", patientName = "علی رضایی",
                    doctorName = "دکتر موسوی", timeShamsi = "۰۹:۴۰", status = 1,
                    insuranceName = (string?)null, waitingTime = 3,
                    roomName = (string?)null, invoiceTotal = 500_000m, paidAmount = 0m,
                },
            },
        };
        api.CannedResponses["lookups/insurance-companies"] = new
        {
            isSuccess = true,
            data = new object[]
            {
                new { id = 5, name = "تأمین اجتماعی" },
                new { id = 6, name = "بیمه آسیا" },
            },
        };
        api.CannedResponses["doctors?page=1&pageSize=200"] = new
        {
            isSuccess = true,
            data = new object[]
            {
                new { id = 1, fullName = "دکتر احمدی", specialtyName = "داخلی", consultationFee = 500_000m },
                new { id = 2, fullName = "دکتر موسوی", specialtyName = "قلب", consultationFee = 800_000m },
            },
        };
        return api;
    }

    private static ReceptionsViewModel MakeVm(FakeApiClient api) =>
        new(api, new FakeSnackbar());

    // ═══════════════ ۱) آمار و فهرست صف امروز ═══════════════

    [Fact]
    public async Task LoadAsync_Should_Populate_QueueStats_And_TodayReceptions()
    {
        var vm = MakeVm(MakeApi());

        await vm.LoadAsync();

        vm.WaitingCount.Should().Be(3);
        vm.ReceivedCount.Should().Be(10);
        vm.InQueue.Should().Be(5);
        vm.TodayRevenue.Should().Be(1_200_000m);
        // نمایش میلیون‌شده — رقم‌ها وابسته به Culture فارسی‌اند؛ پسوند M قطعی است
        vm.TodayRevenueDisplay.Should().EndWith("M",
            because: "درآمد امروز بالای یک میلیون، فشردهٔ M می‌گیرد");
        vm.Receptions.Should().HaveCount(2, because: "دو پذیرش امروز فرستاده شد");
        vm.IsBusy.Should().BeFalse();
        vm.ErrorMessage.Should().BeEmpty();
    }

    // ═══════════════ ۲) رفع چرخهٔ ۲۳: بیمهٔ آزاد همیشه صدر کشوست ═══════════════

    /// <summary>
    /// گزینهٔ «آزاد (بدون بیمه)» باید نمایهٔ صفر واقعی باشد و شرکت‌های سرور
    /// از نمایهٔ ۱ به بعد بیایند — پیش‌تر شرکتِ اولِ سرور با نمایهٔ صفر «بیمهٔ
    /// گم‌شده» می‌شد (آسیا به‌جای آزاد ثبت می‌شد).
    /// </summary>
    [Fact]
    public void Initialize_Should_Put_FreeInsurance_First_And_ServerCompaniesAfter()
    {
        var vm = MakeVm(MakeApi());

        vm.InsuranceCompanies.Should().HaveCount(3,
            because: "آزاد + دو شرکت سرور");
        vm.InsuranceCompanies[0].Id.Should().Be(0);
        vm.InsuranceCompanies[0].Name.Should().Be("آزاد (بدون بیمه)");
        vm.InsuranceCompanies[1].Id.Should().Be(5,
            because: "شرکت اول سرور از نمایهٔ ۱ می‌نشیند نه صفر");
        vm.InsuranceCompanies[2].Id.Should().Be(6);
    }

    // ═══════════════ ۳) کشوی پزشکان ═══════════════

    [Fact]
    public void Initialize_Should_Load_Doctors_Lookup_FromApi()
    {
        var vm = MakeVm(MakeApi());

        vm.Doctors.Should().HaveCount(2);
        vm.Doctors[0].Name.Should().Be("دکتر احمدی");
        vm.Doctors[0].ConsultationFee.Should().Be(500_000m);
        vm.Doctors[1].SpecialtyName.Should().Be("قلب");
    }

    // ═══════════════ ۴) شکست فهرست = پیام خطا، نه کرش ═══════════════

    [Fact]
    public async Task LoadAsync_WhenApiFails_Should_ShowPersianError_AndStayAlive()
    {
        var vm = MakeVm(MakeApi(withData: false));

        await vm.LoadAsync();

        vm.ErrorMessage.Should().NotBeNullOrEmpty(
            because: "نبود پاسخ باید خطای سرور را روی صفحه بیاورد نه سکوت");
        vm.Receptions.Should().BeEmpty();
        vm.IsBusy.Should().BeFalse(because: "چرخهٔ بارگذاری حتی در خطا پایین می‌آید");
    }
    [Fact]
    public async Task Older_insurance_tariffs_cannot_replace_newer_ones_and_submission_waits()
    {
        var api = MakeApi();
        api.CannedResponses["lookups/insurance-companies"] = new { data = new[] {
            new { id = 5, name = "بیمه یک", isContracted = true },
            new { id = 6, name = "بیمه دو", isContracted = true } } };
        api.CannedResponses["lookups/services?search=&insuranceCompanyId="] =
            new { data = new[] { new { serviceId = 11, serviceName = "خدمت", price = 100m } } };
        var first = new TaskCompletionSource<object?>();
        var second = new TaskCompletionSource<object?>();
        api.PendingGetResponses["lookups/services?search=&insuranceCompanyId=5"] = first.Task;
        api.PendingGetResponses["lookups/services?search=&insuranceCompanyId=6"] = second.Task;
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", null, null, null);
        vm.ReceptionModeIndex = 1;
        vm.AddServiceCommand.Execute(vm.Services.Single());
        vm.InsuranceIndex = 1;
        await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "receptions");
        vm.InsuranceIndex = 2;
        var refresh = vm.RefreshServiceTariffsCommand.ExecuteAsync(null);
        second.SetResult(new { data = new[] { new { serviceId = 11, serviceName = "خدمت", price = 300m } } });
        await refresh;
        first.SetResult(new { data = new[] { new { serviceId = 11, serviceName = "خدمت", price = 200m } } });
        await Task.Delay(100);
        vm.Services.Single().Price.Should().Be(300m);
        vm.SelectedServices.Single().Service.Price.Should().Be(300m);
    }

    [Fact]
    public async Task Failed_tariff_refresh_keeps_selected_service_but_blocks_submission()
    {
        var api = MakeApi();
        const string path = "lookups/services?search=&insuranceCompanyId=";
        api.CannedResponses[path] = new { data = new[] { new { serviceId = 11, serviceName = "خدمت", price = 100m } } };
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", null, null, null);
        vm.ReceptionModeIndex = 1;
        vm.AddServiceCommand.Execute(vm.Services.Single());
        api.CannedResponses.Remove(path);
        await vm.RefreshServiceTariffsCommand.ExecuteAsync(null);
        vm.SelectedServices.Should().ContainSingle();
        await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "receptions");
    }

    [Fact]
    public async Task Reception_form_is_locked_until_pending_submission_completes()
    {
        var api = MakeApi();
        var pending = new TaskCompletionSource<object?>();
        api.PendingPostResult = pending.Task;
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", null, null, null);
        vm.SelectedDoctor = vm.Doctors.First();
        var submission = vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        vm.IsSubmitting.Should().BeTrue();
        vm.CanEditReception.Should().BeFalse();
        pending.SetResult(null);
        await submission;
        vm.IsSubmitting.Should().BeFalse();
        vm.CanEditReception.Should().BeTrue();
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task Late_insurance_inquiry_cannot_change_another_patient_or_allow_submission_while_pending()
    {
        var api = MakeApi();
        api.CannedResponses["lookups/insurance-companies"] = new { data = new[] { new { id = 5, name = "بیمه", isContracted = true, coveragePercent = 60m } } };
        var pending = new TaskCompletionSource<object?>();
        api.PendingPostResult = pending.Task;
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار اول", "", "", "بیمه", null, null, 5);
        vm.SelectedDoctor = vm.Doctors.First();
        var inquiry = vm.VerifyInsuranceCommand.ExecuteAsync(null);
        vm.IsInquiring.Should().BeTrue();
        await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "receptions");
        vm.FoundPatient = new QuickPatient(8, "بیمار دوم", "", "", "بیمه", null, null, 5);
        pending.SetResult(new { data = new { isEligible = true, coveragePercent = 99m, approvalCode = "old" } });
        await inquiry;
        vm.Inquiry.Should().BeNull();
        vm.CoveragePercent.Should().Be(60m);
    }

    [Fact]
    public async Task Failed_inquiry_blocks_insured_reception_instead_of_assuming_a_free_invoice()
    {
        var api = MakeApi();
        api.CannedResponses["lookups/insurance-companies"] = new { data = new[] { new { id = 5, name = "بیمه", isContracted = true, coveragePercent = 60m } } };
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", "بیمه", null, null, 5);
        vm.SelectedDoctor = vm.Doctors.First();
        await vm.VerifyInsuranceCommand.ExecuteAsync(null);
        vm.Inquiry.Should().NotBeNull();
        vm.CoveragePercent.Should().Be(0m);
        await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "receptions");
    }

    [Fact]
    public void Automatic_payment_requires_matching_persisted_invoice_and_confirmed_amount()
    {
        var detail = new ReceptionsViewModel.ReceptionInvoiceDetail { Id = 9, PatientId = 7, RemainingAmount = 100m };
        ReceptionsViewModel.CanAutomaticallyPay(detail, 9, 7, 100m).Should().BeTrue();
        ReceptionsViewModel.CanAutomaticallyPay(detail, 9, 7, 99m).Should().BeFalse();
        ReceptionsViewModel.CanAutomaticallyPay(detail, 9, 8, 100m).Should().BeFalse();
        ReceptionsViewModel.CanAutomaticallyPay(detail, 10, 7, 100m).Should().BeFalse();
        ReceptionsViewModel.CanAutomaticallyPay(null, 9, 7, 100m).Should().BeFalse();
        detail.RemainingAmount = null;
        ReceptionsViewModel.CanAutomaticallyPay(detail, 9, 7, 100m).Should().BeFalse();
    }

    [Fact]
    public async Task Search_result_is_discarded_after_user_changes_search_text()
    {
        var api = MakeApi();
        var pending = new TaskCompletionSource<object?>();
        api.PendingGetResponses["patients/search?term=old"] = pending.Task;
        var vm = MakeVm(api);
        vm.SmartSearch = "old";
        var search = vm.SmartSearchCommand.ExecuteAsync(null);
        vm.IsSearchingPatient.Should().BeTrue();
        vm.SmartSearch = "new";
        pending.SetResult(new { data = new[] { new { id = 7, firstName = "بیمار", lastName = "قدیمی", nationalCode = "", mobile = "" } } });
        await search;
        vm.FoundPatient.Should().BeNull();
        vm.PatientResults.Should().BeEmpty();
        vm.IsSearchingPatient.Should().BeFalse();
    }

    [Fact]
    public async Task Doctor_inquiry_percent_that_billing_cannot_reproduce_blocks_reception()
    {
        var api = MakeApi();
        api.CannedResponses["lookups/insurance-companies"] = new { data = new[] { new { id = 5, name = "بیمه", isContracted = true, coveragePercent = 60m } } };
        api.CannedResponses["insurance/inquiries"] = new { data = new { isEligible = true, coveragePercent = 90m } };
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", "بیمه", null, null, 5);
        vm.SelectedDoctor = vm.Doctors.First();
        await vm.VerifyInsuranceCommand.ExecuteAsync(null);
        vm.CoveragePercent.Should().Be(90m);
        await vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "receptions");
    }

    [Fact]
    public async Task Explicit_free_insurance_is_sent_as_zero_not_patient_policy_fallback()
    {
        var api = MakeApi();
        var pending = new TaskCompletionSource<object?>();
        api.PendingPostResult = pending.Task;
        var vm = MakeVm(api);
        vm.FoundPatient = new QuickPatient(7, "بیمار", "", "", "بیمه", null, null, 5);
        vm.InsuranceIndex = 0;
        vm.SelectedDoctor = vm.Doctors.First();
        var submission = vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        var body = System.Text.Json.JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("insuranceCompanyId").GetInt64().Should().Be(0);
        pending.SetResult(null);
        await submission;
    }

    [Fact]
    public async Task Appointment_search_uses_patient_and_doctor_ids_and_submits_appointment_id()
    {
        var api = MakeApi();
        api.CannedResponses["appointments/search"] = new { data = new[] {
            new { id = 77L, appointmentCode = "APT-123", patientId = 7L, doctorId = 1L,
                patientMobile = "09120000000", doctorName = "نام نمایشی متفاوت", status = 1 } } };
        api.CannedResponses["patients/search?term=09120000000"] = new { data = new[] {
            new { id = 7L, firstName = "بیمار", lastName = "آزمایش", nationalCode = "", mobile = "09120000000" } } };
        var vm = MakeVm(api);
        vm.SmartSearch = "APT-123";
        await vm.SmartSearchCommand.ExecuteAsync(null);
        vm.FoundPatient!.Id.Should().Be(7);
        vm.SelectedDoctor!.Id.Should().Be(1);
        var pending = new TaskCompletionSource<object?>();
        api.PendingPostResult = pending.Task;
        var submission = vm.ReceptionAndInvoiceCommand.ExecuteAsync(null);
        System.Text.Json.JsonSerializer.SerializeToElement(api.LastBody).GetProperty("appointmentId").GetInt64().Should().Be(77);
        pending.SetResult(null);
        await submission;
    }

}
