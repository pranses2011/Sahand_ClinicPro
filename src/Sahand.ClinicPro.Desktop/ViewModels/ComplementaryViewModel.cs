using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف قالب چاپ سفارشی — طراح چرخه ۱۳ (درخواست ۱-۶)</summary>
public partial class PrintTemplateRow : ObservableObject
{
    /// <summary>شناسه — صفر یعنی قالب جدید</summary>
    public long Id { get; set; }

    /// <summary>کلید نوع — Invoice و…</summary>
    public string Key { get; set; } = "Invoice";

    /// <summary>نام نمایشی قالب</summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>توضیح</summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>آیکن</summary>
    public string Icon { get; set; } = "🖨";

    /// <summary>نام فارسی نوع</summary>
    public string TypeFa { get; set; } = string.Empty;

    /// <summary>بدنه HTML قالب</summary>
    [ObservableProperty]
    private string _body = string.Empty;

    /// <summary>اندازه کاغذ A4/A5/A6</summary>
    [ObservableProperty]
    private string _paperSize = "A5";

    /// <summary>رنگ تأکیدی هگز</summary>
    [ObservableProperty]
    private string _accentColor = "#16355E";

    /// <summary>اندازه فونت</summary>
    [ObservableProperty]
    private int _fontSize = 13;

    /// <summary>نمایش لوگو</summary>
    [ObservableProperty]
    private bool _showLogo = true;

    /// <summary>فعال</summary>
    [ObservableProperty]
    private bool _isActive = true;

    /// <summary>پیش‌فرض نوع</summary>
    [ObservableProperty]
    private bool _isDefault;

    /// <summary>تغییر کرده و ذخیره نشده</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>برچسب نوع برای نمایش</summary>
    public string BadgeText => $"{(IsDefault ? "★ پیش‌فرض" : "")}{(IsActive ? "" : " ⚪ غیرفعال")}".Trim();

    partial void OnIsDefaultChanged(bool value) => OnPropertyChanged(nameof(BadgeText));
    partial void OnIsActiveChanged(bool value) => OnPropertyChanged(nameof(BadgeText));

    /// <summary>آیکن هر نوع</summary>
    public static string IconOf(string key) => key switch
    {
        "Invoice" => "🧾", "Prescription" => "💊", "LabOrder" => "🧪", "LabResult" => "📄",
        "ImagingOrder" => "📷", "AppointmentTicket" => "🎫", "ReceptionReceipt" => "🩺",
        "MedicalCertificate" => "📝", "PayrollSlip" => "💰", _ => "🖨"
    };
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

    /// <summary>متن خام JSON سوالات — برای بارگذاری در طراح ویرایش (چرخهٔ ۲۸)</summary>
    public string? QuestionsJson { get; init; }
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

    /* ─── تب ۱: قالب‌های چاپ — طراح چرخه ۱۳ ─── */

    /// <summary>همه قالب‌های چاپ (سروری)</summary>
    public ObservableCollection<PrintTemplateRow> PrintTemplates { get; } = new();

    // ═══ چرخهٔ سی‌وششم (گزارش مالک — رفع CS8604 و باگ پنهان زمان-اجرا) ═══
    // TemplateFilterOptions در مقداردهندهٔ استاتیک خود TemplateTypes را
    // می‌خواند؛ پیش از این چرخه TemplateTypes در متن فایل «بعد از» آن تعریف
    // شده بود. مقداردهنده‌های استاتیک به‌ترتیبِ ظاهر در متن اجرا می‌شوند، پس
    // هنگام ساخت TemplateFilterOptions مقدار TemplateTypes هنوز تهی بود:
    // هم هشدار CS8604 کامپایلر صادق بود و هم در زمان اجرا، نخستین دسترسی به
    // این کلاس (بازکردن صفحهٔ «فرم‌های تکمیلی») با کرش TypeInitializationException
    // متوقف می‌شد (متد Concat با آرگومان تهی استثنا می‌دهد). درمان: تعریف
    // TemplateTypes به بالای همهٔ مصرف‌کننده‌های استاتیکش منتقل شد.
    // ⚠️ این ترتیب را جابه‌جا نکنید؛ گارد بسته‌بندی چرخه‌ها پایشش می‌کند.
    /// <summary>
    /// گزینهٔ «نوع سند» برای نمایش در انتخابگرها (کمبوی فیلتر طراح و کمبوی نوع قالب جدید).
    /// ═══ چرخهٔ سی‌وهفتم (ریشهٔ «۹ گزینهٔ سفید بدون متن») ═══
    /// پیش از این چرخه گزینه‌ها ValueTuple‌های (string key, string fa) بودند و
    /// انتخابگرها با DisplayMemberPath="Item2" می‌خواستند نام فارسی را نشان
    /// دهند — ولی Item1/Item2 در ValueTuple «فیلد عمومی»‌اند نه Property، و
    /// مسیرِ پروپرتی WPF فقط به Property بایند می‌شود؛ نتیجه: متن تهی برای همهٔ
    /// گزینه‌ها (۹ گزینهٔ سفید خالی!). درمان: این رکورد با Property واقعی
    /// (Key/Fa) جای ValueTuple نشست و DisplayMemberPath به «Fa» تغییر کرد.
    /// </summary>
    public sealed record TemplateTypeOption(string Key, string Fa);

