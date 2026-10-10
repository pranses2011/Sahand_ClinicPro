using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف اتاق در پنجرهٔ مدیریت اتاق‌ها
/// </summary>
public record RoomManageRow(
    long Id,
    string? RoomNumber,
    string? RoomName,
    int? Floor,
    bool IsActive)
{
    /// <summary>نام نمایشی برای ستون جدول</summary>
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(RoomName) ? RoomName :
        $"اتاق {RoomNumber}";

    /// <summary>متن وضعیت فعال/غیرفعال</summary>
    public string StatusText => IsActive ? "فعال" : "غیرفعال";
}

/// <summary>
/// ViewModel پنجرهٔ مدیریت اتاق‌های معاینه — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
/// «در تعریف پزشک فیلد اتاق برای انتخاب هست ولی جایی برای تعریف اتاق و اسم اتاق نیست»
/// افزودن/ویرایش/غیرفعال‌سازی اتاق از داخل فرم پزشک بدون مراجعه به دیتابیس.
/// ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) ═══ «مدیریت اتاق‌ها رو در منوی کناری هم
/// قرار بده» — همین ویومدل هم‌اکنون پایهٔ صفحهٔ منوی کناری (RoomsView) نیز
/// هست؛ وراثت به ViewModelBase رفت تا عنوان صفحه و پرچم مشغولی از پایه
/// بیاید و پنجرهٔ قبلیِ فرم پزشک بدون هیچ تغییری به کار خود ادامه دهد.
/// </summary>
public partial class RoomsManagerViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه — هدر پوسته (چرخهٔ ۳۹: هم‌اکنون پایهٔ صفحهٔ منوی کناری هم هست)</summary>
    public override string Title => "مدیریت اتاق‌های درمانگاه";

    /// <summary>فهرست کامل اتاق‌ها (شامل غیرفعال)</summary>
    [ObservableProperty]
    private ObservableCollection<RoomManageRow> _rooms = new();

    /// <summary>ردیف انتخاب‌شده برای ویرایش</summary>
    [ObservableProperty]
    private RoomManageRow? _selectedRoom;

    /// <summary>شماره اتاق در فرم افزودن/ویرایش</summary>
    [ObservableProperty]
    private string _roomNumber = string.Empty;

    /// <summary>نام اتاق</summary>
    [ObservableProperty]
    private string? _roomName;

    /// <summary>طبقه</summary>
    [ObservableProperty]
    private int? _floor;

    /* ─── چرخهٔ ۳۹ — خلاصه‌های صفحهٔ منوی کناری ─── */

    /// <summary>جمع اتاق‌های فعال — چیپ آمار بالای صفحه</summary>
    public int ActiveCount => Rooms.Count(r => r.IsActive);

    /// <summary>جمع اتاق‌های غیرفعال (حذف‌نرم‌شده) — چیپ آمار بالای صفحه</summary>
    public int InactiveCount => Rooms.Count(r => !r.IsActive);

    /// <summary>راهنمای صفحه — اتاق‌ها در برنامهٔ کاری پزشک و پذیرش خدمتی استفاده می‌شوند</summary>
    public static string HintText =>
        "اتاق‌ها در «برنامهٔ کاری پزشک» و «پذیرش خدمت درمانی» انتخاب می‌شوند؛ اتاقِ غیرفعال در فهرست‌ها نمی‌آید ولی تاریخچهٔ پذیرش‌های قبلی آن حفظ می‌شود.";

    /// <summary>سازنده — بارگذاری خودکار فهرست</summary>
    public RoomsManagerViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری فهرست کامل اتاق‌ها از سرور</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        var response = await _api.GetAsync<RoomsManageResponse>("lookups/rooms/manage");
        if (response?.Data is not null)
            Rooms = new ObservableCollection<RoomManageRow>(
                response.Data.Select(r => new RoomManageRow(r.Id, r.RoomNumber, r.RoomName, r.Floor, r.IsActive)));
        else
            ErrorMessage = _api.LastError ?? "دریافت فهرست اتاق‌ها ناموفق بود";
        IsBusy = false;
    }

    /// <summary>انتخاب ردیف برای ویرایش — پرکردن فرم با مقادیر فعلی</summary>
    partial void OnSelectedRoomChanged(RoomManageRow? value)
    {
        if (value is null) return;
        RoomNumber = value.RoomNumber ?? string.Empty;
        RoomName = value.RoomName;
        Floor = value.Floor;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// نوسازی چیپ‌های آمار پس از هر بارگذاری — پراپرتی‌های محاسباتی خودکار
    /// اطلاع‌رسانی نمی‌کنند؛ با رویداد تغییر Rooms دستی اعلام می‌شوند (چرخهٔ ۳۹)
    /// </summary>
    partial void OnRoomsChanged(ObservableCollection<RoomManageRow> value)
    {
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(InactiveCount));
    }

    /// <summary>افزودن اتاق جدید</summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        if (!Validate()) return;
        IsBusy = true;
        var ok = await _api.PostAsync("lookups/rooms",
            new { roomNumber = RoomNumber.Trim(), roomName = RoomName, floor = Floor });
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success($"اتاق «{RoomNumber.Trim()}» ثبت شد");
            ResetForm();
            await LoadAsync();
        }
        else
            ErrorMessage = _api.LastError ?? "ثبت اتاق ناموفق بود";
    }

    /// <summary>ذخیرهٔ ویرایش اتاق انتخاب‌شده</summary>
    [RelayCommand]
    private async Task UpdateAsync()
    {
        if (SelectedRoom is null)
        {
            ErrorMessage = "ابتدا یک اتاق از فهرست انتخاب کنید";
            return;
        }
        if (!Validate()) return;
        IsBusy = true;
        var ok = await _api.PutAsync($"lookups/rooms/{SelectedRoom.Id}",
            new
            {
                roomNumber = RoomNumber.Trim(),
                roomName = RoomName,
                floor = Floor,
                isActive = true
            });
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success("اطلاعات اتاق به‌روزرسانی شد");
            ResetForm();
            await LoadAsync();
        }
        else
            ErrorMessage = _api.LastError ?? "ویرایش اتاق ناموفق بود";
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) اتاق انتخاب‌شده</summary>
    [RelayCommand]
    private async Task DeactivateAsync()
    {
        if (SelectedRoom is null)
        {
            ErrorMessage = "ابتدا یک اتاق از فهرست انتخاب کنید";
            return;
        }

        var confirmed = Views.AppDialog.Confirm("غیرفعال‌سازی اتاق", $"اتاق «{SelectedRoom.DisplayName}» غیرفعال شود؟ دیگر در برنامهٔ کاری پزشکان قابل انتخاب نخواهد بود.", Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"lookups/rooms/{SelectedRoom.Id}");
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success("اتاق غیرفعال شد");
            ResetForm();
            await LoadAsync();
        }
        else
            ErrorMessage = _api.LastError ?? "غیرفعال‌سازی ناموفق بود";
    }

    /// <summary>اعتبارسنجی مشترک فرم</summary>
    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(RoomNumber))
        {
            ErrorMessage = "شماره اتاق الزامی است";
            return false;
        }
        return true;
    }

    /// <summary>پاک‌کردن فرم پس از عملیات موفق</summary>
    private void ResetForm()
    {
        SelectedRoom = null;
        RoomNumber = string.Empty;
        RoomName = null;
        Floor = null;
        ErrorMessage = string.Empty;
    }

    /// <summary>پاکت پاسخ فهرست اتاق‌ها</summary>
    public sealed class RoomsManageResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoomManageDto>? Data { get; set; }
    }

    /// <summary>DTO اتاق</summary>
    public sealed class RoomManageDto
    {
        public long Id { get; set; }
        public string? RoomNumber { get; set; }
        public string? RoomName { get; set; }
        public int? Floor { get; set; }
        public bool IsActive { get; set; }
    }
}
