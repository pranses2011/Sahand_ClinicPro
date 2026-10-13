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

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده با کلاینت API و شناسه درخواست</summary>
    public LabResultViewModel(IApiClient api, long orderId)
    {
        _api = api;
        _orderId = orderId;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری اقلام درخواست</summary>
    private async Task LoadAsync()
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
            Items = new ObservableCollection<LabItemRow>((detail.Items ?? new()).Select(i => new LabItemRow
            {
                Id = i.Id,
                TestName = i.TestName,
                Unit = i.Unit,
                NormalRange = i.NormalRange,
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
    }
}