    /// <summary>گزینه‌های نوع سند — ۹ نوع سند قابل طراحی در طراح (تک‌منبع انواع سند)</summary>
    public static TemplateTypeOption[] TemplateTypes { get; } =
    {
        new("Invoice", "فاکتور و قبض پرداخت"),
        new("Prescription", "نسخه پزشکی"),
        new("LabOrder", "درخواست آزمایش"),
        new("LabResult", "نتیجه آزمایش"),
        new("ImagingOrder", "درخواست تصویربرداری"),
        new("AppointmentTicket", "نوبت‌نامه"),
        new("ReceptionReceipt", "رسید پذیرش"),
        new("MedicalCertificate", "گواهی پزشکی"),
        new("PayrollSlip", "فیش حقوقی")
    };

    // ═══ چرخهٔ سی‌وپنجم (گزارش مالک) — فیلتر موجودیت در طراح قالب ═══
    // «در تب طراح قالب چاپ همه قالب‌ها نمایش داده می‌شوند در حالی که باید فقط
    // طرح‌های مربوط به موجودیتی که از لیست انتخاب می‌کنیم نمایش داده شود»:
    // فهرست پالایه‌شده جدا از فهرست کامل نگه داشته می‌شود — فهرست کامل از سرور
    // می‌آید (LoadTemplatesAsync) و هر بار پالایه دوباره روی آن اعمال می‌شود؛
    // جدول طراح فقط به VisibleTemplates بایند است پس همیشه فقط قالب‌های
    // موجودیتِ منتخب دیده می‌شوند (صفر = همه).
    /// <summary>قالب‌های قابل‌رِیت در فهرست — پالایه‌شده بر اساس موجودیت انتخابی</summary>
    public ObservableCollection<PrintTemplateRow> VisibleTemplates { get; } = new();

    /// <summary>پالایهٔ موجودیت طراح قالب — صفر یعنی «همه»</summary>
    [ObservableProperty]
    private int _templateFilterIndex;

    /// <summary>گزینه‌های فیلتر موجودیت — «همه» + ۹ نوع سند؛ به ترتیبِ استاتیکِ TemplateTypes وابسته است (باید بالاتر تعریف بماند)</summary>
    public static TemplateTypeOption[] TemplateFilterOptions { get; } =
        new[] { new TemplateTypeOption("", "همه موجودیت‌ها") }.Concat(TemplateTypes).ToArray();

    /// <summary>بازسازی فهرست پالایه‌شده — پس از بارگذاری یا تغییر پالایه</summary>
    private void ApplyTemplateFilter()
    {
        // اندیسِ امن — اگر پالایه بیرون از بازه شد (مثلاً پس از حذف نوعی) به «همه» برمی‌گردد
        var idx = TemplateFilterIndex;
        if (idx < 0 || idx >= TemplateFilterOptions.Length) idx = 0;
        var key = TemplateFilterOptions[idx].Key;

        VisibleTemplates.Clear();
        foreach (var t in PrintTemplates)
        {
            // کلید خالی = بدون پالایه؛ وگرنه فقط قالب‌های همان نوع
            if (key.Length == 0 || string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase))
                VisibleTemplates.Add(t);
        }

