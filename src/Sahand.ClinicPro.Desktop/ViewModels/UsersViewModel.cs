using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف کاربر در فهرست — فرم ۲۴</summary>
public partial class UserRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>نام کاربری</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>نام کامل از پرسنل مرتبط</summary>
    public string? FullName { get; init; }

    /// <summary>ایمیل</summary>
    public string? Email { get; init; }

    /// <summary>نقش‌ها</summary>
    public List<string> Roles { get; init; } = new();

    /// <summary>آخرین ورود — میلادی UTC برای مبدل شمسی</summary>
    public DateTime? LastLoginAt { get; init; }

    /// <summary>پایان قفل حساب</summary>
    public DateTime? LockoutEnd { get; init; }

    /// <summary>فعال</summary>
    public bool IsActive { get; init; }

    /// <summary>متن وضعیت — 🟢فعال / 🔴قفل / ⚫غیرفعال</summary>
    public string StatusText => !IsActive ? "غیرفعال"
        : LockoutEnd is not null && LockoutEnd > DateTime.UtcNow ? "قفل" : "فعال";

    /// <summary>رنگ نشان وضعیت</summary>
    public string StatusColor => !IsActive ? "#B0BEC5"
        : LockoutEnd is not null && LockoutEnd > DateTime.UtcNow ? "#C62828" : "#2E7D32";

    /// <summary>متن نقش‌ها با کاما</summary>
    public string RolesText => Roles.Count == 0 ? "—" : string.Join("، ", Roles);
}

