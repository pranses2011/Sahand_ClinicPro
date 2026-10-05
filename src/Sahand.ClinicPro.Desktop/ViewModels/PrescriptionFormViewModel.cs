using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>گزینه دارو در جستجو — برای فرم ورود کالای داروخانه</summary>
public record MedicationOption(long Id, string Name, string? Form, string? Strength);

/// <summary>داروی یافته‌شده در جستجو</summary>
public record MedicationResult(
    long Id,
    string NameFa,
    string NameEn,
    string? Form,
    string? Strength,
    string? IrcCode,
    string? Manufacturer,
    string Country);

/// <summary>قلم نسخه قابل ویرایش</summary>
public partial class PrescriptionItemVM : ObservableObject
{
    public long MedicationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;

    [ObservableProperty] private string? _dosage = "۱ قرص";
    [ObservableProperty] private string? _frequency = "روزی ۲ بار";
    [ObservableProperty] private string? _duration = "۷ روز";
    [ObservableProperty] private string? _route = "خوراکی";
    [ObservableProperty] private decimal _quantity = 14;
    [ObservableProperty] private string? _unit = "عدد";
    [ObservableProperty] private string? _instructions;
    [ObservableProperty] private bool _isSos;
}

/// <summary>تداخل نمایشی</summary>
public record InteractionRow(
    string Title,
    int Severity,
    string SeverityText,
    string SeverityColor,
    string? Mechanism,
    string? Recommendation,
    bool IsAllergyConflict);

/// <summary>تمپلیت نمایشی</summary>
public record TemplateOption(long Id, string Name, string? Category, string TemplateJson);

