using System.Text.Json;
using FluentAssertions;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

public class AppointmentBookingSafetyTests
{
    private sealed class Navigation : INavigationService
    {
        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        public void NavigateTo(string pageKey, object? parameter = null)
            => NavigationRequested?.Invoke(this, new NavigationEventArgs(pageKey, parameter));
    }

    [Fact]
    public void New_appointment_clears_draft_and_is_blocked_during_booking()
    {
        var vm = new AppointmentsViewModel(new FakeApiClient(), new FakeSnackbar(), new Navigation());
        vm.SelectedPatient = new QuickPatient(1, "بیمار", "123", "0912", null, null, null);
        vm.SelectedDoctor = new DoctorOption(2, "پزشک", null);
        vm.SelectedSlot = new SlotVM { Time = "10:30", IsAvailable = true };
        vm.Notes = "یادداشت";
        vm.SelectedTypeIndex = 2;
        vm.SelectedPriorityIndex = 1;
        vm.WizardStep = 2;
        vm.IsBooking = true;
        vm.NewAppointmentCommand.Execute(null);
        vm.Notes.Should().Be("یادداشت");
        vm.IsBooking = false;
        vm.NewAppointmentCommand.Execute(null);
        vm.SelectedPatient.Should().BeNull();
        vm.SelectedDoctor.Should().BeNull();
        vm.SelectedSlot.Should().BeNull();
        vm.Notes.Should().BeEmpty();
        vm.SelectedTypeIndex.Should().Be(0);
        vm.SelectedPriorityIndex.Should().Be(0);
        vm.WizardStep.Should().Be(0);
    }

    [Fact]
    public void Editing_search_text_discards_old_patient_before_booking()
    {
        var api = new FakeApiClient();
        var vm = new AppointmentsViewModel(api, new FakeSnackbar(), new Navigation());
        var patient = new QuickPatient(1, "بیمار اول", "123", "0912", null, null, null);
        vm.SelectPatientCommand.Execute(patient);
        vm.SelectedPatient.Should().BeSameAs(patient);
        vm.PatientSearchText = "بیمار دوم";
        vm.SelectedPatient.Should().BeNull();
        vm.PatientSearchResults.Should().BeEmpty();
        vm.BookCommand.Execute(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "appointments");
    }

    [Fact]
    public async Task Waitlist_search_change_clears_patient_and_rejects_late_results()
    {
        var api = new FakeApiClient();
        var pending = new TaskCompletionSource<object?>();
        api.PendingGetResponses["patients/search?term=old"] = pending.Task;
        var vm = new AppointmentsViewModel(api, new FakeSnackbar(), new Navigation());
        vm.SelectWaitlistPatientCommand.Execute(new QuickPatient(1, "بیمار اول", "123", "0912", null, null, null));
        vm.WaitlistPatient.Should().NotBeNull();
        vm.WaitlistPatientSearch = "old";
        vm.WaitlistPatient.Should().BeNull();
        var search = vm.WaitlistPatientSearchCommand.ExecuteAsync(null);
        vm.WaitlistPatientSearch = "new";
        pending.SetResult(new { data = new[] { new { id = 1, firstName = "نام", lastName = "قبلی", nationalCode = "", mobile = "" } } });
        await search;
        vm.WaitlistPatientResults.Should().BeEmpty();
        await vm.AddToWaitlistCommand.ExecuteAsync(null);
        api.Calls.Should().NotContain(c => c.Method == "POST" && c.Endpoint == "appointments/waitlist");
    }

    [Fact]
    public async Task Pending_waitlist_submission_locks_form_and_reports_captured_patient()
    {
        var completion = new TaskCompletionSource<bool>();
        var api = new FakeApiClient { PendingBoolPostResult = completion.Task };
        var snackbar = new FakeSnackbar();
        var vm = new AppointmentsViewModel(api, snackbar, new Navigation());
        var patient = new QuickPatient(1, "بیمار اول", "123", "0912", null, null, null);
        vm.SelectWaitlistPatientCommand.Execute(patient);
        var post = vm.AddToWaitlistCommand.ExecuteAsync(null);
        vm.IsAddingToWaitlist.Should().BeTrue();
        vm.CanEditWaitlist.Should().BeFalse();
        vm.WaitlistPatientSearch = "بیمار دوم"; // تغییر برنامه‌ای؛ ورودی واقعی در UI قفل است
        completion.SetResult(true);
        await post;
        vm.CanEditWaitlist.Should().BeTrue();
        vm.WaitlistPatientSearch.Should().Be("بیمار دوم");
        snackbar.Messages.Should().Contain(m => m.Message.Contains("بیمار اول") && m.Message.Contains("ثبت شد"));
        api.Calls.Count(c => c.Method == "POST" && c.Endpoint == "appointments/waitlist").Should().Be(1);
    }

    [Fact]
    public async Task Pending_booking_locks_form_and_connection_failure_preserves_inputs()
    {
        var completion = new TaskCompletionSource<object?>();
        var api = new FakeApiClient { PendingPostResult = completion.Task };
        var snackbar = new FakeSnackbar();
        var vm = new AppointmentsViewModel(api, snackbar, new Navigation());
        var patient = new QuickPatient(1, "بیمار", "123", "0912", null, null, null);
        vm.SelectedPatient = patient;
        vm.SelectedDoctor = new DoctorOption(2, "پزشک", null);
        vm.SelectedDateShamsi = "1405/07/08";
        vm.SelectedSlot = new SlotVM { Time = "10:30", IsAvailable = true };
        vm.Notes = "یادداشت";
        var booking = vm.BookCommand.ExecuteAsync(null);
        vm.IsBooking.Should().BeTrue();
        vm.CanEditBooking.Should().BeFalse();
        var body = JsonSerializer.SerializeToElement(api.LastBody);
        body.GetProperty("patientId").GetInt64().Should().Be(1);
        body.GetProperty("doctorId").GetInt64().Should().Be(2);
        body.GetProperty("time").GetString().Should().Be("10:30");
        completion.SetException(new IOException("connection lost"));
        await booking;
        vm.CanEditBooking.Should().BeTrue();
        vm.SelectedPatient.Should().BeSameAs(patient);
        vm.Notes.Should().Be("یادداشت");
        snackbar.Messages.Should().Contain(m => m.Message.Contains("پیش از تلاش مجدد"));
    }
}
