using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel ثبت نتیجه آزمایش — قلم‌به‌قلم با ذخیره هر قلم
/// </summary>
public partial class LabResultViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long _orderId;
    private readonly ISnackbarService? _snackbar;
    /// <summary>آخرین جزئیات بارگذاری‌شده — برای چاپ برگهٔ نتیجه (چرخهٔ ۶۷)</summary>
    private LabOrderDetail? _lastDetail;

    /// <summary>عنوان</summary>
    [ObservableProperty]
    private string _header = "ثبت نتیجه آزمایش";

    /// <summary>اقلام</summary>
    [ObservableProperty]
    private ObservableCollection<LabItemRow> _items = new();

    /// <summary>قلم انتخاب‌شده</summary>
    [ObservableProperty]
    private LabItemRow? _selectedItem;

    /// <summary>پیام وضعیت</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال کار</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>چرخهٔ ۶۷: آیا دست‌کم یک قلم نتیجهٔ ثبت‌شده دارد؟ (نمایش دکمهٔ چاپ)</summary>
    [ObservableProperty]
    private bool _hasAnyResult;

    /// <summary>نام کامل بیمار — زمینهٔ فرم اختصاصی ثبت نتیجه (چرخهٔ ۶۹)</summary>
    [ObservableProperty]
    private string _patientContext = string.Empty;

    /// <summary>سن بیمار به متن فارسی — بازهٔ مرجع کودک/بزرگسال (چرخهٔ ۶۹ / F-28)</summary>
    [ObservableProperty]
    private string _patientAgeText = string.Empty;

    /// <summary>شماره درخواست — زمینهٔ فرم اختصاصی ثبت نتیجه (چرخهٔ ۶۹)</summary>
    [ObservableProperty]
    private string _orderNumberText = string.Empty;

    /// <summary>شناسه درخواست — برای باز کردن فرم اختصاصی قلم (چرخهٔ ۶۹)</summary>
    public long OrderId => _orderId;

    /// <summary>
    /// دسترسی فقط‌خواندنی به کلاینت API — برای ساختن ViewModel فرم اختصاصی
    /// قلم از سمت View میزبان (چرخهٔ ۶۹؛ تزریق سازنده باقی می‌ماند منبع اصلی).
    /// </summary>
    public IApiClient Api => _api;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// رویداد درخواست باز کردن فرم اختصاصی ثبت نتیجه — چرخهٔ ۶۹ (گزارش مالک:
    /// «برای ثبت نتیجه باید روی آزمایش دابل کلیک کنیم تا در یک فرم دیگه که
    /// مختص اون آزمایش هست، اطلاعات آزمایش رو نشون بده و نتیجه رو اونجا ثبت
    /// بکنیم»)؛ View میزبان، پنجرهٔ LabTestResultEntryWindow را با زمینهٔ همین
    /// قلم باز می‌کند و پس از ذخیرهٔ موفق، کل لیست تازه می‌شود.
    /// </summary>
    public event EventHandler<LabItemRow>? ItemEntryRequested;

    /// <summary>سازنده با کلاینت API، شناسه درخواست و توست اختیاری (چرخهٔ ۶۹)</summary>
    public LabResultViewModel(IApiClient api, long orderId, ISnackbarService? snackbar = null)
    {
        _api = api;
        _orderId = orderId;
        _snackbar = snackbar;
        _ = LoadAsync();
    }

    /// <summary>
    /// بارگذاری اقلام درخواست — عمومی از چرخهٔ ۶۹: پس از ذخیره در فرم اختصاصی
    /// قلم، پنجرهٔ مادر همین متد را صدا می‌زند تا رنگ ردیف‌ها و بج‌ها تازه شوند.
    /// </summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        // ═══ چرخهٔ ۴۸ (گزارش مالک: «ثبت نتیجه آزمایش ایراد دارد و در لیست هیچ
        //      قلمی نمی‌آورد تا انتخاب کنیم») ═══
        // پاسخ سرور همیشه در پاکت استاندارد ApiResponse است: {isSuccess, message,
        // data}. پیش‌تر پاکت مستقیماً به LabOrderDetail نگاشت می‌شد ⇒ هیچ ویژگی‌ای
        // بایند نمی‌شد، «detail» تهی نمی‌شد (شیء خالی ساخته می‌شد) و Items همیشه
        // تهی/خالی می‌ماند — جدول اقلام همیشه خالی و ثبت نتیجه ناممکن.
        var response = await _api.GetAsync<LabOrderDetailEnvelope>($"lab/orders/{_orderId}");
        var detail = response?.Data;

        if (detail is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت درخواست ناموفق بود";
        }
        else
        {
            Header = $"{detail.OrderNumber ?? _orderId.ToString()} — {detail.PatientName}";
            // زمینهٔ فرم اختصاصی ثبت نتیجه (چرخهٔ ۶۹) — نام/سن بیمار و شناسه درخواست
            PatientContext = detail.PatientName;
            PatientAgeText = detail.PatientAge is { } age ? $"{age} ساله" : "";
            OrderNumberText = detail.OrderNumber ?? _orderId.ToString();
            Items = new ObservableCollection<LabItemRow>((detail.Items ?? new()).Select(i => new LabItemRow
            {
                Id = i.Id,
                TestName = i.TestName,
                Unit = i.Unit,
                NormalRange = i.NormalRange,
                PatientRangeText = i.PatientRangeText,
                AbnormalFlag = i.AbnormalFlag,
                StatusText = ItemStatusText(i.Status),
                Result = i.Result,
                ResultNumeric = i.ResultNumeric?.ToString() ?? string.Empty,
                Comment = i.Comment
            }));
            // چرخهٔ ۶۷ — دکمهٔ چاپ فقط وقتی معنا دارد که نتیجه‌ای ثبت شده باشد
            HasAnyResult = Items.Any(i => !string.IsNullOrWhiteSpace(i.Result) || !string.IsNullOrWhiteSpace(i.ResultNumeric));
            // نگه‌داشتن جزئیات خام برای سند چاپ (LabResultPrinter همان شکل سروری را می‌خواهد)
            _lastDetail = detail;
        }

        IsBusy = false;
    }

    /// <summary>متن فارسی وضعیت قلم</summary>
    private static string ItemStatusText(int status) => status switch
    {
        1 => "در انتظار",
        2 => "در جریان",
        3 => "نتیجه‌دار",
        4 => "تأییدشده",
        _ => "نامشخص"
    };

    /// <summary>ذخیره نتیجه قلم انتخاب‌شده</summary>
    [RelayCommand]
    private async Task SaveItemAsync()
    {
        if (SelectedItem is null) return;

        ErrorMessage = string.Empty;
        StatusMessage = string.Empty;

        decimal? resultNumeric = null;
        if (!string.IsNullOrWhiteSpace(SelectedItem.ResultNumeric))
        {
            if (!decimal.TryParse(SelectedItem.ResultNumeric.Trim(), out var parsed))
            {
                ErrorMessage = "نتیجه عددی معتبر نیست";
                return;
            }
            resultNumeric = parsed;
        }

        IsBusy = true;
        var body = new
        {
            itemId = SelectedItem.Id,
            result = string.IsNullOrWhiteSpace(SelectedItem.Result) ? null : SelectedItem.Result.Trim(),
            resultNumeric,
            comment = string.IsNullOrWhiteSpace(SelectedItem.Comment) ? null : SelectedItem.Comment.Trim()
        };

        var ok = await _api.PutAsync("lab/results", body);
        IsBusy = false;

        if (!ok)
            ErrorMessage = _api.LastError ?? "ثبت نتیجه ناموفق بود";
        else
        {
            StatusMessage = $"نتیجه «{SelectedItem.TestName}» ثبت شد";
            await LoadAsync();
        }
    }

    /// <summary>بستن</summary>
    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// باز کردن فرم اختصاصی ثبت نتیجه برای قلم انتخاب‌شده — چرخهٔ ۶۹ (گزارش
    /// مالک: دابل‌کلیک روی آزمایش ← فرم مختص همان آزمایش با اطلاعات کامل و
    /// ثبت نتیجه در همان‌جا)؛ View میزبان به این رویداد گوش می‌دهد.
    /// </summary>
    [RelayCommand]
    private void OpenItemEntry(LabItemRow? row)
    {
        if (row is null)
            row = SelectedItem; // مسیر دکمه (بدون پارامتر) — قلم انتخاب‌شدهٔ جدول
        if (row is null)
        {
            _snackbar?.Warning("ابتدا یک آزمایش از جدول انتخاب کنید");
            return;
        }

        ItemEntryRequested?.Invoke(this, row);
    }

    /// <summary>
    /// چاپ برگهٔ نتیجهٔ همین درخواست — چرخهٔ ۶۷ (گزارش مالک: «امکان چاپ هم
    /// داشته باشه"). سند فارسی از LabResultPrinter ساخته و از دروازهٔ PrintFlow
    /// (پیش‌نمایش اختیاری طبق تنظیمات چاپ) به چاپگر می‌رود.
    /// </summary>
    [RelayCommand]
    private void PrintSheet()
    {
        if (_lastDetail is null)
        {
            ErrorMessage = "جزئیات درخواست هنوز بارگذاری نشده است";
            return;
        }
        Services.LabResultPrinter.Print(_lastDetail, _orderId);
    }

    /// <summary>
    /// پاکت استاندارد پاسخ جزئیات درخواست — چرخهٔ ۴۸:
    /// همهٔ پاسخ‌های سرور در ApiResponse (isSuccess/message/data) پیچیده می‌شوند؛
    /// بدون این پوشش، ویژگی‌ها هرگز بایند نمی‌شدند و جدول اقلام خالی می‌ماند.
    /// </summary>
    public sealed class LabOrderDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public LabOrderDetail? Data { get; set; }
    }

    /// <summary>جزئیات درخواست منطبق بر سرور</summary>
    public sealed class LabOrderDetail
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? ClinicalInfo { get; set; }
        public List<LabOrderItemDto>? Items { get; set; }

        // ═══ چرخهٔ ۶۹ (فاز ۳ / F-28) — زمینهٔ فرم اختصاصی ثبت نتیجه ═══
        public long PatientId { get; set; }
        public int? PatientAge { get; set; }
    }

    /// <summary>قلم درخواست</summary>
    public sealed class LabOrderItemDto
    {
        public long Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? NormalRange { get; set; }
        public int Status { get; set; }
        public string? Result { get; set; }
        public decimal? ResultNumeric { get; set; }
        public bool IsAbnormal { get; set; }
        public string? Comment { get; set; }

        // ═══ چرخهٔ ۶۹ (فاز ۳ / F-28) — فلگ تفسیر و بازهٔ مخصوص بیمار ═══
        public string? AbnormalFlag { get; set; }
        public string? PatientRangeText { get; set; }
    }
}