using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// میزکار ویزیت پزشک — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
/// «صفحهٔ دکتر پذیرش/ویزیت پزشک نیست». برخلاف «صف ویزیت» (نظر منشی با فیلتر
/// اختیاری پزشک)، این صفحه میزکارِ شخصی خودِ پزشکِ واردشده است:
///   • با users/me خودکار تشخیص می‌دهد کاربر جاری کدام پزشک است
///     (User.EmployeeId → Doctor.EmployeeId) و صفِ «همان» پزشک را می‌آورد؛
///   • اگر حساب کاربر به پزشکی وصل نباشد، انتخابگر پزشک + بنر راهنما عرضه
///     می‌شود تا کار کند ولی محدود نباشد (مثلاً منشی جانشین پزشک)؛
///   • فراخوان بیمار بعدی، ادامه ویزیت فعلی و آمار شخصیِ همان روز.
/// تازه‌سازی زنده: هر ۳۰ ثانیه (هم‌قدم با صف ویزیت) + رویداد SignalR صف.
/// </summary>
public partial class DoctorVisitViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private System.Windows.Threading.DispatcherTimer? _refresher;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "میزکار پزشک";

    /// <summary>نام کامل کاربر جاری — از users/me</summary>
    [ObservableProperty]
    private string _currentUserName = string.Empty;

    /// <summary>شناسهٔ پزشکِ خودکار-شناسایی‌شده از حساب کاربر — تهی یعنی بدون اتصال</summary>
    [ObservableProperty]
    private long? _linkedDoctorId;

    /// <summary>آیا حساب کاربر به پزشکی متصل است؟ (در غیر این صورت انتخابگر دستی نمایش می‌یابد)</summary>
    [ObservableProperty]
    private bool _isLinkedToDoctor = true;

    /// <summary>نمایهٔ انتخابگر پزشکِ دستی — فقط وقتی اتصال خودکار نبود معنا دارد؛ صفر یعنی انتخاب نشده</summary>
    [ObservableProperty]
    private int _doctorIndex;

    /// <summary>فهرست پزشکان — برای حالت بدون اتصال خودکار</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>بیمار فعلیِ این پزشک</summary>
    [ObservableProperty]
    private QueueRow? _currentPatient;

    /// <summary>صف انتظار شخصی</summary>
    [ObservableProperty]
    private ObservableCollection<QueueRow> _waiting = new();

    /// <summary>ویزیت‌شده‌های امروزِ این پزشک</summary>
    [ObservableProperty]
    private ObservableCollection<DoneRow> _done = new();

    /// <summary>شمار ویزیت‌شدهٔ امروز</summary>
    [ObservableProperty]
    private int _visitedCount;

    /// <summary>شمار در صف</summary>
    [ObservableProperty]
    private int _waitingCount;

    /// <summary>میانگین مدت ویزیت‌های امروز این پزشک</summary>
    [ObservableProperty]
    private string _averageDisplay = "—";

    /// <summary>در حال فراخوان</summary>
    [ObservableProperty]
    private bool _isCalling;

    /// <summary>سازنده — شناسایی خودکار پزشک و بارگذاری میزکار</summary>
    public DoctorVisitViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>شناسایی پزشکِ کاربر جاری + نخستین بارگذاری + تایمر ۳۰ ثانیه‌ای</summary>
    private async Task InitializeAsync()
    {
        // ۱) مشخصات کاربر جاری — users/me (نقطهٔ پایانی چرخهٔ بیست‌وهفتم)
        var me = await _api.GetAsync<MeEnvelope>("users/me");
        if (me?.Data is { } profile)
        {
            CurrentUserName = profile.FullName ?? profile.Username ?? "";
            LinkedDoctorId = profile.DoctorId;
            IsLinkedToDoctor = profile.DoctorId.HasValue;
        }

        // ۲) پزشکان برای حالت بدون اتصال (جانشینی/منشی) — همیشه پر می‌شود تا
        //    انتخابگر آماده باشد؛ در حالت متصل کاربردی ندارد و پنهان است
        var doctors = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (doctors?.Data is not null)
            foreach (var d in doctors.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        await LoadAsync();

        // تازه‌سازی زنده — هر ۳۰ ثانیه
        _refresher = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refresher.Tick += async (_, _) => await LoadAsync();
        _refresher.Start();
    }

    /// <summary>
    /// پزشکِ فعال — متصلِ خودکار یا منتخب دستی؛ تهی یعنی هنوز پزشکی مشخص نیست
    /// (صف همه‌پزشکان هرگز در میزکار نمایش داده نمی‌شود — این صفحه شخصی است)
    /// </summary>
    private long? ActiveDoctorId
    {
        get
        {
            if (IsLinkedToDoctor && LinkedDoctorId is { } linked)
                return linked;
            return DoctorIndex > 0 && DoctorIndex <= Doctors.Count ? Doctors[DoctorIndex - 1].Id : null;
        }
    }

    /// <summary>بارگذاری صف شخصی پزشک فعال</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        var doctorId = ActiveDoctorId;
        if (doctorId is null)
        {
            // هنوز پزشکی مشخص نیست — صف‌ها خالی می‌مانند و پیام راهنما در ویو هست
            Waiting = new ObservableCollection<QueueRow>();
            Done = new ObservableCollection<DoneRow>();
            CurrentPatient = null;
            VisitedCount = 0;
            WaitingCount = 0;
            AverageDisplay = "—";
            return;
        }

        var response = await _api.GetAsync<VisitsViewModel.QueueResponse>(
            $"visits/queue?doctorId={doctorId}");
        if (response?.Data is null)
        {
            ErrorMessage = _api.LastError ?? string.Empty;
            return;
        }

        var q = response.Data;
        VisitedCount = q.VisitedCount;
        WaitingCount = q.WaitingCount;
        AverageDisplay = q.AverageVisitMinutes > 0
            ? $"{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(q.AverageVisitMinutes.ToString("0.#"))} دقیقه"
            : "—";
        CurrentPatient = q.Current is null ? null : MapQueue(q.Current);
        Waiting = new ObservableCollection<QueueRow>(
            (q.Waiting ?? new()).Select(MapQueue));
        Done = new ObservableCollection<DoneRow>(
            (q.Done ?? new List<VisitsViewModel.DoneItem>())
                .Select(d => new DoneRow(d.VisitId, d.PatientName, d.TimeRange, d.DiagnosisText)));
    }

    /// <summary>نگاشت ردیف صف — همان نگاشت صف ویزیت (ارقام فارسی برای انتظار)</summary>
    private static QueueRow MapQueue(VisitsViewModel.QueueItem q) => new(
        q.VisitId, q.PatientId, q.PatientName, q.Mobile, q.QueueNumber,
        q.AppointmentTime, q.InsuranceName, q.VisitTypeText, q.WaitingMinutes,
        q.WaitingMinutes > 0
            ? $"{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(q.WaitingMinutes.ToString())} دقیقه"
            : "تازه رسیده",
        q.Allergies);

    /// <summary>تغییر انتخابگر دستی پزشک → بارگذاری صف همان پزشک</summary>
    partial void OnDoctorIndexChanged(int value) => _ = LoadAsync();

    /// <summary>فراخوان بیمار بعدیِ این پزشک — میانبر Space</summary>
    [RelayCommand]
    private async Task CallNextAsync()
    {
        var doctorId = ActiveDoctorId;
        if (doctorId is null)
        {
            _snackbar.Error("ابتدا پزشک میزکار را مشخص کنید");
            return;
        }
        if (IsCalling) return;
        IsCalling = true;
        var response = await _api.PostAsync<VisitsViewModel.CallNextResponse>(
            $"visits/call-next?doctorId={doctorId}", new { });
        IsCalling = false;

        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? "بیماری در صف انتظار شما نیست");
            return;
        }

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
        if (CurrentPatient is null) { _snackbar.Error("بیمار فعالی در ویزیت شما نیست"); return; }
        var window = new VisitWindow(new VisitEditViewModel(_api, _snackbar, CurrentPatient.ReceptionId))
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /* ─── پاسخ users/me — چرخهٔ بیست‌وهفتم ─── */

    /// <summary>پاکت پاسخ users/me</summary>
    public sealed class MeEnvelope
    {
        public bool IsSuccess { get; set; }
        public MeData? Data { get; set; }
    }

    /// <summary>دادهٔ کاربر جاری — شناسه‌های لازم برای شناسایی پزشک</summary>
    public sealed class MeData
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public long? EmployeeId { get; set; }
        public long? DoctorId { get; set; }
        public List<string>? Roles { get; set; }
    }
}
