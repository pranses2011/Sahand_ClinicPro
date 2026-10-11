using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف موجودی داروخانه — چرخهٔ ۴۳ «قلم‌محور»: هر ردیف یک دارو با جمع
/// موجودی همهٔ بچ‌ها، قیمت‌های آخرین خرید و نزدیک‌ترین انقضا؛ داروهای
/// کاتالوگِ بدون ورود کالا هم با موجودی صفر دیده می‌شوند (گزارش مالک:
/// «لیست داروها هم نمایش داده نمیشه» — فهرست قبلی بچ‌محور بود).
/// چرخهٔ ۴۳: داروخانه از این پس فقط دارو را نشان می‌دهد (گزارش مالک:
/// «از داروخانه هم حذف شود چون فقط دارو در اونجا ثبت میشه») — فیلد نوع
/// و فیلتر نوع حذف شدند؛ لوازم مصرفی صفحهٔ مستقل خودشان را دارند.
/// </summary>
public record InventoryRow(
    long Id,
    string MedicationName,
    decimal Quantity,
    decimal ReorderLevel,
    string? ExpiryDate,
    bool IsLowStock,
    bool IsExpiringSoon,
    // شمار بچ‌های ثبت‌شدهٔ این قلم — چرخهٔ ۴۳
    int BatchCount = 0)
{
    /// <summary>متن وضعیت موجودی</summary>
    public string LowStockText => IsLowStock ? "کمبود موجودی" : IsExpiringSoon ? "نزدیک انقضا" : "نرمال";

    /// <summary>نام نمایشی — سازگار با XAML گام U10</summary>
    public string Name => MedicationName;

    /// <summary>تاریخ انقضا شمسی — سازگار با XAML</summary>
    public string? ExpiryDateShamsi => ExpiryDate;

    /// <summary>پیشنهاد سفارش — دو برابر نقطه سفارش منهای موجودی</summary>
    public decimal SuggestedOrder => Math.Max(0, ReorderLevel * 2 - Quantity);
}

