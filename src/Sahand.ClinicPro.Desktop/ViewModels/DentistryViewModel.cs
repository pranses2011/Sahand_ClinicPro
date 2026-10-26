using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>دندان چارت FDI — همان کلاس چرخه‌های پیشین، اکنون در صفحهٔ مستقل دندانپزشکی</summary>
public partial class ToothVM : ObservableObject
{
    public int Number { get; init; }
    public string Quadrant => Number / 10 == 1 || Number / 10 == 2 ? "بالا" : "پایین";
    public string ToolTip => $"دندان {Number} — فک {Quadrant} — {ConditionText}";
    [ObservableProperty] private int _condition; // 0 سالم 1 پوسیدگی 2 ترمیم 3 درمان 4 روکش 5 کشیده 6 ایمپلنت

    // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) — رفع «رنگ‌های وضعیت برای فک
    // بالا و پایین درست نشان داده نمی‌شوند و همگی سفید می‌مانند» ═══
    // «Color» و «ToolTip» و «ConditionText» پراپرتی‌های محاسبه‌ای‌اند و با
    // تغییر Condition هیچ اعلانی برایشان صادر نمی‌شد → اتصال Background در
    // قالب دکمهٔ دندان فقط مقدار لحظهٔ بارگذاری (سالم = سفید) را می‌دید و
    // هرگز تازه نمی‌شد. اکنون تغییر Condition، اعلان تغییر همهٔ موارد
    // وابسته را هم صادر می‌کند تا رنگ دندان بی‌درنگ عوض شود.
    partial void OnConditionChanged(int value)
    {
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(ConditionText));
    }

    /// <summary>این دندان همان دندان انتخاب‌شدهٔ چارت است؟ — چرخه بیست‌وچهارم:
    /// مقدار با انتخاب کاربر توسط OnSelectedToothChanged به‌روز می‌شود تا
    /// قالب دکمه با DataTrigger ساده (مقدار ثابت True) هایلایتش کند</summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>شمارهٔ دندان با ارقام فارسی — «همهٔ اعداد بدون استثنا فارسی» (گزارش هفدهم)</summary>
    public string NumberFa => Helpers.Money.ToPersianDigits(Number.ToString());

    /// <summary>نام جنس دندان بر اساس رقم یکان FDI</summary>
    public string TypeName => (Number % 10) switch
    {
        1 => "پیشین مرکزی", 2 => "پیشین کناری", 3 => "نیش",
        4 => "آسیاب کوچک اول", 5 => "آسیاب کوچک دوم",
        6 => "آسیاب بزرگ اول", 7 => "آسیاب بزرگ دوم", _ => "عقل"
    };

    /// <summary>تعداد ریشهٔ واقعی این دندان (از جدول آناتومی مبدل شکل دندان)</summary>
    public int RootCount => Converters.ToothShapeConverter.RootCount(Number);

    /// <summary>تعداد کانال واقعی این دندان (از جدول آناتومی مبدل شکل دندان)</summary>
    public int CanalCount => Converters.ToothShapeConverter.CanalCount(Number);

    /// <summary>متن آناتومی برای پنل دندان انتخابی — مثلاً «آسیاب بزرگ اول · ۳ ریشه · ۴ کانال»</summary>
    public string AnatomyText =>
        $"{TypeName} · {Helpers.Money.ToPersianDigits(RootCount.ToString())} ریشه · {Helpers.Money.ToPersianDigits(CanalCount.ToString())} کانال";

    /// <summary>متن فارسی وضعیت — برای تولتیپ و نمایش</summary>
    public string ConditionText => Condition switch
    {
        1 => "پوسیدگی", 2 => "ترمیم‌شده", 3 => "درمان‌شده", 4 => "روکش", 5 => "کشیده‌شده", 6 => "ایمپلنت",
        _ => "سالم"
    };

    /// <summary>
    /// رنگ وضعیت — عیناً همان رنگ چیپ‌های راهنمای چارت:
    /// سالم #FFFFFF / پوسیدگی #E53935 / ترمیم #FDD835 / درمان #43A047 /
    /// روکش #1E88E5 / کشیده #37474F / ایمپلنت #8E24AA (چرخهٔ ۳۳).
    /// با تغییر Condition اعلان تازه‌شدن هم صادر می‌شود (بالا) تا اتصال
    /// پس‌زمینهٔ دکمه بی‌درنگ رنگ را بگیرد
    /// </summary>
    public string Color => Condition switch
    {
        1 => "#E53935", 2 => "#FDD835", 3 => "#43A047", 4 => "#1E88E5", 5 => "#37474F", 6 => "#8E24AA",
        _ => "#FFFFFF"
    };
}

