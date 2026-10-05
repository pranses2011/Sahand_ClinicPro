using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف قالب چاپ — گام U19 فرم ۳۵</summary>
public partial class PrintTemplateRow : ObservableObject
{
    /// <summary>کلید تنظیم</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>نام فارسی قالب</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>آیکن</summary>
    public string Icon { get; init; } = "";

    /// <summary>متن قالب — از تنظیمات</summary>
    [ObservableProperty]
    private string? _body;

    /// <summary>تغییر کرده و ذخیره نشده</summary>
    [ObservableProperty]
    private bool _isDirty;
}

/// <summary>ردیف نظرسنجی — فرم ۳۴</summary>
public partial class SurveyRow : ObservableObject
{
    /// <summary>شناسه</summary>
    public long Id { get; init; }

    /// <summary>عنوان</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>توضیح</summary>
    public string? Description { get; init; }

    /// <summary>فعال</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>تعداد پاسخ</summary>
    public int ResponseCount { get; init; }

    /// <summary>میانگین امتیاز ۱ تا ۵</summary>
    public double AverageRating { get; init; }

    /// <summary>متن میانگین</summary>
    public string RatingText => AverageRating <= 0 ? "—" : $"{AverageRating:0.#} / ۵";

    /// <summary>رنگ امتیاز</summary>
    public string RatingColor => AverageRating >= 4 ? "#2E7D32" : AverageRating >= 3 ? "#F9A825" : "#C62828";

    /// <summary>وضعیت فعال</summary>
    public string ActiveText => IsActive ? "🟢 فعال" : "⚪ غیرفعال";
}