/// <summary>
/// ViewModel نسخه‌نویسی — گام U7 طبق پرامپت UI بخش ۲ (فرم ۱۲):
/// جستجوی دارو با Debounce + کارت اقلام با دوز/تعداد/مدت/مسیر + تعداد کل خودکار +
/// پنل تداخل ۴سطحی خودکار + ارسال الکترونیک با کد رهگیری + تمپلیت‌ها
/// </summary>
public partial class PrescriptionFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private System.Windows.Threading.DispatcherTimer? _debounce;

    /// <summary>عنوان</summary>
    [ObservableProperty]
    private string _formTitle = "💊 نسخه‌نویسی";

    /// <summary>پزشکان</summary>
    public ObservableCollection<DoctorOption> Doctors { get; }

    /// <summary>نمایه پزشک</summary>
    [ObservableProperty]
    private int _doctorIndex = -1;

    /// <summary>جستجوی بیمار</summary>
    [ObservableProperty]
    private string _patientSearch = string.Empty;

    /// <summary>بیمار انتخاب‌شده</summary>
    [ObservableProperty]
    private QuickPatient? _patient;

    /// <summary>متن جستجوی دارو</summary>
    [ObservableProperty]
    private string _medicationSearch = string.Empty;

    /// <summary>نتایج جستجوی دارو</summary>
    [ObservableProperty]
    private ObservableCollection<MedicationResult> _medicationResults = new();

    /// <summary>اقلام نسخه</summary>
    public ObservableCollection<PrescriptionItemVM> Items { get; } = new();

    /// <summary>تداخلات شناسایی‌شده</summary>
    public ObservableCollection<InteractionRow> Interactions { get; } = new();

    /// <summary>وجود تداخل شدید</summary>
    [ObservableProperty]
    private bool _hasSevereInteraction;

    /// <summary>تداخل با آلرژی بیمار</summary>
    [ObservableProperty]
    private bool _hasAllergyConflict;

    /// <summary>هیچ تداخلی نیست — پیام سبز</summary>
    [ObservableProperty]
    private bool _noInteractions = true;

    /// <summary>نمایه نوع نسخه</summary>
    [ObservableProperty]
    private int _typeIndex;

    /// <summary>توضیحات پزشک</summary>
    [ObservableProperty]
    private string? _doctorNotes;

    /// <summary>شناسه نسخه ذخیره‌شده</summary>
    [ObservableProperty]
    private long? _savedPrescriptionId;

    /// <summary>کد رهگیری الکترونیک</summary>
    [ObservableProperty]
    private string? _electronicTrackingCode;

    /// <summary>در حال ارسال الکترونیک</summary>
    [ObservableProperty]
    private bool _isSubmitting;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>گزینه‌های دوز</summary>
    public List<string> Dosages { get; } = new()
        { "۱ قرص", "۲ قرص", "نصف قرص", "۱ عدد", "۵ سی‌سی", "۱۰ سی‌سی", "۱ قاشق مرباخوری" };

    /// <summary>گزینه‌های تعداد مصرف</summary>
    public List<string> Frequencies { get; } = new()
        { "روزی یکبار", "روزی ۲ بار", "روزی ۳ بار", "روزی ۴ بار", "هر ۸ ساعت", "هر ۱۲ ساعت", "هر ۶ ساعت",
          "صبح‌ها", "شب‌ها", "صبح و شب", "قبل از خواب", "هفته‌ای یکبار", "هفته‌ای ۲ بار", "در صورت نیاز" };

    /// <summary>گزینه‌های مدت</summary>
    public List<string> Durations { get; } = new()
        { "۳ روز", "۵ روز", "۷ روز", "۱۰ روز", "۱۴ روز", "۲۱ روز", "۳۰ روز", "۶۰ روز", "۹۰ روز", "مصرف مداوم" };

    /// <summary>گزینه‌های مسیر</summary>
    public List<string> Routes { get; } = new()
        { "خوراکی", "زیرزبانی", "تزریق عضلانی", "تزریق وریدی", "استنشاقی", "چشمی", "گوشی", "پوستی", "مقعدی", "واژینال", "زیرجلدی", "اسپری بینی" };

    /// <summary>واحدها</summary>
    public List<string> Units { get; } = new() { "عدد", "ویال", "آمپول", "قوطی", "شیشه", "تیوب", "بسته" };

    /// <summary>تمپلیت‌ها</summary>
    public ObservableCollection<TemplateOption> Templates { get; } = new();

    /// <summary>نمایه تمپلیت انتخابی</summary>
    [ObservableProperty]
    private int _templateIndex = -1;

    /// <summary>نام تمپلیت جدید</summary>
    [ObservableProperty]
    private string? _newTemplateName;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده</summary>
    public PrescriptionFormViewModel(IApiClient api, ISnackbarService snackbar,
        ObservableCollection<DoctorOption> doctors)
    {
        _api = api;
        _snackbar = snackbar;
        Doctors = doctors;
        _ = LoadTemplatesAsync();
    }

    /// <summary>بارگذاری تمپلیت‌ها</summary>
    private async Task LoadTemplatesAsync()
    {
        var response = await _api.GetAsync<TemplatesResponse>("prescriptions/templates");
        if (response?.Data is null) return;
        Templates.Clear();
        foreach (var t in response.Data)
            Templates.Add(new TemplateOption(t.Id, t.Name, t.Category, t.TemplateJson));
    }

    /* ═══════════ جستجوی بیمار ═══════════ */

    /// <summary>جستجوی بیمار با Enter</summary>
    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (PatientSearch.Trim().Length < 4) { _snackbar.Error("کد ملی یا موبایل بیمار را وارد کنید"); return; }
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(PatientSearch.Trim())}");
        var p = response?.Data?.FirstOrDefault();
        if (p is null) { _snackbar.Error("بیمار یافت نشد"); return; }
        Patient = new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode,
            p.Mobile, p.InsuranceName, p.Allergies, null);
        FormTitle = $"💊 نسخه‌نویسی — {Patient.FullName}";
        _snackbar.Success($"بیمار: {Patient.FullName}");
    }

    /* ═══════════ جستجوی دارو با Debounce ۳۰۰ms ═══════════ */

    /// <summary>تغییر متن جستجو</summary>
    partial void OnMedicationSearchChanged(string value)
    {
        _debounce?.Stop();
        if (value.Length < 2) { MedicationResults.Clear(); return; }
        _debounce = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounce.Tick += async (_, _) =>
        {
            _debounce.Stop();
            var response = await _api.GetAsync<MedicationsResponse>(
                $"prescriptions/medications/search?q={Uri.EscapeDataString(value)}&limit=15");
            MedicationResults.Clear();
            if (response?.Data is null) return;
            foreach (var m in response.Data)
                MedicationResults.Add(new MedicationResult(m.Id, m.NameFa, m.NameEn,
                    m.Form, m.Strength, m.IrcCode, m.Manufacturer, m.Country));
        };
        _debounce.Start();
    }

    /// <summary>افزودن دارو به نسخه</summary>
    [RelayCommand]
    private void AddMedication(object? param)
    {
        if (param is not MedicationResult med) return;
        Items.Add(new PrescriptionItemVM
        {
            MedicationId = med.Id,
            Name = med.NameFa,
            Subtitle = $"{med.Form ?? ""} {med.Strength ?? ""} — {med.Manufacturer ?? med.Country}".Trim(' ', '—')
        });
        MedicationSearch = string.Empty;
        MedicationResults.Clear();
        RecalculateQuantity(Items[^1]);
        _ = CheckInteractionsAsync();
    }

    /// <summary>حذف قلم</summary>
    [RelayCommand]
    private void RemoveItem(object? param)
    {
        if (param is not PrescriptionItemVM item) return;
        Items.Remove(item);
        _ = CheckInteractionsAsync();
    }

    /// <summary>محاسبه خودکار تعداد کل — دوز × تعداد × مدت</summary>
    private static void RecalculateQuantity(PrescriptionItemVM item)
    {
        var dosesPerDay = item.Frequency switch
        {
            "روزی یکبار" or "صبح‌ها" or "شب‌ها" or "قبل از خواب" => 1,
            "روزی ۲ بار" or "هر ۱۲ ساعت" or "صبح و شب" => 2,
            "روزی ۳ بار" or "هر ۸ ساعت" => 3,
            "روزی ۴ بار" or "هر ۶ ساعت" => 4,
            "هفته‌ای یکبار" => 0.14,
            "هفته‌ای ۲ بار" => 0.28,
            _ => 1
        };
        var days = item.Duration switch
        {
            "۳ روز" => 3, "۵ روز" => 5, "۷ روز" => 7, "۱۰ روز" => 10, "۱۴ روز" => 14,
            "۲۱ روز" => 21, "۳۰ روز" => 30, "۶۰ روز" => 60, "۹۰ روز" => 90,
            "مصرف مداوم" => 30,
            _ => 7
        };
        var unitsPerDose = item.Dosage switch
        {
            "۲ قرص" or "۲ عدد" => 2,
            "نصف قرص" => 0.5,
            _ => 1
        };
        item.Quantity = Math.Max(1, (decimal)Math.Ceiling(dosesPerDay * days * unitsPerDose));
    }

    /* ═══════════ بررسی تداخل خودکار ═══════════ */

    /// <summary>بررسی تداخل — بعد از هر افزودن/حذف خودکار</summary>
    private async Task CheckInteractionsAsync()
    {
        if (Patient is null || Items.Count == 0)
        {
            Interactions.Clear();
            NoInteractions = true;
            HasSevereInteraction = false;
            HasAllergyConflict = false;
            return;
        }

        var response = await _api.PostAsync<CheckResponse>("prescriptions/check-interactions", new
        {
            patientId = Patient.Id,
            medicationIds = Items.Select(i => i.MedicationId).ToList()
        });

        Interactions.Clear();
        if (response?.Data is null)
        {
            NoInteractions = Items.Count == 0;
            return;
        }

        HasSevereInteraction = response.Data.HasSevere;
        HasAllergyConflict = response.Data.HasAllergyConflict;
        NoInteractions = response.Data.Interactions is { Count: 0 };

        foreach (var i in response.Data.Interactions ?? new List<InteractionItem>())
            Interactions.Add(new InteractionRow(
                $"{i.Medication1Name} × {i.Medication2Name}",
                i.Severity, i.SeverityText, i.SeverityColor,
                i.Mechanism, i.Recommendation, i.IsAllergyConflict));
    }

    /* ═══════════ تمپلیت‌ها ═══════════ */

    /// <summary>بارگذاری تمپلیت — پر کردن اقلام</summary>
    [RelayCommand]
    private void LoadTemplate()
    {
        if (TemplateIndex < 0 || TemplateIndex >= Templates.Count) return;
        var template = Templates[TemplateIndex];
        try
        {
            var items = JsonSerializer.Deserialize<List<TemplateItem>>(template.TemplateJson);
            if (items is null) return;
            Items.Clear();
            foreach (var t in items)
                Items.Add(new PrescriptionItemVM
                {
                    MedicationId = t.MedicationId,
                    Name = t.Name,
                    Subtitle = t.Subtitle,
                    Dosage = t.Dosage,
                    Frequency = t.Frequency,
                    Duration = t.Duration,
                    Route = t.Route,
                    Quantity = t.Quantity,
                    Unit = t.Unit,
                    Instructions = t.Instructions,
                    IsSos = t.IsSos
                });
            _snackbar.Success($"تمپلیت «{template.Name}» بارگذاری شد");
            _ = CheckInteractionsAsync();
        }
        catch
        {
            _snackbar.Error("قالب تمپلیت نامعتبر است");
        }
    }

    /// <summary>ذخیره نسخه فعلی به‌عنوان تمپلیت</summary>
    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        if (Items.Count == 0) { _snackbar.Error("نسخه خالی است"); return; }
        if (string.IsNullOrWhiteSpace(NewTemplateName)) { _snackbar.Error("نام تمپلیت را وارد کنید"); return; }

        var json = JsonSerializer.Serialize(Items.Select(i => new
        {
            medicationId = i.MedicationId, name = i.Name, subtitle = i.Subtitle,
            dosage = i.Dosage, frequency = i.Frequency, duration = i.Duration,
            route = i.Route, quantity = i.Quantity, unit = i.Unit,
            instructions = i.Instructions, isSos = i.IsSos
        }));
        var ok = await _api.PostAsync("prescriptions/templates", new
        {
            name = NewTemplateName.Trim(), category = (string?)null, templateJson = json
        });
        if (ok)
        {
            _snackbar.Success("تمپلیت ذخیره شد");
            NewTemplateName = null;
            await LoadTemplatesAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ذخیره تمپلیت ناموفق بود");
    }

    /* ═══════════ ذخیره و ارسال ═══════════ */

    /// <summary>ذخیره نسخه</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        var error = Validate();
        if (error is not null) { ErrorMessage = error; return; }

        IsBusy = true;
        var body = new
        {
            patientId = Patient!.Id,
            doctorId = Doctors[DoctorIndex].Id,
            visitId = (long?)null,
            type = 1,
            isElectronic = ElectronicTrackingCode is not null,
            notes = DoctorNotes,
            items = Items.Select(i => new
            {
                medicationId = i.MedicationId,
                orderIndex = Items.IndexOf(i) + 1,
                dosage = i.Dosage,
                frequency = i.Frequency,
                duration = i.Duration,
                route = i.Route,
                quantity = i.Quantity,
                unit = i.Unit,
                instructions = i.Instructions,
                isSos = i.IsSos
            }).ToList()
        };
        var response = await _api.PostAsync<SaveResponse>("prescriptions", body);
        IsBusy = false;

        if (response?.Data is null or 0)
        {
            ErrorMessage = _api.LastError ?? "ذخیره نسخه ناموفق بود";
            return;
        }

        SavedPrescriptionId = response.Data;
        _snackbar.Success($"نسخه ذخیره شد — شناسه {response.Data}");
    }

    /// <summary>اعتبارسنجی</summary>
    private string? Validate()
    {
        if (Patient is null) return "ابتدا بیمار را انتخاب کنید";
        if (DoctorIndex < 0) return "پزشک معالج را انتخاب کنید";
        if (Items.Count == 0) return "حداقل یک دارو به نسخه اضافه کنید";
        if (HasAllergyConflict) return "تداخل با آلرژی بیمار — ابتدا داروی متعارض را حذف یا تأیید کنید";
        return null;
    }

    /// <summary>ارسال به سامانه نسخه الکترونیک</summary>
    [RelayCommand]
    private async Task SubmitElectronicAsync()
    {
        if (SavedPrescriptionId is null)
        {
            await SaveAsync();
            if (SavedPrescriptionId is null) return;
        }

        IsSubmitting = true;
        var response = await _api.PostAsync<SubmitResponse>(
            $"prescriptions/{SavedPrescriptionId}/submit-electronic", new { });
        IsSubmitting = false;

        if (response?.Data is null)
        {
            _snackbar.Error(_api.LastError ?? "سامانه بیمه پاسخ نداد");
            return;
        }

        ElectronicTrackingCode = response.Data.TrackingCode;
        _snackbar.Success($"✅ نسخه ارسال شد — کد رهگیری: {ElectronicTrackingCode}");
    }

    /// <summary>کپی کد رهگیری</summary>
    [RelayCommand]
    private void CopyTrackingCode()
    {
        if (ElectronicTrackingCode is null) return;
        try
        {
            System.Windows.Clipboard.SetText(ElectronicTrackingCode);
            _snackbar.Success("کد رهگیری در حافظه کپی شد");
        }
        catch { _snackbar.Error("کپی ناموفق بود"); }
    }

    /// <summary>ارسال SMS کد رهگیری به بیمار</summary>
    [RelayCommand]
    private async Task SendTrackingSmsAsync()
    {
        if (Patient is null || ElectronicTrackingCode is null) return;
        var ok = await _api.PostAsync("sms/send", new
        {
            recipient = Patient.Mobile,
            message = $"{Patient.FullName} عزیز، نسخه شما با کد رهگیری {ElectronicTrackingCode} در سامانه ثبت شد."
        });
        if (ok) _snackbar.Success("پیامک کد رهگیری ارسال شد");
        else _snackbar.Error(_api.LastError ?? "ارسال پیامک ناموفق بود");
    }

    /// <summary>چاپ نسخه</summary>
    [RelayCommand]
    private void Print()
    {
        _snackbar.Info("پیش‌نمایش چاپ در گام U19 (قالب‌های چاپ) متصل می‌شود");
    }

    /// <summary>بستن</summary>
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /* ─── پاسخ‌های API ─── */

    public sealed class MedicationsResponse
    {
        public bool IsSuccess { get; set; }
        public List<MedicationItem>? Data { get; set; }
    }

    public sealed class MedicationItem
    {
        public long Id { get; set; }
        public string NameFa { get; set; } = string.Empty;

        /// <summary>نام نمایشی — برای سازگاری با فرم ورود کالای داروخانه</summary>
        public string Name => NameFa;
        public string NameEn { get; set; } = string.Empty;
        public string? Form { get; set; }
        public string? Strength { get; set; }
        public string? IrcCode { get; set; }
        public string? Manufacturer { get; set; }
        public string Country { get; set; } = "داخلی";
    }

    public sealed class CheckResponse
    {
        public bool IsSuccess { get; set; }
        public CheckData? Data { get; set; }
    }

    public sealed class CheckData
    {
        public List<InteractionItem>? Interactions { get; set; }
        public bool HasSevere { get; set; }
        public bool HasAllergyConflict { get; set; }
    }

    public sealed class InteractionItem
    {
        public string Medication1Name { get; set; } = string.Empty;
        public string Medication2Name { get; set; } = string.Empty;
        public int Severity { get; set; }
        public string SeverityText { get; set; } = string.Empty;
        public string SeverityColor { get; set; } = "#ECEFF1";
        public string? Mechanism { get; set; }
        public string? Recommendation { get; set; }
        public bool IsAllergyConflict { get; set; }
    }

    public sealed class TemplatesResponse
    {
        public bool IsSuccess { get; set; }
        public List<TemplateItemApi>? Data { get; set; }
    }

    public sealed class TemplateItemApi
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string TemplateJson { get; set; } = string.Empty;
    }

    public sealed class SaveResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }

    public sealed class SubmitResponse
    {
        public bool IsSuccess { get; set; }
        public SubmitData? Data { get; set; }
    }

    public sealed class SubmitData
    {
        public long PrescriptionId { get; set; }
        public string? TrackingCode { get; set; }
    }

    /// <summary>آیتم تمپلیت در JSON</summary>
    public sealed class TemplateItem
    {
        public long MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Route { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Instructions { get; set; }
        public bool IsSos { get; set; }
    }
}
