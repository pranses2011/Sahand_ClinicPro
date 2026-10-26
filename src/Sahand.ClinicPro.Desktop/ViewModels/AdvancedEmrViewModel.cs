using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Shared.Growth;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel پرونده بالینی پیشرفته — گام R2 (چرخه چهارم)
/// سه تب: نمودار رشد WHO + سابقه خانوادگی + طرح مراقبت بیمار مزمن
/// </summary>
public partial class AdvancedEmrViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه — در هدر پوسته</summary>
    public override string Title => "پرونده بالینی پیشرفته";

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public AdvancedEmrViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
    }

    // ═══════════ بخش مشترک: انتخاب بیمار ═══════════

    /// <summary>تب فعال — کد پشت به ViewModel وصل است</summary>
    [ObservableProperty]
    private int _indicatorTab;

    /// <summary>عبارت جستجوی بیمار — نام/کدملی/موبایل/پرونده</summary>
    [ObservableProperty]
    private string _patientSearchTerm = string.Empty;

    /// <summary>نتایج جستجوی بیمار — چرخه بیست‌وسوم: فراخوانی ShowPatientResults با هر جایگزینی</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPatientResults))]
    private ObservableCollection<PatientOption> _patientResults = new();

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPatient))]
    [NotifyPropertyChangedFor(nameof(ShowPatientResults))]
    private PatientOption? _selectedPatient;

    /// <summary>آیا بیماری انتخاب شده — نمایش بخش‌ها</summary>
    public bool HasPatient => SelectedPatient is not null;

    /// <summary>
    /// آیا فهرست نتایج جستجو دیده شود؟ — فقط تا پیش از انتخاب بیمار (چرخه بیست‌وسوم).
    /// ریشهٔ «صفحه کمرنگ و غیرفعال با انتخاب بیمار»: پیش‌تر در OnSelectedPatientChanged
    /// بلافاصله PatientResults.Clear() اجرا می‌شد؛ پاک‌شدن آیتم انتخاب‌شده از منبع ListBox
    /// انتخاب را در همان لحظه باطل می‌کرد → SelectedPatient دوباره null می‌شد → HasPatient=false
    /// → TabControl (IsEnabled=HasPatient) برای همیشه غیرفعال و کمرنگ می‌ماند.
    /// راه‌حل: نتایج پاک نمی‌شوند؛ فقط با انتخاب بیمار «مخفی» می‌شوند.
    /// </summary>
    public bool ShowPatientResults => SelectedPatient is null && PatientResults.Count > 0;

    /// <summary>تاریخ تولد بیمار (میلادی) — پایه محاسبه سن نمودار رشد</summary>
    public DateOnly? PatientBirthDate { get; private set; }

    /// <summary>جنسیت بیمار — پسر true</summary>
    public bool IsMale { get; private set; } = true;

    /// <summary>شماره تکرار رسم نمودار — هر بار تغییر داده یک واحد زیاد می‌شود</summary>
    [ObservableProperty]
    private int _chartVersion;

    /// <summary>جستجوی بیمار — حداقل ۲ نویسه</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(PatientSearchTerm) || PatientSearchTerm.Trim().Length < 2)
            return;

        var response = await _api.GetAsync<PatientsSearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearchTerm.Trim())}");
        PatientResults = response?.Data is not null
            ? new ObservableCollection<PatientOption>(response.Data.Select(p => new PatientOption(
                p.Id,
                $"{p.FirstName} {p.LastName} — پرونده {p.MedicalRecordNo}",
                $"کدملی {p.NationalCode} — موبایل {p.Mobile}")))
            : new ObservableCollection<PatientOption>();
    }

    /// <summary>
    /// بازخورد انتخاب بیمار — بارگذاری هر سه مجموعه داده + جزئیات برای سن/جنسیت.
    /// چرخه بیست‌وسوم: PatientResults.Clear() حذف شد — پاک‌کردن منبعِ ListBox هم‌زمان با
    /// انتخاب، آیتم انتخاب‌شده را از بین می‌برد و انتخاب را باطل می‌کرد (HasPatient→false)
    /// و کل صفحه غیرفعال می‌ماند. اکنون نتایج فقط مخفی می‌شوند (ShowPatientResults).
    /// </summary>
    partial void OnSelectedPatientChanged(PatientOption? value)
    {
        if (value is null) return;
        _ = LoadPatientAsync(value.Id);
    }

    /// <summary>
    /// خلاصهٔ بیمار انتخاب‌شده — کارت بالای صفحه (چرخه بیست‌ویکم، گزارش پانزدهم
    /// مالک: «در پروندهٔ بالینی پیشرفته با انتخاب بیمار هیچی نشان نمی‌دهد» —
    /// پیش‌تر پس از انتخاب، تب‌ها خالی بود و هیچ بازخوردی از انتخاب بیمار دیده
    /// نمی‌شد؛ اکنون کارت مشخصات + سن + جنسیت نمایش داده می‌شود)
    /// </summary>
    [ObservableProperty]
    private string _patientSummary = string.Empty;

    /// <summary>بارگذاری کامل داده‌های پرونده بالینی بیمار</summary>
    private async Task LoadPatientAsync(long patientId)
    {
        IsBusy = true;
        try
        {
            // جزئیات بیمار — تاریخ تولد و جنسیت برای z-score + خلاصهٔ کارت بالای صفحه
            var detail = await _api.GetAsync<PatientDetailResponse>($"patients/{patientId}");
            if (detail?.Data is not null)
            {
                IsMale = detail.Data.Gender == 1; // شمارنده Gender: 1=مرد 2=زن
                if (DateOnly.TryParse(detail.Data.BirthDate, out var birth))
                    PatientBirthDate = birth;
                else if (PersianDateHelper.ToGregorian(detail.Data.BirthDateShamsi) is { } gregorian)
                    PatientBirthDate = DateOnly.FromDateTime(gregorian);

                // کارت خلاصه — نام کامل، کد ملی، سن، جنسیت
                var ageText = detail.Data.Age is > 0 ? $"{detail.Data.Age} ساله" : "—";
                PatientSummary = $"{detail.Data.FirstName} {detail.Data.LastName}" +
                    $" | کد ملی: {detail.Data.NationalCode}" +
                    $" | {ageText}" +
                    $" | {(IsMale ? "مرد" : "زن")}";
            }

            await LoadGrowthAsync(patientId);
            await LoadFamilyHistoryAsync(patientId);
            await LoadCarePlansAsync(patientId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══════════ تب ۱: نمودار رشد ═══════════

    /// <summary>شاخص انتخاب‌شده نمودار — ۰=وزن ۱=قد ۲=محیط سر ۳=BMI</summary>
    [ObservableProperty]
    private int _indicatorIndex;

    /// <summary>ردیف‌های اندازه‌گیری رشد — مرتب کهن‌ترین</summary>
    [ObservableProperty]
    private ObservableCollection<GrowthRow> _growthRows = new();

    /// <summary>ردیف انتخاب‌شده — برای حذف</summary>
    [ObservableProperty]
    private GrowthRow? _selectedGrowthRow;

    // ── فرم ثبت جدید ──
    [ObservableProperty] private string _newDateShamsi = PersianDateHelper.ToShamsi(IranClock.Today);
    [ObservableProperty] private string _newWeight = string.Empty;
    [ObservableProperty] private string _newHeight = string.Empty;
    [ObservableProperty] private string _newHeadCirc = string.Empty;
    [ObservableProperty] private string _newGrowthNotes = string.Empty;

    /// <summary>بارگذاری اندازه‌گیری‌های رشد + محاسبه z-score هر ردیف</summary>
    private async Task LoadGrowthAsync(long patientId)
    {
        var response = await _api.GetAsync<GrowthResponse>($"emr/growth/{patientId}");
        var rows = new List<GrowthRow>();
        foreach (var g in response?.Data ?? new List<GrowthRowDto>())
        {
            var zWeight = g.WeightKg is not null
                ? WhoGrowthReference.ZScore(GrowthIndicator.WeightForAge, IsMale, g.AgeMonths, (double)g.WeightKg.Value)
                : null;
            var zHeight = g.HeightCm is not null
                ? WhoGrowthReference.ZScore(GrowthIndicator.HeightForAge, IsMale, g.AgeMonths, (double)g.HeightCm.Value)
                : null;
            var zHead = g.HeadCircumferenceCm is not null
                ? WhoGrowthReference.ZScore(GrowthIndicator.HeadCircumferenceForAge, IsMale, g.AgeMonths, (double)g.HeadCircumferenceCm.Value)
                : null;

            rows.Add(new GrowthRow(g.Id, g.MeasuredAt, g.AgeMonths,
                PersianDateHelper.ToShamsi(g.MeasuredAt.ToDateTime(TimeOnly.MinValue)),
                g.WeightKg, g.HeightCm, g.HeadCircumferenceCm, g.Notes,
                FormatZ(zWeight), FormatZ(zHeight), FormatZ(zHead),
                StatusOf(zWeight), StatusOf(zHeight), StatusOf(zHead)));
        }

        GrowthRows = new ObservableCollection<GrowthRow>(rows);
        ChartVersion++; // رسم مجدد نمودار
    }

    /// <summary>ثبت اندازه‌گیری رشد جدید — به‌روزرسانی رکورد همان تاریخ در صورت وجود</summary>
    [RelayCommand]
    private async Task AddGrowthAsync()
    {
        if (SelectedPatient is null) return;
        ErrorMessage = string.Empty;

        var measuredAt = PersianDateHelper.ToGregorian(NewDateShamsi);
        if (measuredAt is null)
        {
            ErrorMessage = "تاریخ اندازه‌گیری شمسی معتبر نیست (مثال 1404/07/10)";
            return;
        }

        var weight = ParseDecimal(NewWeight);
        var height = ParseDecimal(NewHeight);
        var head = ParseDecimal(NewHeadCirc);
        if (weight is null && height is null && head is null)
        {
            ErrorMessage = "حداقل یکی از مقادیر وزن/قد/محیط سر را وارد کنید";
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync("emr/growth", new
            {
                patientId = SelectedPatient.Id,
                // چرخهٔ ۷۹: قالب‌بندی صریح با فرهنگ خنثی — مقاوم در برابر تقویم فرهنگ جاری
                measuredAt = measuredAt.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                weightKg = weight,
                heightCm = height,
                headCircumferenceCm = head,
                notes = string.IsNullOrWhiteSpace(NewGrowthNotes) ? null : NewGrowthNotes.Trim()
            });

            if (ok)
            {
                _snackbar.Success("اندازه‌گیری رشد ثبت شد");
                NewWeight = NewHeight = NewHeadCirc = NewGrowthNotes = string.Empty;
                await LoadGrowthAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "ثبت اندازه‌گیری ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>حذف اندازه‌گیری — ردیف از CommandParameter</summary>
    [RelayCommand]
    private async Task DeleteGrowthAsync(GrowthRow? row)
    {
        if (SelectedPatient is null || row is null) return;
        IsBusy = true;
        try
        {
            if (await _api.DeleteAsync($"emr/growth/{row.Id}"))
            {
                _snackbar.Success("اندازه‌گیری حذف شد");
                await LoadGrowthAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "حذف ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>بازخورد تغییر شاخص — رسم مجدد منحنی مرجع</summary>
    partial void OnIndicatorIndexChanged(int value) => ChartVersion++;

    // ═══════════ تب ۲: سابقه خانوادگی ═══════════

    /// <summary>گزینه‌های نسبت خویشاوند — هم‌ترتیب شمارنده دامنه</summary>
    public static string[] RelativeOptions { get; } =
    {
        "پدر", "مادر", "برادر", "خواهر", "پسر", "دختر",
        "پدربزرگ", "مادربزرگ", "عمو/دایی", "عمه/خاله", "پسرعمو/پسرخاله", "سایر"
    };

    /// <summary>ردیف‌های سابقه خانوادگی</summary>
    [ObservableProperty]
    private ObservableCollection<FamilyRow> _familyRows = new();

    /// <summary>ردیف انتخاب‌شده برای حذف</summary>
    [ObservableProperty]
    private FamilyRow? _selectedFamilyRow;

    // ── فرم ثبت جدید ──
    [ObservableProperty] private int _relativeIndex;
    [ObservableProperty] private string _disease = string.Empty;
    [ObservableProperty] private string _onsetAge = string.Empty;
    [ObservableProperty] private bool _isGenetic;
    [ObservableProperty] private bool _isDeceased;
    [ObservableProperty] private string _deathAge = string.Empty;
    [ObservableProperty] private string _familyNotes = string.Empty;

    /// <summary>بارگذاری سابقه خانوادگی</summary>
    private async Task LoadFamilyHistoryAsync(long patientId)
    {
        var response = await _api.GetAsync<FamilyHistoryResponse>($"emr/family-history/{patientId}");
        FamilyRows = new ObservableCollection<FamilyRow>((response?.Data ?? new List<FamilyRowDto>())
            .Select(f => new FamilyRow(f.Id,
                RelativeOptions.ElementAtOrDefault(f.Relative - 1) ?? "سایر",
                f.Disease, f.Icd10Code, f.OnsetAge, f.IsGenetic, f.IsDeceased, f.DeathAge, f.Notes)));
    }

    /// <summary>ثبت سابقه خانوادگی جدید</summary>
    [RelayCommand]
    private async Task AddFamilyHistoryAsync()
    {
        if (SelectedPatient is null) return;
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Disease))
        {
            ErrorMessage = "عنوان بیماری الزامی است";
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync("emr/family-history", new
            {
                patientId = SelectedPatient.Id,
                relative = RelativeIndex + 1,
                disease = Disease.Trim(),
                icd10Code = (string?)null,
                onsetAge = ParseInt(OnsetAge),
                isGenetic = IsGenetic,
                isDeceased = IsDeceased,
                deathAge = ParseInt(DeathAge),
                notes = string.IsNullOrWhiteSpace(FamilyNotes) ? null : FamilyNotes.Trim()
            });

            if (ok)
            {
                _snackbar.Success("سابقه خانوادگی ثبت شد");
                Disease = OnsetAge = DeathAge = FamilyNotes = string.Empty;
                IsGenetic = IsDeceased = false;
                await LoadFamilyHistoryAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "ثبت ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>حذف سابقه خانوادگی — ردیف از CommandParameter</summary>
    [RelayCommand]
    private async Task DeleteFamilyHistoryAsync(FamilyRow? row)
    {
        if (SelectedPatient is null || row is null) return;
        IsBusy = true;
        try
        {
            if (await _api.DeleteAsync($"emr/family-history/{row.Id}"))
            {
                _snackbar.Success("سابقه خانوادگی حذف شد");
                await LoadFamilyHistoryAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "حذف ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══════════ تب ۳: طرح مراقبت مزمن ═══════════

    /// <summary>ردیف‌های طرح مراقبت</summary>
    [ObservableProperty]
    private ObservableCollection<CarePlanRow> _carePlanRows = new();

    /// <summary>ردیف انتخاب‌شده</summary>
    [ObservableProperty]
    private CarePlanRow? _selectedCarePlan;

    // ── فرم ثبت جدید ──
    [ObservableProperty] private string _diseaseTitle = string.Empty;
    [ObservableProperty] private string _careIcd10 = string.Empty;
    [ObservableProperty] private string _startShamsi = PersianDateHelper.ToShamsi(IranClock.Today);
    [ObservableProperty] private string _nextReviewShamsi = string.Empty;
    [ObservableProperty] private string _goals = string.Empty;
    [ObservableProperty] private string _actions = string.Empty;
    [ObservableProperty] private string _careTeam = string.Empty;
    [ObservableProperty] private string _careNotes = string.Empty;

    /// <summary>بارگذاری طرح‌های مراقبت بیمار</summary>
    private async Task LoadCarePlansAsync(long patientId)
    {
        var response = await _api.GetAsync<CarePlanResponse>($"emr/care-plans/{patientId}");
        CarePlanRows = new ObservableCollection<CarePlanRow>((response?.Data ?? new List<CarePlanRowDto>())
            .Select(c => new CarePlanRow(c.Id, c.DiseaseTitle, c.Icd10Code,
                StatusText(c.Status), c.Status, c.StartDate, c.NextReviewDate,
                PersianDateHelper.ToShamsi(c.StartDate.ToDateTime(TimeOnly.MinValue)),
                c.NextReviewDate is not null ? PersianDateHelper.ToShamsi(c.NextReviewDate.Value.ToDateTime(TimeOnly.MinValue)) : "—",
                c.Goals, c.Actions, c.CareTeam, c.Notes, c.IsReviewOverdue)));
    }

    /// <summary>ثبت طرح مراقبت جدید</summary>
    [RelayCommand]
    private async Task AddCarePlanAsync()
    {
        if (SelectedPatient is null) return;
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(DiseaseTitle))
        {
            ErrorMessage = "عنوان بیماری مزمن الزامی است";
            return;
        }

        var start = PersianDateHelper.ToGregorian(StartShamsi);
        if (start is null)
        {
            ErrorMessage = "تاریخ شروع شمسی معتبر نیست";
            return;
        }
        var nextReview = PersianDateHelper.ToGregorian(NextReviewShamsi);

        IsBusy = true;
        try
        {
            var ok = await _api.PostAsync("emr/care-plans", new
            {
                patientId = SelectedPatient.Id,
                diseaseTitle = DiseaseTitle.Trim(),
                icd10Code = string.IsNullOrWhiteSpace(CareIcd10) ? null : CareIcd10.Trim(),
                // چرخهٔ ۷۹: قالب‌بندی صریح با فرهنگ خنثی — مقاوم در برابر تقویم فرهنگ جاری
                startDate = start.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                nextReviewDate = nextReview?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                goals = EmptyToNull(Goals),
                actions = EmptyToNull(Actions),
                careTeam = EmptyToNull(CareTeam),
                notes = EmptyToNull(CareNotes)
            });

            if (ok)
            {
                _snackbar.Success("طرح مراقبت ثبت شد");
                DiseaseTitle = CareIcd10 = NextReviewShamsi = Goals = Actions = CareTeam = CareNotes = string.Empty;
                await LoadCarePlansAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "ثبت ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>تغییر وضعیت طرح انتخاب‌شده — کامشان کامل/متوقف/فعال</summary>
    [RelayCommand]
    private async Task ChangePlanStatusAsync(string? status)
    {
        if (SelectedPatient is null || SelectedCarePlan is null || string.IsNullOrEmpty(status)) return;
        IsBusy = true;
        try
        {
            var ok = await _api.PutAsync($"emr/care-plans/{SelectedCarePlan.Id}/status?status={status}", null);
            if (ok)
            {
                _snackbar.Success("وضعیت طرح به‌روزرسانی شد");
                await LoadCarePlansAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "تغییر وضعیت ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>حذف طرح مراقبت — ردیف از CommandParameter</summary>
    [RelayCommand]
    private async Task DeleteCarePlanAsync(CarePlanRow? plan)
    {
        if (SelectedPatient is null || plan is null) return;
        IsBusy = true;
        try
        {
            if (await _api.DeleteAsync($"emr/care-plans/{plan.Id}"))
            {
                _snackbar.Success("طرح مراقبت حذف شد");
                await LoadCarePlansAsync(SelectedPatient.Id);
            }
            else
            {
                ErrorMessage = _api.LastError ?? "حذف ناموفق بود";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ═══════════ ابزارهای کمکی ═══════════

    /// <summary>قالب z-score — «+۱/۲» یا «−۰/۸»</summary>
    private static string FormatZ(double? z)
        => z is null ? "—" : (z >= 0 ? "+" : "−") + Math.Abs(z.Value).ToString("0.#");

    /// <summary>برچسب وضعیت از z-score — با رنگ hex برای UI</summary>
    private static string StatusOf(double? z)
    {
        if (z is null) return "—";
        var (status, _, _) = WhoGrowthReference.Classify(z.Value, GrowthIndicator.WeightForAge);
        return status switch
        {
            GrowthStatus.Normal => "نرمال",
            GrowthStatus.Borderline => "مرز هشدار",
            GrowthStatus.Alert => "هشدار",
            _ => "بحرانی"
        };
    }

    /// <summary>متن فارسی وضعیت طرح</summary>
    private static string StatusText(int status) => status switch
    {
        1 => "فعال", 2 => "نیازمند بازبینی", 3 => "تکمیل‌شده", _ => "متوقف"
    };

    /// <summary>تجزیه عدد اعشاری فارسی/انگلیسی — تهی یعنی وارد نشده</summary>
    private static decimal? ParseDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim().Replace("٫", ".").Replace("،", ".");
        return decimal.TryParse(text, out var value) ? value : null;
    }

    /// <summary>تجزیه عدد صحیح — تهی یعنی وارد نشده</summary>
    private static int? ParseInt(string text)
        => int.TryParse(text?.Trim(), out var value) ? value : null;

    /// <summary>رشته تهی → null برای JSON</summary>
    private static string? EmptyToNull(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    // ═══════════ مدل‌های ردیف و پاسخ ═══════════

    /// <summary>گزینه بیمار در نتایج جستجو</summary>
    public sealed record PatientOption(long Id, string Title, string Subtitle);

    /// <summary>ردیف اندازه‌گیری رشد با z-scoreها</summary>
    public sealed class GrowthRow
    {
        /// <summary>شناسه</summary>
        public long Id { get; }

        /// <summary>تاریخ میلادی</summary>
        public DateOnly MeasuredAt { get; }

        /// <summary>سن ماهانه</summary>
        public int AgeMonths { get; }

        /// <summary>تاریخ شمسی</summary>
        public string DateShamsi { get; }

        /// <summary>وزن</summary>
        public decimal? WeightKg { get; }

        /// <summary>قد</summary>
        public decimal? HeightCm { get; }

        /// <summary>محیط سر</summary>
        public decimal? HeadCirc { get; }

        /// <summary>یادداشت</summary>
        public string? Notes { get; }

        /// <summary>z-score وزن</summary>
        public string ZWeight { get; }

        /// <summary>z-score قد</summary>
        public string ZHeight { get; }

        /// <summary>z-score سر</summary>
        public string ZHead { get; }

        /// <summary>وضعیت وزن</summary>
        public string StatusWeight { get; }

        /// <summary>وضعیت قد</summary>
        public string StatusHeight { get; }

        /// <summary>وضعیت سر</summary>
        public string StatusHead { get; }

        /// <summary>سازنده کامل</summary>
        public GrowthRow(long id, DateOnly measuredAt, int ageMonths, string dateShamsi,
            decimal? weight, decimal? height, decimal? head, string? notes,
            string zWeight, string zHeight, string zHead,
            string statusWeight, string statusHeight, string statusHead)
        {
            Id = id; MeasuredAt = measuredAt; AgeMonths = ageMonths; DateShamsi = dateShamsi;
            WeightKg = weight; HeightCm = height; HeadCirc = head; Notes = notes;
            ZWeight = zWeight; ZHeight = zHeight; ZHead = zHead;
            StatusWeight = statusWeight; StatusHeight = statusHeight; StatusHead = statusHead;
        }
    }

    /// <summary>ردیف سابقه خانوادگی</summary>
    public sealed class FamilyRow
    {
        /// <summary>سازنده کامل</summary>
        public FamilyRow(long id, string relative, string disease, string? icd10,
            int? onsetAge, bool isGenetic, bool isDeceased, int? deathAge, string? notes)
        {
            Id = id; Relative = relative; Disease = disease; Icd10 = icd10;
            OnsetAge = onsetAge; IsGenetic = isGenetic; IsDeceased = isDeceased;
            DeathAge = deathAge; Notes = notes;
        }

        /// <summary>شناسه</summary>
        public long Id { get; }
        /// <summary>نسبت خویشاوند</summary>
        public string Relative { get; }
        /// <summary>بیماری</summary>
        public string Disease { get; }
        /// <summary>کد ICD</summary>
        public string? Icd10 { get; }
        /// <summary>سن شروع</summary>
        public int? OnsetAge { get; }
        /// <summary>ژنتیک؟</summary>
        public bool IsGenetic { get; }
        /// <summary>فوت‌شده؟</summary>
        public bool IsDeceased { get; }
        /// <summary>سن فوت</summary>
        public int? DeathAge { get; }
        /// <summary>یادداشت</summary>
        public string? Notes { get; }

        /// <summary>نمایش ترکیبی — «پدر — دیابت نوع ۲ (ژنتیک)»</summary>
        public string Display => $"{Relative} — {Disease}" + (IsGenetic ? " (ارثی)" : string.Empty);
    }

    /// <summary>ردیف طرح مراقبت</summary>
    public sealed class CarePlanRow
    {
        /// <summary>سازنده کامل</summary>
        public CarePlanRow(long id, string diseaseTitle, string? icd10, string statusText,
            int status, DateOnly startDate, DateOnly? nextReviewDate,
            string startShamsi, string nextReviewShamsi, string? goals,
            string? actions, string? careTeam, string? notes, bool isReviewOverdue)
        {
            Id = id; DiseaseTitle = diseaseTitle; Icd10 = icd10; StatusText = statusText;
            Status = status; StartDate = startDate; NextReviewDate = nextReviewDate;
            StartShamsi = startShamsi; NextReviewShamsi = nextReviewShamsi;
            Goals = goals; Actions = actions; CareTeam = careTeam; Notes = notes;
            IsReviewOverdue = isReviewOverdue;
        }

        /// <summary>شناسه</summary>
        public long Id { get; }
        /// <summary>عنوان بیماری</summary>
        public string DiseaseTitle { get; }
        /// <summary>کد ICD</summary>
        public string? Icd10 { get; }
        /// <summary>وضعیت متن</summary>
        public string StatusText { get; }
        /// <summary>وضعیت عدد</summary>
        public int Status { get; }
        /// <summary>شروع میلادی</summary>
        public DateOnly StartDate { get; }
        /// <summary>بازبینی میلادی</summary>
        public DateOnly? NextReviewDate { get; }
        /// <summary>شروع شمسی</summary>
        public string StartShamsi { get; }
        /// <summary>بازبینی شمسی</summary>
        public string NextReviewShamsi { get; }
        /// <summary>اهداف</summary>
        public string? Goals { get; }
        /// <summary>اقدامات</summary>
        public string? Actions { get; }
        /// <summary>تیم مراقبت</summary>
        public string? CareTeam { get; }
        /// <summary>یادداشت</summary>
        public string? Notes { get; }
        /// <summary>موعد بازبینی گذشته؟</summary>
        public bool IsReviewOverdue { get; }
    }

    // ── مدل‌های پاسخ API ──

    private sealed class PatientsSearchResponse
    {
        public List<PatientSearchDto>? Data { get; set; }
    }

    private sealed class PatientSearchDto
    {
        public long Id { get; set; }
        public string MedicalRecordNo { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }

    private sealed class PatientDetailResponse
    {
        public PatientDetailData? Data { get; set; }
    }

    private sealed class PatientDetailData
    {
        public int Gender { get; set; }
        public string? BirthDate { get; set; }
        public string? BirthDateShamsi { get; set; }

        // چرخه بیست‌ویکم: فیلدهای کارت خلاصهٔ بیمار
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public int? Age { get; set; }
    }

    private sealed class GrowthResponse
    {
        public List<GrowthRowDto>? Data { get; set; }
    }

    private sealed class GrowthRowDto
    {
        public long Id { get; set; }
        public DateOnly MeasuredAt { get; set; }
        public int AgeMonths { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? HeadCircumferenceCm { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class FamilyHistoryResponse
    {
        public List<FamilyRowDto>? Data { get; set; }
    }

    private sealed class FamilyRowDto
    {
        public long Id { get; set; }
        public int Relative { get; set; }
        public string Disease { get; set; } = string.Empty;
        public string? Icd10Code { get; set; }
        public int? OnsetAge { get; set; }
        public bool IsGenetic { get; set; }
        public bool IsDeceased { get; set; }
        public int? DeathAge { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class CarePlanResponse
    {
        public List<CarePlanRowDto>? Data { get; set; }
    }

    private sealed class CarePlanRowDto
    {
        public long Id { get; set; }
        public string DiseaseTitle { get; set; } = string.Empty;
        public string? Icd10Code { get; set; }
        public int Status { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? NextReviewDate { get; set; }
        public string? Goals { get; set; }
        public string? Actions { get; set; }
        public string? CareTeam { get; set; }
        public string? Notes { get; set; }
        public bool IsReviewOverdue { get; set; }
    }
}
