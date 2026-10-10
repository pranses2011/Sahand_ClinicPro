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
public record DentalPlanRow(int Tooth, string Procedure, decimal Price, string StatusText, string StatusColor);

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

    /// <summary>افزودن به طرح درمان</summary>
    [RelayCommand]
    private void AddDentalPlan()
    {
        if (SelectedTooth is null) { _snackbar.Error("ابتدا دندان را از چارت انتخاب کنید"); return; }
        var procedures = new[] { "معاینه", "ترمیم", "عصب‌کشی", "روکش", "کشیدن ساده", "کشیدن جراحی" };
        var price = SelectedTooth.Condition switch
        {
            1 => 1_500_000m, 3 => 5_000_000m, 4 => 8_000_000m, 5 => 2_000_000m, _ => 1_000_000m
        };
        DentalPlan.Add(new DentalPlanRow(SelectedTooth.Number,
            procedures[Math.Clamp(SelectedTooth.Condition, 0, 5)],
            price, "پیشنهاد", "#FFF3E0"));
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
                    r.Status ?? "پیشنهاد", "#FFF3E0"));

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
        var rows = DentalPlan.Select(r => new { tooth = r.Tooth, procedure = r.Procedure, price = r.Price, status = r.StatusText })
            .ToList();

        var planDetails = System.Text.Json.JsonSerializer.Serialize(new { teeth, rows });
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
            DentalChartInfo = $"آخرین ذخیرهٔ چارت: {Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(System.DateTime.Now)}";
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
        public string? Note { get; set; }
    }

    private sealed class DentalPlanRowDto
    {
        public int Tooth { get; set; }
        public string? Procedure { get; set; }
        public decimal Price { get; set; }
        public string? Status { get; set; }
    }
}
