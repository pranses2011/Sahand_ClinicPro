using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>دندان چارت FDI</summary>
public partial class ToothVM : ObservableObject
{
    public int Number { get; init; }
    public string Quadrant => Number / 10 == 1 || Number / 10 == 2 ? "بالا" : "پایین";
    public string ToolTip => $"دندان {Number} — فک {Quadrant}";
    [ObservableProperty] private int _condition; // 0 سالم 1 پوسیدگی 2 ترمیم 3 درمان 4 روکش 5 کشیده 6 ایمپلنت
    public string Color => Condition switch
    {
        1 => "#E53935", 2 => "#FDD835", 3 => "#43A047", 4 => "#1E88E5", 5 => "#37474F", 6 => "#8E24AA",
        _ => "#FFFFFF"
    };
}

/// <summary>ردیف طرح درمان دندان</summary>
public record DentalPlanRow(int Tooth, string Procedure, decimal Price, string StatusText, string StatusColor);

/// <summary>
/// ViewModel پاراکلینیک — گام U12 طبق فرم‌های ۲۰ تا ۲۳:
/// ۴ تب: دندانپزشکی چارت FDI + چشم OD/OS + فیزیوتراپی + تصویربرداری
/// </summary>
public partial class ParaclinicalViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان</summary>
    public override string Title => "پاراکلینیک تخصصی";

    /* ── تب دندانپزشکی ── */
    public ObservableCollection<ToothVM> UpperTeeth { get; } = new();
    public ObservableCollection<ToothVM> LowerTeeth { get; } = new();
    [ObservableProperty] private ToothVM? _selectedTooth;
    [ObservableProperty] private int _toothConditionIndex;
    [ObservableProperty] private string? _dentalNotes;
    public ObservableCollection<DentalPlanRow> DentalPlan { get; } = new();

    /// <summary>برچسب وضعیت دندان</summary>
    public string[] ToothConditions { get; } = { "سالم", "پوسیدگی", "ترمیم‌شده", "درمان‌شده", "روکش", "کشیده‌شده", "ایمپلنت" };

    /* ── تب چشم ── */
    [ObservableProperty] private string? _odSphere;
    [ObservableProperty] private string? _odCylinder;
    [ObservableProperty] private string? _odAxis;
    [ObservableProperty] private string? _odAdd;
    [ObservableProperty] private int _odVisionIndex = 2;
    [ObservableProperty] private string? _odIop;
    [ObservableProperty] private string? _osSphere;
    [ObservableProperty] private string? _osCylinder;
    [ObservableProperty] private string? _osAxis;
    [ObservableProperty] private string? _osAdd;
    [ObservableProperty] private int _osVisionIndex = 2;
    [ObservableProperty] private string? _osIop;
    [ObservableProperty] private string? _pd;
    [ObservableProperty] private int _colorVisionIndex;
    [ObservableProperty] private string? _eyeFindings;

    /// <summary>هشدار گلوکوم چشم راست</summary>
    public string? OdIopWarning => int.TryParse(OdIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;
    /// <summary>هشدار گلوکوم چشم چپ</summary>
    public string? OsIopWarning => int.TryParse(OsIop, out var v) && v > 21 ? "⚠️ احتمال گلوکوم — IOP بالای ۲۱" : null;

    partial void OnOdIopChanged(string? value) => OnPropertyChanged(nameof(OdIopWarning));
    partial void OnOsIopChanged(string? value) => OnPropertyChanged(nameof(OsIopWarning));

    /// <summary>گزینه‌های دید</summary>
    public string[] VisionOptions { get; } =
        { "20/10", "20/15", "20/20", "20/25", "20/30", "20/40", "20/50", "20/60", "20/80", "20/100", "20/200", "CF", "HM", "LP", "NLP" };

    /// <summary>گزینه‌های دید رنگی</summary>
    public string[] ColorVisionOptions { get; } = { "طبیعی", "کاهش نسبی", "کانونی", "آتروفی عصبی" };

    /* ── تب فیزیوتراپی ── */
    public ObservableCollection<string> Techniques { get; } = new()
        { "الکتروتراپی (TENS)", "اولتراسوند", "لیزر درمانی", "تمرینات درمانی", "ماساژ", "هیدروتراپی", "طب سوزنی" };
    [ObservableProperty] private string? _physioOtherTechnique;
    [ObservableProperty] private int _painScore;
    [ObservableProperty] private string? _physioNotes;
    [ObservableProperty] private string _physioPatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _physioPatient;
    public ObservableCollection<string> SelectedTechniques { get; } = new();

    /// <summary>رنگ نمره درد</summary>
    public string PainColor => PainScore <= 3 ? "#43A047" : PainScore <= 6 ? "#FDD835" : "#E53935";

    partial void OnPainScoreChanged(int value) => OnPropertyChanged(nameof(PainColor));

    /* ── تب تصویربرداری ── */
    public ObservableCollection<string> ImagingTypes { get; } =
        new() { "سونوگرافی", "رادیوگرافی", "MRI", "CT Scan", "ماموگرافی", "آنژیوگرافی" };
    [ObservableProperty] private int _imagingTypeIndex;
    [ObservableProperty] private string? _imagingRegion;
    [ObservableProperty] private string? _imagingClinicalInfo;
    [ObservableProperty] private bool _imagingUrgent;
    [ObservableProperty] private string _imagingPatientSearch = string.Empty;
    [ObservableProperty] private QuickPatient? _imagingPatient;

    /// <summary>ناحیه‌های بدن</summary>
    public string[] BodyRegions { get; } =
        { "سر و گردن", "قفسه سینه", "شکم و لگن", "ستون فقرات", "اندام فوقانی", "اندام تحتانی", "مفصل شانه", "مفصل زانو", "تروماتیک" };

    /// <summary>سازنده</summary>
    public ParaclinicalViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        // چارت FDI — فک بالا ۱۱-۱۸ و ۲۱-۲۸ / فک پایین ۳۱-۳۸ و ۴۱-۴۸
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

    /// <summary>جستجوی بیمار فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SearchPhysioPatientAsync() => await SearchPatientGenericAsync(t => PhysioPatient = t, PhysioPatientSearch);

    /// <summary>جستجوی بیمار تصویربرداری</summary>
    [RelayCommand]
    private async Task SearchImagingPatientAsync() => await SearchPatientGenericAsync(t => ImagingPatient = t, ImagingPatientSearch);

    /// <summary>جستجوی مشترک بیمار</summary>
    private async Task SearchPatientGenericAsync(Action<QuickPatient?> assign, string term)
    {
        if (term.Trim().Length < 4) { _snackbar.Error("کد ملی یا موبایل را وارد کنید"); return; }
        var response = await _api.GetAsync<PatientFormViewModel.SearchResponse>(
            $"patients/search?term={Uri.EscapeDataString(term.Trim())}");
        var p = response?.Data?.FirstOrDefault();
        if (p is null) { _snackbar.Error("بیمار یافت نشد"); return; }
        assign(new QuickPatient(p.Id, $"{p.FirstName} {p.LastName}", p.NationalCode, p.Mobile, p.InsuranceName, p.Allergies, null));
        _snackbar.Success($"بیمار: {p.FirstName} {p.LastName}");
    }

    /// <summary>ذخیره جلسه فیزیوتراپی</summary>
    [RelayCommand]
    private async Task SavePhysioSessionAsync()
    {
        if (PhysioPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        var ok = await _api.PostAsync("physiotherapy/sessions", new
        {
            patientId = PhysioPatient.Id,
            sessionNumber = 1,
            sessionDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToShamsi(System.DateTime.Now),
            techniques = string.Join("، ", SelectedTechniques.Concat(string.IsNullOrWhiteSpace(PhysioOtherTechnique) ? System.Linq.Enumerable.Empty<string>() : new[] { PhysioOtherTechnique.Trim() })),
            painScore = PainScore,
            notes = PhysioNotes
        });
        if (ok)
        {
            _snackbar.Success("جلسه فیزیوتراپی ثبت شد");
            PhysioNotes = null;
            PhysioOtherTechnique = null;
            PainScore = 0;
        }
        else _snackbar.Error(_api.LastError ?? "ثبت جلسه ناموفق بود");
    }

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [RelayCommand]
    private async Task SaveImagingOrderAsync()
    {
        if (ImagingPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        var ok = await _api.PostAsync("imaging/orders", new
        {
            patientId = ImagingPatient.Id,
            modalityType = ImagingTypeIndex + 1,
            region = BodyRegions[Math.Clamp(System.Array.IndexOf(BodyRegions, ImagingRegion ?? "سر و گردن"), 0, 8)],
            clinicalInformation = ImagingClinicalInfo,
            isUrgent = ImagingUrgent
        });
        if (ok)
        {
            _snackbar.Success("درخواست تصویربرداری ثبت شد");
            ImagingClinicalInfo = null;
            ImagingUrgent = false;
        }
        else _snackbar.Error(_api.LastError ?? "ثبت درخواست ناموفق بود");
    }

    /// <summary>ذخیره معاینه چشم</summary>
    [RelayCommand]
    private async Task SaveEyeExamAsync()
    {
        if (PhysioPatient is null && ImagingPatient is null)
        { _snackbar.Error("ابتدا بیمار را در یکی از تب‌ها جستجو کنید"); return; }
        var patientId = PhysioPatient?.Id ?? ImagingPatient!.Id;
        var ok = await _api.PostAsync("eye/exams", new
        {
            patientId,
            odSphere = OdSphere, odCylinder = OdCylinder, odAxis = OdAxis, odAdd = OdAdd,
            osSphere = OsSphere, osCylinder = OsCylinder, osAxis = OsAxis, osAdd = OsAdd,
            iopOd = OdIop, iopOs = OsIop,
            pupillaryDistance = Pd,
            findings = EyeFindings
        });
        if (ok) _snackbar.Success("معاینه چشم ثبت شد");
        else _snackbar.Error(_api.LastError ?? "ثبت معاینه ناموفق بود");
    }

    /* ── بخش فایل‌های DICOM — گام R4 ── */

    /// <summary>ردیف درخواست تصویر امروز</summary>
    public sealed class ImagingOrderRow
    {
        /// <summary>سازنده</summary>
        public ImagingOrderRow(long id, string patientName, string modality, string? bodyPart)
        {
            Id = id; PatientName = patientName; Modality = modality; BodyPart = bodyPart;
        }

        /// <summary>شناسه درخواست</summary>
        public long Id { get; }

        /// <summary>عنوان نمایشی</summary>
        public string Display => $"#{Id} — {PatientName} ({Modality})";

        /// <summary>نام بیمار</summary>
        public string PatientName { get; }

        /// <summary>مودالیته</summary>
        public string Modality { get; }

        /// <summary>ناحیه بدن</summary>
        public string? BodyPart { get; }
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

    /// <summary>درخواست‌های تصویر امروز — برای اتصال فایل</summary>
    [ObservableProperty]
    private ObservableCollection<ImagingOrderRow> _imagingOrders = new();

    /// <summary>درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedImagingOrder))]
    private ImagingOrderRow? _selectedImagingOrder;

    /// <summary>آیا درخواستی انتخاب شده</summary>
    public bool HasSelectedImagingOrder => SelectedImagingOrder is not null;

    /// <summary>فایل‌های درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    private ObservableCollection<ImagingFileRow> _imagingFiles = new();

    /// <summary>فایل انتخاب‌شده</summary>
    [ObservableProperty]
    private ImagingFileRow? _selectedImagingFile;

    /// <summary>بارگذاری فهرست درخواست‌های تصویر امروز</summary>
    [RelayCommand]
    private async Task LoadImagingOrdersAsync()
    {
        var response = await _api.GetAsync<ImagingOrdersResponse>("imaging/orders");
        ImagingOrders = new ObservableCollection<ImagingOrderRow>((response?.Data ?? new List<ImagingOrderDto>())
            .Select(o => new ImagingOrderRow(o.Id, o.PatientName, o.Modality, o.BodyPart)));
        if (ImagingOrders.Count == 0)
            _snackbar.Info("درخواست تصویربرداری برای امروز ثبت نشده است");
    }

    /// <summary>بازخورد انتخاب درخواست — فهرست فایل‌های آن</summary>
    partial void OnSelectedImagingOrderChanged(ImagingOrderRow? value)
    {
        if (value is null) return;
        _ = LoadImagingFilesAsync();
    }

    /// <summary>بارگذاری فایل‌های درخواست انتخاب‌شده</summary>
    private async Task LoadImagingFilesAsync()
    {
        if (SelectedImagingOrder is null) return;
        var response = await _api.GetAsync<ImagingFilesResponse>(
            $"imaging/files?orderId={SelectedImagingOrder.Id}");
        ImagingFiles = new ObservableCollection<ImagingFileRow>((response?.Data ?? new List<ImagingFileDto>())
            .Select(f => new ImagingFileRow(f.Id, f.FileName, f.IsDicom, f.FileSize, f.UploadedAt)));
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

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "انتخاب فایل تصویر یا DICOM",
            Filter = "فایل‌های تصویری (*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp)|*.dcm;*.dicom;*.jpg;*.jpeg;*.png;*.bmp|همه فایل‌ها (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var bytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
        var uploadResponse = await _api.PostFileAsync<UploadAck>("imaging/files",
            System.IO.Path.GetFileName(dialog.FileName), bytes,
            new Dictionary<string, string> { ["orderId"] = SelectedImagingOrder.Id.ToString() });

        if (uploadResponse?.IsSuccess == true)
        {
            _snackbar.Success(uploadResponse.Message ?? "فایل بارگذاری شد");
            await LoadImagingFilesAsync();
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

        var bytes = await _api.DownloadFileAsync($"imaging/files/{SelectedImagingFile.Id}/content");
        if (bytes is null || bytes.Length == 0)
        {
            _snackbar.Error(_api.LastError ?? "دریافت فایل ناموفق بود");
            return;
        }

        // ذخیره در پوشه موقت و گشودن در نمایشگر
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"sahand-dicom-{SelectedImagingFile.Id}.dcm");
        await System.IO.File.WriteAllBytesAsync(tempPath, bytes);

        var viewer = new Views.DicomViewerWindow(tempPath)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        };
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

    // ── مدل‌های پاسخ DICOM/فایل ──

    private sealed class ImagingOrdersResponse
    {
        public List<ImagingOrderDto>? Data { get; set; }
    }

    private sealed class ImagingOrderDto
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Modality { get; set; } = string.Empty;
        public string? BodyPart { get; set; }
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
