using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

// ═══════════════════════════════════════════════════════════════════
// چرخهٔ چهل‌وهفتم (گزارش مالک):
// «مدیریت شیفت ها یک صفحه مجزا باشه و همچنین در ثبت شیفت خطای
//  "ثبت شیفت ناموفق بود" میده»
//
// ① صفحهٔ مجزا: تمام منطق شیفت‌ها/تخصیص‌ها که درون تب سوم صفحهٔ «حضور و
//    غیاب» (AttendanceViewModel — نزدیک نیمی از آن کلاس) زندگی می‌کرد به
//    این ViewModel مستقل منتقل شد و نمای خودش (ShiftsView) را گرفت؛
//    صفحهٔ حضور و غیاب اکنون فقط تابلوی امروز/دستگاه‌ها/اصلاح کارت/گزارش
//    ماهانه است و منوی کناری ورودی مستقیم «مدیریت شیفت‌ها» دارد.
// ② خطای ثبت: پیام خطای سرور (دسترسی ناکافی/اعتبارسنجی/تکراری‌بودن کد)
//    پیشتر وقتی پاکت پاسخ تهی می‌آمد گم می‌شد و همیشه متن عمومی
//    «ثبت شیفت ناموفق بود» چاپ می‌شد؛ اکنون زنجیرهٔ «پیام پاکت → LastError
//    کلاینت → متن عمومی» پیام واقعی را نشان می‌دهد تا کاربر بداند چرا.
//
// ساختار و رفتارها عین چرخهٔ ۴۴ است (انتخابگر عقربه‌ای ساعت، انتخابگر تاریخ
// شمسی بازهٔ اعتبار، چیپ‌های روز هفته، پالت رنگ تقویمی، ویرایش/حذف با
// دیالوگ فارسی خود برنامه و پایان تخصیص) — فقط جابه‌جایی، نه بازطراحی.
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// ViewModel صفحهٔ مستقل «مدیریت شیفت‌ها» — تعریف الگوهای زمانی کار،
/// تخصیص آن‌ها به پرسنل و پایان‌دادن تخصیص‌ها؛ پایهٔ محاسبهٔ تأخیر/کارکرد/اضافه‌کاری.
/// </summary>
public partial class ShiftsViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "مدیریت شیفت‌ها";

    /// <summary>ردیف‌های شیفت‌های تعریف‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<ShiftRow> _shifts = new();

    /// <summary>ردیف‌های تخصیص‌های فعال</summary>
    [ObservableProperty]
    private ObservableCollection<AssignmentRow> _assignments = new();

    /// <summary>
    /// گزینهٔ پرسنل در کمبوی تخصیص — جستجو با نام/کد پرسنلی (هم‌الگوی چرخهٔ ۴۴)
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
    public string ShiftFormTitle => EditingShiftId is null
        ? "تعریف شیفت جدید"
        : $"ویرایش شیفت (شناسهٔ {Money.ToPersianDigits(EditingShiftId.Value.ToString())})";

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
    public string[] RecurrenceOptions { get; } =
        { "ثابت — همهٔ روزهای بازه", "هفتگی — روزهای منتخب هفته", "سفارشی — روزهای منتخب" };

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

    /// <summary>سازنده — بارگذاری اولیهٔ شیفت‌ها/تخصیص‌ها/پرسنل</summary>
    public ShiftsViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadShiftsAsync();
    }

    /// <summary>محاسبهٔ بیت‌ماسک روزهای هفته از هفت پرچم فرم</summary>
    private int ComputeWeekDaysMask() =>
        (WeekSat ? 1 << 0 : 0) | (WeekSun ? 1 << 1 : 0) | (WeekMon ? 1 << 2 : 0)
        | (WeekTue ? 1 << 3 : 0) | (WeekWed ? 1 << 4 : 0) | (WeekThu ? 1 << 5 : 0)
        | (WeekFri ? 1 << 6 : 0);

    /// <summary>
    /// بارگذاری شیفت‌ها، تخصیص‌ها و پرسنل‌های فعال — پرسنل‌ها هم همین‌جا
    /// بار می‌شوند تا کمبوی تخصیص از همان لحظه آماده باشد.
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
                $"{PersianDateHelper.ToPersianDigits($"{s.StartTime:HH:mm}")} تا {PersianDateHelper.ToPersianDigits($"{s.EndTime:HH:mm}")}",
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

    /// <summary>
    /// ذخیرهٔ شیفت — ثبت جدید یا به‌روزرسانی.
    /// چرخهٔ ۴۷ (گزارش مالک: «در ثبت شیفت خطای "ثبت شیفت ناموفق بود" میده»):
    /// زنجیرهٔ پیام خطا اکنون «پیام پاکت پاسخ → LastError کلاینت → متن عمومی»
    /// است تا علت واقعی سرور (مجوز attendance.shifts، اعتبارسنجی ۰-۱۲۰ دقیقه،
    /// تکراری‌بودن کد و…) به کاربر برسد نه متنِ عمومیِ بی‌اطلاع.
    /// </summary>
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
            // ═══ چرخهٔ ۶۷ (گزارش مالک: «در مدیریت شیفت‌ها و ثبت شیفت خطای
            //      "اطلاعات ارسالی نامعتبر است" میده») ═══
            // ریشه: دیسریالایز بومی TimeOnly در System.Text.Json نسخهٔ net8.0
            // فقط قالب کامل «HH:mm:ss» را می‌پذیرد؛ فرمت کوتاه «HH:mm» باعث
            // JsonException → خطای ModelState → پیام عمومی ۴۰۰ می‌شد. ارسال با
            // ثانیه، پارس را در هر نسخهٔ NET تضمین می‌کند.
            startTime = start.ToString("HH:mm:ss"),
            endTime = end.ToString("HH:mm:ss"),
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
            // زنجیرهٔ پیام: پاکت → کلاینت → عمومی — چرخهٔ ۴۷
            _snackbar.Error(response?.Message
                ?? _api.LastError
                ?? "ثبت شیفت ناموفق بود");
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

    /// <summary>تخصیص شیفت به پرسنل — فرم کامل (جستجوگر + تاریخ + تکرار)</summary>
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
            _snackbar.Error(response?.Message ?? _api.LastError ?? "ثبت تخصیص ناموفق بود");
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

    /* ─── ردیف‌های نمایش ─── */

    /// <summary>
    /// ردیف شیفت — IsActive برای نشان فعال/غیرفعال و سایر فیلدها برای فرم
    /// ویرایش (بازخوانی مقادیر به فرم)
    /// </summary>
    public record ShiftRow(
        long Id, string Name, string TimeRange, string Kind, string Grace, int Assigned,
        string ColorHex, bool IsActive);

    /// <summary>
    /// ردیف تخصیص — کد پرسنلی، رنگ شیفت، الگوی تکرار، روزهای فعال و
    /// یادداشت برای نمایش حرفه‌ای و دکمهٔ پایان تخصیص
    /// </summary>
    public record AssignmentRow(
        long Id, long EmployeeId, string EmployeeName, string? PersonnelCode,
        string ShiftName, string? ShiftColor, string Range, string Recurrence,
        string WeekDays, int Priority, string? Notes);

    /* ─── پاسخ‌های API ─── */

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
        /// <summary>شیفت فعال است؟ (فیلتر کمبوی تخصیص)</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>پاسخ فهرست پرسنل — کمبوی تخصیص</summary>
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
        /// <summary>الگوی تکرار — ۱ ثابت / ۲ هفتگی / ۳ سفارشی</summary>
        public int Recurrence { get; set; }
        public int WeekDaysMask { get; set; }
        public int Priority { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class IdResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public long Data { get; set; }
    }
}
