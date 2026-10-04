using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف پذیرش — منطبق با ReceptionListDto سرور</summary>
public record ReceptionRow(
    long Id,
    string? Number,
    string PatientName,
    string DoctorName,
    string Time,
    string StatusText,
    int StatusId,
    string? InsuranceName,
    string WaitingText);

/// <summary>
/// ViewModel پذیرش — صف پذیرش‌های امروز با زمان انتظار
/// </summary>
public partial class ReceptionsViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "پذیرش";

    /// <summary>پذیرش‌های امروز</summary>
    [ObservableProperty]
    private ObservableCollection<ReceptionRow> _receptions = new();

    /// <summary>پذیرش انتخاب‌شده</summary>
    [ObservableProperty]
    private ReceptionRow? _selectedReception;

    /// <summary>نمایه پزشک فیلتر — ۱- یعنی همه</summary>
    [ObservableProperty]
    private int _doctorFilterIndex = -1;

    /// <summary>فهرست پزشکان فیلتر</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public ReceptionsViewModel(IApiClient api)
    {
        _api = api;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری پزشکان و پذیرش‌های امروز</summary>
    private async Task InitializeAsync()
    {
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName));

        await LoadAsync();
    }

    /// <summary>بارگذاری پذیرش‌های امروز</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = "receptions/today" +
                       (DoctorFilterIndex >= 0 && DoctorFilterIndex < Doctors.Count
                           ? $"?doctorId={Doctors[DoctorFilterIndex].Id}"
                           : string.Empty);

        var response = await _api.GetAsync<ReceptionsResponse>(endpoint);

        if (response?.Data is not null)
        {
            Receptions = new ObservableCollection<ReceptionRow>(response.Data.Select(MapRow));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت پذیرش‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>نگاشت ردیف سرور</summary>
    private static ReceptionRow MapRow(ReceptionDto r) => new(
        r.Id, r.ReceptionNumber, r.PatientName, r.DoctorName, r.TimeShamsi,
        StatusTextOf(r.Status), r.Status, r.InsuranceName,
        r.WaitingTime.HasValue ? $"{r.WaitingTime} دقیقه" : "—");

    /// <summary>متن فارسی وضعیت پذیرش</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "پذیرش شده",
        2 => "در صف انتظار",
        3 => "در ویزیت",
        4 => "ویزیت‌شده",
        5 => "ترخیص‌شده",
        6 => "لغوشده",
        _ => "نامشخص"
    };

    /// <summary>ثبت پذیرش جدید</summary>
    [RelayCommand]
    private void NewReception()
    {
        var vm = new ReceptionFormViewModel(_api, Doctors);
        var window = new ReceptionFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ارسال به صف</summary>
    [RelayCommand]
    private Task QueueAsync() => ChangeStatusAsync(2);

    /// <summary>شروع ویزیت</summary>
    [RelayCommand]
    private Task StartVisitAsync() => ChangeStatusAsync(3);

    /// <summary>پایان ویزیت</summary>
    [RelayCommand]
    private Task VisitDoneAsync() => ChangeStatusAsync(4);

    /// <summary>ترخیص</summary>
    [RelayCommand]
    private Task DischargeAsync() => ChangeStatusAsync(5);

    /// <summary>لغو پذیرش</summary>
    [RelayCommand]
    private Task CancelAsync() => ChangeStatusAsync(6);

    /// <summary>ارسال تغییر وضعیت</summary>
    private async Task ChangeStatusAsync(int status)
    {
        if (SelectedReception is null) return;

        IsBusy = true;
        var ok = await _api.PutAsync($"receptions/{SelectedReception.Id}/status",
            new { status });
        IsBusy = false;

        if (!ok)
            ErrorMessage = _api.LastError ?? "تغییر وضعیت ناموفق بود";
        else
            await LoadAsync();
    }

    /// <summary>پاسخ فهرست پذیرش</summary>
    public sealed class ReceptionsResponse
    {
        public bool IsSuccess { get; set; }
        public List<ReceptionDto>? Data { get; set; }
    }

    /// <summary>DTO پذیرش منطبق بر سرور</summary>
    public sealed class ReceptionDto
    {
        public long Id { get; set; }
        public string? ReceptionNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string TimeShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? InsuranceName { get; set; }
        public int? WaitingTime { get; set; }
    }
}