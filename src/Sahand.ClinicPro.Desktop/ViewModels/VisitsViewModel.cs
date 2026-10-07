using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف بیمار در صف — فرم ۱۳
/// </summary>
public record QueueRow(
    long ReceptionId,
    long PatientId,
    string PatientName,
    string Mobile,
    int QueueNumber,
    string AppointmentTime,
    string? InsuranceName,
    string VisitTypeText,
    int WaitingMinutes,
    string WaitingText,
    string? Allergies);

/// <summary>
/// ردیف ویزیت‌شده‌ها
/// </summary>
public record DoneRow(long VisitId, string PatientName, string TimeRange, string? DiagnosisText);

/// <summary>
/// ViewModel صف ویزیت — گام U6 طبق پرامپت UI بخش ۲ (فرم ۱۳):
/// کارت‌های آماری + بیمار فعلی + صف انتظار + ویزیت‌شده‌ها + فراخوان (Space)
/// </summary>
public partial class VisitsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private System.Windows.Threading.DispatcherTimer? _refresher;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "صف ویزیت";

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>نمایه پزشک فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _doctorFilterIndex;

    /// <summary>تعداد ویزیت‌شده</summary>
    [ObservableProperty]
    private int _visitedCount;

    /// <summary>در حال ویزیت</summary>
    [ObservableProperty]
    private int _inProgressCount;

    /// <summary>در صف</summary>
    [ObservableProperty]
    private int _waitingCount;

    /// <summary>میانگین مدت ویزیت</summary>
    [ObservableProperty]
    private string _averageDisplay = "—";

    /// <summary>بیمار فعلی</summary>
    [ObservableProperty]
    private QueueRow? _currentPatient;

    /// <summary>صف انتظار</summary>
    [ObservableProperty]
    private ObservableCollection<QueueRow> _waiting = new();

    /// <summary>ویزیت‌شده‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<DoneRow> _done = new();

    /// <summary>در حال فراخوان</summary>
    [ObservableProperty]
    private bool _isCalling;

    /// <summary>سازنده</summary>
    public VisitsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری اولیه + تایمر ۳۰ ثانیه‌ای تازه‌سازی صف</summary>
    private async Task InitializeAsync()
    {
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        await LoadAsync();

        // به‌روزرسانی زنده صف — هر ۳۰ ثانیه طبق فرم ۱۳
        _refresher = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refresher.Tick += async (_, _) => await LoadAsync();
        _refresher.Start();
    }

    /// <summary>بارگذاری صف از سرور</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        var doctorId = DoctorFilterIndex <= 0 ? null : (long?)Doctors[DoctorFilterIndex - 1].Id;
        var endpoint = "visits/queue" + (doctorId.HasValue ? $"?doctorId={doctorId}" : string.Empty);
        var response = await _api.GetAsync<QueueResponse>(endpoint);
        if (response?.Data is null)
        {
            ErrorMessage = _api.LastError ?? string.Empty;
            return;
        }

        var q = response.Data;
        VisitedCount = q.VisitedCount;
        InProgressCount = q.InProgressCount;
        WaitingCount = q.WaitingCount;
        AverageDisplay = q.AverageVisitMinutes > 0 ? $"{q.AverageVisitMinutes:0.#} دقیقه" : "—";
        CurrentPatient = q.Current is null ? null : MapQueue(q.Current);
        Waiting = new ObservableCollection<QueueRow>((q.Waiting ?? new()).Select(MapQueue));
        Done = new ObservableCollection<DoneRow>((q.Done ?? new List<DoneItem>())
                .Select(d => new DoneRow(d.VisitId, d.PatientName, d.TimeRange, d.DiagnosisText)));
    }

    /// <summary>نگاشت ردیف صف</summary>
    private static QueueRow MapQueue(QueueItem q) => new(
        q.VisitId, q.PatientId, q.PatientName, q.Mobile, q.QueueNumber,
        q.AppointmentTime, q.InsuranceName, q.VisitTypeText, q.WaitingMinutes,
        q.WaitingMinutes > 0 ? $"{q.WaitingMinutes} دقیقه" : "تازه رسیده",
        q.Allergies);

    /// <summary>فراخوان بیمار بعدی — میانبر Space</summary>
    [RelayCommand]
    private async Task CallNextAsync()
    {
        if (IsCalling) return;
        IsCalling = true;
        var doctorId = DoctorFilterIndex <= 0 ? null : (long?)Doctors[DoctorFilterIndex - 1].Id;
        var endpoint = "visits/call-next" + (doctorId.HasValue ? $"?doctorId={doctorId}" : string.Empty);
        var response = await _api.PostAsync<CallNextResponse>(endpoint, new { });
        IsCalling = false;

        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? "بیماری در صف انتظار نیست");
            return;
        }

        _snackbar.Success($"بیمار بعدی فراخوانی شد — شماره {response.Data.VisitId % 100}");
        await LoadAsync();

        // باز کردن فرم ویزیت برای بیمار فراخوانی‌شده
        var window = new VisitWindow(new VisitEditViewModel(_api, _snackbar, response.Data.VisitId))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        await LoadAsync();
    }

    /// <summary>ادامه ویزیت بیمار فعلی</summary>
    [RelayCommand]
    private void OpenCurrentVisit()
    {
        if (CurrentPatient is null) { _snackbar.Error("بیمار فعالی در ویزیت نیست"); return; }
        var window = new VisitWindow(new VisitEditViewModel(_api, _snackbar, CurrentPatient.ReceptionId))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /* ─── پاسخ‌های API ─── */

    public sealed class QueueResponse
    {
        public bool IsSuccess { get; set; }
        public QueueData? Data { get; set; }
    }

    public sealed class QueueData
    {
        public QueueItem? Current { get; set; }
        public List<QueueItem>? Waiting { get; set; }
        public List<DoneItem>? Done { get; set; }
        public int VisitedCount { get; set; }
        public int InProgressCount { get; set; }
        public int WaitingCount { get; set; }
        public double AverageVisitMinutes { get; set; }
    }

    public sealed class QueueItem
    {
        public long VisitId { get; set; }
        public long PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public int QueueNumber { get; set; }
        public string AppointmentTime { get; set; } = string.Empty;
        public string? InsuranceName { get; set; }
        public string VisitTypeText { get; set; } = string.Empty;
        public int WaitingMinutes { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Allergies { get; set; }
    }

    public sealed class DoneItem
    {
        public long VisitId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string? DiagnosisText { get; set; }
    }

    public sealed class CallNextResponse
    {
        public bool IsSuccess { get; set; }
        public CallNextData? Data { get; set; }
    }

    public sealed class CallNextData
    {
        public long VisitId { get; set; }
        public long ReceptionId { get; set; }
        public long PatientId { get; set; }
    }
}
