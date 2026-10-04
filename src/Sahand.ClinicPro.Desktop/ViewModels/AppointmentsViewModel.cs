using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف نوبت در فهرست — منطبق با AppointmentListDto سرور</summary>
public record AppointmentRow(
    long Id,
    string? Code,
    string PatientName,
    string DoctorName,
    string Time,
    string StatusText,
    int StatusId,
    string TypeText,
    int QueueNumber);

/// <summary>گزینه پزشک در ComboBox</summary>
public record DoctorOption(long Id, string Name, string? SpecialtyName);

/// <summary>
/// ViewModel نوبت‌دهی — نوبت‌های یک روز با فیلتر پزشک و وضعیت
/// </summary>
public partial class AppointmentsViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نوبت‌دهی";

    /// <summary>نوبت‌های فهرست</summary>
    [ObservableProperty]
    private ObservableCollection<AppointmentRow> _appointments = new();

    /// <summary>نوبت انتخاب‌شده</summary>
    [ObservableProperty]
    private AppointmentRow? _selectedAppointment;

    /// <summary>تاریخ شمسی فیلتر — پیش‌فرض امروز</summary>
    [ObservableProperty]
    private string _dateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow);

    /// <summary>نمایه پزشک انتخاب‌شده در فیلتر — ۱- یعنی همه</summary>
    [ObservableProperty]
    private int _doctorFilterIndex = -1;

    /// <summary>نمایه وضعیت فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    /// <summary>فهرست پزشکان برای فیلتر</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>برچسب وضعیت‌ها — نمایه = مقدار enum منهای یک</summary>
    public string[] StatusLabels { get; } =
        { "رزرو شده", "تأیید شده", "حاضر", "در جریان", "تکمیل شده", "لغو شده", "غایب" };

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public AppointmentsViewModel(IApiClient api)
    {
        _api = api;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری پزشکان و نوبت‌های امروز</summary>
    private async Task InitializeAsync()
    {
        var page = await _api.GetAsync<DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName));

        await LoadAsync();
    }

    /// <summary>بارگذاری نوبت‌ها با فیلترهای فعلی</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = $"appointments?date={Uri.EscapeDataString(DateShamsi ?? string.Empty)}" +
                       (DoctorFilterIndex >= 0 && DoctorFilterIndex < Doctors.Count
                           ? $"&doctorId={Doctors[DoctorFilterIndex].Id}"
                           : string.Empty) +
                       (StatusFilterIndex > 0 ? $"&status={StatusFilterIndex}" : string.Empty);

        var response = await _api.GetAsync<AppointmentsResponse>(endpoint);

        if (response?.Data is not null)
        {
            Appointments = new ObservableCollection<AppointmentRow>(
                response.Data.Select(MapRow));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت نوبت‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف سرور به مدل نمایش</summary>
    private static AppointmentRow MapRow(AppointmentDto a) =>
        new(a.Id, a.AppointmentCode, a.PatientName, a.DoctorName, a.Time,
            StatusTextOf(a.Status), a.Status, TypeTextOf(a.Type), a.QueueNumber);

    /// <summary>متن فارسی وضعیت</summary>
    private static string StatusTextOf(int status) => status is >= 1 and <= 7
        ? new[] { "رزرو شده", "تأیید شده", "حاضر", "در جریان", "تکمیل شده", "لغو شده", "غایب" }[status - 1]
        : "نامشخص";

    /// <summary>متن فارسی نوع نوبت</summary>
    private static string TypeTextOf(int type) => type switch
    {
        1 => "اولیه",
        2 => "پیگیری",
        3 => "اورژانسی",
        _ => "سایر"
    };

    /// <summary>ثبت نوبت جدید</summary>
    [RelayCommand]
    private void NewAppointment()
    {
        var vm = new AppointmentFormViewModel(_api, Doctors);
        var window = new AppointmentFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>تأیید نوبت انتخاب‌شده</summary>
    [RelayCommand]
    private Task ConfirmAsync() => ChangeStatusAsync(2);

    /// <summary>لغو نوبت انتخاب‌شده</summary>
    [RelayCommand]
    private Task CancelAsync() => ChangeStatusAsync(6, "لغو توسط پذیرش");

    /// <summary>ثبت غیبت نوبت انتخاب‌شده</summary>
    [RelayCommand]
    private Task NoShowAsync() => ChangeStatusAsync(7, "بیمار حاضر نشد");

    /// <summary>اتمام نوبت انتخاب‌شده</summary>
    [RelayCommand]
    private Task CompleteAsync() => ChangeStatusAsync(5);

    /// <summary>ارسال تغییر وضعیت به سرور</summary>
    private async Task ChangeStatusAsync(int status, string? reason = null)
    {
        if (SelectedAppointment is null) return;

        IsBusy = true;
        var ok = await _api.PutAsync($"appointments/{SelectedAppointment.Id}/status",
            new { status, reason });
        IsBusy = false;

        if (!ok)
            ErrorMessage = _api.LastError ?? "تغییر وضعیت ناموفق بود";
        else
            await LoadAsync();
    }

    /// <summary>پاسخ فهرست نوبت‌ها</summary>
    public sealed class AppointmentsResponse
    {
        public bool IsSuccess { get; set; }
        public List<AppointmentDto>? Data { get; set; }
    }

    /// <summary>DTO نوبت منطبق بر سرور</summary>
    public sealed class AppointmentDto
    {
        public long Id { get; set; }
        public string? AppointmentCode { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int Status { get; set; }
        public int Type { get; set; }
        public int QueueNumber { get; set; }
    }

    /// <summary>پاسخ صفحه‌بندی‌شده پزشکان</summary>
    public sealed class DoctorsPage
    {
        public bool IsSuccess { get; set; }
        public List<DoctorDto>? Data { get; set; }
    }

    /// <summary>DTO پزشک منطبق بر سرور</summary>
    public sealed class DoctorDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? SpecialtyName { get; set; }
    }
}