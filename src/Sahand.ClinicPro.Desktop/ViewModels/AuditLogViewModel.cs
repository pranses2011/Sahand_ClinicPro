using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف لاگ عملیات — بازطراحی چرخهٔ ۷۰:
/// ستون‌های نمایش اکنون از سرور «فارسی» می‌آیند (PersianEntity/PersianModule
/// که ردیف‌های قدیمی را هم در لحظهٔ خواندن ترجمه می‌کنند) و «شرح» به‌جای
/// برش خام JSON، جملهٔ انسانی است: «ویرایش «بیمار» (#۱۲) — ۳ فیلد تغییر
/// یافت: تلفن ثابت، نشانی، وضعیت تأهل».
/// </summary>
public partial class AuditDetailRow : ObservableObject
{
    /// <summary>شناسهٔ رکورد لاگ</summary>
    public long Id { get; init; }

    /// <summary>نام کامل کاربر (از ادعای displayName سرور؛ ردیف‌های قدیمی: بازیابی از پرسنل)</summary>
    public string? Username { get; init; }

    /// <summary>ماژول — فارسی (داروخانه، پذیرش، مدیریت سیستم و…)</summary>
    public string? Module { get; init; }

    /// <summary>عملیات — فارسی</summary>
    public string? Action { get; init; }

    /// <summary>موجودیت — فارسی (بیمار، نوبت، فاکتور و…)</summary>
    public string? EntityName { get; init; }

    /// <summary>شناسهٔ رکورد — برای ایجادها از پاسخ سرور پر می‌شود</summary>
    public long? EntityId { get; init; }

    /// <summary>شرح انسانی — جملهٔ خلاصهٔ تغییرات (پس از ساخت، از دیف پر می‌شود)</summary>
    public string? Description { get; set; }

    /// <summary>آدرس IP</summary>
    public string? IpAddress { get; init; }

    /// <summary>زمان وقوع (شمسی + ساعت)</summary>
    public string? CreatedAt { get; init; }

    /// <summary>مقادیر قبلی JSON (خام)</summary>
    [ObservableProperty]
    private string? _oldValues;

    /// <summary>مقادیر جدید JSON (خام)</summary>
    [ObservableProperty]
    private string? _newValues;

    /// <summary>
    /// جدول مقایسهٔ فیلدها — خروجی BuildDiff؛ با هر انتخابِ ردیف،
    /// ViewModel این فهرست را برای پنل جزئیات باز می‌سازد.
    /// </summary>
    public List<AuditFieldChange> FieldDiff => AuditFieldCatalog.BuildDiff(Action, OldValues, NewValues);

    /// <summary>شمار فیلدهای واقعاً تغییر‌یافته — برای جملهٔ شرح</summary>
    public int ChangedFieldCount => FieldDiff.Count(f => f.Changed);

    /// <summary>نشان رنگی عملیات</summary>
    public string ActionText => Action switch
    {
        "ایجاد" => "➕ ایجاد", "ویرایش" => "✏️ ویرایش", "حذف" => "🗑 حذف",
        "مشاهده" => "👁 مشاهده", "ورود" => "🔑 ورود", "خروج" => "🚪 خروج",
        "تغییر رمز" => "🔒 تغییر رمز",
        _ => Action ?? "—"
    };

    /// <summary>رنگ نشان</summary>
    public string ActionColor => Action switch
    {
        "ایجاد" => "#2E7D32", "ویرایش" => "#1565C0", "حذف" => "#C62828",
        "ورود" => "#F9A825", "خروج" => "#E65100", "تغییر رمز" => "#8E24AA",
        _ => "#90A4AE"
    };

    /// <summary>JSON خام قبلی — برای اکسپندر «نمایش خام»</summary>
    public string FormattedOld => PrettyJson(OldValues);

    /// <summary>JSON خام جدید — برای اکسپندر «نمایش خام»</summary>
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
/// ViewModel لاگ عملیات — بازطراحی چرخهٔ ۷۰:
/// فیلترهای معنایی (ماژول/عملیات/جستجو) + جدول + پنل «جدول مقایسهٔ تغییرات».
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

    /// <summary>لاگ انتخاب‌شده — پنل جزئیات زیر جدول را پر می‌کند</summary>
    [ObservableProperty]
    private AuditDetailRow? _selectedLog;

    /// <summary>جدول مقایسهٔ ردیف انتخاب‌شده — بازسازی با هر انتخاب</summary>
    [ObservableProperty]
    private List<AuditFieldChange> _selectedDiff = new();

    /// <summary>فیلتر جستجو (ماژول/موجودیت/کاربر)</summary>
    [ObservableProperty]
    private string? _entityFilter;

    /// <summary>شماره صفحه</summary>
    [ObservableProperty]
    private int _page = 1;

    /// <summary>متن شمارنده</summary>
    [ObservableProperty]
    private string _countText = "—";

    /// <summary>گزینه‌های فیلتر عملیات — فارسی</summary>
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
            var action = ActionFilter switch
            {
                "ایجاد" => "Create", "ویرایش" => "Update", "حذف" => "Delete",
                "مشاهده" => "View", "ورود" => "Login", "خروج" => "Logout",
                "تغییر رمز" => "PasswordChange", _ => (string?)null
            };
            var query = $"auditlogs?page={Page}&pageSize=50" +
                        (action is null ? "" : $"&action={Uri.EscapeDataString(action)}") +
                        (string.IsNullOrWhiteSpace(EntityFilter) ? "" : $"&entityName={Uri.EscapeDataString(EntityFilter.Trim())}");

