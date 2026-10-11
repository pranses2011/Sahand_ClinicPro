using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
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

/// <summary>نقش در فهرست سمت راست فرم ۲۵ — چرخهٔ ۴۴: بج شمار و متن وضعیت غنی‌تر</summary>
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

    /// <summary>حرف نخست نام نقش — نشان دایره‌ای حرفی چرخهٔ ۴۴</summary>
    public string Initial => string.IsNullOrWhiteSpace(Name) ? "؟" : Name.Trim()[0].ToString();

    /// <summary>متن نشان سیستمی</summary>
    public string SystemText => IsSystem ? "🔒 سیستمی" : "نقش دلخواه";

    /// <summary>برچسب شمارش دسترسی</summary>
    public string PermText => $"{Money.ToPersianDigits(PermissionCount.ToString())} دسترسی";
}

/// <summary>مجوز در ماتریس — چک‌باکس؛ چرخهٔ ۴۴: رویداد تغییر برای ردیابی ذخیره‌نشده</summary>
public partial class PermissionCheckRow : ObservableObject
{
    /// <summary>رویداد تغییر وضعیت انتخاب — ماتریس والد/ViewModel از این باخبر می‌شود
    /// (شمارنده‌های گروه و نشان «تغییرات ذخیره‌نشده» هر دو به آن وصل‌اند)</summary>
    public event Action? CheckedStateChanged;

    /// <summary>شناسه مجوز</summary>
    public long Id { get; init; }

    /// <summary>نام کامل مثل patients.view</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>عملیات فارسی مثل «مشاهده»</summary>
    public string ActionTitle { get; init; } = string.Empty;

    /// <summary>انتخاب‌شده</summary>
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>اثر تغییر انتخاب → آگاه‌سازی شمارندهٔ گروه و ردیاب تغییرات</summary>
    partial void OnIsCheckedChanged(bool value) => CheckedStateChanged?.Invoke();
}

/// <summary>گروه ماژول در ماتریس — کارت با انتخاب گروهی؛ چرخهٔ ۴۴: جستجو + شمارنده</summary>
public partial class PermissionGroupCard : ObservableObject
{
    /// <summary>عنوان فارسی ماژول</summary>
    public string ModuleTitle { get; init; } = string.Empty;

    /// <summary>نام فنی ماژول (مثل attendance) — برای جستجو و نگاشت آیکون</summary>
    public string ModuleKey { get; init; } = string.Empty;

    /// <summary>مجوزهای کامل ماژول (فهرست اصلی — پالایش روی فهرست نمایشی)</summary>
    public ObservableCollection<PermissionCheckRow> Permissions { get; } = new();

    /// <summary>
    /// فهرست نمایشی پس از پالایش جستجو — چرخهٔ ۴۴؛ بدون جستجو همان
    /// Permissions است (ارجاع مستقیم)
    /// </summary>
    public ObservableCollection<PermissionCheckRow> VisiblePermissions { get; } = new();

    /// <summary>گروه با جستجوی جاری منطبق است؟ (پنهان‌سازی کارت‌های بی‌نتیجه)</summary>
    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>آیا همه انتخاب‌شده‌اند</summary>
    public bool AllChecked => Permissions.All(p => p.IsChecked);

    /// <summary>آیا هیچ انتخاب نشده</summary>
    public bool NoneChecked => Permissions.All(p => !p.IsChecked);

    /// <summary>متن شمارندهٔ «n از m» گوشهٔ کارت</summary>
    public string CountText => $"{Money.ToPersianDigits(Permissions.Count(p => p.IsChecked).ToString())} از {Money.ToPersianDigits(Permissions.Count.ToString())}";

    /// <summary>به‌روزرسانی نمایشی‌های وابسته (شمارنده/پرچم‌های همه-هیچ)</summary>
    public void RefreshCounters()
    {
        OnPropertyChanged(nameof(AllChecked));
        OnPropertyChanged(nameof(NoneChecked));
        OnPropertyChanged(nameof(CountText));
    }

