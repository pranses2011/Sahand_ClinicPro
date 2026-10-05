using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>نقشه عملیات انگلیسی به عنوان فارسی — برای ماتریس دسترسی</summary>
internal static class ActionTitleMapper
{
    private static readonly Dictionary<string, string> Titles = new()
    {
        ["view"] = "مشاهده", ["create"] = "ایجاد", ["edit"] = "ویرایش",
        ["delete"] = "حذف", ["print"] = "چاپ", ["export"] = "خروجی"
    };

    /// <summary>عنوان فارسی عملیات با جایگزین توضیح کامل</summary>
    public static string ToTitle(string? action, string? description, string name)
        => action is not null && Titles.TryGetValue(action, out var title)
            ? title
            : description ?? name;
}

/// <summary>نقش در فهرست سمت راست فرم ۲۵</summary>
public partial class RoleListRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>نام نقش</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>توضیح</summary>
    public string? Description { get; init; }

    /// <summary>نقش سیستمی — غیرقابل حذف</summary>
    public bool IsSystem { get; init; }

    /// <summary>تعداد کاربران</summary>
    public int UserCount { get; init; }

    /// <summary>تعداد دسترسی‌ها</summary>
    public int PermissionCount { get; init; }

    /// <summary>رنگ نشان نقش — پالت رنگی طبق پرامپت</summary>
    public string BadgeColor { get; init; } = "#1E88E5";

    /// <summary>متن نشان سیستمی</summary>
    public string SystemText => IsSystem ? "🔒 سیستمی" : $"{UserCount} کاربر";

    /// <summary>برچسب شمارش دسترسی</summary>
    public string PermText => $"{PermissionCount} دسترسی";
}

/// <summary>مجوز در ماتریس — چک‌باکس</summary>
public partial class PermissionCheckRow : ObservableObject
{
    /// <summary>شناسه مجوز</summary>
    public long Id { get; init; }

    /// <summary>نام کامل مثل patients.view</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>عملیات فارسی مثل «مشاهده»</summary>
    public string ActionTitle { get; init; } = string.Empty;

    /// <summary>انتخاب‌شده</summary>
    [ObservableProperty]
    private bool _isChecked;
}

/// <summary>گروه ماژول در ماتریس — کارت با انتخاب گروهی</summary>
public partial class PermissionGroupCard : ObservableObject
{
    /// <summary>عنوان فارسی ماژول</summary>
    public string ModuleTitle { get; init; } = string.Empty;

    /// <summary>مجوزهای ماژول</summary>
    public ObservableCollection<PermissionCheckRow> Permissions { get; } = new();

    /// <summary>آیا همه انتخاب‌شده‌اند</summary>
    public bool AllChecked => Permissions.All(p => p.IsChecked);

    /// <summary>آیا هیچ انتخاب نشده</summary>
    public bool NoneChecked => Permissions.All(p => !p.IsChecked);

    /// <summary>انتخاب همه مجوزهای این ماژول</summary>
    [RelayCommand]
    private void CheckAll()
    {
        foreach (var p in Permissions) p.IsChecked = true;
        OnPropertyChanged(nameof(AllChecked));
        OnPropertyChanged(nameof(NoneChecked));
    }

    /// <summary>لغو همه مجوزهای این ماژول</summary>
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var p in Permissions) p.IsChecked = false;
        OnPropertyChanged(nameof(AllChecked));
        OnPropertyChanged(nameof(NoneChecked));
    }
}