            var response = await _api.GetAsync<AuditResponse>(query);
            var rows = response?.Data ?? new List<AuditDto>();
            Logs = new ObservableCollection<AuditDetailRow>(rows.Select(MapRow));
            CountText = $"صفحه {Helpers.Money.ToPersianDigits(Page.ToString())} — {Helpers.Money.ToPersianDigits(Logs.Count.ToString())} ردیف";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// نگاشت DTO به ردیف نمایش — ستون‌های فارسی از سرور می‌آیند
    /// (PersianEntity/PersianModule) و شرح، جملهٔ انسانی از دیف ساخته می‌شود.
    /// </summary>
    private static AuditDetailRow MapRow(AuditDto a)
    {
        var action = a.Action switch
        {
            "Create" => "ایجاد", "Update" => "ویرایش", "Delete" => "حذف",
            "View" => "مشاهده", "Login" => "ورود", "Logout" => "خروج",
            "PasswordChange" => "تغییر رمز", _ => a.Action
        };
        var row = new AuditDetailRow
        {
            Id = a.Id,
            Username = a.Username,
            // گارد: اگر سرور قدیمی فیلد فارسی نفرستاد، همان نگاشت محلی اجرا می‌شود
            Module = string.IsNullOrWhiteSpace(a.PersianModule)
                ? Application.Common.AuditSemanticCatalog.PersianizeModule(a.Module)
                : a.PersianModule,
            Action = action,
            EntityName = string.IsNullOrWhiteSpace(a.PersianEntity)
                ? Application.Common.AuditSemanticCatalog.PersianizeEntity(a.EntityName)
                : a.PersianEntity,
            EntityId = a.EntityId,
            IpAddress = a.IpAddress,
            CreatedAt = $"{a.TimeShamsi} {a.Time}",
            OldValues = a.OldValues,
            NewValues = a.NewValues,
        };
        row.Description = BuildHumanSummary(row);
        return row;
    }

    /// <summary>
    /// جملهٔ انسانی برای ستون «شرح» — به‌جای برش خام JSON:
    ///   ویرایش: «۳ فیلد تغییر یافت: تلفن ثابت، نشانی»
    ///   ایجاد:   «ثبت جدید با ۱۲ فیلد»
    ///   حذف:     «حذف رکورد — ۱۲ فیلد ثبت بود»
    ///   بدون دیف: «—»
    /// </summary>
    private static string BuildHumanSummary(AuditDetailRow row)
    {
        var diff = row.FieldDiff;
        if (diff.Count == 0)
            return "—";
        var changed = diff.Where(f => f.Changed).ToList();
        var entityPart = string.IsNullOrWhiteSpace(row.EntityName)
            ? ""
            : $"«{row.EntityName}»" + (row.EntityId is > 0 ? $" (#{Helpers.Money.ToPersianDigits(row.EntityId.Value.ToString())})" : "") + " — ";
        switch (row.Action)
        {
            case "ایجاد":
                return $"{entityPart}ثبت جدید با {Helpers.Money.ToPersianDigits(diff.Count.ToString())} فیلد";
            case "حذف":
                return $"{entityPart}حذف رکورد ({Helpers.Money.ToPersianDigits(diff.Count.ToString())} فیلد)";
            case "ویرایش" when changed.Count > 0:
            {
                var names = string.Join("، ", changed.Take(4).Select(f => f.Field));
                var more = changed.Count > 4 ? $" و {Helpers.Money.ToPersianDigits((changed.Count - 4).ToString())} مورد دیگر" : "";
                return $"{entityPart}{Helpers.Money.ToPersianDigits(changed.Count.ToString())} فیلد تغییر یافت: {names}{more}";
            }
            default:
                return changed.Count > 0
                    ? $"{entityPart}{Helpers.Money.ToPersianDigits(changed.Count.ToString())} فیلد تغییر یافت"
                    : "—";
        }
    }

    /// <summary>با هر انتخاب ردیف، جدول مقایسه بازسازی می‌شود</summary>
    partial void OnSelectedLogChanged(AuditDetailRow? value)
    {
        SelectedDiff = value?.FieldDiff ?? new List<AuditFieldChange>();
    }

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

    /// <summary>پاسخ تک‌لایهٔ سرور — data مستقیماً فهرست ردیف‌هاست</summary>
    private sealed class AuditResponse
    {
        public bool IsSuccess { get; set; }
        public List<AuditDto>? Data { get; set; }
    }

    /// <summary>
    /// ردیف لاگ — منطبق بر AuditLogDto سرور (چرخهٔ ۷۰: PersianEntity/
    /// PersianModule افزوده شدند؛ ردیف‌های قدیمی از همین محل فارسی می‌شوند).
    /// </summary>
    private sealed class AuditDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public long? EntityId { get; set; }
        public string? Module { get; set; }
        public string PersianEntity { get; set; } = string.Empty;
        public string PersianModule { get; set; } = string.Empty;
        public string? TimeShamsi { get; set; }
        public string? Time { get; set; }
        public string? IpAddress { get; set; }
        public string? NewValues { get; set; }
        public string? OldValues { get; set; }
    }
}