    /// <summary>
    /// اعمال فیلتر جستجو — چرخهٔ ۴۴: تطبیق شامل روی عنوان عملیات، نام فنی
    /// مجوز و عنوان ماژول؛ تهی/خالی یعنی بدون فیلتر (همه). کارتی که هیچ
    /// مجوز منطبقی ندارد پنهان می‌شود.
    /// </summary>
    public void ApplyFilter(string? text)
    {
        VisiblePermissions.Clear();
        var isFiltering = !string.IsNullOrWhiteSpace(text);
        var needle = isFiltering ? Normalize(text!) : null;

        foreach (var p in Permissions)
        {
            var match = !isFiltering
                || Normalize(p.ActionTitle).Contains(needle!, StringComparison.Ordinal)
                || Normalize(p.Name).Contains(needle!, StringComparison.Ordinal)
                || Normalize(ModuleTitle).Contains(needle!, StringComparison.Ordinal);
            if (match)
                VisiblePermissions.Add(p);
        }

        IsVisible = VisiblePermissions.Count > 0;
    }

    /// <summary>نرمال‌سازی جستجو — ارقام فارسی/عربی به لاتین + حذف فاصله‌ها</summary>
    private static string Normalize(string text) =>
        Money.PersianDigitsToLatin(text.Trim())
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .ToLowerInvariant();

    /// <summary>انتخاب همه مجوزهای این ماژول</summary>
    [RelayCommand]
    private void CheckAll()
    {
        foreach (var p in Permissions) p.IsChecked = true;
        RefreshCounters();
    }

    /// <summary>لغو همه مجوزهای این ماژول</summary>
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var p in Permissions) p.IsChecked = false;
        RefreshCounters();
    }
}

