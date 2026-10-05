using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف لاگ عملیات با جزئیات Old/New — فرم ۳۰</summary>
public partial class AuditDetailRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>کاربر</summary>
    public string? Username { get; init; }

    /// <summary>ماژول</summary>
    public string? Module { get; init; }

    /// <summary>عملیات</summary>
    public string? Action { get; init; }

    /// <summary>نام موجودیت</summary>
    public string? EntityName { get; init; }

    /// <summary>شناسه موجودیت</summary>
    public long? EntityId { get; init; }

    /// <summary>شرح</summary>
    public string? Description { get; init; }

    /// <summary>آدرس IP</summary>
    public string? IpAddress { get; init; }

    /// <summary>زمان UTC</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>مقادیر قبلی JSON</summary>
    [ObservableProperty]
    private string? _oldValues;

    /// <summary>مقادیر جدید JSON</summary>
    [ObservableProperty]
    private string? _newValues;

    /// <summary>نشان رنگی عملیات — طبق فرم ۳۰</summary>
    public string ActionText => Action switch
    {
        "Create" => "🟢 ایجاد", "Update" => "🔵 ویرایش", "Delete" => "🔴 حذف",
        "View" => "⬜ مشاهده", "Login" => "🟡 ورود", "Logout" => "🟠 خروج",
        "PasswordChange" => "🟣 تغییر رمز",
        _ => Action ?? "—"
    };

    /// <summary>رنگ نشان</summary>
    public string ActionColor => Action switch
    {
        "Create" => "#2E7D32", "Update" => "#1565C0", "Delete" => "#C62828",
        "Login" => "#F9A825", "Logout" => "#E65100", "PasswordChange" => "#8E24AA",
        _ => "#90A4AE"
    };

    /// <summary>JSON قالب‌بندی‌شده مقادیر قبلی — برای جزئیات</summary>
    public string FormattedOld => PrettyJson(OldValues);

    /// <summary>JSON قالب‌بندی‌شده مقادیر جدید</summary>
    public string FormattedNew => PrettyJson(NewValues);

    /// <summary>قالب‌بندی JSON با تورفتگی</summary>
    private static string PrettyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "—";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}

/// <summary>
/// ViewModel لاگ عملیات — گام U15 طبق فرم ۳۰:
/// فیلترها (تاریخ/کاربر/ماژول/عملیات/جستجو) + جدول + جزئیات Old/New
/// </summary>
public partial class AuditLogViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "لاگ عملیات";

    /// <summary>ردیف‌های لاگ</summary>
    [ObservableProperty]
    private ObservableCollection<AuditDetailRow> _logs = new();

    /// <summary>لاگ انتخاب‌شده — نمایش جزئیات</summary>
    [ObservableProperty]
    private AuditDetailRow? _selectedLog;

    /// <summary>فیلتر نام موجودیت/ماژول</summary>
    [ObservableProperty]
    private string? _entityFilter;

    /// <summary>جستجو در شرح</summary>
    [ObservableProperty]
    private string? _search;

    /// <summary>شماره صفحه</summary>
    [ObservableProperty]
    private int _page = 1;

    /// <summary>متن شمارنده</summary>
    [ObservableProperty]
    private string _countText = "—";

    /// <summary>گزینه‌های فیلتر عملیات</summary>
    public List<string> ActionOptions { get; } = new()
        { "همه", "ایجاد", "ویرایش", "حذف", "مشاهده", "ورود", "خروج", "تغییر رمز" };

    /// <summary>فیلتر عملیات انتخابی</summary>
    [ObservableProperty]
    private string? _actionFilter = "همه";

    /// <summary>سازنده</summary>
    public AuditLogViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری لاگ‌ها با فیلترها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // نگاشت فیلتر فارسی به نام انگلیسی موجودیت — سرویس فعلی بر اساس entityName فیلتر می‌کند
            var entity = ActionFilter switch
            {
                "ایجاد" => "Create", "ویرایش" => "Update", "حذف" => "Delete",
                "مشاهده" => "View", "ورود" => "Login", "خروج" => "Logout",
                "تغییر رمز" => "PasswordChange", _ => (string?)null
            };
            var entityParam = string.IsNullOrWhiteSpace(EntityFilter) ? entity : EntityFilter;
            var query = $"auditlogs?page={Page}&pageSize=50" +
                        (entityParam is null ? "" : $"&entityName={Uri.EscapeDataString(entityParam)}");

            var response = await _api.GetAsync<AuditResponse>(query);
            var data = response?.Data;
            Logs = new ObservableCollection<AuditDetailRow>(
                (data?.Items ?? new List<AuditDto>()).Select(MapRow));
            CountText = $"صفحه {Page} — {Logs.Count} ردیف";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>نگاشت DTO به ردیف نمایش</summary>
    private static AuditDetailRow MapRow(AuditDto a) => new()
    {
        Id = a.Id,
        Username = a.Username,
        Module = a.Module,
        Action = a.Action,
        EntityName = a.EntityName,
        EntityId = a.EntityId,
        Description = a.Description,
        IpAddress = a.IpAddress,
        CreatedAt = a.CreatedAt,
        OldValues = a.OldValues,
        NewValues = a.NewValues
    };

    /// <summary>صفحه بعد</summary>
    [RelayCommand]
    private async Task NextPageAsync()
    {
        Page++;
        await LoadAsync();
    }

    /// <summary>صفحه قبل</summary>
    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (Page > 1) Page--;
        await LoadAsync();
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class AuditResponse
    {
        public bool IsSuccess { get; set; }
        public AuditPage? Data { get; set; }
    }

    private sealed class AuditPage
    {
        public List<AuditDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class AuditDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public long? EntityId { get; set; }
        public string? Module { get; set; }
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