/// <summary>
/// ViewModel فرم‌های تکمیلی — گام U19:
/// قالب‌های چاپ ۹گانه + نظرسنجی + همگام‌سازی داده در سه تب
/// </summary>
public partial class ComplementaryViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "فرم‌های تکمیلی";

    /// <summary>زبانه فعال</summary>
    [ObservableProperty]
    private int _tabIndex;

    /* ─── تب ۱: قالب‌های چاپ ─── */

    /// <summary>قالب‌های چاپ ۹گانه</summary>
    public ObservableCollection<PrintTemplateRow> PrintTemplates { get; } = new();

    /// <summary>قالب انتخابی</summary>
    [ObservableProperty]
    private PrintTemplateRow? _selectedTemplate;

    /* ─── تب ۲: نظرسنجی ─── */

    /// <summary>نظرسنجی‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<SurveyRow> _surveys = new();

    /// <summary>نظرسنجی انتخابی</summary>
    [ObservableProperty]
    private SurveyRow? _selectedSurvey;

    /// <summary>عنوان نظرسنجی جدید</summary>
    [ObservableProperty]
    private string? _newSurveyTitle;

    /// <summary>توضیح نظرسنجی جدید</summary>
    [ObservableProperty]
    private string? _newSurveyDescription;

    /* ─── تب ۳: همگام‌سازی داده ─── */

    /// <summary>وضعیت همگام‌سازی</summary>
    [ObservableProperty]
    private string _syncStatusText = "آماده";

    /// <summary>زمان آخرین همگام‌سازی</summary>
    [ObservableProperty]
    private string _lastSyncText = "—";

    /// <summary>سازنده</summary>
    public ComplementaryViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        InitializePrintTemplates();
        _ = LoadSurveysAsync();
    }

    /// <summary>بارگذاری داده زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        if (value == 1) _ = LoadSurveysAsync();
    }

    /* ─── قالب‌های چاپ ─── */

    /// <summary>مقداردهی ۹ قالب چاپ — مطابق فرم ۳۵</summary>
    private void InitializePrintTemplates()
    {
        var catalog = new (string key, string title, string icon)[]
        {
            ("Invoice", "فاکتور و قبض پرداخت", "🧾"),
            ("Prescription", "نسخه پزشکی", "💊"),
            ("LabOrder", "درخواست آزمایش", "🧪"),
            ("LabResult", "نتیجه آزمایش", "📄"),
            ("ImagingOrder", "درخواست تصویربرداری", "📷"),
            ("AppointmentTicket", "نوبت‌نامه", "🎫"),
            ("ReceptionReceipt", "رسید پذیرش", "🩺"),
            ("MedicalCertificate", "گواهی پزشکی", "📝"),
            ("PayrollSlip", "فیش حقوقی", "💰")
        };
        foreach (var (key, title, icon) in catalog)
            PrintTemplates.Add(new PrintTemplateRow
            {
                Key = key,
                Title = title,
                Icon = icon,
                Body = null
            });
    }

    /// <summary>بارگذاری متن قالب انتخابی از تنظیمات</summary>
    partial void OnSelectedTemplateChanged(PrintTemplateRow? value)
    {
        if (value is null || value.Body is not null) return;
        _ = LoadTemplateBodyAsync(value);
    }

    /// <summary>خواندن متن قالب از سرور</summary>
    private async Task LoadTemplateBodyAsync(PrintTemplateRow template)
    {
        var response = await _api.GetAsync<SettingsResponse>(
            $"settings?category=PrintTemplate");
        var map = response?.Data ?? new List<SettingDto>();
        template.Body = map.FirstOrDefault(s => s.Key == $"PrintTemplate.{template.Key}.Body")?.Value
            ?? "قالب پیش‌فرض — متغیرها: {نام بیمار} {تاریخ} {مبلغ} {نام درمانگاه}";
    }

    /// <summary>ذخیره قالب انتخابی</summary>
    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (SelectedTemplate?.Body is null) { _snackbar.Info("ابتدا یک قالب انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PutAsync("settings", new
        {
            key = $"PrintTemplate.{SelectedTemplate.Key}.Body",
            value = SelectedTemplate.Body,
            category = "PrintTemplate"
        });
        IsBusy = false;
        if (ok) { _snackbar.Success($"قالب «{SelectedTemplate.Title}» ذخیره شد"); SelectedTemplate.IsDirty = false; }
        else _snackbar.Error(_api.LastError ?? "ذخیره قالب ناموفق بود");
    }

    /* ─── نظرسنجی ─── */

    /// <summary>بارگذاری نظرسنجی‌ها</summary>
    [RelayCommand]
    public async Task LoadSurveysAsync()
    {
        IsBusy = true;
        try
        {
            var response = await _api.GetAsync<SurveysResponse>("surveys");
            Surveys = new ObservableCollection<SurveyRow>(
                (response?.Data ?? new List<SurveyDto>()).Select(s => new SurveyRow
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    ResponseCount = s.ResponseCount,
                    AverageRating = s.AverageRating
                }));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>ثبت نظرسنجی جدید</summary>
    [RelayCommand]
    private async Task AddSurveyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewSurveyTitle) || NewSurveyTitle.Trim().Length < 3)
        { _snackbar.Error("عنوان نظرسنجی حداقل ۳ کاراکتر است"); return; }

        // سوالات پیش‌فرض رضایت ۵ سطحی
        const string defaultQuestions = "[{\"q\":\"میزان رضایت شما از ویزیت؟\",\"type\":\"rating\"},{\"q\":\"نظرات شما؟\",\"type\":\"text\"}]";

        IsBusy = true;
        var ok = await _api.PostAsync("surveys", new
        {
            title = NewSurveyTitle.Trim(),
            description = NewSurveyDescription,
            questions = defaultQuestions
        });
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success("نظرسنجی ایجاد شد — پس از ویزیت به بیمار ارسال می‌شود");
            NewSurveyTitle = null;
            NewSurveyDescription = null;
            await LoadSurveysAsync();
        }
        else
            _snackbar.Error(_api.LastError ?? "ایجاد نظرسنجی ناموفق بود");
    }

    /// <summary>فعال/غیرفعال‌سازی نظرسنجی</summary>
    [RelayCommand]
    private async Task ToggleSurveyAsync(SurveyRow? row)
    {
        row ??= SelectedSurvey;
        if (row is null) return;
        IsBusy = true;
        var ok = await _api.PostAsync($"surveys/{row.Id}/toggle-active", null);
        IsBusy = false;
        if (ok) { _snackbar.Success("وضعیت نظرسنجی تغییر کرد"); await LoadSurveysAsync(); }
        else _snackbar.Error(_api.LastError ?? "عملیات ناموفق بود");
    }

    /* ─── همگام‌سازی داده ─── */

    /// <summary>اجرای همگام‌سازی دستی داده بین شعبه‌ها — ثبت نقطه کنترل</summary>
    [RelayCommand]
    private async Task SyncNowAsync()
    {
        SyncStatusText = "در حال همگام‌سازی...";
        IsBusy = true;
        var ok = await _api.PostAsync("backup/run", null);
        IsBusy = false;
        if (ok)
        {
            SyncStatusText = "نقطه همگام‌سازی ایجاد شد";
            LastSyncText = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(DateTime.Now);
            _snackbar.Success("همگام‌سازی آغاز شد — وضعیت در لاگ سیستم دنبال می‌شود");
        }
        else
        {
            SyncStatusText = "خطا در همگام‌سازی";
            _snackbar.Error(_api.LastError ?? "همگام‌سازی ناموفق بود");
        }
    }

    /* ─── پاسخ‌ها ─── */

    private sealed class SettingsResponse
    {
        public bool IsSuccess { get; set; }
        public List<SettingDto>? Data { get; set; }
    }

    private sealed class SettingDto
    {
        public string Key { get; set; } = "";
        public string? Value { get; set; }
    }

    private sealed class SurveysResponse
    {
        public bool IsSuccess { get; set; }
        public List<SurveyDto>? Data { get; set; }
    }

    private sealed class SurveyDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int ResponseCount { get; set; }
        public double AverageRating { get; set; }
    }
}
