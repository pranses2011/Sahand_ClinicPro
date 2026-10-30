using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Domain.Entities.Reporting;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف قالب چاپ سفارشی — طراح چرخه ۱۳ (درخواست ۱-۶)</summary>
public partial class PrintTemplateRow : ObservableObject
{
    /// <summary>شناسه — صفر یعنی قالب جدید</summary>
    public long Id { get; set; }

    /// <summary>جزئیات قالب از سرور دریافت شده‌اند.</summary>
    public bool IsLoaded { get; set; }

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

    /// <summary>قبض اصلی یک سند داخلی است و بدنه HTML قابل ویرایش ندارد.</summary>
    public bool IsOriginalReceipt => Key == "ReceptionReceipt" && Body == BuiltInPrintTemplates.ReceptionReceiptMarker;
    /// <summary>آیا ویرایشگر HTML و تنظیمات ظاهری قابل استفاده است؟</summary>
    public bool IsHtmlTemplate => !IsOriginalReceipt;
    partial void OnBodyChanged(string value)
    {
        OnPropertyChanged(nameof(IsOriginalReceipt));
        OnPropertyChanged(nameof(IsHtmlTemplate));
    }

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
    /// <summary>نسخه فرم برای جلوگیری از بازنویسی همزمان.</summary>
    public string? EditVersion { get; init; }
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

    /// <summary>یک متغیر قالب — گروه، آیکون مرتبط، برچسب فارسی و کلید درج در متن</summary>
    public sealed record TemplateVariable(string Group, string Icon, string Label, string Key);

    /// <summary>یک طرح آمادهٔ قالب برای گالری «از بین طرح‌ها انتخاب کنید»</summary>
    public sealed record PresetTemplate(string TypeKey, string Title, string Accent, string Paper, string Body);

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
    private void ApplyTemplateFilter(PrintTemplateRow? preferred = null)
    {
        var previous = preferred ?? SelectedTemplate;
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

        // پاک شدن مجموعه در ListBox ممکن است انتخاب را موقتاً null کند؛ پس از
        // پر کردن مجموعه، انتخاب پیشین را فقط اگر واقعاً در فهرست است بازگردان.
        SelectedTemplate = previous is not null && VisibleTemplates.Contains(previous)
            ? previous : VisibleTemplates.FirstOrDefault();
    }

    /// <summary>پالایه عوض شد — فهرست دوباره پالایه می‌شود (بدون رفت‌وبرگشت به سرور)</summary>
    partial void OnTemplateFilterIndexChanged(int value) => ApplyTemplateFilter();

    /// <summary>قالب انتخابی</summary>
    [ObservableProperty]
    private PrintTemplateRow? _selectedTemplate;

    /// <summary>درخواست نمایش سند داخلی توسط نما.</summary>
    public event Action? OriginalReceiptPreviewRequested;

    /// <summary>بارگذاری جزئیات قالب انتخابی هنوز کامل نشده است.</summary>
    [ObservableProperty]
    private bool _isTemplateLoading;

    /// <summary>آماده بودن ویرایشگر برای جلوگیری از تغییر هنگام دریافت جزئیات.</summary>
    [ObservableProperty]
    private bool _isTemplateSaving;
    public bool IsTemplateReady => !IsTemplateLoading && !IsTemplateSaving;
    partial void OnIsTemplateLoadingChanged(bool value) => OnPropertyChanged(nameof(IsTemplateReady));
    partial void OnIsTemplateSavingChanged(bool value) => OnPropertyChanged(nameof(IsTemplateReady));

    /// <summary>پیش‌نمایش HTML رندرشده</summary>
    [ObservableProperty]
    private string? _previewHtml;

    /// <summary>متغیرهای نوع فعال برای پالت</summary>
    [ObservableProperty]
    private ObservableCollection<TemplateVariable> _activeVariables = new();

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
    private string? _designingSurveyVersion;
    private string? _originalDesignerQuestions;
    private string? _initialDesignerRepresentation;
    private string? DesignerRepresentation() => DesignerQuestions.Count == 0 ? null :
        "[" + string.Join(",", DesignerQuestions.Select(q => q.ToJson())) + "]";

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

    private long _templateLoadVersion;