/// <summary>
/// ViewModel نقش‌ها و ماتریس دسترسی — گام U13 طبق فرم ۲۵:
/// ستون راست نقش‌ها + ستون چپ ماتریس ۲۴ گروهی با انتخاب همه/هیچ و ذخیره
/// </summary>
public partial class RolesViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "نقش‌ها و دسترسی‌ها";

    /// <summary>رنگ‌های نشان نقش — پالت چرخشی</summary>
    private static readonly string[] BadgePalette =
        { "#1E88E5", "#43A047", "#F9A825", "#E65100", "#C62828", "#8E24AA", "#00897B", "#546E7A" };

    /// <summary>فهرست نقش‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<RoleListRow> _roles = new();

    /// <summary>نقش انتخاب‌شده — بارگذاری ماتریس</summary>
    [ObservableProperty]
    private RoleListRow? _selectedRole;

    /// <summary>گروه‌های ماتریس دسترسی</summary>
    [ObservableProperty]
    private ObservableCollection<PermissionGroupCard> _permissionGroups = new();

    /// <summary>نام نقش جدید</summary>
    [ObservableProperty]
    private string? _newRoleName;

    /// <summary>سازنده</summary>
    public RolesViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری فهرست نقش‌ها و کاتالوگ مجوزها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // فهرست نقش‌ها
            var roles = await _api.GetAsync<RolesResponse>("roles");
            Roles = new ObservableCollection<RoleListRow>(
                (roles?.Data ?? new List<RoleItemDto>()).Select((r, i) => new RoleListRow
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystem = r.IsSystem,
                    UserCount = r.UserCount,
                    PermissionCount = r.PermissionCount,
                    BadgeColor = BadgePalette[i % BadgePalette.Length]
                }));

            // کاتالوگ گروه‌های مجوز — یک‌بار
            if (PermissionGroups.Count == 0)
            {
                var groups = await _api.GetAsync<PermissionsResponse>("permissions");
                PermissionGroups = new ObservableCollection<PermissionGroupCard>(
                    (groups?.Data ?? new List<PermissionGroupDto>()).Select(g => new PermissionGroupCard
                    {
                        ModuleTitle = g.ModuleTitle
                    }));
                foreach (var g in PermissionGroups)
                    foreach (var p in groups!.Data!.First(x => x.ModuleTitle == g.ModuleTitle).Permissions)
                        g.Permissions.Add(new PermissionCheckRow
                        {
                            Id = p.Id,
                            Name = p.Name,
                            ActionTitle = ActionTitleMapper.ToTitle(p.Action, p.Description, p.Name)
                        });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بارگذاری ماتریس دسترسی نقش انتخاب‌شده</summary>
    partial void OnSelectedRoleChanged(RoleListRow? value)
    {
        if (value is not null)
            _ = LoadRolePermissionsAsync(value.Id);
    }

    /// <summary>بارگذاری مجوزهای نقش و علامت‌گذاری چک‌باکس‌ها</summary>
    private async Task LoadRolePermissionsAsync(long roleId)
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<PermissionIdsResponse>($"roles/{roleId}/permissions");
            var assigned = new HashSet<long>(response?.Data ?? new List<long>());

            foreach (var group in PermissionGroups)
                foreach (var p in group.Permissions)
                    p.IsChecked = assigned.Contains(p.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ذخیره ماتریس دسترسی نقش انتخاب‌شده</summary>
    [RelayCommand]
    private async Task SavePermissionsAsync()
    {
        if (SelectedRole is null) { _snackbar.Info("ابتدا یک نقش انتخاب کنید"); return; }

        var permissionIds = PermissionGroups
            .SelectMany(g => g.Permissions)
            .Where(p => p.IsChecked)
            .Select(p => p.Id)
            .ToList();

        IsBusy = true;
        var ok = await _api.PutAsync($"roles/{SelectedRole.Id}/permissions",
            new { permissionIds });
        IsBusy = false;

        if (ok) { _snackbar.Success($"دسترسی‌های «{SelectedRole.Name}» ذخیره شد ({permissionIds.Count} مجوز)"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ذخیره دسترسی‌ها ناموفق بود");
    }

    /// <summary>انتخاب همه دسترسی‌ها در همه ماژول‌ها</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var group in PermissionGroups)
            foreach (var p in group.Permissions)
                p.IsChecked = true;
        _snackbar.Info("همه دسترسی‌ها انتخاب شد");
    }

    /// <summary>لغو همه دسترسی‌ها در همه ماژول‌ها</summary>
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var group in PermissionGroups)
            foreach (var p in group.Permissions)
                p.IsChecked = false;
        _snackbar.Info("همه دسترسی‌ها لغو شد");
    }

    /// <summary>ثبت نقش جدید</summary>
    [RelayCommand]
    private async Task AddRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRoleName) || NewRoleName.Trim().Length < 3)
        { _snackbar.Error("نام نقش حداقل ۳ کاراکتر است"); return; }

        IsBusy = true;
        var ok = await _api.PostAsync("roles", new { name = NewRoleName.Trim(), description = (string?)null });
        IsBusy = false;

        if (ok) { _snackbar.Success("نقش ایجاد شد"); NewRoleName = string.Empty; await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "ایجاد نقش ناموفق بود");
    }

    /// <summary>حذف نقش غیرسیستمی بدون کاربر</summary>
    [RelayCommand]
    private async Task DeleteRoleAsync()
    {
        if (SelectedRole is null) { _snackbar.Info("ابتدا یک نقش انتخاب کنید"); return; }
        if (SelectedRole.IsSystem) { _snackbar.Error("نقش سیستمی قابل حذف نیست"); return; }
        if (SelectedRole.UserCount > 0) { _snackbar.Error("این نقش به کاربران اختصاص دارد"); return; }

        IsBusy = true;
        var ok = await _api.DeleteAsync($"roles/{SelectedRole.Id}");
        IsBusy = false;

        if (ok) { _snackbar.Success("نقش حذف شد"); await LoadAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class RolesResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoleItemDto>? Data { get; set; }
    }

    private sealed class RoleItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    private sealed class PermissionsResponse
    {
        public bool IsSuccess { get; set; }
        public List<PermissionGroupDto>? Data { get; set; }
    }

    private sealed class PermissionGroupDto
    {
        public string ModuleTitle { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    private sealed class PermissionItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Module { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
    }

    private sealed class PermissionIdsResponse
    {
        public bool IsSuccess { get; set; }
        public List<long>? Data { get; set; }
    }
}
