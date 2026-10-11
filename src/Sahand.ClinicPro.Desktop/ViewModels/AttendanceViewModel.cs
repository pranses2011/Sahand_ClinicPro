using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel ماژول حضور و غیاب — تابلو امروز، دستگاه‌ها، شیفت‌ها، اصلاحیه‌ها و گزارش ماهانه
/// پنج تب کامل با فرمان‌های برخط
/// </summary>
public partial class AttendanceViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "حضور و غیاب پرسنل";

    /// <summary>نام‌های ماه شمسی</summary>
    public static string[] MonthNames { get; } =
        { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
          "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

    /// <summary>سال‌های قابل انتخاب</summary>
    public static int[] Years { get; } = Enumerable.Range(
        PersianDateHelper.ToJalali(DateTime.Today).Year - 3, 5).Reverse().ToArray();

    /// <summary>برندهای قابل انتخاب</summary>
    public static string[] Brands { get; } = { "ZKTeco", "Hikvision", "Generic" };

    /// <summary>سازنده</summary>
    public AttendanceViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;

        var today = PersianDateHelper.ToJalali(DateTime.Today);
        _selectedYear = today.Year;
        _selectedMonthIndex = today.Month - 1;

        _ = LoadBoardAsync();
        _ = LoadDevicesAsync();
        _ = LoadShiftsAsync();
        _ = LoadCorrectionsAsync();
    }

    // ═══════════════ تب ۱: تابلوی امروز ═══════════════

    /// <summary>تاریخ شمسی تابلو</summary>
    [ObservableProperty]
    private string _boardDate = "—";

    /// <summary>شمار پرسنل</summary>
    [ObservableProperty]
    private int _totalStaff;

    /// <summary>شمار حاضران</summary>
    [ObservableProperty]
    private int _presentCount;

    /// <summary>شمار تأخیری‌ها</summary>
    [ObservableProperty]
    private int _lateCount;

    /// <summary>شمار غایبان</summary>
    [ObservableProperty]
    private int _absentCount;

    /// <summary>شمار مرخصی</summary>
    [ObservableProperty]
    private int _leaveCount;

    /// <summary>شمار مأموریت</summary>
    [ObservableProperty]
    private int _missionCount;

    /// <summary>ردیف‌های تابلو</summary>
    [ObservableProperty]
    private ObservableCollection<BoardRow> _boardRows = new();

    /// <summary>بارگذاری تابلوی امروز</summary>
    [RelayCommand]
    public async Task LoadBoardAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var response = await _api.GetAsync<BoardResponse>("attendance/today-board");
            var data = response?.Data;
            if (data is null)
            {
                ErrorMessage = "دریافت تابلوی امروز ناموفق بود";
                return;
            }

            BoardDate = data.ShamsiDate;
            TotalStaff = data.TotalStaff;
            PresentCount = data.PresentCount;
            LateCount = data.LateCount;
            AbsentCount = data.AbsentCount;
            LeaveCount = data.LeaveCount;
            MissionCount = data.MissionCount;

            BoardRows = new ObservableCollection<BoardRow>(
                data.Rows?.Select(r => new BoardRow(
                    r.EmployeeId,
                    r.FullName,
                    r.PersonnelCode ?? "—",
                    r.DepartmentName ?? "—",
                    r.ShiftName ?? "بدون شیفت",
                    // چرخه هجدهم: ساعت‌های نمایشی حضوروغیاب با ارقام فارسی
                    r.CheckIn is { } ci ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits($"{ci:HH:mm}") : "—",
                    r.CheckOut is { } co ? Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits($"{co:HH:mm}") : "—",
                    r.LateMinutes > 0 ? $"{r.LateMinutes} دقیقه" : "—",
                    StatusText(r.Status),
                    StatusColor(r.Status),
                    r.IsManuallyEdited)) ?? []);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══════════════ تب ۲: دستگاه‌ها ═══════════════

    /// <summary>ردیف‌های دستگاه‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<DeviceRow> _devices = new();

    /// <summary>دستگاه انتخاب‌شده — برای آزمون/همگام‌سازی/نگاشت</summary>
    [ObservableProperty]
    private DeviceRow? _selectedDevice;

    /// <summary>حالت ویرایش — شناسه تهی یعنی ثبت جدید</summary>
    [ObservableProperty]
    private long? _editingDeviceId;

    /// <summary>عنوان فرم دستگاه — بر اساس حالت ثبت/ویرایش</summary>
    public string DeviceFormTitle => EditingDeviceId is null
        ? "➕ افزودن دستگاه جدید"
        : $"✏️ ویرایش دستگاه #{EditingDeviceId}";

    /// <summary>اعلان تغییر عنوان فرم هنگام تغییر حالت ویرایش</summary>
    partial void OnEditingDeviceIdChanged(long? value)
        => OnPropertyChanged(nameof(DeviceFormTitle));

    /// <summary>نام دستگاه فرم</summary>
    [ObservableProperty]
    private string _formName = string.Empty;

    /// <summary>برند فرم</summary>
    [ObservableProperty]
    private int _formBrandIndex;

    /// <summary>IP فرم</summary>
    [ObservableProperty]
    private string _formIp = "192.168.1.201";

    /// <summary>پورت فرم</summary>
    [ObservableProperty]
    private int _formPort = 4370;

    /// <summary>محل فرم</summary>
    [ObservableProperty]
    private string _formLocation = string.Empty;

    /// <summary>بازه همگام‌سازی فرم (دقیقه)</summary>
    [ObservableProperty]
    private int _formInterval = 5;

    /// <summary>کلید ارتباطی فرم</summary>
    [ObservableProperty]
    private string _formCommKey = string.Empty;

    /// <summary>ردیف‌های نگاشت دستگاه انتخابی</summary>
    [ObservableProperty]
    private ObservableCollection<MappingRow> _mappings = new();

    /// <summary>شناسه دستگاه فرم نگاشت</summary>
    [ObservableProperty]
    private string _mapEnrollNumber = string.Empty;

    /// <summary>شناسه پرسنل فرم نگاشت</summary>
    [ObservableProperty]
    private long _mapEmployeeId;

    /// <summary>بارگذاری فهرست دستگاه‌ها</summary>
    [RelayCommand]
    public async Task LoadDevicesAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var response = await _api.GetAsync<DevicesResponse>("attendance/devices");
            var data = response?.Data ?? [];
            Devices = new ObservableCollection<DeviceRow>(data.Select(d => new DeviceRow(
                d.Id, d.Name, d.Brand.ToString(), d.Model ?? "—",
                $"{d.IpAddress}:{d.Port}", d.Location ?? "—",
                DeviceStatusText(d.Status), DeviceStatusColor(d.Status),
                d.LastSyncAt is { } ls ? PersianDateHelper.ToShamsiWithTime(ls) : "هرگز",
                d.MappedUsersCount,
                d.LastError)));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بارگذاری نگاشت‌های دستگاه انتخابی</summary>
    partial void OnSelectedDeviceChanged(DeviceRow? value)
    {
        if (value is not null)
            _ = LoadMappingsAsync();
    }

    /// <summary>بارگذاری نگاشت‌ها</summary>
    [RelayCommand]
    public async Task LoadMappingsAsync()
    {
        if (SelectedDevice is null) return;
        try
        {
            var response = await _api.GetAsync<MappingsResponse>(
                $"attendance/devices/{SelectedDevice.Id}/mappings");
            var data = response?.Data ?? [];
            Mappings = new ObservableCollection<MappingRow>(data.Select(m => new MappingRow(
                m.Id, m.EnrollNumber, m.EmployeeId, m.EmployeeName, m.PersonnelCode ?? "—")));
        }
        catch (Exception ex)
        {
            _snackbar.Error($"بارگذاری نگاشت‌ها ناموفق: {ex.Message}");
        }
    }

    /// <summary>ذخیره فرم دستگاه — ثبت یا ویرایش</summary>
    [RelayCommand]
    public async Task SaveDeviceAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var response = await _api.PostAsync<IdResponse>("attendance/devices", new
            {
                id = EditingDeviceId,
                name = FormName,
                brand = FormBrandIndex + 1,
                model = (string?)null,
                serialNumber = (string?)null,
                ipAddress = FormIp,
                port = FormPort,
                communicationKey = string.IsNullOrWhiteSpace(FormCommKey) ? null : FormCommKey,
                location = string.IsNullOrWhiteSpace(FormLocation) ? null : FormLocation,
                syncIntervalMinutes = FormInterval,
                notes = (string?)null
            });

            if (response?.IsSuccess == true)
            {
                _snackbar.Success(EditingDeviceId is null
                    ? $"دستگاه «{FormName}» ثبت شد"
                    : $"دستگاه «{FormName}» به‌روزرسانی شد");
                ResetDeviceForm();
                await LoadDevicesAsync();
            }
            else
            {
                ErrorMessage = response?.Message ?? "ذخیره دستگاه ناموفق بود";
                _snackbar.Error(ErrorMessage);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ویرایش دستگاه انتخابی — پرکردن فرم</summary>
    [RelayCommand]
    public void EditDevice()
    {
        if (SelectedDevice is null) return;
        EditingDeviceId = SelectedDevice.Id;
        FormName = SelectedDevice.Name;
        FormBrandIndex = Math.Max(0, Array.IndexOf(Brands, SelectedDevice.Brand));
        FormIp = SelectedDevice.EndPoint.Split(':')[0];
        FormPort = int.TryParse(SelectedDevice.EndPoint.Split(':').Last(), out var p) ? p : 4370;
    }

    /// <summary>بازنشانی فرم دستگاه</summary>
    [RelayCommand]
    public void ResetDeviceForm()
    {
        EditingDeviceId = null;
        FormName = string.Empty;
        FormBrandIndex = 0;
        FormIp = "192.168.1.201";
        FormPort = 4370;
        FormLocation = string.Empty;
        FormInterval = 5;
        FormCommKey = string.Empty;
    }

    /// <summary>آزمون اتصال دستگاه انتخابی</summary>
    [RelayCommand]
    public async Task TestDeviceAsync()
    {
        if (SelectedDevice is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<MessageResponse>(
                $"attendance/devices/{SelectedDevice.Id}/test", null);
            if (response?.IsSuccess == true)
            {
                _snackbar.Success(response.Data?.Message ?? "اتصال برقرار است");
            }
            else
            {
                _snackbar.Error(response?.Data?.Message ?? response?.Message ?? "آزمون ناموفق بود");
            }
            await LoadDevicesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>همگام‌سازی فوری دستگاه انتخابی</summary>
    [RelayCommand]
    public async Task SyncDeviceAsync()
    {
        if (SelectedDevice is null) return;
        IsBusy = true;
        try
        {
            var response = await _api.PostAsync<SyncResponse>(
                $"attendance/devices/{SelectedDevice.Id}/sync", null);
            if (response?.IsSuccess == true && response.Data is { } sync)
            {
                _snackbar.Success(sync.Message);
            }
            else
            {
                _snackbar.Error(response?.Message ?? "همگام‌سازی ناموفق بود");
            }
            await LoadDevicesAsync();
            await LoadBoardAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ثبت نگاشت کاربر دستگاه</summary>
    [RelayCommand]
    public async Task SaveMappingAsync()
    {
        if (SelectedDevice is null)
        {
            _snackbar.Error("ابتدا یک دستگاه انتخاب کنید");
            return;
        }
        if (string.IsNullOrWhiteSpace(MapEnrollNumber))
        {
            _snackbar.Error("شناسه روی دستگاه را وارد کنید");
            return;
        }

        var response = await _api.PostAsync<IdResponse>("attendance/devices/mappings", new
        {
            deviceId = SelectedDevice.Id,
            enrollNumber = MapEnrollNumber,
            employeeId = MapEmployeeId
        });

        if (response?.IsSuccess == true)
        {
            _snackbar.Success("نگاشت ثبت شد");
            MapEnrollNumber = string.Empty;
            await LoadMappingsAsync();
            await LoadDevicesAsync();
        }
        else
        {
            _snackbar.Error(response?.Message ?? "ثبت نگاشت ناموفق بود");
        }
    }

    /// <summary>حذف نگاشت</summary>
    [RelayCommand]
    public async Task DeleteMappingAsync(MappingRow row)
    {
        if (row is null) return;
        var response = await _api.PostAsync($"attendance/mappings/{row.Id}/delete", null);
        if (response)
        {
            _snackbar.Success("نگاشت حذف شد");
            await LoadMappingsAsync();
        }
        else
        {
            _snackbar.Error("حذف نگاشت ناموفق بود");
        }
    }

    // ═══════════════ تب ۳: شیفت‌ها و تخصیص — بازسازی کامل چرخهٔ ۴۴ ═══════════════
    // گزارش مالک: «تعریف شیفت ها و تخصیص به پرسنل پیاده سازی نشده. بصورت
    // حرفه ای و کامل و زیبا پیاده سازی بکن». بک‌اند از ابتدا کامل بود (ثبت/
    // ویرایش/حذف شیفت + تخصیص با الگوی تکرار + پایان تخصیص + اعتبارسنجی)
    // ولی رابط کاربری فقط دو فرم خام داشت: ساعت‌ها «تایپ دستی HH:MM» بودند
    // (نقض قانون انتخابگر ساعت مالک) و تخصیص با «تایپ شناسهٔ عددی پرسنل و
    // شیفت» انجام می‌شد! بازسازی: انتخابگر ساعت عقربه‌ای + کمبوی جستجویی
    // پرسنل و شیفت + انتخابگر تاریخ شمسی بازه + چیپ‌های روز هفته + پالت
    // رنگ شیفت + ویرایش/حذف با دیالوگ فارسی خود برنامه + پایان تخصیص.

    /// <summary>ردیف‌های شیفت‌های تعریف‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<ShiftRow> _shifts = new();

    /// <summary>ردیف‌های تخصیص‌های فعال</summary>
    [ObservableProperty]
    private ObservableCollection<AssignmentRow> _assignments = new();

    /// <summary>
    /// گزینهٔ پرسنل در کمبوی تخصیص — جستجو با نام/کد پرسنلی (چرخهٔ ۴۴؛
    /// جایگزین «تایپ شناسهٔ عددی» که هیچ کاربری نمی‌دانست شناسه چیست)
    /// </summary>
    public record EmployeeOption(long Id, string DisplayName, string Subtitle);

    /// <summary>پرسنل‌های فعال برای کمبوی تخصیص</summary>
    [ObservableProperty]
    private ObservableCollection<EmployeeOption> _employees = new();

    /// <summary>گزینهٔ شیفت در کمبوی تخصیص — عنوان + بازهٔ ساعتی</summary>
    public record ShiftOption(long Id, string DisplayName, string Subtitle, string ColorHex);

    /// <summary>شیفت‌ها برای کمبوی تخصیص (از همان فهرست بارگذاری‌شده)</summary>
    [ObservableProperty]
    private ObservableCollection<ShiftOption> _shiftOptions = new();

    /* ─── فرم شیفت (ثبت/ویرایش) ─── */

    /// <summary>شناسهٔ شیفتِ در حال ویرایش — تهی یعنی حالت ثبت جدید</summary>
    [ObservableProperty]
    private long? _editingShiftId;

    /// <summary>عنوان دکمهٔ ثبت فرم شیفت — ثبت یا به‌روزرسانی</summary>
    public string ShiftFormTitle => EditingShiftId is null ? "تعریف شیفت جدید" : $"ویرایش شیفت (شناسهٔ {Money.ToPersianDigits(EditingShiftId.Value.ToString())})";

    /// <summary>نام شیفت فرم</summary>
    [ObservableProperty]
    private string _shiftName = string.Empty;

    /// <summary>ساعت شروع فرم — قرارداد انتخابگر ساعت (HH:mm لاتین)</summary>
    [ObservableProperty]
    private string _shiftStart = "08:00";

    /// <summary>ساعت پایان فرم — قرارداد انتخابگر ساعت (HH:mm لاتین)</summary>
    [ObservableProperty]
    private string _shiftEnd = "16:00";

    /// <summary>شیفت شب؟</summary>
    [ObservableProperty]
    private bool _shiftIsNight;

    /// <summary>تحمل تأخیر فرم (دقیقه)</summary>
    [ObservableProperty]
    private int _shiftLateGrace = 10;

    /// <summary>تحمل تعجیل فرم (دقیقه)</summary>
    [ObservableProperty]
    private int _shiftEarlyGrace = 10;

    /// <summary>استراحت کسرشونده فرم (دقیقه)</summary>
    [ObservableProperty]
    private int _shiftBreak = 30;

    /// <summary>شروع اضافه‌کاری پس از پایان شیفت (دقیقه)</summary>
    [ObservableProperty]
    private int _shiftOvertimeAfter = 15;

    /// <summary>رنگ تقویمی شیفت — هگز از پالت</summary>
    [ObservableProperty]
    private string _shiftColor = "#42A5F5";

    /// <summary>پالت رنگ شیفت‌ها — هماهنگ با رنگ‌های تقویم و کاشی‌های برنامه</summary>
    public static readonly string[] ShiftColorPalette =
        { "#42A5F5", "#66BB6A", "#FFA726", "#AB47BC", "#EF5350", "#26A69A", "#7E57C2", "#78909C" };

    partial void OnEditingShiftIdChanged(long? value)
        => OnPropertyChanged(nameof(ShiftFormTitle));

    /* ─── فرم تخصیص ─── */

    /// <summary>پرسنل انتخاب‌شدهٔ فرم تخصیص</summary>
    [ObservableProperty]
    private EmployeeOption? _selectedAssignEmployee;

    /// <summary>شیفت انتخاب‌شدهٔ فرم تخصیص</summary>
    [ObservableProperty]
    private ShiftOption? _selectedAssignShift;

    /// <summary>شروع بازهٔ تخصیص — تاریخ شمسی (قرارداد انتخابگر تاریخ)</summary>
    [ObservableProperty]
    private string? _assignFromDate;

    /// <summary>پایان بازهٔ تخصیص — تاریخ شمسی؛ تهی یعنی بی‌پایان</summary>
    [ObservableProperty]
    private string? _assignToDate;

    /// <summary>گزینه‌های الگوی تکرار — ثابت / هفتگی / روزهای منتخب</summary>
    public string[] RecurrenceOptions { get; } = { "ثابت — همهٔ روزهای بازه", "هفتگی — روزهای منتخب هفته", "سفارشی — روزهای منتخب" };

    /// <summary>نمایهٔ الگوی تکرار انتخاب‌شده — ۰ ثابت، ۱ هفتگی، ۲ سفارشی</summary>
    [ObservableProperty]
    private int _assignRecurrenceIndex;

    /// <summary>روزهای فعال هفته — بیت۰=شنبه تا بیت۶=جمعه</summary>
    [ObservableProperty]
    private bool _weekSat = true, _weekSun = true, _weekMon = true,
        _weekTue = true, _weekWed = true, _weekThu = true, _weekFri = false;

    /// <summary>اولویت تخصیص در تعارض — بزرگ‌تر برنده</summary>
    [ObservableProperty]
    private int _assignPriority;

    /// <summary>یادداشت تخصیص</summary>
    [ObservableProperty]
    private string? _assignNotes;

    /// <summary>
    /// نمایش چیپ‌های روز هفته فقط در حالت‌های هفتگی/سفارشی — با تغییر الگو
    /// بی‌درنگ نمایان/پنهان می‌شوند
    /// </summary>
    public bool ShowWeekDays => AssignRecurrenceIndex >= 1;

    partial void OnAssignRecurrenceIndexChanged(int value)
        => OnPropertyChanged(nameof(ShowWeekDays));

    /// <summary>محاسبهٔ بیت‌ماسک روزهای هفته از هفت پرچم فرم</summary>
    private int ComputeWeekDaysMask() =>
        (WeekSat ? 1 << 0 : 0) | (WeekSun ? 1 << 1 : 0) | (WeekMon ? 1 << 2 : 0)
        | (WeekTue ? 1 << 3 : 0) | (WeekWed ? 1 << 4 : 0) | (WeekThu ? 1 << 5 : 0)
        | (WeekFri ? 1 << 6 : 0);

    /// <summary>
    /// بارگذاری شیفت‌ها، تخصیص‌ها و پرسنل‌های فعال — چرخهٔ ۴۴: پرسنل‌ها هم
    /// همین‌جا بار می‌شوند تا کمبوی تخصیص از همان لحظه آماده باشد.
    /// </summary>
    [RelayCommand]
    public async Task LoadShiftsAsync()
    {
        IsBusy = true;
        try
        {
            // شیفت‌ها — ردیف‌های کارت‌مانند با رنگ و شمار تخصیص
            var shiftsResponse = await _api.GetAsync<ShiftsResponse>("attendance/shifts");
            var shifts = shiftsResponse?.Data ?? [];
            Shifts = new ObservableCollection<ShiftRow>(shifts.Select(s => new ShiftRow(
                s.Id, s.ShiftName,
                $"{Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits($"{s.StartTime:HH:mm}")} تا {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToPersianDigits($"{s.EndTime:HH:mm}")}",
                s.IsNightShift ? "🌙 شبانه" : "☀ روزانه",
                $"تأخیر ±{Money.ToPersianDigits(s.LateGraceMinutes.ToString())} دقیقه",
                s.AssignedCount,
                s.Color ?? "#42A5F5",
                s.IsActive)));

            // کمبوی شیفت فرم تخصیص
            ShiftOptions = new ObservableCollection<ShiftOption>(shifts.Where(s => s.IsActive)
                .Select(s => new ShiftOption(s.Id, s.ShiftName,
                    $"{PersianDateHelper.ToPersianDigits($"{s.StartTime:HH:mm}")} تا {PersianDateHelper.ToPersianDigits($"{s.EndTime:HH:mm}")}",
                    s.Color ?? "#42A5F5")));

            // تخصیص‌های فعال — با الگوی تکرار و رنگ شیفت
            var assignmentsResponse = await _api.GetAsync<AssignmentsResponse>("attendance/shift-assignments");
            var assignments = assignmentsResponse?.Data ?? [];
            Assignments = new ObservableCollection<AssignmentRow>(assignments.Select(a => new AssignmentRow(
                a.Id, a.EmployeeId, a.EmployeeName, a.PersonnelCode, a.ShiftName, a.ShiftColor,
                $"{PersianDateHelper.ToShamsi(a.FromDate.ToDateTime(TimeOnly.MinValue))}" +
                (a.ToDate is { } td ? $" تا {PersianDateHelper.ToShamsi(td.ToDateTime(TimeOnly.MinValue))}" : " (بی‌پایان)"),
                a.Recurrence switch
                {
                    2 => "هفتگی", 3 => "روزهای منتخب", _ => "ثابت",
                },
                WeekDaysMaskToFa(a.WeekDaysMask),
                a.Priority, a.Notes)));

            // پرسنل‌های فعال — کمبوی جستجویی تخصیص (یک‌بار؛ فهرست مرجع)
            if (Employees.Count == 0)
            {
                var employeesResponse = await _api.GetAsync<EmployeesResponse>("hr/employees");
                var employees = employeesResponse?.Data ?? [];
                Employees = new ObservableCollection<EmployeeOption>(
                    employees.Where(e => e.IsActive)
                        .Select(e => new EmployeeOption(e.Id, e.FullName,
                            $"{e.PersonnelCode ?? "—"} • {e.DepartmentName ?? e.PositionName ?? "پرسنل"}")));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بیت‌ماسک روزهای هفته → متن فارسی کوتاه (مثل «ش ی د س چ پ»)</summary>
    private static string WeekDaysMaskToFa(int mask)
    {
        if (mask == 0b01111111) return "همهٔ روزها";
        if (mask == 0b00011111) return "شنبه تا چهارشنبه";
        var letters = new[] { "ش", "ی", "د", "س", "چ", "پ", "ج" };
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 7; i++)
            if ((mask & (1 << i)) != 0) sb.Append(letters[i]).Append(' ');
        return sb.ToString().TrimEnd();
    }

    /// <summary>پر کردن فرم شیفت از ردیف انتخاب‌شده — حالت ویرایش</summary>
    [RelayCommand]
    private void EditShift(ShiftRow? row)
    {
        if (row is null) return;
        EditingShiftId = row.Id;
        ShiftName = row.Name;
        // بازهٔ متنی ردیف («۰۸:۰۰ تا ۱۶:۰۰») به دو جزء ساعت قرارداد انتخابگر
        var parts = row.TimeRange.Split(" تا ");
        if (parts.Length == 2)
        {
            ShiftStart = Money.PersianDigitsToLatin(parts[0].Trim());
            ShiftEnd = Money.PersianDigitsToLatin(parts[1].Trim());
        }
        ShiftIsNight = row.Kind.Contains("شبانه");
        ShiftColor = row.ColorHex;
        _snackbar.Info($"حالت ویرایش شیفت «{row.Name}» — پس از اعمال، دکمهٔ به‌روزرسانی را بزنید");
    }

    /// <summary>پاک‌کردن فرم شیفت و بازگشت به حالت ثبت جدید</summary>
    [RelayCommand]
    private void CancelShiftEdit()
    {
        EditingShiftId = null;
        ShiftName = string.Empty;
        ShiftStart = "08:00";
        ShiftEnd = "16:00";
        ShiftIsNight = false;
        ShiftLateGrace = 10;
        ShiftEarlyGrace = 10;
        ShiftBreak = 30;
        ShiftOvertimeAfter = 15;
        ShiftColor = "#42A5F5";
    }

    /// <summary>ذخیرهٔ شیفت — ثبت جدید یا به‌روزرسانی (چرخهٔ ۴۴: ویرایش پشتیبانی شد)</summary>
    [RelayCommand]
    public async Task SaveShiftAsync()
    {
        if (string.IsNullOrWhiteSpace(ShiftName))
        {
            _snackbar.Error("نام شیفت الزامی است");
            return;
        }
        if (!TimeOnly.TryParse(ShiftStart, out var start)
            || !TimeOnly.TryParse(ShiftEnd, out var end))
        {
            _snackbar.Error("ساعت‌ها را از انتخابگر زمان انتخاب کنید");
            return;
        }

        var response = await _api.PostAsync<IdResponse>("attendance/shifts", new
        {
            id = EditingShiftId,
            shiftName = ShiftName.Trim(),
            shiftCode = (string?)null,
            startTime = start.ToString("HH:mm"),
            endTime = end.ToString("HH:mm"),
            shiftType = ShiftIsNight ? 2 : 1,
            isNightShift = ShiftIsNight,
            lateGraceMinutes = ShiftLateGrace,
            earlyLeaveGraceMinutes = ShiftEarlyGrace,
            minMinutesBetweenPunches = 5,
            breakMinutes = ShiftBreak,
            overtimeStartAfterMinutes = ShiftOvertimeAfter,
            color = ShiftColor
        });

        if (response?.IsSuccess == true)
        {
            var verb = EditingShiftId is null ? "ثبت" : "به‌روزرسانی";
            _snackbar.Success($"شیفت «{ShiftName.Trim()}» {verb} شد");
            CancelShiftEdit();
            await LoadShiftsAsync();
        }
        else
        {
            _snackbar.Error(response?.Message ?? "ثبت شیفت ناموفق بود");
        }
    }

    /// <summary>حذف شیفت — دیالوگ تأیید فارسی خود برنامه (قانون مالک)</summary>
    [RelayCommand]
    private async Task DeleteShiftAsync(ShiftRow? row)
    {
        if (row is null) return;
        if (row.Assigned > 0)
        {
            _snackbar.Error($"شیفت «{row.Name}» به {Money.ToPersianDigits(row.Assigned.ToString())} پرسنل تخصیص دارد — نخست تخصیص‌ها را پایان دهید");
            return;
        }

        var confirmed = Views.AppDialog.Confirm("حذف شیفت",
            $"شیفت «{row.Name}» ({PersianDateHelper.ToPersianDigits(row.TimeRange)}) حذف شود؟ این عمل بازگشت‌ناپذیر است.",
            Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        var ok = await _api.DeleteAsync($"attendance/shifts/{row.Id}");
        if (ok)
        {
            _snackbar.Success($"شیفت «{row.Name}» حذف شد");
            await LoadShiftsAsync();
        }
        else
        {
            _snackbar.Error(_api.LastError ?? "حذف شیفت ناموفق بود");
        }
    }

    /// <summary>تخصیص شیفت به پرسنل — فرم کامل چرخهٔ ۴۴ (جستجوگر + تاریخ + تکرار)</summary>
    [RelayCommand]
    public async Task AssignShiftAsync()
    {
        if (SelectedAssignEmployee is null || SelectedAssignShift is null)
        {
            _snackbar.Error("پرسنل و شیفت را از فهرست انتخاب کنید");
            return;
        }

        // تاریخ شروع — شمسیِ انتخابگر → میلادی استاندارد سرور
        var fromGregorian = PersianDateHelper.ToGregorian(AssignFromDate ?? "");
        if (fromGregorian is null)
        {
            _snackbar.Error("تاریخ شروع بازه را از انتخابگر تاریخ انتخاب کنید");
            return;
        }
        var fromDate = DateOnly.FromDateTime(fromGregorian.Value);
        DateOnly? toDate = null;
        if (!string.IsNullOrWhiteSpace(AssignToDate))
        {
            var toGregorian = PersianDateHelper.ToGregorian(AssignToDate);
            if (toGregorian is null)
            {
                _snackbar.Error("قالب تاریخ پایان معتبر نیست — از انتخابگر تاریخ انتخاب کنید");
                return;
            }
            toDate = DateOnly.FromDateTime(toGregorian.Value);
        }

        // الگوی تکرار → مقدار سرور (۱ ثابت / ۲ هفتگی / ۳ سفارشی) + ماسک روزها
        var recurrence = AssignRecurrenceIndex switch { 1 => 2, 2 => 3, _ => 1 };
        var mask = AssignRecurrenceIndex == 0 ? 0b01111111 : ComputeWeekDaysMask();
        if (AssignRecurrenceIndex >= 1 && mask == 0)
        {
            _snackbar.Error("دست‌کم یک روز هفته را انتخاب کنید");
            return;
        }

        var response = await _api.PostAsync<IdResponse>("attendance/shift-assignments", new
        {
            employeeId = SelectedAssignEmployee.Id,
            shiftId = SelectedAssignShift.Id,
            fromDate = fromDate.ToString("yyyy-MM-dd"),
            toDate = toDate?.ToString("yyyy-MM-dd"),
            recurrence,
            weekDaysMask = mask,
            priority = AssignPriority,
            notes = string.IsNullOrWhiteSpace(AssignNotes) ? null : AssignNotes.Trim()
        });

        if (response?.IsSuccess == true)
        {
            _snackbar.Success($"شیفت «{SelectedAssignShift.DisplayName}» به «{SelectedAssignEmployee.DisplayName}» تخصیص شد");
            SelectedAssignEmployee = null;
            SelectedAssignShift = null;
            AssignToDate = null;
            AssignNotes = null;
            await LoadShiftsAsync();
        }
        else
        {
            _snackbar.Error(response?.Message ?? "ثبت تخصیص ناموفق بود");
        }
    }

    /// <summary>پایان تخصیص شیفت — از امروز دیگر اعتبار ندارد (دیالوگ فارسی)</summary>
    [RelayCommand]
    private async Task EndAssignmentAsync(AssignmentRow? row)
    {
        if (row is null) return;

        var confirmed = Views.AppDialog.Confirm("پایان تخصیص شیفت",
            $"تخصیص شیفت «{row.ShiftName}» برای «{row.EmployeeName}» از امروز پایان یابد؟ ردیف از فهرست تخصیص‌های فعال کنده می‌شود.",
            Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        var ok = await _api.PostAsync($"attendance/shift-assignments/{row.Id}/end",
            new { endDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd") });
        if (ok)
        {
            _snackbar.Success($"تخصیص شیفت «{row.ShiftName}» پایان یافت");
            await LoadShiftsAsync();
        }
        else
        {
            _snackbar.Error(_api.LastError ?? "پایان تخصیص ناموفق بود");
        }
    }

    // ═══════════════ تب ۴: اصلاحیه‌ها ═══════════════

    /// <summary>ردیف‌های اصلاحیه</summary>
    [ObservableProperty]
    private ObservableCollection<CorrectionRow> _corrections = new();

    /// <summary>فقط در انتظارها؟</summary>
    [ObservableProperty]
    private bool _onlyPending = true;

    /// <summary>بارگذاری اصلاحیه‌ها</summary>
    [RelayCommand]
    public async Task LoadCorrectionsAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<CorrectionsResponse>(
                $"attendance/corrections?onlyPending={(OnlyPending ? "true" : "false")}");
            var data = response?.Data ?? [];
            Corrections = new ObservableCollection<CorrectionRow>(data.Select(c => new CorrectionRow(
                c.Id, c.EmployeeName, c.ShamsiDate, c.FieldName,
                $"{c.OldValue ?? "—"} ← {c.NewValue}",
                c.Reason,
                c.Status == 1 ? "در انتظار" : c.Status == 2 ? "تأییدشده" : "ردشده",
                c.Status == 1,
                c.Status == 1 ? "#FB8C00" : c.Status == 2 ? "#43A047" : "#D32F2F")));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>تغییر فیلتر — بارگذاری مجدد</summary>
    partial void OnOnlyPendingChanged(bool value) => _ = LoadCorrectionsAsync();

    /// <summary>تأیید اصلاحیه</summary>
    [RelayCommand]
    public async Task ApproveCorrectionAsync(CorrectionRow row)
    {
        if (row is null) return;
        var response = await _api.PostAsync($"attendance/corrections/{row.Id}/decide",
            new { approve = true, note = (string?)null });
        if (response)
        {
            _snackbar.Success("اصلاحیه تأیید و اعمال شد");
            await LoadCorrectionsAsync();
            await LoadBoardAsync();
        }
        else
        {
            _snackbar.Error("تأیید اصلاحیه ناموفق بود");
        }
    }

    /// <summary>رد اصلاحیه</summary>
    [RelayCommand]
    public async Task RejectCorrectionAsync(CorrectionRow row)
    {
        if (row is null) return;
        var response = await _api.PostAsync($"attendance/corrections/{row.Id}/decide",
            new { approve = false, note = "رد شد" });
        if (response)
        {
            _snackbar.Success("اصلاحیه رد شد");
            await LoadCorrectionsAsync();
        }
        else
        {
            _snackbar.Error("رد اصلاحیه ناموفق بود");
        }
    }

    // ═══════════════ تب ۵: گزارش ماهانه ═══════════════

    /// <summary>سال گزارش</summary>
    [ObservableProperty]
    private int _selectedYear;

    /// <summary>اندیس ماه گزارش</summary>
    [ObservableProperty]
    private int _selectedMonthIndex;

    /// <summary>ردیف‌های گزارش</summary>
    [ObservableProperty]
    private ObservableCollection<MonthlyRow> _monthlyRows = new();

    /// <summary>جمع کارکرد ماه</summary>
    [ObservableProperty]
    private decimal _totalWorked;

    /// <summary>جمع اضافه‌کاری ماه</summary>
    [ObservableProperty]
    private decimal _totalOvertime;

    /// <summary>بارگذاری گزارش ماهانه</summary>
    [RelayCommand]
    public async Task LoadMonthlyAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var response = await _api.GetAsync<MonthlyResponse>(
                $"attendance/monthly-report?year={SelectedYear}&month={SelectedMonthIndex + 1}");
            var data = response?.Data ?? [];
            MonthlyRows = new ObservableCollection<MonthlyRow>(data.Select(r => new MonthlyRow(
                r.FullName, r.PersonnelCode ?? "—", r.DepartmentName ?? "—",
                r.PresentDays.ToString(), r.LateCount.ToString(),
                r.TotalLateMinutes > 0 ? $"{r.TotalLateMinutes} دقیقه" : "—",
                $"{r.TotalWorkedHours:0.##}", $"{r.TotalOvertimeHours:0.##}",
                r.AbsentDays.ToString())));
            TotalWorked = data.Sum(r => r.TotalWorkedHours);
            TotalOvertime = data.Sum(r => r.TotalOvertimeHours);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطا: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══════════════ کمکی‌ها ═══════════════

    /// <summary>متن فارسی وضعیت حضور</summary>
    private static string StatusText(int status) => status switch
    {
        1 => "حاضر",
        2 => "غایب",
        3 => "مرخصی",
        4 => "مأموریت",
        5 => "تأخیر",
        _ => "نامشخص",
    };

    /// <summary>رنگ وضعیت حضور</summary>
    private static string StatusColor(int status) => status switch
    {
        1 => "#43A047",
        2 => "#D32F2F",
        3 => "#1E88E5",
        4 => "#8E24AA",
        5 => "#FB8C00",
        _ => "#757575",
    };

    /// <summary>متن فارسی وضعیت دستگاه</summary>
    private static string DeviceStatusText(int status) => status switch
    {
        1 => "متصل",
        2 => "قطع",
        3 => "در حال سینک",
        4 => "خطا",
        5 => "غیرفعال",
        _ => "؟",
    };

    /// <summary>رنگ وضعیت دستگاه</summary>
    private static string DeviceStatusColor(int status) => status switch
    {
        1 => "#43A047",
        2 => "#757575",
        3 => "#1E88E5",
        4 => "#D32F2F",
        5 => "#9E9E9E",
        _ => "#757575",
    };

    // ═══════════════ DTO های JSON ═══════════════

    private sealed class BoardResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public BoardData? Data { get; set; }
    }

    private sealed class BoardData
    {
        public string ShamsiDate { get; set; } = "—";
        public int TotalStaff { get; set; }
        public int PresentCount { get; set; }
        public int LateCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int MissionCount { get; set; }
        public List<BoardDto>? Rows { get; set; }
    }

    private sealed class BoardDto
    {
        public long EmployeeId { get; set; }
        public string FullName { get; set; } = "";
        public string? PersonnelCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? ShiftName { get; set; }
        public TimeOnly? CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public int LateMinutes { get; set; }
        public int Status { get; set; }
        public bool IsManuallyEdited { get; set; }
    }

    private sealed class DevicesResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<DeviceDto>? Data { get; set; }
    }

    private sealed class DeviceDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public int Brand { get; set; }
        public string? Model { get; set; }
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public string? Location { get; set; }
        public int Status { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public int MappedUsersCount { get; set; }
        public string? LastError { get; set; }
    }

    private sealed class MappingsResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<MappingDto>? Data { get; set; }
    }

    private sealed class MappingDto
    {
        public long Id { get; set; }
        public string EnrollNumber { get; set; } = "";
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string? PersonnelCode { get; set; }
    }

    private sealed class ShiftsResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<ShiftDto>? Data { get; set; }
    }

    private sealed class ShiftDto
    {
        public long Id { get; set; }
        public string ShiftName { get; set; } = "";
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsNightShift { get; set; }
        public int LateGraceMinutes { get; set; }
        public int AssignedCount { get; set; }
        public string? Color { get; set; }
        /// <summary>شیفت فعال است؟ — چرخهٔ ۴۴ (فیلتر کمبوی تخصیص)</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>پاسخ فهرست پرسنل — کمبوی تخصیص چرخهٔ ۴۴</summary>
    private sealed class EmployeesResponse
    {
        public bool IsSuccess { get; set; }
        public List<EmployeeDto>? Data { get; set; }
    }

    /// <summary>ردیف پرسنل — منطبق بر EmployeeListDto سرور</summary>
    private sealed class EmployeeDto
    {
        public long Id { get; set; }
        public string? PersonnelCode { get; set; }
        public string FullName { get; set; } = "";
        public string? PositionName { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class AssignmentsResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<AssignmentDto>? Data { get; set; }
    }

    private sealed class AssignmentDto
    {
        public long Id { get; set; }
        public long EmployeeId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string? PersonnelCode { get; set; }
        public string ShiftName { get; set; } = "";
        public string? ShiftColor { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        /// <summary>الگوی تکرار — ۱ ثابت / ۲ هفتگی / ۳ سفارشی (چرخهٔ ۴۴)</summary>
        public int Recurrence { get; set; }
        public int WeekDaysMask { get; set; }
        public int Priority { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class CorrectionsResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<CorrectionDto>? Data { get; set; }
    }

    private sealed class CorrectionDto
    {
        public long Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public string ShamsiDate { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string? OldValue { get; set; }
        public string NewValue { get; set; } = "";
        public string Reason { get; set; } = "";
        public int Status { get; set; }
    }

    private sealed class MonthlyResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public List<MonthlyDto>? Data { get; set; }
    }

    private sealed class MonthlyDto
    {
        public string FullName { get; set; } = "";
        public string? PersonnelCode { get; set; }
        public string? DepartmentName { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateCount { get; set; }
        public int TotalLateMinutes { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
    }

    private sealed class IdResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public long Data { get; set; }
    }

    private sealed class MessageResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public MessageData? Data { get; set; }
    }

    private sealed class MessageData
    {
        public string? Message { get; set; }
    }

    private sealed class SyncResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public SyncResultDto? Data { get; set; }
    }

    private sealed class SyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    // ═══════════════ ردیف‌های نمایشی ═══════════════

    /// <summary>ردیف تابلوی امروز</summary>
    public record BoardRow(
        long EmployeeId, string FullName, string PersonnelCode,
        string Department, string Shift, string CheckIn, string CheckOut,
        string Late, string Status, string StatusHex, bool Edited);

    /// <summary>ردیف دستگاه</summary>
    public record DeviceRow(
        long Id, string Name, string Brand, string Model, string EndPoint,
        string Location, string Status, string StatusHex, string LastSync,
        int MappedUsers, string? LastError);

    /// <summary>ردیف نگاشت</summary>
    public record MappingRow(
        long Id, string EnrollNumber, long EmployeeId, string EmployeeName, string PersonnelCode);

    /// <summary>
    /// ردیف شیفت — چرخهٔ ۴۴: IsActive برای نشان فعال/غیرفعال و سایر فیلدها
    /// برای فرم ویرایش (بازخوانی مقادیر به فرم) اضافه شدند
    /// </summary>
    public record ShiftRow(
        long Id, string Name, string TimeRange, string Kind, string Grace, int Assigned,
        string ColorHex, bool IsActive);

    /// <summary>
    /// ردیف تخصیص — چرخهٔ ۴۴: کد پرسنلی، رنگ شیفت، الگوی تکرار، روزهای
    /// فعال و یادداشت برای نمایش حرفه‌ای و دکمهٔ پایان تخصیص
    /// </summary>
    public record AssignmentRow(
        long Id, long EmployeeId, string EmployeeName, string? PersonnelCode,
        string ShiftName, string? ShiftColor, string Range, string Recurrence,
        string WeekDays, int Priority, string? Notes);

    /// <summary>ردیف اصلاحیه</summary>
    public record CorrectionRow(
        long Id, string EmployeeName, string Date, string Field, string Change,
        string Reason, string Status, bool Pending, string StatusHex);

    /// <summary>ردیف گزارش ماهانه</summary>
    public record MonthlyRow(
        string FullName, string PersonnelCode, string Department,
        string Present, string LateCount, string LateTotal,
        string Worked, string Overtime, string Absent);
}
