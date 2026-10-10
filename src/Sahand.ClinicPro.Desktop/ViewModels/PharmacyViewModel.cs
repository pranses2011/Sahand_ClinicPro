using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف موجودی داروخانه
/// ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) ═══ نوع قلم هم برمی‌گردد تا تب موجودی
/// بتواند داروها و لوازم مصرفی را جدا فیلتر/برچسب کند
/// </summary>
public record InventoryRow(
    long Id,
    string MedicationName,
    string? BatchNumber,
    decimal Quantity,
    decimal ReorderLevel,
    string? ExpiryDate,
    bool IsLowStock,
    bool IsExpiringSoon,
    // نوع قلم — ۱ دارو، ۲ لوازم مصرفی (چرخهٔ ۳۹) — کامنت عادی (درس CS1587)
    int ItemKind = 1,
    // متن فارسی نوع — «دارو» یا «لوازم مصرفی»
    string KindText = "دارو")
{
    /// <summary>متن وضعیت موجودی</summary>
    public string LowStockText => IsLowStock ? "کمبود موجودی" : IsExpiringSoon ? "نزدیک انقضا" : "نرمال";

    /// <summary>نام نمایشی — سازگار با XAML گام U10</summary>
    public string Name => MedicationName;

    /// <summary>بچ/سری — به‌جای فرم در جدول موجودی</summary>
    public string? Form => BatchNumber;

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

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — فیلتر نوع قلم داروخانه ═══
    // «سرنگ و سرم و گاز استریل… خرید و انبار و قیمت‌های این لوازم هم باید باشه»
    // — نمایهٔ ۰: همهٔ اقلام | ۱: فقط داروها | ۲: فقط لوازم مصرفی

    /// <summary>نمایهٔ فیلتر نوع قلم — ۰ همه، ۱ دارو، ۲ لوازم مصرفی</summary>
    [ObservableProperty]
    private int _inventoryKindIndex;

    /// <summary>گزینه‌های فیلتر نوع قلم</summary>
    public string[] InventoryKindFilters { get; } =
    {
        "🗂️ همهٔ اقلام",
        "💊 فقط داروها",
        "🧰 فقط لوازم مصرفی"
    };

    /// <summary>تغییر فیلتر نوع → بارگذاری مجدد موجودی با پارامتر جدید</summary>
    partial void OnInventoryKindIndexChanged(int value) => _ = LoadAsync();

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

        // ═══ چرخهٔ ۳۹ — فیلتر نوع قلم به‌علاوهٔ فیلتر کمبود ═══ نمایهٔ ۰ = همه؛
        // ۱ = دارو؛ ۲ = لوازم مصرفی (پارامتر itemKind اندپوینت موجودی)
        var kindParam = InventoryKindIndex switch
        {
            1 => "&itemKind=1",
            2 => "&itemKind=2",
            _ => ""
        };
        var endpoint = "pharmacy/inventory?" + (LowStockOnly ? "lowStock=true&" : "") + kindParam.TrimStart('&');
        var response = await _api.GetAsync<InventoryResponse>(endpoint);

        if (response?.Data is not null)
        {
            Inventory = new ObservableCollection<InventoryRow>(response.Data.Select(i =>
                new InventoryRow(i.Id, i.MedicationName, i.BatchNumber, i.Quantity,
                    i.ReorderLevel, i.ExpiryDateShamsi, i.IsLowStock, i.IsExpiringSoon,
                    i.ItemKind, i.KindText)));
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
        var vm = new StockInFormViewModel(_api);
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
        var vm = new PharmacyItemFormViewModel(_api);
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

        var detail = await _api.GetAsync<DispenseDetail>($"prescriptions/{id}");
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

    /// <summary>DTO موجودی</summary>
    public sealed class InventoryDto
    {
        public long Id { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public decimal Quantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string? ExpiryDateShamsi { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpiringSoon { get; set; }

        // ═══ چرخهٔ ۳۹ — نوع قلم: ۱ دارو، ۲ لوازم مصرفی (سرنگ/سرم/گاز و…) ═══
        public int ItemKind { get; set; } = 1;
        public string KindText { get; set; } = "دارو";
    }

    /// <summary>جزئیات نسخه برای تحویل</summary>
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

    /// <summary>بارگذاری اقلام زیر نقطه سفارش</summary>
    [RelayCommand]
    private async Task LoadLowStockAsync()
    {
        var response = await _api.GetAsync<InventoryResponse>("pharmacy/inventory");
        if (response?.Data is null) return;
        LowStockItems = new ObservableCollection<InventoryRow>(
            response.Data.Where(i => i.IsLowStock)
                .Select(i => new InventoryRow(i.Id, i.MedicationName, i.BatchNumber, i.Quantity,
                    i.ReorderLevel, i.ExpiryDateShamsi, i.IsLowStock, i.IsExpiringSoon)));
    }
}