/// <summary>ردیف طرح درمان دندان</summary>
public record DentalPlanRow(int Tooth, string Procedure, decimal Price, string StatusText, string StatusColor,
    string Surface = "—", string Schedule = "بدون نوبت");

/// <summary>قلم کاتالوگ خدمات دندانپزشکی — نام، دسته، مدت جلسه و تعرفه</summary>
public record DentalCatalogItem(string Name, string Category, int DurationMin, decimal Price)
{
    /// <summary>متن نمایشی کاتالوگ در فهرست کشویی</summary>
    public string Display => $"{Name} · {Helpers.Money.ToPersianDigits(DurationMin.ToString())} دقیقه";
}

/// <summary>
/// ViewModel صفحهٔ مستقل «دندانپزشکی» — چرخهٔ چهل‌وسوم (گزارش مالک)
/// </summary>
/// <remarks>
/// «دندانپزشکی و چشم پزشکی و تصویربرداری و فزیوتراپی در یک صفحه و تب های
/// جداگانه هستند. برای هر کدوم یک صفحه مستقل طراحی بکن تا با همدیگه
/// تداخلی نداشته باشن» — چارت FDI، طرح درمان و پروندهٔ بیمار این تب از
/// پاراکلینیکِ تب‌دار جدا و به صفحهٔ اختصاصی منتقل شد؛ نام پراپرتی‌ها
/// عیناً حفظ شده تا قالب‌های XAML بدون تغییر بایند شوند. منطق جستجوی
/// بیمار به هلپر مشترک PatientQuickSearch رفت.
/// </remarks>
public partial class DentistryViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "دندانپزشکی";

    /// <summary>دندان‌های فک بالا — چارت FDI</summary>
    public ObservableCollection<ToothVM> UpperTeeth { get; } = new();

    /// <summary>دندان‌های فک پایین</summary>
    public ObservableCollection<ToothVM> LowerTeeth { get; } = new();

    /// <summary>دندان انتخاب‌شدهٔ فعلی</summary>
    [ObservableProperty] private ToothVM? _selectedTooth;

    /// <summary>نمایهٔ وضعیت دندان انتخاب‌شده</summary>
    [ObservableProperty] private int _toothConditionIndex;

    /// <summary>گزینه‌های وضعیت دندان</summary>
    public string[] ToothConditions { get; } = { "سالم", "پوسیدگی", "ترمیم‌شده", "درمان‌شده", "روکش", "کشیده‌شده", "ایمپلنت" };

    /// <summary>ردیف‌های طرح درمان بیمار</summary>
    public ObservableCollection<DentalPlanRow> DentalPlan { get; } = new();

    /// <summary>کاتالوگ خدمات دندانپزشکی (تعرفه و مدت جلسه) — قابلیت تازهٔ تأییدشده</summary>
    public DentalCatalogItem[] CatalogItems { get; } =
    {
        new("ترمیم کامپوزیت", "ترمیمی", 30, 1_200_000m),
        new("ترمیم آمالگام", "ترمیمی", 30, 900_000m),
        new("عصب‌کشی (درمان ریشه)", "درمان ریشه", 45, 1_850_000m),
        new("روکش زیرکونیا", "پروتز", 40, 1_800_000m),
        new("روکش PFM", "پروتز", 40, 1_400_000m),
        new("ایمپلنت + روکش", "جراحی", 60, 9_500_000m),
        new("کشیدن ساده", "جراحی", 20, 700_000m),
        new("جراحی دندان عقل", "جراحی", 45, 2_500_000m),
        new("جرم‌گیری و بروساژ", "پیشگیری", 30, 800_000m),
        new("بلیچینگ", "زیبایی", 45, 3_000_000m),
    };

    /// <summary>خدمت برگزیده از کاتالوگ برای افزودن به طرح درمان</summary>
    [ObservableProperty] private DentalCatalogItem? _selectedCatalogItem;

    /// <summary>سطوح قابل انتخاب دندان (استاندارد M/O/D/B/L)</summary>
    public string[] SurfaceOptions { get; } = { "O · اکلوزال", "M · مزیال", "D · دیستال", "B · باکال", "L · لینگوال", "MOD · سه‌سطحی" };

    /// <summary>سطح درگیر برگزیده</summary>
    [ObservableProperty] private string _selectedSurface = "O · اکلوزال";

    /// <summary>نوبت پیشنهادی قلم تازهٔ طرح درمان (روز و ساعت جلسه)</summary>
    [ObservableProperty] private string _planScheduleText = string.Empty;

    /// <summary>یادداشت بالینی دندان انتخابی</summary>
    [ObservableProperty] private string _clinicalNote = string.Empty;

    /// <summary>خلاصهٔ چارت پریودنتال (عمق پاکت، خونریزی، لقی) — همراه چارت ذخیره می‌شود</summary>
    [ObservableProperty] private string _perioSummary = string.Empty;

    /// <summary>خلاصهٔ فرم پزشکی و حساسیت‌ها — همراه چارت ذخیره می‌شود</summary>
    [ObservableProperty] private string _medicalSummary = string.Empty;

    /// <summary>متن نسخه و دستورات پس از درمان — همراه چارت ذخیره می‌شود</summary>
    [ObservableProperty] private string _prescriptionText = string.Empty;

    /// <summary>یادداشت رادیوگرافی پیوندخورده به دندان‌ها — همراه چارت ذخیره می‌شود</summary>
    [ObservableProperty] private string _radioNote = string.Empty;

    /// <summary>رضایت‌نامهٔ درمان امضا شده است؟</summary>
    [ObservableProperty] private bool _consentSigned;

    /// <summary>جمع کل طرح درمان — با هر تغییر ردیف‌ها تازه می‌شود</summary>
    public decimal PlanTotal => DentalPlan.Sum(r => r.Price);

    /// <summary>جمع کل طرح با ارقام فارسی برای نمایش</summary>
    public string PlanTotalText => Helpers.Money.ToPersianDigits(PlanTotal.ToString("N0"));

    /// <summary>اسلات نوبت‌دهی طرح درمان — روز و ساعت و وضعیت (خالی/رزرو/استراحت)</summary>
    public record DentalSlot(string Day, string Time, string State)
    {
        /// <summary>متن نمایشی اسلات</summary>
        public string Display => $"{Day} {Helpers.Money.ToPersianDigits(Time)}";
        /// <summary>رنگ پس‌زمینه بر اساس وضعیت اسلات</summary>
        public string Back => State switch { "رزرو" => "#FFCDD2", "استراحت" => "#FFE082", _ => "#C8E6C9" };
    }

    /// <summary>اسلات‌های هفتهٔ پیش رو برای نوبت‌دهی قلم طرح درمان</summary>
    public DentalSlot[] ScheduleSlots { get; } =
    {
        new("شنبه", "09:00", "خالی"), new("شنبه", "10:00", "رزرو"), new("شنبه", "11:00", "خالی"),
        new("یکشنبه", "09:00", "استراحت"), new("یکشنبه", "10:00", "خالی"), new("یکشنبه", "12:00", "خالی"),
        new("دوشنبه", "09:00", "رزرو"), new("دوشنبه", "11:00", "خالی"), new("دوشنبه", "12:00", "خالی"),
        new("سه‌شنبه", "10:00", "خالی"), new("چهارشنبه", "09:00", "خالی"), new("چهارشنبه", "11:00", "رزرو"),
    };

    /// <summary>اسلات برگزیدهٔ نوبت‌دهی — روی فیلد نوبت پیشنهادی می‌نشیند</summary>
    [ObservableProperty] private DentalSlot? _selectedSlot;

    partial void OnSelectedSlotChanged(DentalSlot? value)
    {
        if (value is not null && value.State == "خالی")
            PlanScheduleText = value.Display;
    }

    /// <summary>چارت کودک (شیری ۵۱ تا ۸۵) فعال است؟</summary>
    [ObservableProperty] private bool _isChildChart;

    partial void OnIsChildChartChanged(bool value) => RebuildChart(value);

    /// <summary>بازسازی چارت برای بزرگسال (FDI دائمی) یا کودک (شیری)</summary>
    private void RebuildChart(bool child)
    {
        UpperTeeth.Clear(); LowerTeeth.Clear(); SelectedTooth = null;
        if (child)
        {
            foreach (var n in new[] { 55, 54, 53, 52, 51 }) UpperTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 61, 62, 63, 64, 65 }) UpperTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 85, 84, 83, 82, 81 }) LowerTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 71, 72, 73, 74, 75 }) LowerTeeth.Add(new ToothVM { Number = n });
        }
        else
        {
            foreach (var n in new[] { 18, 17, 16, 15, 14, 13, 12, 11 }) UpperTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 21, 22, 23, 24, 25, 26, 27, 28 }) UpperTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 48, 47, 46, 45, 44, 43, 42, 41 }) LowerTeeth.Add(new ToothVM { Number = n });
            foreach (var n in new[] { 31, 32, 33, 34, 35, 36, 37, 38 }) LowerTeeth.Add(new ToothVM { Number = n });
        }
    }

    /* ── بیمار و ماندگاری چارت (چرخهٔ ۲۸) ── */

    /// <summary>عبارت جستجوی بیمار دندانپزشکی</summary>
    [ObservableProperty] private string _dentalPatientSearch = string.Empty;

    /// <summary>بیمار منتخب — چارت و طرح به پروندهٔ او وصل است</summary>
    [ObservableProperty] private QuickPatient? _dentalPatient;

    /// <summary>اطلاعات آخرین ذخیرهٔ چارت — «آخرین ذخیره: ۱۴۰۴/۰۷/۱۲» یا راهنما</summary>
    [ObservableProperty]
    private string _dentalChartInfo = "بیمار را جستجو کنید تا چارت ذخیره‌شدهٔ او بارگذاری شود";

    /// <summary>سازنده — چارت FDI: فک بالا ۱۱-۱۸ و ۲۱-۲۸ / فک پایین ۳۱-۳۸ و ۴۱-۴۸</summary>
    public DentistryViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        foreach (var n in new[] { 18, 17, 16, 15, 14, 13, 12, 11 })
            UpperTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 21, 22, 23, 24, 25, 26, 27, 28 })
            UpperTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 48, 47, 46, 45, 44, 43, 42, 41 })
            LowerTeeth.Add(new ToothVM { Number = n });
        foreach (var n in new[] { 31, 32, 33, 34, 35, 36, 37, 38 })
            LowerTeeth.Add(new ToothVM { Number = n });
        SelectedCatalogItem = CatalogItems[0];
        DentalPlan.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PlanTotal));
            OnPropertyChanged(nameof(PlanTotalText));
        };
    }

    /// <summary>انتخاب دندان از چارت</summary>
    [RelayCommand]
    private void SelectTooth(ToothVM tooth)
    {
        SelectedTooth = tooth;
        ToothConditionIndex = tooth.Condition;
    }

    /// <summary>
    /// همگام‌سازی پرچم IsSelected دندان‌ها با انتخاب فعلی — چرخهٔ بیست‌وچهارم:
    /// Value در DataTrigger وابستگی DependencyProperty نیست و Binding روی آن
    /// XamlParseException می‌اندازد؛ فقط همین پرچم ساده به‌روز می‌شود.
    /// </summary>
    partial void OnSelectedToothChanged(ToothVM? oldValue, ToothVM? newValue)
    {
        if (oldValue is not null) oldValue.IsSelected = false;
        if (newValue is not null) newValue.IsSelected = true;
    }

    /// <summary>اعمال وضعیت روی دندان انتخابی</summary>
    partial void OnToothConditionIndexChanged(int value)
    {
        if (SelectedTooth is not null)
            SelectedTooth.Condition = value;
    }

    /// <summary>افزودن به طرح درمان — از کاتالوگ خدمات، با سطح درگیر و نوبت پیشنهادی</summary>
    [RelayCommand]
    private void AddDentalPlan()
    {
        if (SelectedTooth is null) { _snackbar.Error("ابتدا دندان را از چارت انتخاب کنید"); return; }
        var item = SelectedCatalogItem ?? CatalogItems[0];
        // درمان ریشه: نام خدمت با تعداد کانال واقعی همان دندان کامل می‌شود
        var procedure = item.Name.Contains("عصب‌کشی")
            ? $"{item.Name} — {Helpers.Money.ToPersianDigits(SelectedTooth.CanalCount.ToString())} کانال"
            : item.Name;
        var surface = (SelectedSurface ?? "—").Split(' ')[0];
        var schedule = string.IsNullOrWhiteSpace(PlanScheduleText) ? "بدون نوبت" : PlanScheduleText.Trim();
        DentalPlan.Add(new DentalPlanRow(SelectedTooth.Number, procedure, item.Price,
            "پیشنهاد", "#FFF3E0", surface, schedule));
    }

    /// <summary>حذف یک ردیف از طرح درمان</summary>
    [RelayCommand]
    private void RemoveDentalPlan(DentalPlanRow? row)
    {
        if (row is not null) DentalPlan.Remove(row);
    }

    /// <summary>
    /// جستجوی بیمار دندانپزشکی — چرخهٔ ۲۸: با انتخاب بیمار، آخرین چارت
    /// ذخیره‌شدهٔ او خودکار بارگذاری می‌شود تا ادامهٔ همان پرونده باشد.
    /// </summary>
    [RelayCommand]
    private async Task SearchDentalPatientAsync()
    {
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, DentalPatientSearch);
        if (patient is null) return;
        DentalPatient = patient;
        _ = LoadDentalChartAsync();
    }

    /// <summary>بازیابی آخرین چارت دندان و طرح درمان بیمار — GET dental/chart</summary>
    private async Task LoadDentalChartAsync()
    {
        DentalPlan.Clear();
        if (DentalPatient is null) return;
        var response = await _api.GetAsync<DentalChartResponse>(
            $"dental/chart?patientId={DentalPatient.Id}");
        var chart = response?.Data;
        if (chart is null)
        {
            DentalChartInfo = "بازیابی چارت ناموفق بود — می‌توانید چارت تازه ذخیره کنید";
            return;
        }

        // اعمال وضعیت دندان‌ها از نقشهٔ ذخیره‌شده — هر ۳۲ دندان
        foreach (var tooth in UpperTeeth.Concat(LowerTeeth))
            if (chart.Teeth is not null && chart.Teeth.TryGetValue(tooth.Number.ToString(), out var cond))
                tooth.Condition = cond;

        // ردیف‌های طرح درمان ذخیره‌شده
        if (chart.Rows is not null)
            foreach (var r in chart.Rows)
                DentalPlan.Add(new DentalPlanRow(r.Tooth, r.Procedure ?? "—", r.Price,
                    r.Status ?? "پیشنهاد", "#FFF3E0", r.Surface ?? "—", r.Schedule ?? "بدون نوبت"));

        // بخش‌های تازه (پریودنتال/پزشکی/نسخه/رادیوگرافی/رضایت) — داخل همان JSON طرح
        if (chart.Extras is not null)
        {
            PerioSummary = chart.Extras.Perio ?? string.Empty;
            MedicalSummary = chart.Extras.Medical ?? string.Empty;
            PrescriptionText = chart.Extras.Prescription ?? string.Empty;
            RadioNote = chart.Extras.Radio ?? string.Empty;
            ConsentSigned = chart.Extras.Consent;
        }

        DentalChartInfo = string.IsNullOrWhiteSpace(chart.CreatedDateShamsi)
            ? "چارتی برای این بیمار ثبت نشده — نخستین ثبت"
            : $"آخرین ذخیرهٔ چارت: {chart.CreatedDateShamsi}";
    }

    /// <summary>
    /// ذخیرهٔ چارت کامل + طرح درمان برای بیمار منتخب — POST dental/plans؛
    /// وضعیت هر ۳۲ دندان و ردیف‌های طرح به‌صورت JSON در PlanDetails طرح
    /// درمان ذخیره می‌شود و با هر بار بازیابی، چارت همان وضعیت را برمی‌گرداند.
    /// </summary>
    [RelayCommand]
    private async Task SaveDentalChartAsync()
    {
        if (DentalPatient is null)
        { _snackbar.Error("ابتدا بیمار را جستجو کنید — چارت باید به پروندهٔ یک بیمار وصل باشد"); return; }

        // نقشهٔ وضعیت همهٔ دندان‌ها — اعداد کلیدی؛ وضعیت صفر هم ذخیره می‌شود تا
        // بازیابی، تصویر کامل چارت (و نه فقط تغییرات) را بازسازی کند
        var teeth = UpperTeeth.Concat(LowerTeeth)
            .ToDictionary(t => t.Number.ToString(), t => t.Condition);

        // ردیف‌های طرح درمان — متن/قیمت/وضعیت
        var rows = DentalPlan.Select(r => new
            {
                tooth = r.Tooth, procedure = r.Procedure, price = r.Price, status = r.StatusText,
                surface = r.Surface, schedule = r.Schedule
            })
            .ToList();

        // بخش‌های تازه داخل همان PlanDetails ذخیره می‌شوند — بدون تغییر دیتابیس
        var extras = new
        {
            perio = PerioSummary, medical = MedicalSummary,
            prescription = PrescriptionText, radio = RadioNote, consent = ConsentSigned
        };
        var planDetails = System.Text.Json.JsonSerializer.Serialize(new { teeth, rows, extras });
        var ok = await _api.PostAsync("dental/plans", new
        {
            patientId = DentalPatient.Id,
            dentistId = (long?)null,
            totalCost = DentalPlan.Sum(r => r.Price),
            estimatedSessions = DentalPlan.Count,
            planDetails
        });
        if (ok)
        {
            _snackbar.Success($"چارت و طرح درمان برای «{DentalPatient.FullName}» ذخیره شد");
            DentalChartInfo = $"آخرین ذخیرهٔ چارت: {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(IranClock.Now)}";
        }
        else _snackbar.Error(_api.LastError ?? "ذخیرهٔ چارت ناموفق بود");
    }

    /// <summary>پاک‌کردن بیمار دندانپزشکی — چارت تازه برای بیمار بعدی</summary>
    [RelayCommand]
    private void ClearDentalPatient()
    {
        DentalPatient = null;
        DentalPatientSearch = string.Empty;
        DentalPlan.Clear();
        PerioSummary = MedicalSummary = PrescriptionText = RadioNote = string.Empty;
        ConsentSigned = false;
        PlanScheduleText = string.Empty;
        foreach (var t in UpperTeeth.Concat(LowerTeeth)) t.Condition = 0;
        SelectedTooth = null;
        DentalChartInfo = "بیمار را جستجو کنید تا چارت ذخیره‌شدهٔ او بارگذاری شود";
    }

    // ── مدل‌های پاسخ چارت دندان — چرخهٔ ۲۸ ──

    /// <summary>پاسخ GET dental/chart — وضعیت دندان‌ها + ردیف‌های طرح</summary>
    private sealed class DentalChartResponse
    {
        public DentalChartData? Data { get; set; }
    }

    private sealed class DentalChartData
    {
        public long? PlanId { get; set; }
        public string? CreatedDateShamsi { get; set; }
        /// <summary>نقشهٔ شمارهٔ دندان → وضعیت (۰ سالم تا ۶ ایمپلنت)</summary>
        public Dictionary<string, int>? Teeth { get; set; }
        public List<DentalPlanRowDto>? Rows { get; set; }
        public DentalExtrasDto? Extras { get; set; }
        public string? Note { get; set; }
    }

    private sealed class DentalPlanRowDto
    {
        public int Tooth { get; set; }
        public string? Procedure { get; set; }
        public decimal Price { get; set; }
        public string? Status { get; set; }
        public string? Surface { get; set; }
        public string? Schedule { get; set; }
    }

    /// <summary>بخش‌های تازهٔ ذخیره‌شده در JSON طرح — پریودنتال/پزشکی/نسخه/رادیوگرافی/رضایت</summary>
    private sealed class DentalExtrasDto
    {
        public string? Perio { get; set; }
        public string? Medical { get; set; }
        public string? Prescription { get; set; }
        public string? Radio { get; set; }
        public bool Consent { get; set; }
    }
}
