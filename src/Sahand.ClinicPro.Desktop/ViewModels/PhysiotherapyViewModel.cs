using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>یک تکنیک درمانی با مدت اجرا (دقیقه)</summary>
public partial class PhysioTechniqueItem : ObservableObject
{
    /// <summary>نام تکنیک</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>انتخاب‌شده در این جلسه؟</summary>
    [ObservableProperty] private bool _isSelected;
    /// <summary>مدت اجرا به دقیقه</summary>
    [ObservableProperty] private int _minutes = 10;
}

/// <summary>یک گزینهٔ چک‌لیستی (پرچم قرمز / هدف / معیار ترخیص)</summary>
public partial class PhysioCheckItem : ObservableObject
{
    /// <summary>متن گزینه</summary>
    public string Text { get; init; } = string.Empty;
    /// <summary>علامت خورده؟</summary>
    [ObservableProperty] private bool _isChecked;
}

/// <summary>یک ردیف ارزیابی ساخت‌یافته (شروع در برابر امروز)</summary>
public partial class PhysioAssessmentRow : ObservableObject
{
    /// <summary>نام شاخص</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>بیشینهٔ مقیاس شاخص</summary>
    public double Max { get; init; } = 100;
    /// <summary>معکوس است؟ (بیشتر = بهتر)</summary>
    public bool Reverse { get; init; }
    /// <summary>مقدار شروع دوره</summary>
    [ObservableProperty] private double _baseValue;
    /// <summary>مقدار امروز</summary>
    [ObservableProperty] private double _nowValue;

    /// <summary>درصد بهبود این شاخص</summary>
    public double Improvement => BaseValue == 0 ? 0 : (Reverse ? (NowValue - BaseValue) / BaseValue * 100 : (BaseValue - NowValue) / BaseValue * 100);
    /// <summary>متن درصد بهبود</summary>
    public string ImprovementText => $"{Math.Round(Improvement)}٪";
    /// <summary>پهنای میلهٔ شروع (پیکسل، از ۲۰۰)</summary>
    public double BaseBar => Math.Min(200, BaseValue / Max * 200);
    /// <summary>پهنای میلهٔ امروز (پیکسل، از ۲۰۰)</summary>
    public double NowBar => Math.Min(200, NowValue / Max * 200);

    partial void OnBaseValueChanged(double value) { OnPropertyChanged(nameof(Improvement)); OnPropertyChanged(nameof(ImprovementText)); OnPropertyChanged(nameof(BaseBar)); }
    partial void OnNowValueChanged(double value) { OnPropertyChanged(nameof(Improvement)); OnPropertyChanged(nameof(ImprovementText)); OnPropertyChanged(nameof(NowBar)); }
}

