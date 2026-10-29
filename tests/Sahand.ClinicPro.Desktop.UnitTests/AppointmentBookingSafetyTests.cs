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