/// <summary>
/// ViewModel داروخانه — موجودی + ورود دارو + تحویل نسخه
/// </summary>
public partial class PharmacyViewModel : ViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "داروخانه";

    /// <summary>موجودی</summary>
    [ObservableProperty]
    private ObservableCollection<InventoryRow> _inventory = new();

    /// <summary>ردیف انتخاب‌شده</summary>
    [ObservableProperty]
    private InventoryRow? _selectedRow;

    /// <summary>فقط کالاهای کمبود</summary>
    [ObservableProperty]
    private bool _lowStockOnly;

    // ═══ چرخهٔ ۴۳ (گزارش مالک: «از داروخانه هم حذف شود چون فقط دارو در
    // اونجا ثبت میشه») — فیلتر نوع قلم حذف شد: تب موجودی داروخانه از این پس
    // فقط «داروها» را نشان می‌دهد (itemKind=1 ثابت در بارگذاری) و لوازم
    // مصرفی صفحهٔ مستقل خودشان را دارند. ═══

    /// <summary>شناسه نسخه برای تحویل</summary>
    [ObservableProperty]
    private string _prescriptionId = string.Empty;

    /// <summary>نام بیمار نسخه بارگذاری‌شده</summary>
    [ObservableProperty]
    private string _dispensePatient = string.Empty;

    /// <summary>اقلام نسخه بارگذاری‌شده</summary>
    public ObservableCollection<string> DispenseItems { get; } = new();

    /// <summary>شناسه نسخه بارگذاری‌شده</summary>
    private long? _loadedPrescriptionId;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public PharmacyViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    /// <summary>بارگذاری موجودی</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        // ═══ چرخهٔ ۴۳ — قلم‌محور + فقط داروها ═══ فهرست سرور در سطح «قلم» است
        // (قلم‌های بی‌بچ هم می‌آیند) و itemKind=1 یعنی فقط داروها (گزارش مالک)
        var endpoint = "pharmacy/inventory?" + (LowStockOnly ? "lowStock=true&" : "") + "itemKind=1";
        var response = await _api.GetAsync<InventoryResponse>(endpoint);

        if (response?.Data is not null)
        {
            Inventory = new ObservableCollection<InventoryRow>(response.Data.Select(i =>
                new InventoryRow(i.ItemId, i.MedicationName, i.TotalQuantity,
                    i.ReorderLevel, i.NearestExpiryShamsi, i.IsLowStock, i.IsExpiringSoon,
                    i.BatchCount)));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت موجودی ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>فرم ورود موجودی جدید</summary>
    [RelayCommand]
    private void StockIn()
    {
        var vm = new StockInFormViewModel(_api, isConsumableContext: false);
        var window = new StockInFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — افزودن قلم جدید انبار ═══
    // «خرید و انبار و قیمت‌های این لوازم هم باید باشه» — تا این چرخه کاتالوگ
    // اقلام فقط از بذر اولیه سرور می‌آمد و ساختن قلم جدید (سرنگ/سرم/گاز و…)
    // از خود برنامه ممکن نبود؛ این فرمان پنجرهٔ «قلم جدید» را می‌گشاید.

    /// <summary>
    /// افزودن قلم جدید انبار — دارو یا لوازم مصرفی؛ پس از ثبت موفق، فهرست
    /// موجودی تازه می‌شود تا قلم تازه (حتی بدون موجودی) در تب مناسب دیده شود
    /// </summary>
    [RelayCommand]
    private void AddItem()
    {
        var vm = new PharmacyItemFormViewModel(_api, isConsumableContext: false);
        var window = new PharmacyItemFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>بارگذاری نسخه برای تحویل</summary>
    [RelayCommand]
    private async Task LoadPrescriptionAsync()
    {
        ErrorMessage = string.Empty;
        DispenseItems.Clear();
        _loadedPrescriptionId = null;
        DispensePatient = string.Empty;

        if (!long.TryParse(PrescriptionId.Trim(), out var id) || id <= 0)
        {
            ErrorMessage = "شناسه نسخه را به‌صورت عدد وارد کنید";
            return;
        }

        // ═══ چرخهٔ ۴۸ — پاکت استاندارد ApiResponse باز شود (قانون هم‌کلاسی‌ها) ═══
        // نگاشت مستقیمِ پیشین یعنی PatientName/Items همیشه تهی — بارگذاری نسخه
        // «موفق» دیده می‌شد اما فهرست اقلام برای تحویل همیشه خالی می‌ماند.
        var envelope = await _api.GetAsync<DispenseDetailEnvelope>($"prescriptions/{id}");
        var detail = envelope?.Data;
        if (detail is null)
        {
            ErrorMessage = _api.LastError ?? "نسخه یافت نشد";
            return;
        }

        _loadedPrescriptionId = id;
        DispensePatient = detail.PatientName;
        foreach (var item in detail.Items ?? new List<DispenseItem>())
            DispenseItems.Add($"{item.MedicationName} × {item.Quantity} {item.Unit ?? string.Empty}");
    }

    /// <summary>تحویل نسخه — کاهش FEFO و هشدار کمبود</summary>
    [RelayCommand]
    private async Task DispenseAsync()
    {
        ErrorMessage = string.Empty;

        if (_loadedPrescriptionId is null)
        {
            ErrorMessage = "ابتدا نسخه را بارگذاری کنید";
            return;
        }

        IsBusy = true;
        var ok = await _api.PostAsync($"pharmacy/dispense/{_loadedPrescriptionId}", null);
        IsBusy = false;

        if (!ok)
        {
            ErrorMessage = _api.LastError ?? "تحویل نسخه ناموفق بود";
            return;
        }

        DispensePatient = string.Empty;
        DispenseItems.Clear();
        PrescriptionId = string.Empty;
        _loadedPrescriptionId = null;
        await LoadAsync();
    }

    /// <summary>پاسخ موجودی</summary>
    public sealed class InventoryResponse
    {
        public bool IsSuccess { get; set; }
        public List<InventoryDto>? Data { get; set; }
    }

    /// <summary>DTO موجودی — قلم‌محور (چرخهٔ ۴۳؛ هم‌شکل InventoryItemDto سرور)</summary>
    public sealed class InventoryDto
    {
        public long ItemId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string? NearestExpiryShamsi { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int ItemKind { get; set; } = 1;
        public string KindText { get; set; } = "دارو";
        public decimal SellPrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public int BatchCount { get; set; }
    }

    /// <summary>
    /// پاکت استاندارد پاسخ نسخه — چرخهٔ ۴۸: بدون این پوشش، اقلام نسخه هرگز
    /// بایند نمی‌شدند و تحویل‌دارو همیشه فهرست خالی می‌دید.
    /// </summary>
    public sealed class DispenseDetailEnvelope
    {
        public bool IsSuccess { get; set; }
        public DispenseDetail? Data { get; set; }
    }

    public sealed class DispenseDetail
    {
        public long Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<DispenseItem>? Items { get; set; }
    }

    /// <summary>قلم نسخه</summary>
    public sealed class DispenseItem
    {
        public string MedicationName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    /* ── اقلام زیر نقطه سفارش — تب سفارش خرید گام U10 ── */

    /// <summary>اقلام نیازمند سفارش</summary>
    [ObservableProperty]
    private ObservableCollection<InventoryRow> _lowStockItems = new();

    /// <summary>
    /// بارگذاری اقلام زیر نقطهٔ سفارش — چرخهٔ ۴۳: قلم‌محور و فقط داروها
    /// (لوازم مصرفی صفحهٔ مستقل خودشان را دارند؛ lowStock سمت سرور پالایش می‌کند)
    /// </summary>
    [RelayCommand]
    private async Task LoadLowStockAsync()
    {
        var response = await _api.GetAsync<InventoryResponse>("pharmacy/inventory?lowStock=true&itemKind=1");
        if (response?.Data is null) return;
        LowStockItems = new ObservableCollection<InventoryRow>(
            response.Data.Select(i => new InventoryRow(i.ItemId, i.MedicationName, i.TotalQuantity,
                    i.ReorderLevel, i.NearestExpiryShamsi, i.IsLowStock, i.IsExpiringSoon, i.BatchCount)));
    }
}