        // اگر قالب انتخابی فعلی در فهرست پالایه‌شده نیست، اولین قالب فهرست انتخاب می‌شود
        if (SelectedTemplate is null || (key.Length > 0 && SelectedTemplate.Key != key))
            SelectedTemplate = VisibleTemplates.Count > 0 ? VisibleTemplates[0] : null;
    }

    /// <summary>پالایه عوض شد — فهرست دوباره پالایه می‌شود (بدون رفت‌وبرگشت به سرور)</summary>
    partial void OnTemplateFilterIndexChanged(int value) => ApplyTemplateFilter();

    /// <summary>قالب انتخابی</summary>
    [ObservableProperty]
    private PrintTemplateRow? _selectedTemplate;

    /// <summary>پیش‌نمایش HTML رندرشده</summary>
    [ObservableProperty]
    private string? _previewHtml;

    /// <summary>متغیرهای نوع فعال برای پالت</summary>
    [ObservableProperty]
    private ObservableCollection<string> _activeVariables = new();

    /// <summary>نمایه نوع در فرم ساخت سریع — اندیس داخل TemplateTypes (که در چرخهٔ سی‌وششم به بالای مصرف‌کننده‌های استاتیکش منتقل شد)</summary>
    [ObservableProperty]
    private int _newTypeIndex;

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

    // ═══ چرخهٔ بیست‌وهشتم (گزارش هجدهم مالک) ═══
    // «نظرسنجی‌ها طراحی نمی‌شوند و فقط به لیست اضافه می‌شوند + دکمه‌های
    // عملیاتی لیست ناقص است» — طراح سوالات (سازندهٔ پویا) به همین تب
    // آورده شد؛ سوالات موجود نظرسنجی از JSON بارگذاری و ویرایش می‌شوند
    // و کلیدهای سطری ویرایش/نتایج/حذف به جدول اضافه شدند.

    /// <summary>آیا طراح سوالات باز است</summary>
    [ObservableProperty]
    private bool _isSurveyDesignerOpen;

    /// <summary>شناسهٔ نظرسنجی در حال ویرایش در طراح</summary>
    private long _designingSurveyId;

    /// <summary>عنوان در طراح سوالات</summary>
    [ObservableProperty]
    private string _designerTitle = string.Empty;

    /// <summary>توضیح در طراح سوالات</summary>
    [ObservableProperty]
    private string? _designerDescription;

    /// <summary>سوالات طراح — سازندهٔ پویا (همان آیتم صفحهٔ نظرسنجی)</summary>
    public ObservableCollection<SurveyQuestionItem> DesignerQuestions { get; } = new();

    // ═══ چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک) — قالب‌های آمادهٔ نظرسنجی ═══
    // «قالب‌های پیش‌فرض نظرسنجی» — چهار نظرسنجی حرفه‌ای آماده با یک کلیک
    // افزوده می‌شوند؛ سؤال‌ها از سه نوع پشتیبانی‌شدهٔ سامانه ساخته شده‌اند:
    // rating (امتیاز ۵ ستاره) | text (متن آزاد) | choice (چندگزینه‌ای با options)
    /// <summary>قالب نظرسنجی آماده — با یک کلیک ثبت می‌شود</summary>
    /// <param name="Title">عنوان قالب</param>
    /// <param name="Icon">ایموجی دکمهٔ قالب</param>
    /// <param name="Description">توضیح قالب</param>
    /// <param name="Questions">جیسون سؤال‌ها — همان ساختار فرم نظرسنجی</param>
    public record SurveyTemplateRow(string Icon, string Title, string Description, string Questions);

    /// <summary>قالب‌های آمادهٔ نظرسنجی — چرخه ۲۷</summary>
    public IReadOnlyList<SurveyTemplateRow> SurveyTemplates { get; } = new SurveyTemplateRow[]
    {
        new("⭐", "رضایت عمومی بیماران", "نظرسنجی استاندارد رضایت از پذیرش تا ویزیت (۵ سؤال)",
            "[{\"q\":\"میزان رضایت شما از برخورد پذیرش؟\",\"type\":\"rating\"}," +
            "{\"q\":\"میزان رضایت شما از پزشک معالج؟\",\"type\":\"rating\"}," +
            "{\"q\":\"نظافت و آراستگی درمانگاه؟\",\"type\":\"rating\"}," +
            "{\"q\":\"مدت انتظار شما چگونه بود؟\",\"type\":\"choice\",\"options\":[\"کم\",\"متوسط\",\"زیاد\"]}," +
            "{\"q\":\"پیشنهاد یا انتقاد شما؟\",\"type\":\"text\"}]"),
        new("🤝", "ارزیابی کیفیت پرسنل", "برخورد، پاسخگویی و آموزش پرسنل (۴ سؤال)",
            "[{\"q\":\"پاسخگویی تلفنی چگونه بود؟\",\"type\":\"choice\",\"options\":[\"عالی\",\"خوب\",\"ضعیف\"]}," +
            "{\"q\":\"برخورد پرسنل پذیرش؟\",\"type\":\"rating\"}," +
            "{\"q\":\"توضیح پزشک درباره بیماری و داروها؟\",\"type\":\"rating\"}," +
            "{\"q\":\"نکته‌ای که باید بهتر شود؟\",\"type\":\"text\"}]"),
        new("💊", "پیگیری پس از درمان", "وضعیت بیمار پس از ویزیت/دندان‌پزشکی/عمل (۴ سؤال)",
            "[{\"q\":\"وضعیت عمومی شما اکنون چگونه است؟\",\"type\":\"choice\",\"options\":[\"کاملاً بهبود\",\"در حال بهبود\",\"بدون تغییر\",\"بدتر شده\"]}," +
            "{\"q\":\"عوارض یا درد جدید داشته‌اید؟\",\"type\":\"choice\",\"options\":[\"خیر\",\"خفیف\",\"متوسط\",\"شدید\"]}," +
            "{\"q\":\"داروها را طبق دستور مصرف کرده‌اید؟\",\"type\":\"choice\",\"options\":[\"بله\",\"مخالف\",\"قطع کرده‌ام\"]}," +
            "{\"q\":\"توضیح بیشتر برای پزشک؟\",\"type\":\"text\"}]"),
        new("🏥", "ارزیابی خدمات درمانگاه", "نظرسنجی جامع کیفیت خدمات و امکانات (۵ سؤال)",
            "[{\"q\":\"آسان‌یابی مسیر و تابلوها؟\",\"type\":\"rating\"}," +
            "{\"q\":\"راحتی انتظار و امکانات راهرو؟\",\"type\":\"rating\"}," +
            "{\"q\":\"کیفیت خدمات آزمایشگاه/تصویربرداری؟\",\"type\":\"rating\"}," +
            "{\"q\":\"تعرفه‌ها و شفافیت هزینه‌ها؟\",\"type\":\"choice\",\"options\":[\"مناسب\",\"متوسط\",\"نامناسب\"]}," +
            "{\"q\":\"آیا درمانگاه را به دیگران پیشنهاد می‌کنید؟ چرا؟\",\"type\":\"text\"}]"),
    };

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
        _ = LoadTemplatesAsync();
        _ = LoadSurveysAsync();
    }

    /// <summary>بارگذاری داده زبانه فعال</summary>
    partial void OnTabIndexChanged(int value)
    {
        if (value == 1) _ = LoadSurveysAsync();
        if (value == 0) _ = LoadTemplatesAsync();
    }

    /* ─── قالب‌های چاپ سفارشی — چرخه ۱۳ ─── */

    /// <summary>بارگذاری فهرست قالب‌ها از سرور</summary>
    [RelayCommand]
    public async Task LoadTemplatesAsync()
    {
        IsBusy = true;
        var response = await _api.GetAsync<TemplatesEnvelope>("print-templates");
        IsBusy = false;
        PrintTemplates.Clear();
        foreach (var t in response?.Data ?? new List<TemplateApi>())
        {
            var row = new PrintTemplateRow
            {
                Id = t.Id,
                Key = t.Type ?? "Invoice",
                Title = t.TemplateName,
                TypeFa = t.TypeFa ?? "",
                Icon = PrintTemplateRow.IconOf(t.Type ?? ""),
                PaperSize = t.PaperSize ?? "A5",
                IsActive = t.IsActive,
                IsDefault = t.IsDefault
            };
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(PrintTemplateRow.IsDirty)) row.IsDirty = true;
            };
            PrintTemplates.Add(row);
        }
        // چرخهٔ سی‌وپنجم: پس از بارگذاری کامل، پالایهٔ موجودیت دوباره اعمال می‌شود
        // تا فهرست طراح همیشه فقط قالب‌های موجودیتِ منتخب را نشان دهد
        ApplyTemplateFilter();
        if (SelectedTemplate is null && PrintTemplates.Count > 0)
            SelectedTemplate = PrintTemplates[0];
    }

    /// <summary>بارگذاری کامل بدنه و تنظیمات قالب انتخابی</summary>
    partial void OnSelectedTemplateChanged(PrintTemplateRow? value)
    {
        if (value is null) return;
        _ = LoadTemplateBodyAsync(value);
    }

    /// <summary>خواندن کامل قالب + پالت متغیرها</summary>
    private async Task LoadTemplateBodyAsync(PrintTemplateRow template)
    {
        if (template.Id == 0) { RefreshPalette(template.Key); return; }
        var response = await _api.GetAsync<TemplateDetailEnvelope>($"print-templates/{template.Id}");
        var d = response?.Data;
        if (d is null) return;
        template.Title = d.TemplateName;
        template.Description = d.Description;
        template.Body = d.HtmlBody;
        template.PaperSize = d.PaperSize;
        template.AccentColor = d.AccentColor ?? "#16355E";
        template.FontSize = d.FontSize;
        template.ShowLogo = d.ShowClinicLogo;
        template.IsActive = d.IsActive;
        template.IsDefault = d.IsDefault;
        template.IsDirty = false;
        RefreshPalette(template.Key);
    }

    /// <summary>پالت متغیرهای نوع فعال</summary>
    private void RefreshPalette(string key)
    {
        var map = new Dictionary<string, string[]>
        {
            ["Invoice"] = new[] { "invoice_code", "invoice_date", "patient_name", "insurance_name", "items_html", "subtotal", "discount", "insurance_share", "patient_share", "total" },
            ["Prescription"] = new[] { "patient_name", "patient_age", "patient_national_code", "doctor_name", "visit_date", "prescription_code", "items_html", "notes" },
            ["LabOrder"] = new[] { "patient_name", "doctor_name", "order_number", "order_date", "tests_html", "clinical_info" },
            ["LabResult"] = new[] { "patient_name", "order_number", "result_date", "results_html", "verified_by" },
            ["ImagingOrder"] = new[] { "patient_name", "modality", "order_number", "order_date", "clinical_info" },
            ["AppointmentTicket"] = new[] { "patient_name", "doctor_name", "appointment_date", "appointment_time", "queue_number", "tracking_code" },
            ["ReceptionReceipt"] = new[] { "patient_name", "reception_date", "reception_code", "service_name", "amount", "payment_method" },
            ["MedicalCertificate"] = new[] { "patient_name", "doctor_name", "issue_date", "content", "duration" },
            ["PayrollSlip"] = new[] { "employee_name", "period", "base_salary", "overtime_pay", "insurance", "tax", "net_pay" }
        };
        ActiveVariables = new ObservableCollection<string>(
            map.TryGetValue(key, out var vars) ? vars : Array.Empty<string>());
    }

    /// <summary>ساخت قالب جدید از نوع انتخابی</summary>
    [RelayCommand]
    private void NewTemplate()
    {
        var typeOpt = TemplateTypes[Math.Max(0, Math.Min(NewTypeIndex, TemplateTypes.Length - 1))];
        var (key, fa) = (typeOpt.Key, typeOpt.Fa);
        var row = new PrintTemplateRow
        {
            Id = 0,
            Key = key,
            Title = $"قالب {fa} — جدید",
            TypeFa = fa,
            Icon = PrintTemplateRow.IconOf(key),
            Body = "<div style='font-size:15px;font-weight:800'>عنوان سند</div>\n<div>متن با متغیر: {{patient_name}}</div>",
            PaperSize = "A5",
            IsActive = true,
            IsDirty = true
        };
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PrintTemplateRow.IsDirty)) row.IsDirty = true;
        };
        PrintTemplates.Add(row);
        // چرخهٔ سی‌وپنجم: اگر فیلتر فعلی با نوع قالب تازه یکی نیست، فیلتر به همان
        // موجودیت می‌رود تا قالب تازه بلافاصله در فهرست دیده شود (وگرنه پالایه
        // قالب تازه را پنهان می‌کرد و کاربر فکر می‌کرد ساخته نشده است)
        var filterIdx = Array.FindIndex(TemplateFilterOptions, o => o.Key == key);
        if (TemplateFilterIndex != filterIdx && filterIdx >= 0)
            TemplateFilterIndex = filterIdx;
        else
            ApplyTemplateFilter();
        SelectedTemplate = row;
        RefreshPalette(key);
        _snackbar.Info("قالب جدید ساخته شد — پس از ویرایش ذخیره کنید");
    }

    /// <summary>ذخیره قالب (ثبت جدید یا ویرایش)</summary>
    [RelayCommand]
    private async Task SaveTemplateAsync()
    {
        if (SelectedTemplate is null) { _snackbar.Info("ابتدا یک قالب انتخاب کنید"); return; }
        if (SelectedTemplate.Title.Trim().Length < 3) { _snackbar.Error("نام قالب حداقل ۳ نویسه باشد"); return; }
        if (SelectedTemplate.Body.Trim().Length < 10) { _snackbar.Error("بدنه قالب خالی است"); return; }

        IsBusy = true;
        var payload = new
        {
            templateName = SelectedTemplate.Title,
            type = SelectedTemplate.Key,
            description = SelectedTemplate.Description,
            htmlBody = SelectedTemplate.Body,
            paperSize = SelectedTemplate.PaperSize,
            orientation = "Portrait",
            fontSize = SelectedTemplate.FontSize,
            fontFamily = (string?)null,
            showClinicLogo = SelectedTemplate.ShowLogo,
            accentColor = SelectedTemplate.AccentColor,
            isActive = SelectedTemplate.IsActive,
            isDefault = SelectedTemplate.IsDefault
        };
        var ok = SelectedTemplate.Id == 0
            ? await _api.PostAsync("print-templates", payload)
            : await _api.PutAsync($"print-templates/{SelectedTemplate.Id}", payload);
        IsBusy = false;

        if (ok)
        {
            _snackbar.Success($"قالب «{SelectedTemplate.Title}» ذخیره شد");
            SelectedTemplate.IsDirty = false;
            await LoadTemplatesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ذخیره قالب ناموفق بود");
    }

    /// <summary>کپی قالب انتخابی به‌عنوان نسخه جدید</summary>
    [RelayCommand]
    private async Task DuplicateTemplateAsync()
    {
        if (SelectedTemplate is null || SelectedTemplate.Id == 0) { _snackbar.Info("ابتدا یک قالب ذخیره‌شده انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PostAsync($"print-templates/{SelectedTemplate.Id}/duplicate",
            new { newName = $"{SelectedTemplate.Title} (کپی)" });
        IsBusy = false;
        if (ok) { _snackbar.Success("نسخه کپی ساخته شد"); await LoadTemplatesAsync(); }
        else _snackbar.Error(_api.LastError ?? "کپی ناموفق بود");
    }

    /// <summary>تعیین به‌عنوان قالب پیش‌فرض نوع</summary>
    [RelayCommand]
    private async Task SetDefaultTemplateAsync()
    {
        if (SelectedTemplate is null || SelectedTemplate.Id == 0) { _snackbar.Info("ابتدا یک قالب ذخیره‌شده انتخاب کنید"); return; }
        IsBusy = true;
        var ok = await _api.PostAsync($"print-templates/{SelectedTemplate.Id}/set-default", new { });
        IsBusy = false;
        if (ok) { _snackbar.Success("قالب پیش‌فرض این نوع سند شد"); await LoadTemplatesAsync(); }
        else _snackbar.Error(_api.LastError ?? "عملیات ناموفق بود");
    }

    /// <summary>حذف قالب</summary>
    [RelayCommand]
    private async Task DeleteTemplateAsync()
    {
        if (SelectedTemplate is null || SelectedTemplate.Id == 0) { _snackbar.Info("ابتدا یک قالب ذخیره‌شده انتخاب کنید"); return; }
        if (SelectedTemplate.IsDefault) { _snackbar.Error("قالب پیش‌فرض قابل حذف نیست — ابتدا قالب دیگر را پیش‌فرض کنید"); return; }
        IsBusy = true;
        var ok = await _api.DeleteAsync($"print-templates/{SelectedTemplate.Id}");
        IsBusy = false;
        if (ok) { _snackbar.Success("قالب حذف شد"); await LoadTemplatesAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /// <summary>پیش‌نمایش زنده با داده نمونه</summary>
    [RelayCommand]
    private async Task PreviewTemplateAsync()
    {
        if (SelectedTemplate is null) { _snackbar.Info("ابتدا یک قالب انتخاب کنید"); return; }
        // برای قالب ذخیره‌نشده ابتدا ذخیره می‌کنیم تا پیش‌نمایش سروری دقیق باشد
        if (SelectedTemplate.IsDirty || SelectedTemplate.Id == 0)
            await SaveTemplateAsync();
        if (SelectedTemplate.Id == 0) return;

        IsBusy = true;
        var response = await _api.GetAsync<PreviewEnvelope>($"print-templates/{SelectedTemplate.Id}/preview");
        IsBusy = false;
        if (response?.Data?.Html is { Length: > 0 } html)
        {
            PreviewHtml = html;
            _snackbar.Success("پیش‌نمایش آماده شد");
        }
        else _snackbar.Error(_api.LastError ?? "پیش‌نمایش ناموفق بود");
    }

    /// <summary>درج متغیر در محل نشانگر — با پیام ساده (درج در انتها)</summary>
    [RelayCommand]
    private void InsertVariable(string? variable)
    {
        if (SelectedTemplate is null || string.IsNullOrWhiteSpace(variable)) return;
        SelectedTemplate.Body += $"{{{{{variable}}}}}";
        SelectedTemplate.IsDirty = true;
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
                    AverageRating = s.AverageRating,
                    QuestionsJson = s.Questions
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

    /// <summary>
    /// گشودن طراح سوالات یک نظرسنجی — چرخهٔ بیست‌وهشتم: سوالات موجود از
    /// JSON فهرست بارگذاری می‌شوند (GET از چرخهٔ ۲۸ سوالات را برمی‌گرداند)
    /// تا ویرایش واقعی باشد نه بازنویسی از نو.
    /// </summary>
    [RelayCommand]
    private void OpenSurveyDesigner(SurveyRow? row)
    {
        row ??= SelectedSurvey;
        if (row is null) { _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید"); return; }

        _designingSurveyId = row.Id;
        DesignerTitle = row.Title;
        DesignerDescription = row.Description;
        DesignerQuestions.Clear();
        var existing = SurveyQuestionItem.ParseList(row.QuestionsJson);
        if (existing.Count > 0)
            foreach (var q in existing)
                DesignerQuestions.Add(q);
        else
            DesignerQuestions.Add(new SurveyQuestionItem { Text = "میزان رضایت کلی شما از خدمات درمانگاه؟" });
        IsSurveyDesignerOpen = true;
    }

    /// <summary>افزودن سوال به طراح</summary>
    [RelayCommand]
    private void AddDesignerQuestion() => DesignerQuestions.Add(new SurveyQuestionItem());

    /// <summary>حذف سوال از طراح</summary>
    [RelayCommand]
    private void RemoveDesignerQuestion(SurveyQuestionItem question) => DesignerQuestions.Remove(question);

    /// <summary>ذخیرهٔ نظرسنجی از طراح — PUT surveys/{id}</summary>
    [RelayCommand]
    private async Task SaveSurveyDesignerAsync()
    {
        if (string.IsNullOrWhiteSpace(DesignerTitle) || DesignerTitle.Trim().Length < 3)
        { _snackbar.Error("عنوان نظرسنجی حداقل ۳ کاراکتر است"); return; }
        if (DesignerQuestions.Count == 0)
        { _snackbar.Error("حداقل یک سوال اضافه کنید"); return; }

        var questionsJson = "[" + string.Join(",", DesignerQuestions.Select(q => q.ToJson())) + "]";
        IsBusy = true;
        var ok = await _api.PutAsync($"surveys/{_designingSurveyId}", new
        {
            title = DesignerTitle.Trim(),
            description = DesignerDescription,
            questions = questionsJson,
            isActive = (bool?)null
        });
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success("نظرسنجی با سوالات جدید به‌روزرسانی شد");
            IsSurveyDesignerOpen = false;
            await LoadSurveysAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ذخیرهٔ نظرسنجی ناموفق بود");
    }

    /// <summary>بستن طراح بدون ذخیره</summary>
    [RelayCommand]
    private void CancelSurveyDesigner() => IsSurveyDesignerOpen = false;

    /// <summary>
    /// حذف نرم نظرسنجی — با تأییدیهٔ فارسی خود برنامه (قانون مالک — نه کادر ویندوز)
    /// </summary>
    [RelayCommand]
    private async Task DeleteSurveyAsync(SurveyRow? row)
    {
        row ??= SelectedSurvey;
        if (row is null) { _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید"); return; }

        var confirmed = Views.AppDialog.Confirm(
            "حذف نظرسنجی",
            $"نظرسنجی «{row.Title}» حذف شود؟\nپاسخ‌های ثبت‌شده در دیتابیس حفظ می‌شوند.",
            Views.AppDialogIcon.Warning);
        if (confirmed != true) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"surveys/{row.Id}");
        IsBusy = false;
        if (ok) { _snackbar.Success("نظرسنجی حذف شد"); await LoadSurveysAsync(); }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    /// <summary>
    /// نمایش خلاصهٔ نتایج نظرسنجی — کارت‌های خلاصه + توزیع ستاره‌ها در دیالوگ فارسی
    /// </summary>
    [RelayCommand]
    private async Task ShowSurveyResultsAsync(SurveyRow? row)
    {
        row ??= SelectedSurvey;
        if (row is null) { _snackbar.Error("ابتدا یک نظرسنجی از جدول انتخاب کنید"); return; }

        var response = await _api.GetAsync<SurveyResultsResponse>($"surveys/{row.Id}/results");
        var r = response?.Data;
        if (r?.Summary is null)
        { _snackbar.Error(_api.LastError ?? "دریافت نتایج ناموفق بود"); return; }

        // توزیع ستاره‌ها — خط خوانا از ۵ تا ۱
        var distText = string.Join("\n", (r.Distribution ?? new List<StarDistributionDto>())
            .OrderByDescending(d => d.Star)
            .Select(d => $"ستاره {d.Star}: {d.Count} پاسخ"));

        Views.AppDialog.Info(
            $"نتایج «{row.Title}»",
            $"کل پاسخ‌ها: {r.Summary.TotalResponses}\n" +
            $"میانگین امتیاز: {(r.Summary.AverageRating > 0 ? r.Summary.AverageRating.ToString("0.##") + " از ۵" : "—")}\n" +
            $"پاسخ‌های ۳۰ روز اخیر: {r.Summary.Last30Days}\n\n{distText}");
    }

    /// <summary>
    /// افزودن نظرسنجی از قالب آماده — چرخهٔ بیست‌وهفتم (گزارش هجدهم مالک):
    /// «قالب‌های پیش‌فرض نظرسنجی» — قالب حرفه‌ای آماده با یک کلیک ثبت
    /// می‌شود؛ اگر نظرسنجی هم‌نامِ فعلی وجود داشته باشد، عنوان پسوند «(جدید)» می‌گیرد.
    /// </summary>
    [RelayCommand]
    private async Task AddSurveyFromTemplateAsync(SurveyTemplateRow? template)
    {
        if (template is null) return;
        IsBusy = true;
        // عنوان یکتا — اگر نظرسنجی هم‌نام فعال هست، پسوند «(جدید)» می‌گیرد
        var title = Surveys.Any(s => s.Title == template.Title && s.IsActive)
            ? $"{template.Title} (جدید)"
            : template.Title;
        var ok = await _api.PostAsync("surveys", new
        {
            title,
            description = template.Description,
            questions = template.Questions
        });
        IsBusy = false;
        if (ok)
        {
            _snackbar.Success($"نظرسنجی «{title}» از قالب آماده ساخته شد");
            await LoadSurveysAsync();
        }
        else
            _snackbar.Error(_api.LastError ?? "ایجاد نظرسنجی از قالب ناموفق بود");
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
            LastSyncText = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsiFa(IranClock.Now); // چرخه هجدهم: ارقام فارسی
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

    /// <summary>پاسخ فهرست قالب‌های چاپ — چرخه ۱۳</summary>
    public sealed class TemplatesEnvelope
    {
        public bool IsSuccess { get; set; }
        public List<TemplateApi>? Data { get; set; }
    }

    /// <summary>ردیف قالب در فهرست</summary>
    public sealed class TemplateApi
    {
        public long Id { get; set; }
        public string TemplateName { get; set; } = "";
        public string? Type { get; set; }
        public string? TypeFa { get; set; }
        public string? PaperSize { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>پاسخ جزئیات قالب</summary>
    public sealed class TemplateDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public TemplateDetailApi? Data { get; set; }
    }

    /// <summary>جزئیات کامل قالب</summary>
    public sealed class TemplateDetailApi
    {
        public long Id { get; set; }
        public string TemplateName { get; set; } = "";
        public string? Description { get; set; }
        public string HtmlBody { get; set; } = "";
        public string PaperSize { get; set; } = "A5";
        public int FontSize { get; set; } = 13;
        public bool ShowClinicLogo { get; set; } = true;
        public string? AccentColor { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>پاسخ پیش‌نمایش</summary>
    public sealed class PreviewEnvelope
    {
        public bool IsSuccess { get; set; }
        public PreviewData? Data { get; set; }
    }

    /// <summary>داده پیش‌نمایش</summary>
    public sealed class PreviewData
    {
        public string? Html { get; set; }
        public string? PaperSize { get; set; }
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
        /// <summary>سوالات JSON — چرخهٔ ۲۸: از GET / فهرست برای طراح</summary>
        public string? Questions { get; set; }
    }

    /// <summary>پاسخ خلاصهٔ نتایج نظرسنجی — چرخهٔ ۲۸ (دیالوگ نتایج)</summary>
    private sealed class SurveyResultsResponse
    {
        public bool IsSuccess { get; set; }
        public SurveyResultsData? Data { get; set; }
    }

    private sealed class SurveyResultsData
    {
        public SurveyResultsSummary? Summary { get; set; }
        public List<StarDistributionDto>? Distribution { get; set; }
    }

    private sealed class SurveyResultsSummary
    {
        public int TotalResponses { get; set; }
        public double AverageRating { get; set; }
        public int Last30Days { get; set; }
    }

    private sealed class StarDistributionDto
    {
        public int Star { get; set; }
        public int Count { get; set; }
    }
}
