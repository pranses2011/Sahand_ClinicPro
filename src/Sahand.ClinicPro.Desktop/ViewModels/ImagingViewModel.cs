using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel صفحهٔ مستقل «تصویربرداری» — چرخهٔ چهل‌وسوم (گزارش مالک)
/// </summary>
/// <remarks>
/// «برای هر کدوم یک صفحه مستقل طراحی بکن تا با همدیگه تداخلی نداشته باشن» —
/// درخواست تصویربرداری + آپلود/نمایش فایل‌های DICOM این تب از پاراکلینیکِ
/// تب‌دار جدا و به صفحهٔ اختصاصی منتقل شد؛ نام پراپرتی‌ها عیناً حفظ شد تا
/// قالب XAML بدون تغییر بایند شود. جستجوی بیمار از هلپر مشترک PatientQuickSearch.
/// </remarks>
public partial class ImagingViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه در هدر و منو</summary>
    public override string Title => "تصویربرداری";

    /// <summary>انواع مودالیتهٔ تصویربرداری</summary>
    public ObservableCollection<string> ImagingTypes { get; } =
        new() { "سونوگرافی", "رادیوگرافی", "MRI", "CT Scan", "ماموگرافی", "آنژیوگرافی" };

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

    /// <summary>ناحیه‌های بدن — گزینه‌های پیشنهادی</summary>
    public string[] BodyRegions { get; } =
        { "سر و گردن", "قفسه سینه", "شکم و لگن", "ستون فقرات", "اندام فوقانی", "اندام تحتانی", "مفصل شانه", "مفصل زانو", "تروماتیک" };

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public ImagingViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        // ═══ چرخهٔ ۷۹ (گزارش مالک: «توست درخواست تصویربرداری همراه همه‌چیز
        // نمایش داده می‌شود») — بارگذاریِ خودکارِ سازنده حذف شد ═══
        // این ViewModel تک‌نمونه است و در سازندهٔ MainWindowViewModel (شروع
        // برنامه) ساخته می‌شود؛ لودِ fire-and-forget قبلی یعنی «هر بار اجرای
        // برنامه» یک توست آبی «امروز درخواستی ثبت نشده» روی صفحهٔ داشبورد
        // می‌پرید — بی‌ربط به کنش کاربر. اکنون داده در «اولین ناوبری» به
        // صفحهٔ تصویربرداری با فرمان استاندارد RefreshCommand (بی‌صدا) و یا
        // با دکمهٔ صریح کاربر (با بازخورد) بارگذاری می‌شود.
    }

    /// <summary>ثبت درخواست تصویربرداری</summary>
    [RelayCommand]
    private async Task SaveImagingOrderAsync()
    {
        if (ImagingPatient is null) { _snackbar.Error("ابتدا بیمار را جستجو کنید"); return; }
        // ═══ چرخهٔ ۴۸ — سازگاری کامل با قرارداد سرور CreateImagingOrderDto ═══
        // بدنهٔ قبلی نام‌های ناهم‌جنس داشت (modalityType/region/clinicalInformation/
        // isUrgent) که با هیچ ویژگی‌ای از DTO سرور (modality/bodyPart/priority/
        // clinicalInfo) بایند نمی‌شد ⇒ مودالیته صفر و ناحیهٔ بدن تهی؛ اعتبارسنج
        // «ناحیهٔ بدن الزامی است» هر درخواست را رد می‌کرد و فرم تصویربرداری
        // هرگز ذخیره نمی‌شد (گزارش مالک: قابلیت‌های ارجاع پیاده نشده‌اند).
        var ok = await _api.PostAsync("imaging/orders", new
        {
            patientId = ImagingPatient.Id,
            referringDoctorId = (long?)null,
            // نمایهٔ انتخابگر مودالیته — همان ترتیب enum سرور: ۱ سونوگرافی، ۲ رادیوگرافی، …
            modality = ImagingTypeIndex + 1,
            bodyPart = BodyRegions[Math.Clamp(System.Array.IndexOf(BodyRegions, ImagingRegion ?? "سر و گردن"), 0, 8)],
            // اولویت سرور: ۱ اورژانس، ۲ بالا، ۳ عادی — چک‌باکس «فوری» یعنی اورژانس
            priority = ImagingUrgent ? 1 : 3,
            clinicalInfo = ImagingClinicalInfo
        });
        if (ok)
        {
            _snackbar.Success("درخواست تصویربرداری ثبت شد");
            ImagingClinicalInfo = null;
            ImagingUrgent = false;
            await LoadImagingOrdersAsync();
        }
        else _snackbar.Error(_api.LastError ?? "ثبت درخواست ناموفق بود");
    }

    /// <summary>جستجوی بیمار تصویربرداری</summary>
    [RelayCommand]
    private async Task SearchImagingPatientAsync()
    {
        var patient = await Helpers.PatientQuickSearch.FindAsync(_api, _snackbar, ImagingPatientSearch);
        if (patient is not null) ImagingPatient = patient;
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

        /// <summary>شناسهٔ درخواست</summary>
        public long Id { get; }

        /// <summary>عنوان نمایشی</summary>
        public string Display => $"#{Id} — {PatientName} ({Modality})";

        /// <summary>نام بیمار</summary>
        public string PatientName { get; }

        /// <summary>مودالیته</summary>
        public string Modality { get; }

        /// <summary>ناحیهٔ بدن</summary>
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

    /// <summary>
    /// بارگذاری فهرست درخواست‌های تصویر امروز
    /// </summary>
    /// <param name="notifyWhenEmpty">
    /// چرخهٔ ۷۹: فقط کنش «صریح کاربر» (دکمهٔ درخواست‌های امروز) حق دارد پیام
    /// «چیزی ثبت نشده» بدهد؛ بارگذاری‌های خودکار/ناوبری همیشه بی‌صدا هستند.
    /// </param>
    private async Task LoadImagingOrdersCoreAsync(bool notifyWhenEmpty)
    {
        var response = await _api.GetAsync<ImagingOrdersResponse>("imaging/orders");
        ImagingOrders = new ObservableCollection<ImagingOrderRow>((response?.Data ?? new List<ImagingOrderDto>())
            .Select(o => new ImagingOrderRow(o.Id, o.PatientName, o.Modality, o.BodyPart)));
        if (notifyWhenEmpty && ImagingOrders.Count == 0)
            _snackbar.Info("درخواست تصویربرداری برای امروز ثبت نشده است");
    }

    /// <summary>
    /// نوسازی بی‌صدای فهرست درخواست‌ها — فرمان استاندارد RefreshCommand؛
    /// پوستهٔ اصلی پس از هر ناوبری به این صفحه آن را اجرا می‌کند
    /// (TryRefreshCurrentPageData) تا داده همیشه تازه و بدون توست مزاحم باشد.
    /// </summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadImagingOrdersCoreAsync(notifyWhenEmpty: false);

    /// <summary>بارگذاری با کنش کاربر — بازخورد «خالی بودن» فقط این‌جا مجاز است</summary>
    [RelayCommand]
    private Task LoadImagingOrdersAsync() => LoadImagingOrdersCoreAsync(notifyWhenEmpty: true);

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

        // ذخیره در پوشهٔ موقت و گشودن در نمایشگر
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