/// <summary>یک میلهٔ دامنهٔ حرکت / قدرت در پنل ارزیابی</summary>
public sealed class PhysioBar
{
    /// <summary>برچسب میله</summary>
    public string Label { get; init; } = string.Empty;
    /// <summary>پهنای میله (پیکسل از ۲۰۰)</summary>
    public double BarPx { get; init; }
    /// <summary>متن مقدار</summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>پروتکل درمانی آماده</summary>
public sealed class PhysioProtocol
{
    /// <summary>نام عارضه</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>تعداد جلسه‌های دوره</summary>
    public int Sessions { get; init; } = 12;
    /// <summary>شرح دوره</summary>
    public string Plan { get; init; } = string.Empty;
    /// <summary>تکنیک‌های پروتکل با مدت</summary>
    public (string Name, int Minutes)[] Techniques { get; init; } = Array.Empty<(string, int)>();
    /// <summary>تمرین خانگی پیشنهادی</summary>
    public string HomeProgram { get; init; } = string.Empty;
    /// <summary>هدف پایان دوره</summary>
    public string Goal { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel صفحهٔ مستقل «فیزیوتراپی» — ماژول حرفه‌ای (طرح تأییدشدهٔ مالک،
/// نسخهٔ ۱۵ پیش‌نمایش): نقشهٔ بدن مرد/زن با انتخاب ناحیه و شدت درد، تکنیک‌های
/// جلسه با مدت، غربالگری پرچم‌های قرمز، پروتکل‌های آماده، ارزیابی ساخت‌یافته
/// با درصد بهبود خودکار، نمودار روند دوره از روی سابقهٔ واقعی جلسات، تمرین
/// خانگی، گزارش ارجاع‌دهنده و اهداف ICF و معیار ترخیص. داده‌های ساخت‌یافتهٔ
/// جلسه در ProgressNote (JSON) ذخیره می‌شود — بدون تغییر دیتابیس.
/// </summary>
public partial class PhysiotherapyViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "فیزیوتراپی";

    /// <summary>رویداد تغییر وضعیت نقشهٔ بدن (نما برای رنگ‌آمیزی دوباره)</summary>
    public event Action? BodyMapChanged;

    // ─────────────── بیمار و دوره ───────────────
    /// <summary>عبارت جستجوی بیمار</summary>
    [ObservableProperty] private string _physioPatientSearch = string.Empty;
    /// <summary>بیمار منتخب</summary>
    [ObservableProperty] private QuickPatient? _physioPatient;
    /// <summary>مجموع جلسه‌های دوره</summary>
    [ObservableProperty] private int _totalSessions = 12;
    /// <summary>شمارهٔ این جلسه</summary>
    public int SessionNumber => History.Count + 1;
    /// <summary>درصد پیشرفت دوره</summary>
    public double CourseProgress => TotalSessions <= 0 ? 0 : Math.Min(100, (double)History.Count / TotalSessions * 100);
    /// <summary>پروتکل فعال</summary>
    [ObservableProperty] private string? _activeProtocolName;
    /// <summary>هدف پروتکل فعال</summary>
    [ObservableProperty] private string? _protocolGoal;

    // ─────────────── نقشهٔ بدن ───────────────
    /// <summary>نام همهٔ ناحیه‌های نقشهٔ بدن</summary>
    public static readonly string[] ZoneNames =
    {
        "گردن و سر", "شانه راست", "شانه چپ", "بازو راست", "بازو چپ", "ساعد و دست راست", "ساعد و دست چپ",
        "سینه و قفسه سینه", "شکم و مرکزی (Core)", "پهلو و مایل شکمی", "ذوزنقه و بالای پشت",
        "لت و میان‌پشت راست", "لت و میان‌پشت چپ", "کمر (لومبار)", "لگن و کشاله",
        "باسن (گلوتئال) راست", "باسن (گلوتئال) چپ", "چهارسر و ران راست", "چهارسر و ران چپ",
        "همسترینگ راست", "همسترینگ چپ", "زانو راست", "زانو چپ",
        "ساق و مچ پای راست", "ساق و مچ پای چپ", "ساق (دوقلو) راست", "ساق (دوقلو) چپ"
    };

    /// <summary>شدت درد هر ناحیه (۰ تا ۱۰)</summary>
    public Dictionary<string, int> ZonePain { get; } = new();
    /// <summary>ناحیهٔ انتخاب‌شده روی نقشه</summary>
    [ObservableProperty] private string _selectedZone = "کمر (لومبار)";
    /// <summary>پیکر زنانه نمایش داده شود؟ (پیش‌فرض از جنسیت بیمار)</summary>
    [ObservableProperty] private bool _isFemale;
    /// <summary>مقیاس درد ۰ تا ۱۰ برای دکمه‌ها</summary>
    public int[] PainScale { get; } = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    /// <summary>درد ناحیهٔ انتخابی</summary>
    public int SelectedZonePain => ZonePain.TryGetValue(SelectedZone, out var v) ? v : 0;

    /// <summary>انتخاب ناحیه از روی نقشه (فراخوانی از کدِ نما)</summary>
    public void SelectZone(string zone)
    {
        SelectedZone = zone;
        OnPropertyChanged(nameof(SelectedZonePain));
        BodyMapChanged?.Invoke();
    }

    /// <summary>ثبت شدت درد برای ناحیهٔ انتخابی</summary>
    [RelayCommand]
    private void SetZonePain(int value)
    {
        ZonePain[SelectedZone] = value;
        PainScore = value;
        OnPropertyChanged(nameof(SelectedZonePain));
        BodyMapChanged?.Invoke();
    }

    /// <summary>تغییر جنسیت پیکر</summary>
    [RelayCommand]
    private void SetGender(bool female)
    {
        IsFemale = female;
        BodyMapChanged?.Invoke();
    }

    partial void OnIsFemaleChanged(bool value) => BodyMapChanged?.Invoke();

    // ─────────────── جلسهٔ امروز ───────────────
    /// <summary>تکنیک‌های درمانی با مدت</summary>
    public ObservableCollection<PhysioTechniqueItem> TechniqueItems { get; } = new()
    {
        new() { Name = "الکتروتراپی (TENS)", Minutes = 15 },
        new() { Name = "اولتراسوند", Minutes = 8 },
        new() { Name = "مگنت‌تراپی", Minutes = 20 },
        new() { Name = "لیزر درمانی", Minutes = 10 },
        new() { Name = "تمرینات درمانی", Minutes = 25 },
        new() { Name = "موبیلایزیشن مفصلی", Minutes = 10 },
        new() { Name = "ماساژ و آزادسازی بافت نرم", Minutes = 15 },
        new() { Name = "درای نیدلینگ", Minutes = 10 },
        new() { Name = "هیدروتراپی", Minutes = 20 },
        new() { Name = "کشش مکانیکی (تراکشن)", Minutes = 15 },
    };

    /// <summary>تکنیک دیگر (متن آزاد)</summary>
    [ObservableProperty] private string? _physioOtherTechnique;
    /// <summary>نمرهٔ درد کلی ۰ تا ۱۰</summary>
    [ObservableProperty] private int _painScore;
    /// <summary>یادداشت جلسه</summary>
    [ObservableProperty] private string? _physioNotes;
    /// <summary>رنگ نمرهٔ درد — سبز/زرد/قرمز</summary>
    public string PainColor => PainScore <= 3 ? "#43A047" : PainScore <= 6 ? "#F9A825" : "#E53935";
    partial void OnPainScoreChanged(int value) => OnPropertyChanged(nameof(PainColor));
    /// <summary>مدت کل جلسه (مجموع مدت تکنیک‌های منتخب)</summary>
    public int TotalDuration => TechniqueItems.Where(t => t.IsSelected).Sum(t => t.Minutes);

    /// <summary>پرچم‌های قرمز غربالگری</summary>
    public ObservableCollection<PhysioCheckItem> RedFlags { get; } = new()
    {
        new() { Text = "کاهش وزن بی‌دلیل" }, new() { Text = "تب یا عفونت اخیر" },
        new() { Text = "ضربه یا شکستگی تازه" }, new() { Text = "بی‌اختیاری ادرار/مدفوع" },
        new() { Text = "ضعف پیشروندهٔ اندام" }, new() { Text = "درد شبانهٔ بیدارکننده" },
        new() { Text = "سابقهٔ سرطان" }, new() { Text = "بی‌حسی ناحیهٔ زینی" },
    };
    /// <summary>هشدار فعال بودن پرچم قرمز</summary>
    public bool HasRedFlag => RedFlags.Any(f => f.IsChecked);

    // ─────────────── پروتکل‌ها ───────────────
    /// <summary>پروتکل‌های درمانی آماده</summary>
    public PhysioProtocol[] Protocols { get; } =
    {
        new() { Name = "کمردرد مزمن", Sessions = 12, Plan = "۱۲ جلسه · ۳ بار در هفته · هر جلسه ۴۵ دقیقه",
            Techniques = new[] { ("الکتروتراپی (TENS)", 15), ("اولتراسوند", 8), ("تمرینات درمانی", 25), ("موبیلایزیشن مفصلی", 10) },
            HomeProgram = "بریج ۳×۱۰ — زانو به سینه ۳×۳۰ ثانیه — پلانک ۳×۲۰ ثانیه — پیاده‌روی روزانه ۲۰ دقیقه",
            Goal = "هدف: درد ≤۲ و Oswestry زیر ۲۰٪ در پایان دوره" },
        new() { Name = "آرتروز زانو", Sessions = 10, Plan = "۱۰ جلسه · ۲ بار در هفته · هر جلسه ۴۰ دقیقه",
            Techniques = new[] { ("الکتروتراپی (TENS)", 15), ("تمرینات درمانی", 25), ("ماساژ و آزادسازی بافت نرم", 10) },
            HomeProgram = "بالا آوردن پای صاف (SLR) ‏۳×۱۰ — انقباض چهارسر ۳×۱۰ — دوچرخهٔ ثابت ۱۵ دقیقه",
            Goal = "هدف: دامنهٔ کامل زانو و بالا رفتن از پله بدون درد" },
        new() { Name = "شانهٔ منجمد", Sessions = 14, Plan = "۱۴ جلسه · ۳ بار در هفته · هر جلسه ۵۰ دقیقه",
            Techniques = new[] { ("اولتراسوند", 8), ("موبیلایزیشن مفصلی", 15), ("تمرینات درمانی", 25) },
            HomeProgram = "تمرین پاندولی ۳×۱ دقیقه — خزیدن انگشت روی دیوار ۳×۱۰ — کشش کپسول خلفی ۳×۳۰ ثانیه",
            Goal = "هدف: ابداکشن بالای ۱۵۰ درجه و خواب بدون درد" },
        new() { Name = "گردن‌درد و دیسک گردن", Sessions = 10, Plan = "۱۰ جلسه · ۲ بار در هفته · هر جلسه ۴۰ دقیقه",
            Techniques = new[] { ("کشش مکانیکی (تراکشن)", 15), ("الکتروتراپی (TENS)", 15), ("تمرینات درمانی", 20) },
            HomeProgram = "چین‌تاک گردن ۳×۱۰ — جمع‌کردن کتف ۳×۱۲ — اصلاح وضعیت نشستن هر ۳۰ دقیقه",
            Goal = "هدف: درد گردن ≤۲ و بازگشت کامل دامنهٔ چرخش" },
        new() { Name = "توان‌بخشی پس از سکته", Sessions = 20, Plan = "۲۰ جلسه · ۵ بار در هفته · هر جلسه ۶۰ دقیقه",
            Techniques = new[] { ("تمرینات درمانی", 40), ("موبیلایزیشن مفصلی", 15), ("ماساژ و آزادسازی بافت نرم", 10) },
            HomeProgram = "تمرین نشستن و برخاستن با کمک ۳×۸ — تمرین تعادل کنار دیوار — تمرین گرفتن و رها کردن اشیا",
            Goal = "هدف: برگ ۵۰+ و راه‌رفتن مستقل با وسیلهٔ کمکی" },
        new() { Name = "پیچ‌خوردگی مچ پا", Sessions = 6, Plan = "۶ جلسه · ۲ بار در هفته · هر جلسه ۳۰ دقیقه",
            Techniques = new[] { ("اولتراسوند", 8), ("تمرینات درمانی", 20), ("ماساژ و آزادسازی بافت نرم", 10) },
            HomeProgram = "الفبای مچ پا ۳×۱۰ — تمرین تعادل تک‌پا ۳×۳۰ ثانیه — بالا رفتن روی پنجه ۳×۱۲",
            Goal = "هدف: تعادل تک‌پا ۳۰ ثانیه و بازگشت به ورزش" },
    };

    /// <summary>اعمال پروتکل آماده روی جلسه و دوره</summary>
    [RelayCommand]
    private void ApplyProtocol(PhysioProtocol? protocol)
    {
        if (protocol is null) return;
        TotalSessions = protocol.Sessions;
        ActiveProtocolName = protocol.Name;
        ProtocolGoal = protocol.Goal;
        HomeProgram = protocol.HomeProgram;
        foreach (var item in TechniqueItems)
        {
            var match = protocol.Techniques.FirstOrDefault(t => t.Name == item.Name);
            item.IsSelected = match.Name is not null;
            if (match.Name is not null) item.Minutes = match.Minutes;
        }
        OnPropertyChanged(nameof(TotalDuration));
        _snackbar.Success($"پروتکل «{protocol.Name}» بارگذاری شد");
    }

    // ─────────────── ارزیابی ساخت‌یافته ───────────────
    /// <summary>ردیف‌های ارزیابی دوره</summary>
    public ObservableCollection<PhysioAssessmentRow> Assessments { get; } = new()
    {
        new() { Name = "مقیاس درد (VAS)", Max = 10, BaseValue = 8, NowValue = 8 },
        new() { Name = "ناتوانی — Oswestry ‏(٪)", Max = 100, BaseValue = 46, NowValue = 46 },
        new() { Name = "خم‌شدن کمر (درجه)", Max = 90, Reverse = true, BaseValue = 40, NowValue = 40 },
        new() { Name = "بلندکردن پای صاف SLR (درجه)", Max = 90, Reverse = true, BaseValue = 45, NowValue = 45 },
        new() { Name = "پلانک (ثانیه)", Max = 60, Reverse = true, BaseValue = 10, NowValue = 10 },
        new() { Name = "تعادل برگ Berg (از ۵۶)", Max = 56, Reverse = true, BaseValue = 48, NowValue = 48 },
    };
    /// <summary>میانگین بهبود همهٔ شاخص‌ها</summary>
    public double TotalImprovement => Assessments.Count == 0 ? 0 : Assessments.Average(a => a.Improvement);
    /// <summary>متن میانگین بهبود</summary>
    public string TotalImprovementText => $"{Math.Round(TotalImprovement)}٪";

    /// <summary>میله‌های دامنهٔ حرکت مفصل درگیر (از روی ارزیابی زنده)</summary>
    public ObservableCollection<PhysioBar> RomBars { get; } = new();

    /// <summary>تازه‌سازی میله‌های دامنهٔ حرکت از ردیف‌های ارزیابی</summary>
    private void RefreshRomBars()
    {
        RomBars.Clear();
        var flex = Assessments[2];
        var slr = Assessments[3];
        var plank = Assessments[4];
        RomBars.Add(new() { Label = "فلکشن کمر", BarPx = Math.Min(200, flex.NowValue / 90 * 200), Text = $"{flex.NowValue}° از ۹۰°" });
        RomBars.Add(new() { Label = "بلندکردن پای صاف (SLR)", BarPx = Math.Min(200, slr.NowValue / 90 * 200), Text = $"{slr.NowValue}° از ۹۰°" });
        RomBars.Add(new() { Label = "استقامت عضلات مرکزی (پلانک)", BarPx = Math.Min(200, plank.NowValue / 60 * 200), Text = $"{plank.NowValue} ثانیه از ۶۰" });
    }

    /// <summary>تمرین خانگی بیمار</summary>
    [ObservableProperty] private string? _homeProgram;
    /// <summary>اهداف عملکردی ICF</summary>
    public ObservableCollection<PhysioCheckItem> IcfGoals { get; } = new()
    {
        new() { Text = "نشستن ۳۰ دقیقه بدون درد" }, new() { Text = "بلند کردن بار ۵ کیلویی با تکنیک درست" },
        new() { Text = "پیاده‌روی ۳۰ دقیقه‌ای روزانه" }, new() { Text = "بازگشت به کار تمام‌وقت" },
        new() { Text = "خواب شبانه بدون بیداری از درد" },
    };
    /// <summary>معیارهای ترخیص از دوره</summary>
    public ObservableCollection<PhysioCheckItem> DischargeCriteria { get; } = new()
    {
        new() { Text = "درد ≤۲ در دو جلسهٔ متوالی" }, new() { Text = "دامنهٔ حرکت حداقل ۸۰٪ سمت سالم" },
        new() { Text = "اجرای مستقل تمرین خانگی" }, new() { Text = "بدون پرچم قرمز در غربالگری پایانی" },
    };
    /// <summary>گزارش پیشرفت برای پزشک ارجاع‌دهنده</summary>
    [ObservableProperty] private string? _referralReport;

    /// <summary>ساخت خودکار گزارش ارجاع‌دهنده از داده‌های دوره</summary>
    [RelayCommand]
    private void GenerateReport()
    {
        var name = PhysioPatient?.FullName ?? "بیمار";
        var pain = Assessments[0];
        var osw = Assessments[1];
        var flex = Assessments[2];
        ReferralReport =
            $"گزارش پیشرفت فیزیوتراپی — {name}" + Environment.NewLine +
            $"پروتکل فعال: {ActiveProtocolName ?? "—"} · جلسهٔ {SessionNumber} از {TotalSessions}" + Environment.NewLine +
            $"درد (VAS): از {pain.BaseValue} به {pain.NowValue} ({pain.ImprovementText} بهبود)" + Environment.NewLine +
            $"ناتوانی Oswestry: از {osw.BaseValue}٪ به {osw.NowValue}٪ ({osw.ImprovementText} بهبود)" + Environment.NewLine +
            $"خم‌شدن کمر: از {flex.BaseValue}° به {flex.NowValue}° ({flex.ImprovementText} بهبود)" + Environment.NewLine +
            $"میانگین بهبود دوره: {TotalImprovementText}" + Environment.NewLine +
            "پیشنهاد: ادامهٔ پروتکل تا پایان دوره و ارزیابی پایانی در جلسهٔ آخر.";
    }

    // ─────────────── سابقه و نمودار روند ───────────────
    /// <summary>سابقهٔ جلسات بیمار</summary>
    public ObservableCollection<PhysioSessionDto> History { get; } = new();
    /// <summary>نقاط نمودار روند درد</summary>
    [ObservableProperty] private System.Windows.Media.PointCollection _painTrendPoints = new();
    /// <summary>نقاط نمودار روند خم‌شدن</summary>
    [ObservableProperty] private System.Windows.Media.PointCollection _romTrendPoints = new();
    /// <summary>خلاصهٔ متنی روند</summary>
    [ObservableProperty] private string? _trendSummary;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public PhysiotherapyViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        foreach (var row in Assessments)
            row.PropertyChanged += (_, _) => { OnPropertyChanged(nameof(TotalImprovement)); OnPropertyChanged(nameof(TotalImprovementText)); RefreshRomBars(); };
        RefreshRomBars();
        foreach (var t in TechniqueItems)
            t.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalDuration));
        foreach (var f in RedFlags)
            f.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HasRedFlag));
    }

    partial void OnPhysioPatientChanged(QuickPatient? value)
    {
        if (value is not null && value.Gender > 0)
            IsFemale = value.Gender == 2;
        _ = LoadHistoryAsync();
    }

    /// <summary>جستجوی بیمار فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SearchPhysioPatientAsync()
    {
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, PhysioPatientSearch);
        if (patient is not null) PhysioPatient = patient;
    }

    /// <summary>بارگذاری سابقهٔ جلسات و ساخت نمودار روند واقعی</summary>
    private async Task LoadHistoryAsync()
    {
        History.Clear();
        PainTrendPoints = new();
        RomTrendPoints = new();
        TrendSummary = null;
        OnPropertyChanged(nameof(SessionNumber));
        OnPropertyChanged(nameof(CourseProgress));
        if (PhysioPatient is null) return;

        var response = await _api.GetAsync<PhysioSessionsResponse>($"physiotherapy/sessions?patientId={PhysioPatient.Id}");
        var sessions = response?.Data;
        if (sessions is null || sessions.Count == 0) return;
        foreach (var s in sessions) History.Add(s);

        // نمودار درد از PainScore واقعی جلسات (محور: ۰ تا ۱۰ ← ۲۰۰ تا ۲۰)
        var painPts = new System.Windows.Media.PointCollection();
        var romPts = new System.Windows.Media.PointCollection();
        for (var i = 0; i < sessions.Count; i++)
        {
            var x = sessions.Count == 1 ? 300 : 40 + i * (520.0 / Math.Max(1, sessions.Count - 1));
            var pain = sessions[i].PainScore ?? 0;
            painPts.Add(new System.Windows.Point(x, 200 - pain * 18));
            var flex = ExtractFlexion(sessions[i].ProgressNote);
            if (flex is not null)
                romPts.Add(new System.Windows.Point(x, 200 - flex.Value * 2));
        }
        PainTrendPoints = painPts;
        RomTrendPoints = romPts;
        var first = sessions[0].PainScore ?? 0;
        var last = sessions[^1].PainScore ?? 0;
        TrendSummary = sessions.Count > 1
            ? $"درد از {first} به {last} رسیده است ({sessions.Count} جلسهٔ ثبت‌شده)."
            : "پس از ثبت جلسهٔ دوم، روند دوره اینجا نمایش داده می‌شود.";

        // ارزیابی: شروع از نخستین جلسه، امروز از آخرین جلسه
        ApplyAssessmentsFrom(sessions[0].ProgressNote, baseSide: true);
        ApplyAssessmentsFrom(sessions[^1].ProgressNote, baseSide: false);
        OnPropertyChanged(nameof(SessionNumber));
        OnPropertyChanged(nameof(CourseProgress));
    }

    /// <summary>استخراج خم‌شدن کمر از یادداشت ساخت‌یافتهٔ جلسه</summary>
    private static double? ExtractFlexion(string? progressNote)
    {
        if (string.IsNullOrWhiteSpace(progressNote)) return null;
        try
        {
            using var doc = JsonDocument.Parse(progressNote);
            if (doc.RootElement.TryGetProperty("flexion", out var f) && f.TryGetDouble(out var v)) return v;
        }
        catch (JsonException) { /* یادداشت‌های قدیمی متن آزاد بوده‌اند */ }
        return null;
    }

    /// <summary>نشاندن ارزیابی‌های ذخیره‌شده روی جدول (سمت شروع یا امروز)</summary>
    private void ApplyAssessmentsFrom(string? progressNote, bool baseSide)
    {
        if (string.IsNullOrWhiteSpace(progressNote)) return;
        try
        {
            using var doc = JsonDocument.Parse(progressNote);
            if (!doc.RootElement.TryGetProperty("assessments", out var arr)) return;
            foreach (var item in arr.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var n)) continue;
                var row = Assessments.FirstOrDefault(a => a.Name == n.GetString());
                if (row is null) continue;
                if (baseSide && item.TryGetProperty("base", out var b) && b.TryGetDouble(out var bv)) row.BaseValue = bv;
                if (!baseSide && item.TryGetProperty("now", out var w) && w.TryGetDouble(out var wv)) row.NowValue = wv;
            }
        }
        catch (JsonException) { /* سازگاری با سابقهٔ قدیمی */ }
    }

    /// <summary>ذخیرهٔ جلسهٔ فیزیوتراپی با همهٔ داده‌های ساخت‌یافته</summary>
    [RelayCommand]
    private async Task SavePhysioSessionAsync()
    {
        if (PhysioPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        if (HasRedFlag)
            _snackbar.Error("توجه: پرچم قرمز فعال است — پیش از ادامهٔ درمان، ارجاع پزشکی را بررسی کنید");

        var techniques = TechniqueItems.Where(t => t.IsSelected).Select(t => $"{t.Name} ({t.Minutes} دقیقه)").ToList();
        if (!string.IsNullOrWhiteSpace(PhysioOtherTechnique)) techniques.Add(PhysioOtherTechnique.Trim());
        var treatments = string.Join("، ", techniques);
        if (!string.IsNullOrWhiteSpace(ActiveProtocolName))
            treatments = $"پروتکل {ActiveProtocolName}: {treatments}";

        var details = new
        {
            bodyMap = ZonePain.Where(z => z.Value > 0).ToDictionary(z => z.Key, z => z.Value),
            redFlags = RedFlags.Where(f => f.IsChecked).Select(f => f.Text).ToArray(),
            assessments = Assessments.Select(a => new { name = a.Name, @base = a.BaseValue, now = a.NowValue }).ToArray(),
            flexion = Assessments[2].NowValue,
            protocol = ActiveProtocolName,
            homeProgram = HomeProgram,
            goals = IcfGoals.Where(g => g.IsChecked).Select(g => g.Text).ToArray(),
            discharge = DischargeCriteria.Where(g => g.IsChecked).Select(g => g.Text).ToArray(),
            note = PhysioNotes
        };
        var progressNote = JsonSerializer.Serialize(details);

        var ok = await _api.PostAsync("physiotherapy/sessions", new
        {
            patientId = PhysioPatient.Id,
            therapistId = (long?)null,
            sessionNumber = SessionNumber,
            treatments,
            duration = TotalDuration > 0 ? TotalDuration : (int?)null,
            painScore = (byte)Math.Clamp(PainScore, 0, 10),
            progressNote
        });
        if (ok)
        {
            _snackbar.Success("جلسه فیزیوتراپی با نقشهٔ بدن و ارزیابی ساخت‌یافته ثبت شد");
            PhysioNotes = null;
            await LoadHistoryAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ثبت جلسه ناموفق بود");
    }
}

/// <summary>پاسخ سابقهٔ جلسات فیزیوتراپی از سرور</summary>
public sealed class PhysioSessionsResponse
{
    /// <summary>موفقیت پاسخ</summary>
    public bool IsSuccess { get; set; }
    /// <summary>فهرست جلسات</summary>
    public List<PhysioSessionDto>? Data { get; set; }
}

/// <summary>یک جلسهٔ فیزیوتراپی در سابقه</summary>
public sealed class PhysioSessionDto
{
    /// <summary>شناسه</summary>
    public long Id { get; set; }
    /// <summary>تاریخ شمسی</summary>
    public string? DateShamsi { get; set; }
    /// <summary>شمارهٔ جلسه</summary>
    public int SessionNumber { get; set; }
    /// <summary>نمرهٔ درد</summary>
    public byte? PainScore { get; set; }
    /// <summary>مدت جلسه</summary>
    public int? Duration { get; set; }
    /// <summary>اقدامات جلسه</summary>
    public string? Treatments { get; set; }
    /// <summary>یادداشت ساخت‌یافته (JSON)</summary>
    public string? ProgressNote { get; set; }
}