/// <summary>
/// ViewModel نقش‌ها و ماتریس دسترسی — گام U13 طبق فرم ۲۵:
/// ستون راست نقش‌ها + ستون چپ ماتریس گروهی با انتخاب همه/هیچ و ذخیره.
/// ═══ چرخهٔ چهل‌وچهارم (گزارش مالک) ═══
/// «صفحه نقش ها و دسترسی ها رو با امکانات و صفحات جدید بروز کن و ظاهر
/// صفحه رو حرفه ای تر و زیباتر بکن»:
///   ① سرور: پنج ماژول چرخه‌های اخیر (حضور و غیاب/پرونده بالینی/حقوق/
///      نظرسنجی/اعلان‌ها) عنوان فارسی گرفتند — گروه‌ها دیگر انگلیسی نیستند
///   ② جستجوی زندهٔ مجوزها (عنوان/نام فنی/ماژول) با پنهان‌سازی گروه بی‌نتیجه
///   ③ نشان «تغییرات ذخیره‌نشده» + دکمهٔ ذخیرهٔ برجسته + هشدار هنگام تعویض نقش
///   ④ شمارندهٔ «n از m» روی هر کارت ماژول + خلاصهٔ نقش منتخب در سربرگ
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

    /// <summary>عبارت جستجوی مجوزها — پالایش زندهٔ کارت‌های ماتریس (چرخهٔ ۴۴)</summary>
    [ObservableProperty]
    private string? _permissionSearch;

    /// <summary>نام نقش جدید</summary>
    [ObservableProperty]
    private string? _newRoleName;

    /// <summary>
    /// تغییرات ذخیره‌نشده در ماتریس هست؟ — چرخهٔ ۴۴: دکمهٔ ذخیره برجسته می‌شود
    /// و تعویض نقش هشدار می‌دهد تا انتخاب مدیر ناخواسته نپرد
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>متن خلاصهٔ سربرگ — شمار نقش‌ها و مجوزهای کاتالوگ (چرخهٔ ۴۴)</summary>
    public string SummaryText
    {
        get
        {
            var roleCount = Money.ToPersianDigits(Roles.Count.ToString());
            var permCount = Money.ToPersianDigits(PermissionGroups.Sum(g => g.Permissions.Count).ToString());
            var groupCount = Money.ToPersianDigits(PermissionGroups.Count.ToString());
            return $"{roleCount} نقش • {groupCount} ماژول • {permCount} مجوز";
        }
    }

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

            // کاتالوگ گروه‌های مجوز — یک‌بار؛ بازخوانی بعدی فقط شمارنده‌ها را نو می‌کند
            if (PermissionGroups.Count == 0)
            {
                var groups = await _api.GetAsync<PermissionsResponse>("permissions");
                var catalog = groups?.Data ?? new List<PermissionGroupDto>();
                PermissionGroups = new ObservableCollection<PermissionGroupCard>(
                    catalog.Select(g => new PermissionGroupCard
                    {
                        ModuleTitle = g.ModuleTitle
                    }));
                foreach (var g in PermissionGroups)
                {
                    // مجوزهای همین گروه از کاتالوگ (هم‌عنوان) + رویداد تغییر برای
                    // شمارنده و نشان ذخیره‌نشده (چرخهٔ ۴۴)
                    foreach (var p in catalog.First(x => x.ModuleTitle == g.ModuleTitle).Permissions)
                    {
                        var row = new PermissionCheckRow
                        {
                            Id = p.Id,
                            Name = p.Name,
                            ActionTitle = ActionTitleMapper.ToTitle(p.Action, p.Description, p.Name)
                        };
                        row.CheckedStateChanged += () =>
                        {
                            g.RefreshCounters();
                            MarkDirty();
                        };
                        g.Permissions.Add(row);
                    }

                    // شروع بدون فیلتر — نمایشی همان فهرست کامل
                    g.ApplyFilter(null);
                    g.RefreshCounters();
                }
            }
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    /// <summary>جستجوی زندهٔ مجوزها — پالایش همهٔ کارت‌ها + پنهان‌سازی بی‌نتیجه‌ها</summary>
    partial void OnPermissionSearchChanged(string? value)
    {
        foreach (var g in PermissionGroups)
            g.ApplyFilter(value);
    }

    /// <summary>
    /// بارگذاری ماتریس دسترسی نقش انتخاب‌شده — چرخهٔ ۴۴: اگر تغییرات
    /// ذخیره‌نشده هست، مدیر با پیام فارسی هشدار می‌گیرد (انتخابِ ناخواسته
    /// نپرد؛ تصمیم نهایی با اوست)
    /// </summary>
    partial void OnSelectedRoleChanged(RoleListRow? value)
    {
        if (value is not null)
        {
            if (HasUnsavedChanges)
                _snackbar.Warning("تغییرات دسترسی‌های نقش قبلی ذخیره نشد — برای نگه‌داشتنشان، دکمهٔ «ذخیره تغییرات» را بزنید");
            _ = LoadRolePermissionsAsync(value.Id);
        }
    }

    /// <summary>نشان‌گذاری تغییر ذخیره‌نشده (چرخهٔ ۴۴)</summary>
    private void MarkDirty() => HasUnsavedChanges = true;

    /// <summary>بارگذاری مجوزهای نقش و علامت‌گذاری چک‌باکس‌ها</summary>
    private async Task LoadRolePermissionsAsync(long roleId)
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<PermissionIdsResponse>($"roles/{roleId}/permissions");
            var assigned = new HashSet<long>(response?.Data ?? new List<long>());

            foreach (var group in PermissionGroups)
            {
                foreach (var p in group.Permissions)
                    p.IsChecked = assigned.Contains(p.Id);
                group.RefreshCounters();
            }

            // بارگذاریِ تازه جایگزین ماتریس قبلی است — دیگر «ذخیره‌نشده» نیست
            HasUnsavedChanges = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ذخیرهٔ ماتریس دسترسی نقش انتخاب‌شده</summary>
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

        if (ok)
        {
            HasUnsavedChanges = false;
            _snackbar.Success($"دسترسی‌های «{SelectedRole.Name}» ذخیره شد ({Money.ToPersianDigits(permissionIds.Count.ToString())} مجوز)");
            await LoadAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ذخیره دسترسی‌ها ناموفق بود");
    }

    /// <summary>انتخاب همه دسترسی‌ها در همه ماژول‌ها</summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var group in PermissionGroups)
        {
            foreach (var p in group.Permissions) p.IsChecked = true;
            group.RefreshCounters();
        }
        _snackbar.Info("همه دسترسی‌ها انتخاب شد");
    }

    /// <summary>لغو همه دسترسی‌ها در همه ماژول‌ها</summary>
    [RelayCommand]
    private void UncheckAll()
    {
        foreach (var group in PermissionGroups)
        {
            foreach (var p in group.Permissions) p.IsChecked = false;
            group.RefreshCounters();
        }
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

        // قانون مالک: به‌جای کادر پیام ویندوز — دیالوگ فارسی خود برنامه
        var confirmed = Views.AppDialog.Confirm("حذف نقش",
            $"نقش «{SelectedRole.Name}» حذف شود؟ این عمل بازگشت‌ناپذیر است.",
            Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

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
