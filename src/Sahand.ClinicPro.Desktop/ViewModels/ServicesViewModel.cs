using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف بیمار در صف خدمات — نام خدمت و اتاق انجام هر ردیف همراه بیمار
/// </summary>
public record ServiceQueueRow(
    long ReceptionId,
    long PatientId,
    string PatientName,
    string Mobile,
    int QueueNumber,
    string ServiceName,
    string RoomDisplay,
    string ReceptionTime,
    int WaitingMinutes,
    string WaitingText,
    string? Notes);

/// <summary>
/// ردیف انجام‌شده‌های خدمات امروز
/// </summary>
public record ServiceDoneRow(long ReceptionId, string PatientName, string ServiceName, string? RoomDisplay);

/// <summary>
/// ═══ چرخهٔ ۷۸ (گزارش مالک) — ViewModel صفحهٔ مستقل «صف خدمات درمانی» ═══
/// «در صفحه ویزیت و معاینه فقط صف پذیرش‌های پزشکان رو نشون بده و برای صف و
/// انجام خدمات درمانی یک صفحه جداگانه باشه» — پذیرش‌های خدمتیِ بدون پزشک
/// (تزریقات، پانسمان، فیزیوتراپی و…) از صف ویزیت جدا شدند و اینجا در سه
/// ستون «در انتظار / در حال انجام / انجام‌شده» با فراخوان به اتاق و اعلام
/// انجام مدیریت می‌شوند. تازه‌سازی هر ۳۰ ثانیه (هم‌الگوی صف ویزیت).
/// </summary>
public partial class ServicesViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private System.Windows.Threading.DispatcherTimer? _refresher;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "صف خدمات درمانی";

    /// <summary>در انتظار نوبت خدمت</summary>
    [ObservableProperty]
    private int _waitingCount;

    /// <summary>روی میز خدمت (فراخوانده‌شده)</summary>
    [ObservableProperty]
    private int _inProgressCount;

    /// <summary>انجام‌شدهٔ امروز</summary>
    [ObservableProperty]
    private int _doneCount;

    /// <summary>ردیف‌های در انتظار</summary>
    [ObservableProperty]
    private ObservableCollection<ServiceQueueRow> _waiting = new();

    /// <summary>ردیف‌های روی میز خدمت</summary>
    [ObservableProperty]
    private ObservableCollection<ServiceQueueRow> _inProgress = new();

    /// <summary>انجام‌شده‌های امروز</summary>
    [ObservableProperty]
    private ObservableCollection<ServiceDoneRow> _done = new();

    /// <summary>در حال فراخوان/ثبت انجام</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>سازنده — بارگذاری اولیه + تایمر ۳۰ ثانیه‌ای</summary>
    public ServicesViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری اولیه + تایمر تازه‌سازی صف</summary>
    private async Task InitializeAsync()
    {
        await LoadAsync();
        _refresher = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refresher.Tick += async (_, _) => await LoadAsync();
        _refresher.Start();
    }

    /// <summary>بارگذاری صف خدمات از سرور</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        var response = await _api.GetAsync<ServiceQueueResponse>("receptions/service-queue");
        if (response?.Data is null)
        {
            ErrorMessage = _api.LastError ?? string.Empty;
            return;
        }

        var q = response.Data;
        WaitingCount = q.WaitingCount;
        InProgressCount = q.InProgressCount;
        DoneCount = q.DoneCount;
        Waiting = new ObservableCollection<ServiceQueueRow>((q.Waiting ?? new()).Select(MapRow));
        InProgress = new ObservableCollection<ServiceQueueRow>((q.InProgress ?? new()).Select(MapRow));
        Done = new ObservableCollection<ServiceDoneRow>((q.Done ?? new List<ServiceDoneItem>())
            .Select(d => new ServiceDoneRow(d.ReceptionId, d.PatientName, d.ServiceName,
                d.RoomName is null ? null : $"اتاق {d.RoomName}")));
    }

    /// <summary>
    /// نگاشت ردیف — چرخهٔ ۷۸: زمان انتظار و ساعت با ارقام فارسی؛ نام اتاق
    /// همیشه با پیشوند «اتاق» تا در نگاه اول از نام خدمت تفکیک شود
    /// </summary>
    private static ServiceQueueRow MapRow(ServiceQueueItem q) => new(
        q.ReceptionId, q.PatientId, q.PatientName, q.Mobile, q.QueueNumber,
        q.ServiceName,
        q.RoomName is null ? "بدون اتاق" : $"اتاق {q.RoomName}",
        Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(q.ReceptionTime),
        q.WaitingMinutes,
        q.WaitingMinutes > 0
            ? $"{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits(q.WaitingMinutes.ToString())} دقیقه"
            : "تازه رسیده",
        q.Notes);

    /// <summary>
    /// فراخوان بیمار خدمت به اتاق انجام — ردیف به «در حال انجام» می‌رود و
    /// نمایشگر سالن همان لحظه به‌روز می‌شود (پخش بلادرنگ سرور).
    /// </summary>
    /// <param name="row">ردیف کلیک‌شده</param>
    [RelayCommand]
    private async Task CallServiceAsync(ServiceQueueRow? row)
    {
        if (row is null) return;
        if (IsBusy) return;
        IsBusy = true;
        // فرمان‌های بدون‌داده با PostAsync بولی — همان الگوی ثبت/لغو لیست انتظار
        var ok = await _api.PostAsync($"receptions/service-call/{row.ReceptionId}", new { });
        IsBusy = false;

        if (!ok)
        {
            _snackbar.Error(_api.LastError ?? "فراخوان این بیمار ممکن نشد");
            await LoadAsync();
            return;
        }

        _snackbar.Success($"«{row.PatientName}» به {row.RoomDisplay} فراخوانده شد");
        await LoadAsync();
    }

    /// <summary>
    /// اعلام انجام خدمت — پایان ردیف و انتقال به «انجام‌شده‌های امروز»؛
    /// از هر دو ستون (در انتظار/در حال انجام) قابل اجراست.
    /// </summary>
    /// <param name="row">ردیف کلیک‌شده</param>
    [RelayCommand]
    private async Task CompleteServiceAsync(ServiceQueueRow? row)
    {
        if (row is null) return;
        if (IsBusy) return;

        // تأیید فارسی برنامه — به‌جای MessageBox ویندوزی (قانون پروژه)
        if (!Views.AppDialog.Confirm("اعلام انجام خدمت",
                $"خدمت «{row.ServiceName}» برای «{row.PatientName}» انجام شد و ثبت گردد؟"))
            return;

        IsBusy = true;
        var ok = await _api.PostAsync($"receptions/service-complete/{row.ReceptionId}", new { });
        IsBusy = false;

        if (!ok)
        {
            _snackbar.Error(_api.LastError ?? "ثبت انجام خدمت ممکن نشد");
            await LoadAsync();
            return;
        }

        _snackbar.Success($"خدمت «{row.ServiceName}» برای «{row.PatientName}» انجام‌شده ثبت شد");
        await LoadAsync();
    }

    /* ─── پاسخ‌های API ─── */

    /// <summary>پاکت پاسخ صف خدمات</summary>
    public sealed class ServiceQueueResponse
    {
        public bool IsSuccess { get; set; }
        public ServiceQueueData? Data { get; set; }
    }

    /// <summary>دادهٔ صف خدمات — سه بخش + شمارنده‌ها</summary>
    public sealed class ServiceQueueData
    {
        public List<ServiceQueueItem>? Waiting { get; set; }
        public List<ServiceQueueItem>? InProgress { get; set; }
        public List<ServiceDoneItem>? Done { get; set; }
        public int WaitingCount { get; set; }
        public int InProgressCount { get; set; }
        public int DoneCount { get; set; }
    }

    /// <summary>ردیف صف خدمات از سرور</summary>
    public sealed class ServiceQueueItem
    {
        public long ReceptionId { get; set; }
        public long PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public int QueueNumber { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? RoomName { get; set; }
        public string ReceptionTime { get; set; } = string.Empty;
        public int WaitingMinutes { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>ردیف انجام‌شده از سرور</summary>
    public sealed class ServiceDoneItem
    {
        public long ReceptionId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string? RoomName { get; set; }
    }
}
