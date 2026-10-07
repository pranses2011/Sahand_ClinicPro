using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>قلم نسخه در نمایش جزئیات</summary>
public record PrescriptionItemView(
    string MedicationName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    decimal Quantity,
    string? Unit,
    string? Instructions);

/// <summary>
/// ViewModel جزئیات نسخه — نمایش فقط‌خواندنی اقلام
/// </summary>
public partial class PrescriptionDetailViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long _prescriptionId;

    /// <summary>عنوان پنجره</summary>
    [ObservableProperty]
    private string _header = "جزئیات نسخه";

    /// <summary>اقلام نسخه</summary>
    [ObservableProperty]
    private ObservableCollection<PrescriptionItemView> _items = new();

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال بارگذاری</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>سازنده با کلاینت API و شناسه نسخه</summary>
    public PrescriptionDetailViewModel(IApiClient api, long prescriptionId)
    {
        _api = api;
        _prescriptionId = prescriptionId;
        _ = LoadAsync();
    }

    /// <summary>
    /// بارگذاری جزئیات — چرخه بیست‌ویکم (گزارش پانزدهم مالک):
    /// «جزئیات نسخه‌های ذخیره‌شده را نشان نمی‌دهد و همه نسخه‌ها خالی دیده می‌شوند» —
    /// پاسخ سرور در پاکت استاندارد ApiResponse (isSuccess/data) می‌آید ولی اینجا
    /// مستقیم روی ریشهٔ پاسخ نگاشت می‌شد؛ در نتیجه همهٔ فیلدها تهی می‌ماند.
    /// اکنون از پاکت خوانده می‌شود.
    /// </summary>
    private async Task LoadAsync()
    {
        IsBusy = true;
        var envelope = await _api.GetAsync<PrescriptionDetailEnvelope>($"prescriptions/{_prescriptionId}");
        var detail = envelope?.Data;

        if (detail is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت جزئیات نسخه ناموفق بود";
        }
        else
        {
            Header = $"نسخه {detail.PrescriptionCode ?? _prescriptionId.ToString()} — {detail.PatientName}";
            Items = new ObservableCollection<PrescriptionItemView>(
                (detail.Items ?? new()).Select(i => new PrescriptionItemView(
                    i.MedicationName, i.Dosage, i.Frequency, i.Duration,
                    i.Quantity, i.Unit, i.Instructions)));
        }

        IsBusy = false;
    }

    /// <summary>بستن پنجره</summary>
    public event EventHandler? CloseRequested;

    /// <summary>فرمان بستن</summary>
    public void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاکت استاندارد پاسخ جزئیات نسخه — چرخه بیست‌ویکم</summary>
    public sealed class PrescriptionDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public PrescriptionDetail? Data { get; set; }
    }

    /// <summary>جزئیات نسخه منطبق بر سرور</summary>
    public sealed class PrescriptionDetail
    {
        public long Id { get; set; }
        public string? PrescriptionCode { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? Notes { get; set; }
        public List<PrescriptionItem>? Items { get; set; }
    }

    /// <summary>قلم نسخه</summary>
    public sealed class PrescriptionItem
    {
        public long MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Instructions { get; set; }
    }
}