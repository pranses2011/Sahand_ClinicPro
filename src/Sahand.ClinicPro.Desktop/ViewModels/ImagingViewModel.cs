using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>قالب آمادهٔ گزارش تصویربرداری (پیش‌نمایش نسخهٔ ۲ تأییدشده)</summary>
public sealed class ImagingTemplate
{
    /// <summary>نام قالب</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>تکنیک انجام</summary>
    public string Technique { get; init; } = string.Empty;
    /// <summary>یافته‌های استاندارد</summary>
    public string Findings { get; init; } = string.Empty;
    /// <summary>نتیجه‌گیری</summary>
    public string Impression { get; init; } = string.Empty;
    /// <summary>توصیه و پیگیری</summary>
    public string Recommendation { get; init; } = string.Empty;
    /// <summary>قالب یافتهٔ بحرانی است؟</summary>
    public bool IsCritical { get; init; }
}

/// <summary>ردیف پروتکل استاندارد بخش (پارامترها و سطح مرجع دوز)</summary>
public sealed class ImagingProtocolRow
{
    /// <summary>نام پروتکل</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>پارامترهای انجام</summary>
    public string Params { get; init; } = string.Empty;
    /// <summary>سطح مرجع تشخیصی دوز (DRL)</summary>
    public string Drl { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel صفحهٔ مستقل «تصویربرداری» — ماژول حرفه‌ای (طرح تأییدشدهٔ مالک،
/// نسخهٔ ۲ پیش‌نمایش): صف کار روزانه با کارت‌های آماریِ فیلتر، جست‌وجو و
/// فیلتر مدالیته و گردش وضعیت؛ نمایشگر PACS با پنجره‌های آمادهٔ مغز/استخوان/
/// ریه/بافت نرم، تنظیم W/L و زوم، پخش برش‌ها (Cine)، اندازه‌گیری روی تصویر و
/// اطلاعات DICOM؛ گزارش‌نویسی ساخت‌یافته با قالب‌های آماده، پایش دوز نسبت
/// به DRL، یافتهٔ بحرانی با ثبت اطلاع‌رسانی و امضا/الحاقیه؛ چک‌لیست ایمنی
/// اختصاصی هر مدالیته و پروتکل‌های استاندارد؛ داشبورد کیفیت (زمان پاسخ،
/// دوز، دفتر اطلاع‌های بحرانی و بار کاری). جزئیات ساخت‌یافتهٔ گزارش در
/// ReportDetails (JSON) سرور ذخیره می‌شود.
/// </summary>
public partial class ImagingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "تصویربرداری";

    /// <summary>رویداد تغییر وضعیت نمایشگر (نما تصویر را دوباره رندر می‌کند)</summary>
    public event Action? ViewerStateChanged;
    /// <summary>رویداد رسیدن تصویر تازه برای نمایشگر</summary>
    public event Action? ImageSourceChanged;

    // ─────────────── ثبت درخواست تازه ───────────────
    /// <summary>انواع مودالیتهٔ تصویربرداری</summary>
    public ObservableCollection<string> ImagingTypes { get; } =
        new() { "سونوگرافی", "رادیوگرافی", "MRI", "CT Scan", "ماموگرافی" };
    /// <summary>نمایهٔ نوع تصویربرداری</summary>
    [ObservableProperty] private int _imagingTypeIndex;
    /// <summary>ناحیهٔ بدن (متن آزاد)</summary>
    [ObservableProperty] private string? _imagingRegion;
    /// <summary>اطلاعات بالینی</summary>
    [ObservableProperty] private string? _imagingClinicalInfo;
    /// <summary>درخواست فوری است؟</summary>
    [ObservableProperty] private bool _imagingUrgent;
    /// <summary>عبارت جستجوی بیمار تصویربرداری</summary>
    [ObservableProperty] private string _imagingPatientSearch = string.Empty;
    /// <summary>بیمار منتخب</summary>
    [ObservableProperty] private QuickPatient? _imagingPatient;
    [ObservableProperty] private bool _isSavingImagingOrder;
    [ObservableProperty] private bool _isSearchingImagingPatient;
    /// <summary>فرم هنگام جستجوی بیمار یا ثبت درخواست قفل می‌شود.</summary>
    public bool CanEditImagingOrder => !IsSavingImagingOrder && !IsSearchingImagingPatient;
    partial void OnIsSavingImagingOrderChanged(bool value) => OnPropertyChanged(nameof(CanEditImagingOrder));
    partial void OnIsSearchingImagingPatientChanged(bool value) => OnPropertyChanged(nameof(CanEditImagingOrder));

    /// <summary>ناحیه‌های بدن — گزینه‌های پیشنهادی</summary>
    public string[] BodyRegions { get; } =
        { "سر و گردن", "قفسه سینه", "شکم و لگن", "ستون فقرات", "اندام فوقانی", "اندام تحتانی", "مفصل شانه", "مفصل زانو", "تروماتیک" };

    // ─────────────── صف کار ───────────────
    /// <summary>همهٔ درخواست‌های امروز</summary>
    [ObservableProperty] private ObservableCollection<ImagingOrderRow> _imagingOrders = new();
    /// <summary>درخواست‌های پس از فیلتر/جست‌وجو — صف نمایشی</summary>
    public ObservableCollection<ImagingOrderRow> FilteredOrders { get; } = new();
    private int _queueRequestVersion;
    private int _fileRequestVersion;
    private int _imageRequestVersion;
    private bool _queueLoaded;
    [ObservableProperty] private string _queueStatus = "صف هنوز دریافت نشده است.";
    partial void OnQueueStatusChanged(string value) => OnPropertyChanged(nameof(QueueEmptyMessage));
    /// <summary>پیام دقیق حالت خالی یا خطای دریافت صف.</summary>
    public string QueueEmptyMessage => !_queueLoaded || !string.IsNullOrEmpty(QueueStatus)
        ? QueueStatus : ImagingOrders.Count == 0 ? "صف امروز خالی است." : "درخواستی با فیلترهای انتخاب‌شده پیدا نشد.";

    /// <summary>فیلتر آماری فعال: all/wait/rep/stat/done</summary>
    [ObservableProperty] private string _workFilter = "all";
    /// <summary>فیلتر مدالیته (نام فارسی یا all)</summary>
    [ObservableProperty] private string _modalityFilter = "all";
    /// <summary>عبارت جست‌وجوی صف کار</summary>
    [ObservableProperty] private string _workSearch = string.Empty;

    /// <summary>تعداد کل امروز</summary>
    public int TotalToday => ImagingOrders.Count;
    /// <summary>در انتظار انجام</summary>
    public int WaitingCount => ImagingOrders.Count(o => o.Status is 1 or 2);
    /// <summary>انجام‌شده ولی گزارش‌نشده</summary>
    public int UnreportedCount => ImagingOrders.Count(o => o.Status == 3);
    /// <summary>اورژانسی‌های باز (STAT)</summary>
    public int StatCount => ImagingOrders.Count(o => o.Priority == 1 && o.Status != 4 && o.Status != 5);
    /// <summary>گزارش نهایی امروز</summary>
    public int ReportedCount => ImagingOrders.Count(o => o.Status == 4);

    partial void OnWorkFilterChanged(string value) => ApplyWorkFilter();
    partial void OnModalityFilterChanged(string value) => ApplyWorkFilter();
    partial void OnWorkSearchChanged(string value) => ApplyWorkFilter();

    /// <summary>انتخاب فیلتر آماری از کارت‌های بالای صف</summary>
    [RelayCommand]
    private void SetWorkFilter(string? filter) { if (!string.IsNullOrEmpty(filter)) WorkFilter = filter; }
    /// <summary>انتخاب فیلتر مدالیته</summary>
    [RelayCommand]
    private void SetModalityFilter(string? modality) { if (!string.IsNullOrEmpty(modality)) ModalityFilter = modality; }

    private void ApplyWorkFilter()
    {
        FilteredOrders.Clear();
        foreach (var o in ImagingOrders)
        {
            var ok = WorkFilter switch
            {
                "wait" => o.Status is 1 or 2,
                "rep" => o.Status == 3,
                "stat" => o.Priority == 1 && o.Status != 4 && o.Status != 5,
                "done" => o.Status == 4,
                _ => true
            };
            if (ok && ModalityFilter != "all" && o.ModalityFa != ModalityFilter) ok = false;
            if (ok && !string.IsNullOrWhiteSpace(WorkSearch)
                && !(o.PatientName + o.BodyPart + o.ModalityFa).Contains(WorkSearch.Trim())) ok = false;
            if (ok) FilteredOrders.Add(o);
        }
        OnPropertyChanged(nameof(QueueEmptyMessage));
    }

    private void RaiseStats()
    {
        OnPropertyChanged(nameof(TotalToday));
        OnPropertyChanged(nameof(WaitingCount));
        OnPropertyChanged(nameof(UnreportedCount));
        OnPropertyChanged(nameof(StatCount));
        OnPropertyChanged(nameof(ReportedCount));
        RefreshKpi();
    }

    // ─────────────── درخواست انتخاب‌شده و گزارش ───────────────
    /// <summary>درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedImagingOrder))]
    private ImagingOrderRow? _selectedImagingOrder;
    /// <summary>آیا درخواستی انتخاب شده</summary>
    public bool HasSelectedImagingOrder => SelectedImagingOrder is not null;

    [ObservableProperty] private bool _hasUnsupportedReportDetails;
    [ObservableProperty] private bool _isSavingImagingReport;
    /// <summary>زمان ثبت گزارش، ویرایش و انتخاب از صف قفل می‌شود.</summary>
    public bool CanEditImagingReport => !IsSavingImagingReport;
    partial void OnIsSavingImagingReportChanged(bool value) => OnPropertyChanged(nameof(CanEditImagingReport));

    /// <summary>شرح حال بالینی مطالعهٔ انتخابی</summary>
    [ObservableProperty] private string? _reportClinicalInfo;
    /// <summary>تکنیک انجام</summary>
    [ObservableProperty] private string? _reportTechnique;
    /// <summary>یافته‌ها</summary>
    [ObservableProperty] private string? _reportFindings;
    /// <summary>نتیجه‌گیری</summary>
    [ObservableProperty] private string? _reportImpression;
    /// <summary>توصیه و پیگیری</summary>
    [ObservableProperty] private string? _reportRecommendation;
    /// <summary>گزینه‌های مقایسه با قبلی</summary>
    public string[] ComparisonOptions { get; } =
        { "بدون مقایسه", "انجام شد — مطالعهٔ قبلی موجود است", "مطالعهٔ قبلی موجود نیست" };
    /// <summary>مقایسهٔ انتخاب‌شده</summary>
    [ObservableProperty] private string _reportComparison = "بدون مقایسه";
    /// <summary>گزینه‌های وضعیت گزارش</summary>
    public string[] ReportStatusOptions { get; } =
        { "پیش‌نویس", "اولیه (Preliminary)", "نهایی و امضاشده", "الحاقیه (Addendum)" };
    /// <summary>وضعیت گزارش</summary>
    [ObservableProperty] private string _reportStatus = "نهایی و امضاشده";
    /// <summary>دوز CTDIvol (mGy)</summary>
    [ObservableProperty] private string? _doseCtdi;
    /// <summary>دوز DLP (mGy·cm)</summary>
    [ObservableProperty] private string? _doseDlp;
    /// <summary>مرجع بالینی تنظیم‌شده در این نسخه موجود نیست.</summary>
    public double CurrentDrl => 0;
    /// <summary>هشدار خودکار تا تعریف مرجع معتبر برای پروتکل و بیمار غیرفعال است.</summary>
    public bool DoseHigh => false;
    partial void OnDoseCtdiChanged(string? value) => OnPropertyChanged(nameof(DoseHigh));
    /// <summary>یافتهٔ بحرانی فعال است؟</summary>
    [ObservableProperty] private bool _criticalFinding;
    /// <summary>گیرندهٔ اطلاع بحرانی</summary>
    [ObservableProperty] private string? _criticalNotifiedTo;
    /// <summary>روش‌های اطلاع بحرانی</summary>
    public string[] CriticalMethodOptions { get; } = { "تماس تلفنی", "حضوری", "پیامک سیستمی" };
    /// <summary>روش اطلاع بحرانی</summary>
    [ObservableProperty] private string _criticalMethod = "تماس تلفنی";
    /// <summary>پیام وضعیت فرم گزارش</summary>
    [ObservableProperty] private string? _reportMessage;
    /// <summary>نشان ذخیرهٔ خودکار پیش‌نویس</summary>
    [ObservableProperty] private string _draftStamp = "پیش‌نویس خودکار فعال است";

    /// <summary>قالب‌های آمادهٔ گزارش</summary>
    public ImagingTemplate[] Templates { get; } =
    {
        new() { Name = "CT مغز نرمال", Technique = "CT مغز بدون تزریق، برش‌های ۵ میلی‌متری از قاعدهٔ جمجمه تا ورتکس",
            Findings = "ساختارهای مغزی، بطن‌ها و فضای ساب‌آراکنوئید طبیعی‌اند. خونریزی، توده یا انحراف خط وسط دیده نمی‌شود. سینوس‌ها و سلول‌های هوایی ماستوئید شفاف‌اند.",
            Impression = "طبیعی — بدون یافتهٔ حاد داخل جمجمه.", Recommendation = "پیگیری بالینی در صورت تداوم علائم." },
        new() { Name = "CT مغز — خونریزی", Technique = "CT مغز بدون تزریق، برش‌های ۵ میلی‌متری",
            Findings = "خونریزی حاد داخل مغزی همراه با ادم اطراف و اثر فشاری بر بطن مجاور دیده می‌شود. انحراف خط وسط اندازه‌گیری و قید شده است.",
            Impression = "خونریزی حاد داخل مغزی — یافتهٔ بحرانی.", Recommendation = "اطلاع فوری به پزشک درخواست‌کننده و مشاورهٔ جراحی مغز و اعصاب.", IsCritical = true },
        new() { Name = "رادیولوژی قفسه سینه نرمال", Technique = "گرافی PA قفسه سینه در دم کامل",
            Findings = "شفافیت هر دو ریه طبیعی است. کدورت، افیوژن پلورال یا پنوموتوراکس دیده نمی‌شود. سایهٔ قلب و مدیاستن طبیعی است.",
            Impression = "طبیعی — بدون یافتهٔ فعال ریوی.", Recommendation = "—" },
        new() { Name = "MRI زانو", Technique = "MRI زانو بدون تزریق؛ PD-FS و T1/T2 در سه پلان",
            Findings = "منیسک‌های داخلی و خارجی، رباط‌های صلیبی و جانبی و غضروف مفصلی بررسی شد. افیوژن مفصلی در صورت وجود قید شده است.",
            Impression = "نتیجه بر اساس یافته‌های فوق تفسیر شود.", Recommendation = "مطابقت با معاینهٔ ارتوپدی." },
        new() { Name = "سونوگرافی شکم نرمال", Technique = "سونوگرافی شکم با پروب کانوکس ۳.۵ مگاهرتز، بیمار ناشتا",
            Findings = "کبد، کیسه و مجاری صفراوی، پانکراس، طحال و کلیه‌ها از نظر اندازه و اکو طبیعی‌اند. مایع آزاد دیده نمی‌شود.",
            Impression = "سونوگرافی شکم طبیعی.", Recommendation = "—" },
        new() { Name = "ماموگرافی غربالگری", Technique = "ماموگرافی دوطرفه CC و MLO",
            Findings = "تراکم فیبروگلاندولار بدون توده، میکروکلسیفیکاسیون مشکوک یا به‌هم‌ریختگی معماری. BI-RADS 1.",
            Impression = "منفی — BI-RADS 1.", Recommendation = "غربالگری سالانهٔ بعدی." },
    };

    /// <summary>اعمال قالب آماده روی فرم گزارش</summary>
    [RelayCommand]
    private void ApplyTemplate(ImagingTemplate? template)
    {
        if (template is null) return;
        ReportTechnique = template.Technique;
        ReportFindings = template.Findings;
        ReportImpression = template.Impression;
        ReportRecommendation = template.Recommendation;
        CriticalFinding = template.IsCritical;
        DraftStamp = $"قالب «{template.Name}» بار شد — ویرایش آزاد است";
    }

    /// <summary>فعال‌سازی سریع یافتهٔ بحرانی از نمایشگر</summary>
    [RelayCommand]
    private void MarkCritical()
    {
        CriticalFinding = true;
        ReportMessage = "یافتهٔ بحرانی فعال شد؛ گیرنده و روش اطلاع را در فرم گزارش کامل کنید.";
    }

    // ─────────────── ایمنی و پروتکل ───────────────
    /// <summary>چک‌لیست ایمنی مطالعهٔ انتخابی (بر اساس مدالیته ساخته می‌شود)</summary>
    public ObservableCollection<PhysioCheckItem> SafetyItems { get; } = new();
    /// <summary>درصد آمادگی ایمنی</summary>
    public double SafetyProgress => SafetyItems.Count == 0 ? 0 : SafetyItems.Count(i => i.IsChecked) * 100.0 / SafetyItems.Count;
    /// <summary>همهٔ موارد ایمنی کامل است؟</summary>
    public bool SafetyComplete => SafetyItems.Count > 0 && SafetyItems.All(i => i.IsChecked);

    private static string[] SafetyFor(string? modality) => modality switch
    {
        "MRI" => new[] { "احراز هویت بیمار با دو شناسه", "غربالگری ضربان‌ساز و ایمپلنت فلزی", "غربالگری اجسام فلزی داخل چشم/بدن", "بررسی کلاستروفوبیا و نیاز به آرام‌بخشی", "حذف همهٔ اشیای فلزی و کارت‌های مغناطیسی" },
        "CT" => new[] { "احراز هویت بیمار با دو شناسه", "بررسی حساسیت به مادهٔ حاجب (در صورت تزریق)", "بررسی کراتینین/eGFR اخیر (در صورت تزریق)", "احتمال بارداری بررسی شد", "حذف اشیای فلزی از ناحیهٔ اسکن" },
        "Mammography" => new[] { "احراز هویت بیمار با دو شناسه", "احتمال بارداری بررسی شد", "عدم استفاده از دئودورانت/پودر", "بررسی پروتز یا جراحی قبلی پستان" },
        "Ultrasound" => new[] { "احراز هویت بیمار با دو شناسه", "ناشتایی ۶ ساعته (شکم) بررسی شد", "مثانهٔ پر برای بررسی لگن" },
        _ => new[] { "احراز هویت بیمار با دو شناسه", "احتمال بارداری بررسی شد", "حذف اشیای فلزی از ناحیه", "استفاده از محافظ سربی در صورت نیاز" },
    };

    private void BuildSafety()
    {
        foreach (var old in SafetyItems) old.PropertyChanged -= SafetyItemChanged;
        SafetyItems.Clear();
        foreach (var text in SafetyFor(SelectedImagingOrder?.Modality))
        {
            var item = new PhysioCheckItem { Text = text };
            item.PropertyChanged += SafetyItemChanged;
            SafetyItems.Add(item);
        }
        OnPropertyChanged(nameof(SafetyProgress));
        OnPropertyChanged(nameof(SafetyComplete));
    }

    private void SafetyItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SafetyProgress));
        OnPropertyChanged(nameof(SafetyComplete));
    }

    /// <summary>پروتکل‌های استاندارد بخش</summary>
    public ImagingProtocolRow[] Protocols { get; } =
    {
        new() { Name = "CT مغز روتین", Params = "120 kVp · 240 mAs · برش ۵mm", Drl = "CTDIvol ≤ 55 mGy" },
        new() { Name = "CT شکم با کنتراست", Params = "120 kVp · 180 mAs · برش ۳mm", Drl = "CTDIvol ≤ 25 mGy" },
        new() { Name = "MRI زانو", Params = "PD-FS ساژیتال/کرونال · T1/T2", Drl = "—" },
        new() { Name = "ماموگرافی غربالگری", Params = "CC+MLO دوطرفه · 28 kVp", Drl = "≤ 2.5 mGy/نما" },
        new() { Name = "رادیولوژی قفسه سینه", Params = "PA · 110 kVp · دم کامل", Drl = "≤ 0.4 mGy" },
    };

    // ─────────────── شاخص‌ها ───────────────
    /// <summary>میله‌های زمان پاسخ‌گویی به تفکیک مدالیته</summary>
    public ObservableCollection<PhysioBar> TatBars { get; } = new();
    /// <summary>میله‌های میانگین دوز نسبت به مرجع</summary>
    public ObservableCollection<PhysioBar> DoseBars { get; } = new();
    /// <summary>دفتر ثبت اطلاع‌رسانی یافته‌های بحرانی</summary>
    public ObservableCollection<string> CriticalLog { get; } = new();
    /// <summary>میانگین زمان انجام تا گزارش امروز (دقیقه)</summary>
    public string AvgTatText { get; private set; } = "—";
    /// <summary>درصد گزارش به‌موقع (نهایی‌شده از کل انجام‌شده‌ها)</summary>
    public string OnTimeText { get; private set; } = "—";

    private void RefreshKpi()
    {
        TatBars.Clear();
        DoseBars.Clear();
        CriticalLog.Clear();
        var reported = ImagingOrders.Where(o => o.Status == 4 && o.PerformedAt is not null && o.ReportedAt is not null).ToList();
        AvgTatText = reported.Count > 0
            ? $"{Math.Round(reported.Average(o => (o.ReportedAt!.Value - o.PerformedAt!.Value).TotalMinutes))} دقیقه"
            : "—";
        var performed = ImagingOrders.Count(o => o.Status is 3 or 4);
        OnTimeText = performed > 0 ? $"{Math.Round(ReportedCount * 100.0 / performed)}٪" : "—";
        OnPropertyChanged(nameof(AvgTatText));
        OnPropertyChanged(nameof(OnTimeText));

        foreach (var g in ImagingOrders.GroupBy(o => o.ModalityFa))
        {
            var reps = g.Where(o => o.Status == 4 && o.PerformedAt is not null && o.ReportedAt is not null).ToList();
            var avg = reps.Count > 0 ? reps.Average(o => (o.ReportedAt!.Value - o.PerformedAt!.Value).TotalMinutes) : 0;
            TatBars.Add(new() { Label = g.Key, BarPx = Math.Min(200, avg * 2.2), Text = reps.Count > 0 ? $"{Math.Round(avg)} دقیقه ({g.Count()} مطالعه)" : $"بدون گزارش نهایی ({g.Count()} مطالعه)" });
            var doses = g.Select(ParseDose).Where(d => d is not null).Select(d => d!.Value).ToList();
            if (doses.Count > 0)
                DoseBars.Add(new() { Label = g.Key, BarPx = Math.Min(200, doses.Average() * 3), Text = $"میانگین {Math.Round(doses.Average(), 1)} mGy" });
        }
        if (DoseBars.Count == 0)
            DoseBars.Add(new() { Label = "دوزی ثبت نشده است", BarPx = 0, Text = "—" });

        foreach (var o in ImagingOrders)
        {
            var crit = ParseCritical(o.ReportDetails);
            if (crit is not null)
                CriticalLog.Add($"{o.PatientName} — {o.ModalityFa} {o.BodyPart}: {crit}");
        }
        if (CriticalLog.Count == 0)
            CriticalLog.Add("امروز یافتهٔ بحرانی ثبت نشده است.");
    }

    private static double? ParseDose(ImagingOrderRow row)
    {
        if (string.IsNullOrWhiteSpace(row.ReportDetails)) return null;
        try
        {
            using var doc = JsonDocument.Parse(row.ReportDetails);
            if (doc.RootElement.TryGetProperty("ctdi", out var c) && c.TryGetDouble(out var v)) return v;
        }
        catch (JsonException) { }
        return null;
    }

    private static string? ParseCritical(string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return null;
        try
        {
            using var doc = JsonDocument.Parse(details);
            var root = doc.RootElement;
            if (root.TryGetProperty("critical", out var c) && c.ValueKind == JsonValueKind.True)
            {
                var to = root.TryGetProperty("criticalTo", out var t) ? t.GetString() : "";
                var method = root.TryGetProperty("criticalMethod", out var m) ? m.GetString() : "";
                return $"یافتهٔ بحرانی ← {to} · {method}";
            }
        }
        catch (JsonException) { }
        return null;
    }

    // ─────────────── نمایشگر PACS ───────────────
    /// <summary>پهنای پنجره (W)</summary>
    [ObservableProperty] private double _windowWidth = 80;
    /// <summary>سطح پنجره (L)</summary>
    [ObservableProperty] private double _windowLevel = 40;
    /// <summary>بزرگ‌نمایی (درصد)</summary>
    [ObservableProperty] private double _zoomPercent = 100;
    /// <summary>شمارهٔ برش نمایشی</summary>
    [ObservableProperty] private int _sliceIndex = 14;
    /// <summary>تعداد کل برش‌ها</summary>
    public int SliceTotal => 32;
    /// <summary>معکوس (Invert)</summary>
    [ObservableProperty] private bool _isInverted;
    /// <summary>زاویهٔ چرخش</summary>
    [ObservableProperty] private double _rotation;
    /// <summary>برگردان افقی</summary>
    [ObservableProperty] private bool _isFlipped;
    /// <summary>حالت اندازه‌گیری فعال است؟</summary>
    [ObservableProperty] private bool _measureMode;
    /// <summary>پیام/وضعیت نمایشگر</summary>
    [ObservableProperty] private string _viewerMessage = "مطالعه‌ای از صف کار انتخاب کنید؛ فایل‌های تصویر آن اینجا نمایش داده می‌شود.";
    /// <summary>بایت‌های تصویر جاری (تصاویر معمولی)</summary>
    public byte[]? CurrentImageBytes { get; private set; }
    /// <summary>نام تصویر جاری</summary>
    public string? CurrentImageName { get; private set; }
    /// <summary>اندازه‌گیری‌های ثبت‌شده روی تصویر</summary>
    public ObservableCollection<string> Measurements { get; } = new();

    partial void OnWindowWidthChanged(double value) => ViewerStateChanged?.Invoke();
    partial void OnWindowLevelChanged(double value) => ViewerStateChanged?.Invoke();
    partial void OnZoomPercentChanged(double value) => ViewerStateChanged?.Invoke();
    partial void OnSliceIndexChanged(int value) => ViewerStateChanged?.Invoke();
    partial void OnIsInvertedChanged(bool value) => ViewerStateChanged?.Invoke();
    partial void OnRotationChanged(double value) => ViewerStateChanged?.Invoke();
    partial void OnIsFlippedChanged(bool value) => ViewerStateChanged?.Invoke();

    /// <summary>اعمال پنجرهٔ آماده (مغز/استخوان/ریه/بافت نرم/خونریزی)</summary>
    [RelayCommand]
    private void ApplyWindowPreset(string? preset)
    {
        (WindowWidth, WindowLevel) = preset switch
        {
            "bone" => (400d, 40d),
            "lung" => (400d, -40d),
            "soft" => (350d, 40d),
            "blood" => (90d, 35d),
            _ => (80d, 40d),
        };
    }

    /// <summary>معکوس‌کردن تصویر</summary>
    [RelayCommand] private void ToggleInvert() => IsInverted = !IsInverted;
    /// <summary>چرخش ۹۰ درجه</summary>
    [RelayCommand] private void Rotate90() => Rotation = (Rotation + 90) % 360;
    /// <summary>برگردان افقی</summary>
    [RelayCommand] private void ToggleFlip() => IsFlipped = !IsFlipped;
    /// <summary>بازنشانی نمایشگر</summary>
    [RelayCommand]
    private void ResetViewer()
    {
        WindowWidth = 80; WindowLevel = 40; ZoomPercent = 100; SliceIndex = 14;
        IsInverted = false; Rotation = 0; IsFlipped = false; MeasureMode = false;
    }
    /// <summary>روشن/خاموش‌کردن حالت اندازه‌گیری</summary>
    [RelayCommand] private void ToggleMeasure() => MeasureMode = !MeasureMode;

    /// <summary>ثبت اندازه‌گیری از نمای نمایشگر (میلی‌متر)</summary>
    public void AddMeasurement(double millimeters)
    {
        var text = $"ضایعهٔ مشخص‌شده: {Math.Round(millimeters)} میلی‌متر (برش {SliceIndex})";
        Measurements.Insert(0, text);
        ReportFindings = string.IsNullOrWhiteSpace(ReportFindings)
            ? $"اندازهٔ ضایعه در برش محوری: {Math.Round(millimeters)} میلی‌متر."
            : ReportFindings + Environment.NewLine + $"اندازهٔ ضایعه در برش محوری: {Math.Round(millimeters)} میلی‌متر.";
    }

    // ─────────────── سازنده و بارگذاری ───────────────
    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public ImagingViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        // بارگذاری خودکار سازنده حذف شده است (چرخهٔ ۷۹) — داده در اولین
        // ناوبری با RefreshCommand بی‌صدا تازه می‌شود.
    }

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [RelayCommand]
    private async Task SaveImagingOrderAsync()
    {
        if (!CanEditImagingOrder) return;
        if (ImagingPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        // ترتیب فهرست رابط با مقادیر enum یکسان نیست: MRI = ۴ و CT = ۳.
        var modality = ImagingTypeIndex switch { 0 => 1, 1 => 2, 2 => 4, 3 => 3, 4 => 5, _ => 0 };
        if (modality == 0) { _snackbar.Error("نوع تصویربرداری معتبر را انتخاب کنید"); return; }
        var bodyPart = ImagingRegion?.Trim();
        if (string.IsNullOrWhiteSpace(bodyPart) || bodyPart.Length > 200)
        { _snackbar.Error("ناحیهٔ بدن الزامی و حداکثر ۲۰۰ نویسه است"); return; }
        var patient = ImagingPatient;
        var request = new
        {
            patientId = patient.Id, referringDoctorId = (long?)null, modality, bodyPart,
            priority = ImagingUrgent ? 1 : 3, clinicalInfo = ImagingClinicalInfo
        };
        IsSavingImagingOrder = true;
        try
        {
            bool ok;
            try { ok = await _api.PostAsync("imaging/orders", request); }
            catch
            { _snackbar.Error("پاسخ ثبت دریافت نشد؛ پیش از تلاش مجدد صف تصویربرداری را بررسی کنید."); return; }
            if (ok)
            {
                _snackbar.Success("درخواست تصویربرداری ثبت شد");
                if (ImagingPatient?.Id == patient.Id) { ImagingClinicalInfo = null; ImagingUrgent = false; }
                await LoadImagingOrdersCoreAsync(notifyWhenEmpty: false);
            }
            else _snackbar.Error((_api.LastError ?? "ثبت درخواست ناموفق بود") + "؛ در صورت قطع ارتباط، پیش از تلاش مجدد صف را بررسی کنید.");
        }
        finally { IsSavingImagingOrder = false; }
    }

    /// <summary>جستجوی بیمار تصویربرداری؛ نتیجهٔ قدیمی پس از جستجوی ناموفق انتخاب نمی‌ماند.</summary>
    [RelayCommand]
    private async Task SearchImagingPatientAsync()
    {
        if (!CanEditImagingOrder) return;
        if (ImagingPatient is not null && (!string.IsNullOrWhiteSpace(ImagingClinicalInfo) || ImagingUrgent) &&
            Views.AppDialog.Confirm("تغییر بیمار", "اطلاعات بالینی و اولویتِ ثبت‌نشده برای بیمار فعلی پاک می‌شود. ادامه می‌دهید؟", Views.AppDialogIcon.Warning) != true) return;
        if (ImagingPatient is not null) { ImagingClinicalInfo = null; ImagingUrgent = false; }
        ImagingPatient = null;
        IsSearchingImagingPatient = true;
        try { ImagingPatient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, ImagingPatientSearch); }
        finally { IsSearchingImagingPatient = false; }
    }

    /* ── بخش فایل‌های DICOM — گام R4 (حفظ‌شده) ── */

    /// <summary>ردیف درخواست تصویر امروز</summary>
    public sealed class ImagingOrderRow
    {
        /// <summary>سازنده</summary>
        public ImagingOrderRow(long id, long patientId, string patientName, string modality, string? bodyPart,
            int status, int priority, string? clinicalInfo, string? technique, string? findings,
            string? impression, string? recommendation, string? reportDetails,
            DateTime createdAt, DateTime? performedAt, DateTime? reportedAt,
            string? patientGender, string? patientBirthShamsi)
        {
            Id = id; PatientId = patientId; PatientName = patientName; Modality = modality; BodyPart = bodyPart;
            Status = status; Priority = priority; ClinicalInfo = clinicalInfo; Technique = technique;
            Findings = findings; Impression = impression; Recommendation = recommendation;
            ReportDetails = reportDetails; CreatedAt = createdAt; PerformedAt = performedAt;
            ReportedAt = reportedAt; PatientGender = patientGender; PatientBirthShamsi = patientBirthShamsi;
        }

        /// <summary>شناسهٔ درخواست</summary>
        public long Id { get; }
        /// <summary>شناسهٔ بیمار</summary>
        public long PatientId { get; }
        /// <summary>نام بیمار</summary>
        public string PatientName { get; }
        /// <summary>مودالیته (نام enum سرور)</summary>
        public string Modality { get; }
        /// <summary>مودالیتهٔ فارسی</summary>
        public string ModalityFa => Modality switch
        {
            "Ultrasound" => "سونوگرافی", "XRay" => "رادیوگرافی", "CT" => "CT",
            "MRI" => "MRI", "Mammography" => "ماموگرافی", _ => Modality
        };
        /// <summary>ناحیهٔ بدن</summary>
        public string? BodyPart { get; }
        /// <summary>وضعیت عددی (ImagingOrderStatus)</summary>
        public int Status { get; set; }
        /// <summary>فقط درخواست منتظر یا زمان‌بندی‌شده قابل ثبت به‌عنوان انجام‌شده است.</summary>
        public bool CanMarkPerformed => Status is 1 or 2;
        /// <summary>متن وضعیت</summary>
        public string StatusText => Status switch
        {
            1 => "در انتظار انجام", 2 => "زمان‌بندی‌شده", 3 => "انجام‌شده · گزارش‌نشده",
            4 => "گزارش نهایی", 5 => "لغوشده", _ => "—"
        };
        /// <summary>اولویت عددی (۱ اورژانس، ۲ بالا، ۳ عادی)</summary>
        public int Priority { get; }
        /// <summary>متن اولویت</summary>
        public string PriorityText => Priority switch { 1 => "STAT اورژانسی", 2 => "بالا", _ => "عادی" };
        /// <summary>رنگ اولویت</summary>
        public string PriorityColor => Priority switch { 1 => "#C62828", 2 => "#EF8A17", _ => "#5E7A71" };
        /// <summary>اطلاعات بالینی</summary>
        public string? ClinicalInfo { get; }
        /// <summary>تکنیک</summary>
        public string? Technique { get; set; }
        /// <summary>یافته‌ها</summary>
        public string? Findings { get; set; }
        /// <summary>نتیجه‌گیری</summary>
        public string? Impression { get; set; }
        /// <summary>توصیه</summary>
        public string? Recommendation { get; set; }
        /// <summary>جزئیات ساخت‌یافته (JSON)</summary>
        public string? ReportDetails { get; set; }
        /// <summary>زمان ثبت</summary>
        public DateTime CreatedAt { get; }
        /// <summary>ساعت ثبت نمایشی</summary>
        public string TimeText => IranClock.FromUtc(CreatedAt).ToString("HH:mm");
        /// <summary>زمان انجام</summary>
        public DateTime? PerformedAt { get; set; }
        /// <summary>زمان گزارش</summary>
        public DateTime? ReportedAt { get; set; }
        /// <summary>جنسیت بیمار (Male/Female)</summary>
        public string? PatientGender { get; }
        /// <summary>تاریخ تولد شمسی بیمار</summary>
        public string? PatientBirthShamsi { get; }
        /// <summary>عنوان نمایشی</summary>
        public string Display => $"#{Id} — {PatientName} ({ModalityFa})";
    }

    /// <summary>ردیف فایل تصویر</summary>
    public sealed class ImagingFileRow
    {
        /// <summary>سازنده</summary>
        public ImagingFileRow(long id, string? fileName, bool isDicom, long? fileSize, DateTime? uploadedAt)
        {
            Id = id; FileName = fileName ?? $"فایل {id}"; IsDicom = isDicom;
            FileSize = fileSize ?? 0; UploadedAt = uploadedAt;
        }
        /// <summary>شناسه</summary>
        public long Id { get; }
        /// <summary>نام فایل</summary>
        public string FileName { get; }
        /// <summary>DICOM است؟</summary>
        public bool IsDicom { get; }
        /// <summary>حجم بایت</summary>
        public long FileSize { get; }
        /// <summary>حجم خوانا</summary>
        public string SizeText => FileSize >= 1_048_576
            ? $"{FileSize / 1_048_576.0:0.#} MB" : $"{FileSize / 1024.0:0.#} KB";
        /// <summary>نوع با برچسب</summary>
        public string KindText => IsDicom ? "DICOM" : "عادی";
        /// <summary>زمان آپلود</summary>
        public DateTime? UploadedAt { get; }
    }

    /// <summary>فایل‌های درخواست انتخاب‌شده</summary>
    [ObservableProperty] private ObservableCollection<ImagingFileRow> _imagingFiles = new();
    /// <summary>فایل انتخاب‌شده</summary>
    [ObservableProperty] private ImagingFileRow? _selectedImagingFile;

    /// <summary>بارگذاری فهرست درخواست‌های تصویر امروز</summary>
    private async Task LoadImagingOrdersCoreAsync(bool notifyWhenEmpty)
    {
        var version = ++_queueRequestVersion;
        QueueStatus = "در حال دریافت صف؛ اطلاعات قبلی ممکن است به‌روز نباشند.";
        try
        {
            var response = await _api.GetAsync<ImagingOrdersResponse>("imaging/orders");
            if (version != _queueRequestVersion) return;
            if (response?.Data is null)
            {
                QueueStatus = "دریافت صف ناموفق بود؛ اطلاعات قبلی ممکن است قدیمی باشند. دوباره تلاش کنید.";
                _snackbar.Error(_api.LastError ?? "دریافت صف تصویربرداری ناموفق بود");
                return;
            }
            ImagingOrders = new ObservableCollection<ImagingOrderRow>(response.Data
                .Select(o => new ImagingOrderRow(o.Id, o.PatientId, o.PatientName, o.Modality, o.BodyPart,
                    o.Status, o.Priority, o.ClinicalInfo, o.Technique, o.Findings, o.Impression,
                    o.Recommendation, o.ReportDetails, o.CreatedAt, o.PerformedAt, o.ReportedAt,
                    o.PatientGender, o.PatientBirthShamsi)));
            _queueLoaded = true;
            QueueStatus = response.Data.Count >= 100 ? "حداکثر ۱۰۰ درخواست آخر امروز نمایش داده می‌شود؛ آمار مربوط به همین فهرست است." : string.Empty;
            ApplyWorkFilter();
            RaiseStats();
            if (notifyWhenEmpty && ImagingOrders.Count == 0)
                _snackbar.Info("درخواست تصویربرداری برای امروز ثبت نشده است");
        }
        catch (Exception)
        {
            if (version != _queueRequestVersion) return;
            QueueStatus = "دریافت صف ناموفق بود؛ اطلاعات قبلی ممکن است قدیمی باشند. دوباره تلاش کنید.";
            _snackbar.Error("بارگذاری صف تصویربرداری ناموفق بود؛ اتصال سرور را بررسی کنید.");
        }
    }

    /// <summary>نوسازی بی‌صدا — فرمان استاندارد پوسته پس از هر ناوبری</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadImagingOrdersCoreAsync(notifyWhenEmpty: false);

    /// <summary>بارگذاری با کنش کاربر</summary>
    [RelayCommand]
    private Task LoadImagingOrdersAsync() => LoadImagingOrdersCoreAsync(notifyWhenEmpty: true);

    /// <summary>ثبت انجام‌شدن مطالعه، نه صرفاً آغاز آن.</summary>
    [RelayCommand]
    private async Task StartStudyAsync(ImagingOrderRow? row)
    {
        if (row is null || !row.CanMarkPerformed) return;
        if (Views.AppDialog.Confirm("ثبت انجام تصویربرداری",
                $"آیا تصویربرداری {row.ModalityFa} برای {row.PatientName} واقعاً انجام شده است؟ وضعیت به «انجام‌شده» تغییر می‌کند.",
                Views.AppDialogIcon.Warning) != true) return;
        if (await _api.PostAsync("imaging/orders/status", new { orderId = row.Id, status = 3 }))
        {
            row.Status = 3;
            row.PerformedAt = DateTime.UtcNow;
            ApplyWorkFilter();
            RaiseStats();
            _snackbar.Success($"انجام {row.ModalityFa} {row.PatientName} ثبت شد");
        }
        else _snackbar.Error(_api.LastError ?? "تغییر وضعیت ناموفق بود");
    }

    /// <summary>بازخورد انتخاب درخواست — پرکردن گزارش، ایمنی و فایل‌ها</summary>
    partial void OnSelectedImagingOrderChanged(ImagingOrderRow? value)
    {
        _fileRequestVersion++;
        _imageRequestVersion++;
        ImagingFiles = new ObservableCollection<ImagingFileRow>();
        SelectedImagingFile = null;
        CurrentImageBytes = null;
        CurrentImageName = null;
        ImageSourceChanged?.Invoke();
        if (value is null) { ViewerMessage = "مطالعه‌ای انتخاب نشده است."; return; }
        try
        {
            OnSelectedImagingOrderChangedCore(value);
        }
        catch (Exception ex)
        {
            _snackbar.Error("بارگذاری جزئیات مطالعه ناموفق بود: " + ex.Message);
        }
    }

    /// <summary>هستهٔ بازخورد انتخاب مطالعه — خطاهایش در پوشش بالا گرفته می‌شود</summary>
    private void OnSelectedImagingOrderChangedCore(ImagingOrderRow value)
    {
        ReportClinicalInfo = value.ClinicalInfo;
        ReportTechnique = value.Technique;
        ReportFindings = value.Findings;
        ReportImpression = value.Impression;
        ReportRecommendation = value.Recommendation;
        DoseCtdi = null; DoseDlp = null; CriticalFinding = false;
        CriticalNotifiedTo = null; CriticalMethod = CriticalMethodOptions[0]; ReportComparison = ComparisonOptions[0];
        ReportStatus = "نهایی و امضاشده"; Measurements.Clear();
        HasUnsupportedReportDetails = !SupportedReportDetails(value.ReportDetails);
        ReportMessage = HasUnsupportedReportDetails
            ? "جزئیات گزارش با این نسخه سازگار نیست؛ برای جلوگیری از حذف اطلاعات، ذخیره غیرفعال است."
            : value.Status is 4 or 5 ? "گزارش نهایی یا لغوشده قابل بازنویسی نیست؛ الحاقیه هنوز پشتیبانی نمی‌شود." : string.Empty;
        // هر مطالعه چک‌لیست مستقل دارد؛ علامت‌های بیمار قبلی نباید منتقل شود.
        BuildSafety();
        if (!HasUnsupportedReportDetails && !string.IsNullOrWhiteSpace(value.ReportDetails))
        {
            try
            {
                using var doc = JsonDocument.Parse(value.ReportDetails);
                var r = doc.RootElement;
                if (r.TryGetProperty("ctdi", out var c) && c.ValueKind != JsonValueKind.Null) DoseCtdi = c.ToString();
                if (r.TryGetProperty("dlp", out var d) && d.ValueKind != JsonValueKind.Null) DoseDlp = d.ToString();
                if (r.TryGetProperty("comparison", out var cmp) && cmp.GetString() is { } cs && ComparisonOptions.Contains(cs)) ReportComparison = cs;
                if (r.TryGetProperty("status", out var st) && st.GetString() is { } ss && ReportStatusOptions.Contains(ss)) ReportStatus = ss;
                CriticalFinding = r.TryGetProperty("critical", out var cr) && cr.ValueKind == JsonValueKind.True;
                if (r.TryGetProperty("criticalTo", out var ct)) CriticalNotifiedTo = ct.GetString();
                if (r.TryGetProperty("criticalMethod", out var cm) && cm.GetString() is { } ms && CriticalMethodOptions.Contains(ms)) CriticalMethod = ms;
                if (r.TryGetProperty("measurements", out var meas) && meas.ValueKind == JsonValueKind.Array)
                    foreach (var m in meas.EnumerateArray())
                        if (m.GetString() is { } mt) Measurements.Add(mt);
                if (r.TryGetProperty("safety", out var saf) && saf.ValueKind == JsonValueKind.Array)
                {
                    // چک‌لیست پس از ساخته‌شدن بر اساس مدالیته علامت می‌خورد
                    var done = saf.EnumerateArray().Select(e => e.GetString()).ToHashSet();
                    foreach (var item in SafetyItems) item.IsChecked = done.Contains(item.Text);
                }
            }
            catch (JsonException) { /* گزارش‌های قدیمی بدون جزئیات ساخت‌یافته */ }
        }
        OnPropertyChanged(nameof(CurrentDrl));
        OnPropertyChanged(nameof(DoseHigh));
        ViewerMessage = $"{value.ModalityFa} {value.BodyPart} — {value.PatientName}";
        _ = LoadImagingFilesAsync();
    }

    // از بازنویسی دادهٔ نسخه‌های دیگر که این ویرایشگر قادر به نگهداری آن
    // نیست جلوگیری می‌کند؛ گزارش اصلی و چک‌لیست همچنان قابل مشاهده‌اند.
    private bool SupportedReportDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            var properties = doc.RootElement.EnumerateObject().ToList();
            if (properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Count) return false;
            foreach (var p in properties)
            {
                var v = p.Value;
                switch (p.Name)
                {
                    case "ctdi": case "dlp":
                        if (v.ValueKind is not (JsonValueKind.Number or JsonValueKind.Null)) return false;
                        break;
                    case "comparison":
                        if (v.ValueKind != JsonValueKind.Null && (v.ValueKind != JsonValueKind.String || !ComparisonOptions.Contains(v.GetString()))) return false;
                        break;
                    case "status":
                        if (v.ValueKind != JsonValueKind.Null && (v.ValueKind != JsonValueKind.String ||
                            (v.GetString() != "نهایی" && !ReportStatusOptions.Contains(v.GetString())))) return false;
                        break;
                    case "criticalTo":
                        if (v.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)) return false;
                        break;
                    case "criticalMethod":
                        if (v.ValueKind != JsonValueKind.Null && (v.ValueKind != JsonValueKind.String || !CriticalMethodOptions.Contains(v.GetString()))) return false;
                        break;
                    case "critical":
                        if (v.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
                        break;
                    case "measurements": case "safety":
                        if (v.ValueKind != JsonValueKind.Array || v.EnumerateArray().Any(x => x.ValueKind != JsonValueKind.String)) return false;
                        if (p.Name == "safety" && v.EnumerateArray().Any(x => !SafetyFor(SelectedImagingOrder?.Modality).Contains(x.GetString()))) return false;
                        break;
                    default: return false;
                }
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    /// <summary>بارگذاری فایل‌های درخواست انتخاب‌شده</summary>
    private async Task LoadImagingFilesAsync()
    {
        if (SelectedImagingOrder is null) return;
        var orderId = SelectedImagingOrder.Id;
        var version = ++_fileRequestVersion;
        ImagingFilesResponse? response;
        try { response = await _api.GetAsync<ImagingFilesResponse>($"imaging/files?orderId={orderId}"); }
        catch
        {
            if (version == _fileRequestVersion && SelectedImagingOrder?.Id == orderId)
                ViewerMessage = "دریافت فایل‌های مطالعه ناموفق بود.";
            return;
        }
        if (version != _fileRequestVersion || SelectedImagingOrder?.Id != orderId) return;
        if (response?.Data is null) { ViewerMessage = _api.LastError ?? "دریافت فایل‌های مطالعه ناموفق بود."; return; }
        ImagingFiles = new ObservableCollection<ImagingFileRow>(response.Data
            .Select(f => new ImagingFileRow(f.Id, f.FileName, f.IsDicom, f.FileSize, f.UploadedAt)));
        SelectedImagingFile = ImagingFiles.FirstOrDefault();
        if (ImagingFiles.Count == 0)
            ViewerMessage = "برای این مطالعه هنوز فایلی بارگذاری نشده است — از دکمهٔ بارگذاری فایل استفاده کنید.";
    }

    /// <summary>بازخورد انتخاب فایل — بارگذاری تصویر در نمایشگر</summary>
    partial void OnSelectedImagingFileChanged(ImagingFileRow? value)
    {
        var version = ++_imageRequestVersion;
        CurrentImageBytes = null;
        CurrentImageName = value?.FileName;
        ImageSourceChanged?.Invoke();
        if (value is not null) _ = LoadCurrentImageAsync(value, version);
    }

    private async Task LoadCurrentImageAsync(ImagingFileRow file, int version)
    {
        if (file.IsDicom)
        {
            ViewerMessage = "فایل DICOM است — برای مشاهده با پنجره/سطح واقعی، دکمهٔ «نمایش DICOM» را بزنید.";
            return;
        }
        byte[]? bytes;
        try { bytes = await _api.DownloadFileAsync($"imaging/files/{file.Id}/content"); }
        catch
        {
            if (version == _imageRequestVersion && SelectedImagingFile?.Id == file.Id)
                ViewerMessage = "دریافت تصویر ناموفق بود.";
            return;
        }
        if (version != _imageRequestVersion || SelectedImagingFile?.Id != file.Id) return;
        if (bytes is null || bytes.Length == 0)
        {
            ViewerMessage = _api.LastError ?? "دریافت فایل ناموفق بود";
        }
        else
        {
            CurrentImageBytes = bytes;
            ViewerMessage = file.FileName;
        }
        ImageSourceChanged?.Invoke();
    }

    /// <summary>ذخیره گزارش نهایی یا پیش‌نویس؛ این مسیر امضای دیجیتال ندارد.</summary>
    [RelayCommand]
    private async Task SaveReportAsync(bool isFinal)
    {
        if (!CanEditImagingReport) return;
        var order = SelectedImagingOrder;
        if (order is null) { _snackbar.Error("ابتدا مطالعه‌ای از صف کار انتخاب کنید"); return; }
        if (HasUnsupportedReportDetails) { _snackbar.Error("ساختار جزئیات گزارش پشتیبانی نمی‌شود؛ ذخیره برای حفاظت از داده متوقف شد."); return; }
        if (order.Status is 4 or 5) { _snackbar.Error("گزارش نهایی یا درخواست لغوشده قابل بازنویسی نیست؛ الحاقیه هنوز پشتیبانی نمی‌شود."); return; }
        if (isFinal && order.Status != 3) { _snackbar.Error("پیش از نهایی‌کردن گزارش، انجام مطالعه را در صف ثبت کنید"); return; }
        if (isFinal && string.IsNullOrWhiteSpace(ReportFindings) && string.IsNullOrWhiteSpace(ReportImpression))
        { _snackbar.Error("برای نهایی‌کردن گزارش، یافته یا نتیجه‌گیری لازم است"); return; }
        if (isFinal && CriticalFinding && string.IsNullOrWhiteSpace(CriticalNotifiedTo))
        { _snackbar.Error("یافتهٔ بحرانی: گیرندهٔ اطلاع‌رسانی را وارد کنید"); return; }
        decimal? ctdi = null, dlp = null;
        if (!string.IsNullOrWhiteSpace(DoseCtdi))
        {
            if (!Helpers.PlainNumber.TryParse(DoseCtdi, out var c) || c < 0)
            { _snackbar.Error("دوز CTDIvol باید عدد غیرمنفی باشد"); return; }
            ctdi = c;
        }
        if (!string.IsNullOrWhiteSpace(DoseDlp))
        {
            if (!Helpers.PlainNumber.TryParse(DoseDlp, out var d) || d < 0)
            { _snackbar.Error("دوز DLP باید عدد غیرمنفی باشد"); return; }
            dlp = d;
        }
        var details = new
        {
            ctdi,
            dlp,
            comparison = ReportComparison,
            status = isFinal ? "نهایی" : "پیش‌نویس",
            critical = CriticalFinding,
            criticalTo = CriticalFinding ? CriticalNotifiedTo : null,
            criticalMethod = CriticalFinding ? CriticalMethod : null,
            measurements = Measurements.ToArray(),
            safety = SafetyItems.Where(i => i.IsChecked).Select(i => i.Text).ToArray(),
        };
        var request = new
        {
            orderId = order.Id, technique = ReportTechnique, findings = ReportFindings,
            impression = ReportImpression, recommendation = ReportRecommendation,
            reportDetails = JsonSerializer.Serialize(details), isFinal
        };
        IsSavingImagingReport = true;
        try
        {
            bool ok;
            try { ok = await _api.PutAsync("imaging/reports", request); }
            catch { _snackbar.Error("پاسخ ثبت گزارش دریافت نشد؛ پیش از تلاش مجدد وضعیت مطالعه را بررسی کنید."); return; }
            if (!ok) { _snackbar.Error((_api.LastError ?? "ثبت گزارش ناموفق بود") + "؛ در صورت قطع ارتباط وضعیت مطالعه را بررسی کنید."); return; }
            order.Technique = request.technique;
            order.Findings = request.findings;
            order.Impression = request.impression;
            order.Recommendation = request.recommendation;
            order.ReportDetails = request.reportDetails;
            if (isFinal) { order.Status = 4; order.ReportedAt = DateTime.UtcNow; }
            if (SelectedImagingOrder?.Id == order.Id)
            {
                DraftStamp = isFinal ? "گزارش نهایی ذخیره شد" : "پیش‌نویس ذخیره شد";
                ReportMessage = isFinal
                    ? "گزارش نهایی ذخیره شد؛ امضای دیجیتال و اطلاع‌رسانی خودکار انجام نشده است."
                    : "پیش‌نویس ذخیره شد؛ گزارش هنوز نهایی نیست.";
            }
            _snackbar.Success(isFinal ? "گزارش تصویربرداری نهایی شد" : "پیش‌نویس گزارش ذخیره شد");
            ApplyWorkFilter();
            RaiseStats();
        }
        finally { IsSavingImagingReport = false; }
    }

    /// <summary>آپلود فایل تصویر برای درخواست انتخاب‌شده — انتخاب از دیسک</summary>
    [RelayCommand]
    private async Task UploadImagingFileAsync()
    {
        if (SelectedImagingOrder is null)
        {
            _snackbar.Error("ابتدا یک درخواست تصویربرداری از فهرست انتخاب کنید");
            return;
        }

        var orderId = SelectedImagingOrder.Id;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "انتخاب فایل تصویر یا DICOM",
            Filter = "فایل‌های تصویری (*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp)|*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp|همه فایل‌ها (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var bytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
        var uploadResponse = await _api.PostFileAsync<UploadAck>("imaging/files",
            System.IO.Path.GetFileName(dialog.FileName), bytes,
            new Dictionary<string, string> { ["orderId"] = orderId.ToString() });

        if (uploadResponse?.IsSuccess == true)
        {
            _snackbar.Success(uploadResponse.Message ?? "فایل بارگذاری شد");
            if (SelectedImagingOrder?.Id == orderId) await LoadImagingFilesAsync();
        }
        else
        {
            _snackbar.Error(_api.LastError ?? "بارگذاری ناموفق بود");
        }
    }

    /// <summary>گشودن فایل انتخاب‌شده در نمایشگر DICOM</summary>
    [RelayCommand]
    private async Task OpenDicomViewerAsync()
    {
        if (SelectedImagingFile is null)
        {
            _snackbar.Error("ابتدا یک فایل از فهرست انتخاب کنید");
            return;
        }

        var file = SelectedImagingFile;
        var orderId = SelectedImagingOrder?.Id;
        var version = _imageRequestVersion;
        var bytes = await _api.DownloadFileAsync($"imaging/files/{file.Id}/content");
        if (version != _imageRequestVersion || SelectedImagingFile?.Id != file.Id || SelectedImagingOrder?.Id != orderId) return;
        if (bytes is null || bytes.Length == 0)
        {
            _snackbar.Error(_api.LastError ?? "دریافت فایل ناموفق بود");
            return;
        }

        var viewer = new Views.DicomViewerWindow
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
        viewer.LoadBytes(bytes, file.FileName);
        viewer.Show();
    }

    /// <summary>حذف فایل انتخاب‌شده</summary>
    [RelayCommand]
    private async Task DeleteImagingFileAsync(ImagingFileRow? row)
    {
        if (row is null) return;
        if (await _api.DeleteAsync($"imaging/files/{row.Id}"))
        {
            _snackbar.Success("فایل حذف شد");
            await LoadImagingFilesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "حذف ناموفق بود");
    }

    // ── مدل‌های پاسخ ──

    private sealed class ImagingOrdersResponse
    {
        public List<ImagingOrderDto>? Data { get; set; }
    }

    private sealed class ImagingOrderDto
    {
        public long Id { get; set; }
        public long PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientGender { get; set; }
        public string? PatientBirthShamsi { get; set; }
        public string Modality { get; set; } = string.Empty;
        public string? BodyPart { get; set; }
        public int Status { get; set; }
        public int Priority { get; set; }
        public string? ClinicalInfo { get; set; }
        public string? Technique { get; set; }
        public string? Findings { get; set; }
        public string? Impression { get; set; }
        public string? Recommendation { get; set; }
        public string? ReportDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PerformedAt { get; set; }
        public DateTime? ReportedAt { get; set; }
    }

    private sealed class ImagingFilesResponse
    {
        public List<ImagingFileDto>? Data { get; set; }
    }

    private sealed class ImagingFileDto
    {
        public long Id { get; set; }
        public string? FileName { get; set; }
        public bool IsDicom { get; set; }
        public long? FileSize { get; set; }
        public DateTime? UploadedAt { get; set; }
    }

    private sealed class UploadAck
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public UploadAckData? Data { get; set; }
    }

    private sealed class UploadAckData
    {
        public long FileId { get; set; }
        public bool IsDicom { get; set; }
    }
}