    /// <summary>بارگذاری فهرست قالب‌ها از سرور بدون حذف تغییرات ذخیره‌نشده.</summary>
    [RelayCommand]
    public async Task LoadTemplatesAsync()
    {
        if (PrintTemplates.Any(t => t.IsDirty))
        {
            _snackbar.Warning("برای بارگذاری دوباره، ابتدا همه قالب‌های تغییرکرده را ذخیره کنید.");
            return;
        }
        var version = ++_templateLoadVersion;
        IsBusy = true;
        var response = await _api.GetAsync<TemplatesEnvelope>("print-templates");
        IsBusy = false;
        if (version != _templateLoadVersion) return;
        if (PrintTemplates.Any(t => t.IsDirty))
        {
            _snackbar.Warning("فهرست تازه اعمال نشد؛ قالبی هنگام دریافت تغییر کرده است.");
            return;
        }
        if (response?.Data is null) { _snackbar.Error(_api.LastError ?? "بارگذاری قالب‌ها ناموفق بود"); return; }
        var selectedId = SelectedTemplate?.Id;
        PrintTemplates.Clear();
        foreach (var t in response.Data)
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
                if (e.PropertyName != nameof(PrintTemplateRow.IsDirty))
                {
                    row.IsDirty = true;
                    if (ReferenceEquals(SelectedTemplate, row)) PreviewHtml = null;
                }
            };
            PrintTemplates.Add(row);
        }
        // چرخهٔ سی‌وپنجم: پس از بارگذاری کامل، پالایهٔ موجودیت دوباره اعمال می‌شود
        // تا فهرست طراح همیشه فقط قالب‌های موجودیتِ منتخب را نشان دهد
        ApplyTemplateFilter(PrintTemplates.FirstOrDefault(t => t.Id == selectedId));
    }

    /// <summary>بارگذاری کامل بدنه و تنظیمات قالب انتخابی</summary>
    partial void OnSelectedTemplateChanged(PrintTemplateRow? value)
    {
        PreviewHtml = null;
        IsTemplateLoading = value is { Id: > 0, IsLoaded: false };
        if (value is null) { ActiveVariables.Clear(); return; }
        if (value.IsLoaded) { RefreshPalette(value.Key); return; }
        _ = LoadTemplateBodyAsync(value);
    }

    /// <summary>خواندن کامل قالب + پالت متغیرها</summary>
    private async Task LoadTemplateBodyAsync(PrintTemplateRow template)
    {
        if (template.Id == 0) { RefreshPalette(template.Key); return; }
        var response = await _api.GetAsync<TemplateDetailEnvelope>($"print-templates/{template.Id}");
        if (!ReferenceEquals(SelectedTemplate, template)) return;
        IsTemplateLoading = false;
        var d = response?.Data;
        if (d is null) { _snackbar.Error(_api.LastError ?? "بارگذاری جزئیات قالب ناموفق بود"); IsTemplateLoading = true; return; }
        if (template.IsDirty) { _snackbar.Error("قالب هنگام بارگذاری تغییر کرد؛ دوباره انتخاب کنید"); IsTemplateLoading = true; return; }
        ApplyTemplateDetails(template, d);
    }

    private void ApplyTemplateDetails(PrintTemplateRow template, TemplateDetailApi d)
    {
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
        template.IsLoaded = true;
        RefreshPalette(template.Key);
    }

    /// <summary>
    /// پالت متغیرهای نوع فعال — گروه‌بندی‌شده (بیمار/سند/مالی/درمانگاه) و هر
    /// متغیر با آیکون مرتبط خودش (بازطراحی تأییدشده: «متغیرهای کامل و مورد
    /// نیاز هر بخش با آیکون‌های مرتبط؛ الان همه یک مدل هستند»)
    /// </summary>
    private void RefreshPalette(string key)
    {
        var map = new Dictionary<string, TemplateVariable[]>
        {
            ["Invoice"] = new[]
            {
                new TemplateVariable("سند", "📄", "شماره فاکتور", "invoice_code"),
                new TemplateVariable("سند", "📅", "تاریخ فاکتور", "invoice_date"),
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "🛡️", "بیمه", "insurance_name"),
                new TemplateVariable("مالی", "🧾", "اقلام (جدول)", "items_html"),
                new TemplateVariable("مالی", "➕", "جمع جزء", "subtotal"),
                new TemplateVariable("مالی", "🏷️", "تخفیف", "discount"),
                new TemplateVariable("مالی", "🛡️", "سهم بیمه", "insurance_share"),
                new TemplateVariable("مالی", "💳", "پرداخت‌شده", "paid"),
                new TemplateVariable("مالی", "📌", "مانده", "remaining"),
                new TemplateVariable("مالی", "✅", "وضعیت پرداخت", "payment_status"),
                new TemplateVariable("مالی", "🧮", "مالیات", "tax"),
                new TemplateVariable("مالی", "👛", "سهم بیمار", "patient_share"),
                new TemplateVariable("مالی", "💰", "جمع قبل از سهم بیمه", "total"),
                new TemplateVariable("درمانگاه", "🏥", "نام درمانگاه", "clinic_name"),
                new TemplateVariable("درمانگاه", "🖨️", "تاریخ چاپ", "print_date")
            },
            ["Prescription"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "🎂", "سن بیمار", "patient_age"),
                new TemplateVariable("بیمار", "🪪", "کد ملی", "patient_national_code"),
                new TemplateVariable("ویزیت", "🩺", "نام پزشک", "doctor_name"),
                new TemplateVariable("ویزیت", "📅", "تاریخ ویزیت", "visit_date"),
                new TemplateVariable("ویزیت", "🔖", "کد نسخه", "prescription_code"),
                new TemplateVariable("دارو", "💊", "داروها (جدول)", "items_html"),
                new TemplateVariable("دارو", "📝", "یادداشت پزشک", "notes"),
                new TemplateVariable("درمانگاه", "🏥", "نام درمانگاه", "clinic_name")
            },
            ["LabOrder"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("درخواست", "🩺", "پزشک", "doctor_name"),
                new TemplateVariable("درخواست", "🔖", "شماره درخواست", "order_number"),
                new TemplateVariable("درخواست", "📅", "تاریخ درخواست", "order_date"),
                new TemplateVariable("آزمایش", "🧪", "آزمایش‌ها (جدول)", "tests_html"),
                new TemplateVariable("آزمایش", "📋", "اطلاعات بالینی", "clinical_info")
            },
            ["LabResult"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "🪪", "کد ملی", "patient_national_code"),
                new TemplateVariable("درخواست", "🔖", "شماره درخواست", "order_number"),
                new TemplateVariable("درخواست", "📅", "تاریخ جواب", "result_date"),
                new TemplateVariable("نتایج", "📊", "جدول نتایج", "results_html"),
                new TemplateVariable("نتایج", "✅", "تأییدکننده", "verified_by"),
                new TemplateVariable("درمانگاه", "🏥", "نام درمانگاه", "clinic_name")
            },
            ["ImagingOrder"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("مطالعه", "🩻", "مدالیته", "modality"),
                new TemplateVariable("مطالعه", "🔖", "شماره درخواست", "order_number"),
                new TemplateVariable("مطالعه", "📅", "تاریخ مطالعه", "order_date"),
                new TemplateVariable("گزارش", "📝", "یافته‌ها", "findings"),
                new TemplateVariable("گزارش", "🧾", "نتیجه‌گیری", "impression"),
                new TemplateVariable("گزارش", "☢️", "دوز تابش", "dose_info")
            },
            ["AppointmentTicket"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "📱", "موبایل", "patient_mobile"),
                new TemplateVariable("نوبت", "🩺", "پزشک", "doctor_name"),
                new TemplateVariable("نوبت", "📅", "تاریخ نوبت", "appointment_date"),
                new TemplateVariable("نوبت", "⏰", "ساعت نوبت", "appointment_time"),
                new TemplateVariable("نوبت", "🔢", "شماره صف", "queue_number"),
                new TemplateVariable("نوبت", "🔖", "کد پیگیری", "tracking_code")
            },
            ["ReceptionReceipt"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "🪪", "کد ملی", "patient_national_code"),
                new TemplateVariable("بیمار", "📱", "موبایل", "patient_mobile"),
                new TemplateVariable("پذیرش", "🔢", "شماره صف", "queue_number"),
                new TemplateVariable("پذیرش", "📅", "تاریخ پذیرش", "reception_date"),
                new TemplateVariable("پذیرش", "⏰", "ساعت پذیرش", "reception_time"),
                new TemplateVariable("پذیرش", "🧾", "خدمت", "service_name"),
                new TemplateVariable("پذیرش", "🩺", "انجام‌دهنده/پزشک", "performer_name"),
                new TemplateVariable("پذیرش", "🚪", "اتاق", "room_name"),
                new TemplateVariable("پذیرش", "🛡️", "بیمه", "insurance_name"),
                new TemplateVariable("مالی", "🧾", "اقلام (جدول)", "items_html"),
                new TemplateVariable("مالی", "💰", "جمع اقلام", "items_total"),
                new TemplateVariable("مالی", "🛡️", "سهم بیمه", "insurance_share"),
                new TemplateVariable("مالی", "👛", "سهم بیمار", "patient_share"),
                new TemplateVariable("مالی", "✅", "پرداخت‌شده", "paid_amount"),
                new TemplateVariable("مالی", "💳", "روش پرداخت", "payment_method"),
                new TemplateVariable("مالی", "📄", "شماره فاکتور", "invoice_number"),
                new TemplateVariable("درمانگاه", "🏥", "نام درمانگاه", "clinic_name"),
                new TemplateVariable("درمانگاه", "📞", "تلفن درمانگاه", "clinic_phone"),
                new TemplateVariable("درمانگاه", "📍", "نشانی درمانگاه", "clinic_address"),
                new TemplateVariable("درمانگاه", "🖨️", "تاریخ چاپ", "print_date")
            },
            ["MedicalCertificate"] = new[]
            {
                new TemplateVariable("بیمار", "👤", "نام بیمار", "patient_name"),
                new TemplateVariable("بیمار", "🪪", "کد ملی", "patient_national_code"),
                new TemplateVariable("گواهی", "🩺", "پزشک", "doctor_name"),
                new TemplateVariable("گواهی", "📅", "تاریخ صدور", "issue_date"),
                new TemplateVariable("گواهی", "📝", "متن گواهی", "content"),
                new TemplateVariable("گواهی", "⏳", "مدت استراحت", "duration")
            },
            ["PayrollSlip"] = new[]
            {
                new TemplateVariable("پرسنل", "💼", "نام پرسنل", "employee_name"),
                new TemplateVariable("پرسنل", "📅", "دورهٔ پرداخت", "period"),
                new TemplateVariable("پرسنل", "🧭", "سمت", "position_name"),
                new TemplateVariable("مالی", "💰", "حقوق پایه", "base_salary"),
                new TemplateVariable("مالی", "⏰", "اضافه‌کار", "overtime_pay"),
                new TemplateVariable("مالی", "🛡️", "بیمه", "insurance"),
                new TemplateVariable("مالی", "🧮", "مالیات", "tax"),
                new TemplateVariable("مالی", "✅", "خالص پرداختی", "net_pay")
            }
        };
        ActiveVariables = new ObservableCollection<TemplateVariable>(
            map.TryGetValue(key, out var vars) ? vars : Array.Empty<TemplateVariable>());
    }

    /// <summary>
    /// قالب‌های آمادهٔ هر سند — چند طرح آماده برای هر نوع تا کاربر از بین
    /// آن‌ها انتخاب کند (بازطراحی تأییدشده)؛ با یک کلیک به‌صورت قالب تازه
    /// (ذخیره‌نشده) ساخته می‌شود تا کاربر نام بدهد و ذخیره/پیش‌فرض کند
    /// </summary>
    public static IReadOnlyList<PresetTemplate> PresetTemplates { get; } = new[]
    {
        new PresetTemplate("ReceptionReceipt", "حرارتی شیک (برند)", "#16355E", "Thermal80",
            "<div style='text-align:center;font-weight:800;font-size:16px'>{{clinic_name}}</div><div style='text-align:center'>قبض پذیرش و پرداخت</div><div style='text-align:center;font-size:20px;font-weight:800;border:2px solid #16355E;border-radius:8px;padding:6px;margin:8px 0'>شماره صف: {{queue_number}}</div><div>بیمار: {{patient_name}} — {{reception_date}} {{reception_time}}</div><div>خدمت/پزشک: {{service_name}} {{performer_name}}</div>{{items_html}}<div>جمع: {{items_total}} | سهم بیمه: {{insurance_share}} | سهم بیمار: {{patient_share}}</div><div>پرداخت‌شده: {{paid_amount}} ({{payment_method}})</div>"),
        new PresetTemplate("ReceptionReceipt", "حرارتی فشرده", "#0B7A63", "Thermal80",
            "<div style='text-align:center;font-weight:800'>{{clinic_name}}</div><div>صف {{queue_number}} — {{patient_name}}</div>{{items_html}}<div>قابل پرداخت: {{patient_share}} — پرداخت‌شده: {{paid_amount}}</div>"),
        new PresetTemplate("ReceptionReceipt", "A5 اداری", "#6A1B9A", "A5",
            "<div style='text-align:center;font-weight:800;font-size:17px'>{{clinic_name}}</div><div style='text-align:center'>{{clinic_address}} — {{clinic_phone}}</div><hr/><div>بیمار: {{patient_name}} | کد ملی: {{patient_national_code}} | تاریخ: {{reception_date}}</div><div>بیمه: {{insurance_name}} | فاکتور: {{invoice_number}}</div>{{items_html}}<div>جمع: {{items_total}} | سهم بیمه: {{insurance_share}} | سهم بیمار: {{patient_share}}</div>"),
        new PresetTemplate("Invoice", "فاکتور رسمی A4", "#16355E", "A4",
            "<div style='text-align:center;font-weight:800;font-size:17px'>{{clinic_name}}</div><div>فاکتور {{invoice_code}} — {{invoice_date}}</div><div>بیمار: {{patient_name}} | بیمه: {{insurance_name}}</div>{{items_html}}<div>جمع جزء: {{subtotal}} | تخفیف: {{discount}} | سهم بیمه: {{insurance_share}} | قابل پرداخت: {{total}}</div>"),
        new PresetTemplate("Invoice", "فاکتور A5 خلاصه", "#0B7A63", "A5",
            "<div style='font-weight:800'>{{clinic_name}} — فاکتور {{invoice_code}}</div><div>{{patient_name}} — {{invoice_date}}</div>{{items_html}}<div>قابل پرداخت: {{total}}</div>"),
        new PresetTemplate("Invoice", "فاکتور بیمه‌ای", "#E65100", "A4",
            "<div style='text-align:center;font-weight:800'>{{clinic_name}}</div><div>فاکتور بیمه‌ای {{invoice_code}} — بیمه: {{insurance_name}}</div><div>بیمار: {{patient_name}}</div>{{items_html}}<div>سهم سازمان: {{insurance_share}} | سهم بیمار: {{patient_share}} | جمع: {{total}}</div>"),
        new PresetTemplate("Prescription", "نسخهٔ کلاسیک پزشک", "#16355E", "A5",
            "<div style='text-align:center;font-weight:800'>{{doctor_name}}</div><div>بیمار: {{patient_name}} — سن: {{patient_age}} — {{visit_date}}</div><div style='font-size:22px;font-weight:800'>℞</div>{{items_html}}<div>{{notes}}</div>"),
        new PresetTemplate("Prescription", "نسخهٔ مدرن رنگی", "#0B7A63", "A5",
            "<div style='background:#0B7A63;color:#fff;padding:8px;font-weight:800'>نسخه — {{doctor_name}}</div><div>{{patient_name}} | {{visit_date}} | کد نسخه: {{prescription_code}}</div>{{items_html}}<div>{{notes}}</div>"),
        new PresetTemplate("LabResult", "گزارش استاندارد آزمایشگاه", "#00695C", "A4",
            "<div style='text-align:center;font-weight:800'>{{clinic_name}} — آزمایشگاه</div><div>بیمار: {{patient_name}} | درخواست: {{order_number}} | تاریخ جواب: {{result_date}}</div>{{results_html}}<div>تأییدکننده: {{verified_by}}</div>"),
        new PresetTemplate("LabResult", "گزارش فشرده", "#455A64", "A5",
            "<div style='font-weight:800'>نتیجهٔ آزمایش — {{patient_name}}</div>{{results_html}}"),
        new PresetTemplate("ImagingOrder", "گزارش رسمی رادیولوژی", "#37474F", "A4",
            "<div style='text-align:center;font-weight:800'>{{clinic_name}} — تصویربرداری</div><div>بیمار: {{patient_name}} | {{modality}} | {{order_date}}</div><div><b>یافته‌ها:</b> {{findings}}</div><div><b>نتیجه‌گیری:</b> {{impression}}</div>"),
        new PresetTemplate("AppointmentTicket", "قبض نوبت حرارتی", "#0B7A63", "Thermal80",
            "<div style='text-align:center;font-weight:800'>{{clinic_name}}</div><div style='text-align:center;font-size:20px;font-weight:800'>صف {{queue_number}}</div><div>{{patient_name}} — {{doctor_name}}</div><div>{{appointment_date}} ساعت {{appointment_time}} — کد پیگیری: {{tracking_code}}</div>"),
        new PresetTemplate("PayrollSlip", "فیش استاندارد", "#16355E", "A5",
            "<div style='text-align:center;font-weight:800'>فیش حقوقی {{period}}</div><div>{{employee_name}} — {{position_name}}</div><div>حقوق پایه: {{base_salary}} | اضافه‌کار: {{overtime_pay}} | بیمه: {{insurance}} | مالیات: {{tax}}</div><div style='font-weight:800'>خالص پرداختی: {{net_pay}}</div>"),
        new PresetTemplate("MedicalCertificate", "گواهی استراحت رسمی", "#37474F", "A5",
            "<div style='text-align:center;font-weight:800'>گواهی پزشکی</div><div>گواهی می‌شود آقای/خانم {{patient_name}} در تاریخ {{issue_date}} معاینه شد. {{content}} مدت استراحت: {{duration}}</div><div>پزشک: {{doctor_name}}</div>")
    };

    /// <summary>قالب‌های آمادهٔ نوع فیلتر فعلی — برای گالری «طرح‌های آماده»</summary>
    public IEnumerable<PresetTemplate> VisiblePresets =>
        PresetTemplates.Where(p => TemplateFilterIndex <= 0
            || p.TypeKey == TemplateTypes[Math.Max(0, TemplateFilterIndex - 1)].Key);

    /// <summary>ساخت قالب تازه از روی یک طرح آماده</summary>
    [RelayCommand]
    private void AddPresetTemplate(PresetTemplate? preset)
    {
        if (preset is null) return;
        var fa = TemplateTypes.FirstOrDefault(t => t.Key == preset.TypeKey)?.Fa ?? preset.TypeKey;
        var row = new PrintTemplateRow
        {
            Id = 0,
            Key = preset.TypeKey,
            Title = preset.Title,
            TypeFa = fa,
            Icon = PrintTemplateRow.IconOf(preset.TypeKey),
            Body = preset.Body,
            PaperSize = preset.Paper,
            AccentColor = preset.Accent,
            IsActive = true,
            IsDirty = true
        };
        row.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PrintTemplateRow.IsDirty))
                {
                    row.IsDirty = true;
                    if (ReferenceEquals(SelectedTemplate, row)) PreviewHtml = null;
                }
        };
        PrintTemplates.Add(row);
        var filterIdx = Array.FindIndex(TemplateFilterOptions, o => o.Key == row.Key);
        if (filterIdx >= 0 && TemplateFilterIndex != filterIdx) TemplateFilterIndex = filterIdx;
        ApplyTemplateFilter(row);
        _snackbar.Info($"قالب آمادهٔ «{preset.Title}» ساخته شد — نام بدهید و ذخیره کنید");
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
            if (e.PropertyName != nameof(PrintTemplateRow.IsDirty))
                {
                    row.IsDirty = true;
                    if (ReferenceEquals(SelectedTemplate, row)) PreviewHtml = null;
                }
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
        if (!IsTemplateReady) { _snackbar.Error("ابتدا منتظر پایان عملیات قالب بمانید"); return; }
        if (SelectedTemplate.Title.Trim().Length < 3) { _snackbar.Error("نام قالب حداقل ۳ نویسه باشد"); return; }
        if (SelectedTemplate.Body.Trim().Length < 10) { _snackbar.Error("بدنه قالب خالی است"); return; }

        var template = SelectedTemplate;
        IsBusy = true;
        var payload = new
        {
            templateName = template.Title,
            type = template.Key,
            description = template.Description,
            htmlBody = template.Body,
            paperSize = template.PaperSize,
            orientation = "Portrait",
            fontSize = template.FontSize,
            fontFamily = (string?)null,
            showClinicLogo = template.ShowLogo,
            accentColor = template.AccentColor,
            isActive = template.IsActive,
            isDefault = template.IsDefault
        };
        bool ok;
        IsTemplateSaving = true;
        try
        {
            if (template.Id == 0)
            {
                var created = await _api.PostAsync<TemplateDetailEnvelope>("print-templates", payload);
                ok = created?.IsSuccess == true && created.Data is { Id: > 0 };
                if (ok) template.Id = created!.Data!.Id;
            }
            else ok = await _api.PutAsync($"print-templates/{template.Id}", payload);
        }
        finally { IsTemplateSaving = false; IsBusy = false; }

        if (ok)
        {
            // کاربر می‌تواند هنگام درخواست ویرایش یا انتخاب را عوض کند؛
            // فقط همان عکس ارسالی ذخیره شده و تغییرات بعدی نباید حذف شوند.
            template.IsLoaded = true;
            var unchanged = template.Title == payload.templateName && template.Key == payload.type
                && template.Description == payload.description && template.Body == payload.htmlBody
                && template.PaperSize == payload.paperSize && template.FontSize == payload.fontSize
                && template.ShowLogo == payload.showClinicLogo && template.AccentColor == payload.accentColor
                && template.IsActive == payload.isActive && template.IsDefault == payload.isDefault;
            if (!unchanged)
            {
                template.IsDirty = true;
                _snackbar.Warning("نسخهٔ قبلی ذخیره شد؛ تغییرات جدید هنوز ذخیره نشده‌اند.");
                return;
            }
            _snackbar.Success($"قالب «{template.Title}» ذخیره شد");
            template.IsDirty = false;
            await LoadTemplatesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ذخیره قالب ناموفق بود");
    }

    /// <summary>انصراف تأییدشده از تغییرات قالب انتخابی، بدون تغییر ذخیرهٔ سرور.</summary>
    [RelayCommand]
    private async Task DiscardTemplateChangesAsync()
    {
        var template = SelectedTemplate;
        if (!IsTemplateReady) { _snackbar.Error("ابتدا منتظر پایان عملیات قالب بمانید"); return; }
        if (template is null || !template.IsDirty) { _snackbar.Info("تغییری برای انصراف وجود ندارد"); return; }
        if (Views.AppDialog.Confirm("انصراف از تغییرات قالب",
                "تغییرات ذخیره‌نشدهٔ این قالب حذف شوند؟", Views.AppDialogIcon.Warning) != true) return;
        if (template.Id == 0)
        {
            PrintTemplates.Remove(template);
            ApplyTemplateFilter();
            return;
        }
        IsTemplateLoading = true;
        var response = await _api.GetAsync<TemplateDetailEnvelope>($"print-templates/{template.Id}");
        if (!ReferenceEquals(SelectedTemplate, template)) return;
        IsTemplateLoading = false;
        if (response?.Data is not { } detail)
        {
            _snackbar.Error(_api.LastError ?? "دریافت نسخه ذخیره‌شده ناموفق بود؛ تغییرات حفظ شدند");
            return;
        }
        ApplyTemplateDetails(template, detail);
        _snackbar.Info("نسخه ذخیره‌شده قالب بازگردانده شد");
    }

    /// <summary>کپی قالب انتخابی به‌عنوان نسخه جدید</summary>
    [RelayCommand]
    private async Task DuplicateTemplateAsync()
    {
        if (SelectedTemplate is null || SelectedTemplate.Id == 0) { _snackbar.Info("ابتدا یک قالب ذخیره‌شده انتخاب کنید"); return; }
        if (PrintTemplates.Any(t => t.IsDirty)) { _snackbar.Warning("ابتدا تغییرات همه قالب‌ها را ذخیره کنید."); return; }
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
        if (PrintTemplates.Any(t => t.IsDirty)) { _snackbar.Warning("ابتدا تغییرات همه قالب‌ها را ذخیره کنید."); return; }
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
        if (PrintTemplates.Any(t => t.IsDirty)) { _snackbar.Warning("ابتدا تغییرات همه قالب‌ها را ذخیره کنید."); return; }
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
        if (IsTemplateLoading) { _snackbar.Error("ابتدا منتظر بارگذاری جزئیات قالب بمانید"); return; }
        var template = SelectedTemplate;
        PreviewHtml = null;
        if (template.IsDirty || template.Id == 0)
        {
            _snackbar.Info("برای پیش‌نمایش ابتدا تغییرات قالب را ذخیره کنید");
            return;
        }
        if (template.IsOriginalReceipt)
        {
            OriginalReceiptPreviewRequested?.Invoke();
            return;
        }
        IsBusy = true;
        var response = await _api.GetAsync<PreviewEnvelope>($"print-templates/{template.Id}/preview");
        IsBusy = false;
        if (!ReferenceEquals(SelectedTemplate, template) || template.IsDirty) return;
        if (response?.Data?.Html is { Length: > 0 } html)
        {
            PreviewHtml = html;
            _snackbar.Success("پیش‌نمایش آماده شد");
        }
        else _snackbar.Error(_api.LastError ?? "پیش‌نمایش ناموفق بود");
    }

    /// <summary>درج متغیر در محل نشانگر — با پیام ساده (درج در انتها)</summary>
    [RelayCommand]
    private void InsertVariable(TemplateVariable? variable)
    {
        if (SelectedTemplate is null || variable is null || SelectedTemplate.IsOriginalReceipt || IsTemplateLoading) return;
        SelectedTemplate.Body += $"{{{{{variable.Key}}}}}";
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
            if (response?.Data is null)
            { _snackbar.Error(_api.LastError ?? "دریافت فهرست ناموفق بود؛ فهرست قبلی ممکن است قدیمی باشد."); return; }
            Surveys = new ObservableCollection<SurveyRow>(
                response.Data.Select(s => new SurveyRow
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IsActive = s.IsActive,
                    ResponseCount = s.ResponseCount,
                    AverageRating = s.AverageRating,
                    QuestionsJson = s.Questions,
                    EditVersion = s.EditVersion
                }));
        }
        catch (Exception)
        { _snackbar.Error("دریافت فهرست ناموفق بود؛ فهرست قبلی ممکن است قدیمی باشد."); }
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
        if (row is null || IsBusy) return;
        if (string.IsNullOrWhiteSpace(row.EditVersion))
        { _snackbar.Error("نسخهٔ نظرسنجی موجود نیست؛ سرور و برنامه را به‌روز و فهرست را تازه‌سازی کنید."); return; }
        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync($"surveys/{row.Id}/toggle-active", new { editVersion = row.EditVersion, desiredActive = !row.IsActive });
            if (ok) { _snackbar.Success("وضعیت نظرسنجی تغییر کرد"); await LoadSurveysAsync(); }
            else _snackbar.Error((_api.LastError ?? "عملیات ناموفق بود") + "؛ پیش از تکرار وضعیت را بررسی کنید.");
        }
        catch (Exception) { _snackbar.Error("پاسخ تغییر وضعیت دریافت نشد؛ پیش از تکرار فهرست را تازه‌سازی کنید."); }
        finally { IsBusy = false; }
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

        if (string.IsNullOrWhiteSpace(row.EditVersion))
        { _snackbar.Error("نسخهٔ نظرسنجی در فهرست موجود نیست؛ سرور و برنامه را به‌روز و فهرست را تازه‌سازی کنید."); return; }
        if (!SurveyQuestionItem.TryParseList(row.QuestionsJson, out var existing))
        { _snackbar.Error("ساختار سؤال‌ها پشتیبانی نمی‌شود؛ ویرایش برای حفاظت داده باز نشد."); return; }
        _designingSurveyId = row.Id;
        _designingSurveyVersion = row.EditVersion;
        _originalDesignerQuestions = row.QuestionsJson;
        DesignerTitle = row.Title;
        DesignerDescription = row.Description;
        DesignerQuestions.Clear();
        foreach (var q in existing) DesignerQuestions.Add(q);
        _initialDesignerRepresentation = DesignerRepresentation();
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
        if (DesignerQuestions.Count > 100 || DesignerQuestions.Any(q => string.IsNullOrWhiteSpace(q.Text) || q.Text.Length > 2000))
        { _snackbar.Error("متن سؤال‌ها را وارد کنید؛ حداکثر ۱۰۰ سؤال و ۲۰۰۰ نویسه برای هر سؤال مجاز است."); return; }
        if (DesignerQuestions.Any(q => q.TypeKey == "choice" && q.Choices.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is var choices &&
            (choices.Length < 2 || choices.Length > 100 || choices.Any(c => c.Length > 2000))))
        { _snackbar.Error("برای سؤال چندگزینه‌ای بین ۲ تا ۱۰۰ گزینه معتبر وارد کنید."); return; }
        var representation = DesignerRepresentation();
        var questionsJson = representation == _initialDesignerRepresentation ? _originalDesignerQuestions : representation;
        if (questionsJson?.Length > 200_000)
        { _snackbar.Error("حجم سؤال‌ها بیش از حد مجاز است."); return; }
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var ok = await _api.PutAsync($"surveys/{_designingSurveyId}", new
            {
                title = DesignerTitle.Trim(),
                description = DesignerDescription,
                questions = questionsJson,
                isActive = (bool?)null,
                editVersion = _designingSurveyVersion
            });
            if (ok)
            {
                _snackbar.Success("نظرسنجی با سوالات جدید به‌روزرسانی شد");
                IsSurveyDesignerOpen = false;
                await LoadSurveysAsync();
            }
            else _snackbar.Error((_api.LastError ?? "ذخیرهٔ نظرسنجی ناموفق بود") + "؛ پیش از تکرار فهرست را بررسی کنید.");
        }
        catch (Exception)
        { _snackbar.Error("پاسخ ذخیره دریافت نشد؛ پیش از تلاش مجدد فهرست را بررسی کنید."); }
        finally { IsBusy = false; }
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
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(row.EditVersion))
        { _snackbar.Error("نسخهٔ نظرسنجی موجود نیست؛ سرور و برنامه را به‌روز و فهرست را تازه‌سازی کنید."); return; }

        var confirmed = Views.AppDialog.Confirm(
            "حذف نظرسنجی",
            $"نظرسنجی «{row.Title}» حذف شود؟\nپاسخ‌ها در دیتابیس باقی می‌مانند، اما این نظرسنجی و نتایج آن از برنامه خارج می‌شوند و بازیابی از این صفحه ممکن نیست.",
            Views.AppDialogIcon.Warning);
        if (confirmed != true) return;

        IsBusy = true;
        var ok = await _api.DeleteAsync($"surveys/{row.Id}?editVersion={Uri.EscapeDataString(row.EditVersion)}");
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
        public string? EditVersion { get; set; }
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