/// <summary>
/// ViewModel مدیریت کاربران — گام U13 طبق فرم ۲۴:
/// فهرست با جستجو و فیلتر نقش/وضعیت + عملیات ردیف (ویرایش/ریست رمز/قفل/حذف)
/// </summary>
public partial class UsersViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly IServiceProvider _sp;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "کاربران";

    /// <summary>فهرست کاربران</summary>
    [ObservableProperty]
    private ObservableCollection<UserRow> _users = new();

    /// <summary>کاربر انتخاب‌شده</summary>
    [ObservableProperty]
    private UserRow? _selectedUser;

    /// <summary>جستجو — نام کاربری/نام/ایمیل</summary>
    [ObservableProperty]
    private string? _search;

    /// <summary>فیلتر وضعیت — همه/فعال/قفل/غیرفعال</summary>
    [ObservableProperty]
    private string? _statusFilter;

    /// <summary>گزینه‌های فیلتر وضعیت</summary>
    public List<string> StatusOptions { get; } = new() { "همه", "فعال", "قفل", "غیرفعال" };

    /// <summary>نقش‌ها برای فیلتر</summary>
    public ObservableCollection<string> RoleOptions { get; } = new() { "همه" };

    /// <summary>فیلتر نقش</summary>
    [ObservableProperty]
    private string? _roleFilter = "همه";

    /// <summary>متن سربرگ تعداد</summary>
    [ObservableProperty]
    private string _countText = "کاربران سیستم";

    /// <summary>سازنده</summary>
    public UsersViewModel(IApiClient api, ISnackbarService snackbar, IServiceProvider sp)
    {
        _api = api;
        _snackbar = snackbar;
        _sp = sp;
        StatusFilter = "همه";
        _ = LoadAsync();
    }

    /// <summary>بارگذاری فهرست کاربران و نقش‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // نقش‌ها برای فیلتر — یک‌بار بارگذاری
            if (RoleOptions.Count <= 1)
            {
                var roles = await _api.GetAsync<RolesResponse>("roles");
                foreach (var r in roles?.Data ?? new List<RoleItemDto>())
                    RoleOptions.Add(r.Name);
            }

            var status = StatusFilter switch
            {
                "فعال" => "active", "قفل" => "locked", "غیرفعال" => "inactive", _ => (string?)null
            };
            var role = RoleFilter is null or "همه" ? null : RoleFilter;

            var query = $"users?page=1&pageSize=100" +
                        (string.IsNullOrWhiteSpace(Search) ? "" : $"&search={Uri.EscapeDataString(Search)}") +
                        (status is null ? "" : $"&status={status}") +
                        (role is null ? "" : $"&role={Uri.EscapeDataString(role)}");

            var response = await _api.GetAsync<UsersResponse>(query);
            Users = new ObservableCollection<UserRow>(
                (response?.Data?.Items ?? new List<UserItemDto>()).Select(u => new UserRow
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Roles = u.Roles ?? new List<string>(),
                    LastLoginAt = u.LastLoginAt,
                    LockoutEnd = u.LockoutEnd,
                    IsActive = u.IsActive
                }));
            CountText = $"کل: {Users.Count} کاربر";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>باز کردن فرم ثبت کاربر جدید</summary>
    [RelayCommand]
    private void AddUser()
    {
        var window = _sp.GetRequiredService<UserFormWindow>();
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ویرایش کاربر — از دکمه ردیف یا انتخاب‌شده</summary>
    [RelayCommand]
    private void EditUser(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null) { _snackbar.Info("ابتدا یک کاربر انتخاب کنید"); return; }
        var window = _sp.GetRequiredService<UserFormWindow>();
        window.Owner = System.Windows.Application.Current.MainWindow;
        _ = window.ViewModel.LoadForEdit(user.Id);
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ریست رمز عبور — تولید رمز جدید و نمایش برای کپی</summary>
    [RelayCommand]
    private async Task ResetPasswordAsync(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null) { _snackbar.Info("ابتدا یک کاربر انتخاب کنید"); return; }
        IsBusy = true;
        var result = await _api.PostAsync<ResetPasswordResponse>($"users/{user.Id}/reset-password", null);
        IsBusy = false;
        if (result?.IsSuccess == true && result.Data?.NewPassword is not null)
        {
            // نمایش رمز تولیدشده — فقط یک‌بار قابل مشاهده است
            System.Windows.Clipboard.SetText(result.Data.NewPassword);
            _snackbar.Success($"رمز جدید: {result.Data.NewPassword} — در حافظه کپی شد");
        }
        else
            _snackbar.Error(_api.LastError ?? "تولید رمز ناموفق بود");
    }

    /// <summary>قفل/آزادسازی حساب کاربر</summary>
    [RelayCommand]
    private async Task ToggleLockAsync(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null) { _snackbar.Info("ابتدا یک کاربر انتخاب کنید"); return; }
        var locked = user.LockoutEnd is not null && user.LockoutEnd > DateTime.UtcNow;
        IsBusy = true;
        var ok = await _api.PostAsync($"users/{user.Id}/{(locked ? "unlock" : "lock")}", null);
        IsBusy = false;
        if (ok) { _snackbar.Success(locked ? "حساب آزاد شد" : "حساب به مدت ۳۰ دقیقه قفل شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "عملیات ناموفق بود");
    }

    /// <summary>غیرفعال‌سازی (حذف) کاربر</summary>
    [RelayCommand]
    private async Task DeleteUserAsync(UserRow? row)
    {
        var user = row ?? SelectedUser;
        if (user is null) { _snackbar.Info("ابتدا یک کاربر انتخاب کنید"); return; }
        if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        { _snackbar.Error("کاربر مدیر اصلی قابل حذف نیست"); return; }

        IsBusy = true;
        var ok = await _api.DeleteAsync($"users/{user.Id}");
        IsBusy = false;
        if (ok) { _snackbar.Success("کاربر غیرفعال شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class UsersResponse
    {
        public bool IsSuccess { get; set; }
        public UsersPage? Data { get; set; }
    }

    private sealed class UsersPage
    {
        public List<UserItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private sealed class UserItemDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public List<string>? Roles { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool IsActive { get; set; }
    }

    internal sealed class RolesResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoleItemDto>? Data { get; set; }
    }

    internal sealed class RoleItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    private sealed class ResetPasswordResponse
    {
        public bool IsSuccess { get; set; }
        public ResetPasswordData? Data { get; set; }
    }

    private sealed class ResetPasswordData
    {
        public string? NewPassword { get; set; }
    }
}
