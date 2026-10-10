using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف بچ قلم مصرفی — یک ورود کالا از انبار (قلم + بچ + موجودی + قیمت‌ها)
/// </summary>
/// <param name="Id">شناسهٔ ردیف انبار</param>
/// <param name="Name">نام قلم (فارسی یا ژنریک)</param>
/// <param name="BatchNumber">شماره بچ — تهی یعنی بدون بچ</param>
/// <param name="Quantity">موجودی باقی‌ماندهٔ این بچ</param>
/// <param name="ReorderLevel">نقطه سفارش مجدد</param>
/// <param name="ExpiryShamsi">تاریخ انقضا شمسی — تهی یعنی بدون انقضا</param>
/// <param name="SellPrice">قیمت فروش هر واحد این بچ (ریال/تومان طبق واحد سرور)</param>
/// <param name="PurchasePrice">قیمت خرید هر واحد این بچ</param>
/// <param name="IsLowStock">موجودی زیر نقطه سفارش است؟</param>
/// <param name="IsExpiringSoon">انقضا در ۹۰ روز آینده است؟</param>
public record ConsumableBatchRow(
    long Id, string Name, string? BatchNumber, decimal Quantity, decimal ReorderLevel,
    string? ExpiryShamsi, decimal SellPrice, decimal PurchasePrice,
    bool IsLowStock, bool IsExpiringSoon)
{
    /// <summary>متن وضعیت بچ — همان واژگان تب موجودی داروخانه</summary>
    public string StatusText => IsLowStock ? "کمبود موجودی" : IsExpiringSoon ? "نزدیک انقضا" : "نرمال";

    /// <summary>ارزش فروش این بچ (موجودی × قیمت فروش) — برای جمع ارزش انبار</summary>
    public decimal StockValue => Quantity * SellPrice;
}

/// <summary>
/// ViewModel صفحهٔ «لوازم مصرفی درمانگاه» — چرخهٔ چهل‌ویکم (گزارش مالک)
/// </summary>
/// <remarks>
/// «احتمال داره یک درمانگاه ماژول داروخانه رو نداشته باشه و اونموقع نمیتونه
/// لوازم مصرفی رو تعریف و استفاده بکنه» — این صفحه نقطهٔ مدیریتیِ مستقلِ
/// لوازم مصرفی است: تعریف قلم (سرنگ/سرم/گاز/…)، ورود کالا با بچ و قیمت، و
/// پایش موجودی/کمبود/انقضا — همه روی اندپوینت‌های api/v1/consumables که
/// خارج از گیت ماژول لایسنس داروخانه‌اند. داروخانه‌دارانِ دارای ماژول هم
/// همان داده را از تب موجودی داروخانه می‌بینند (یک انبار مشترک، دو منظر).
/// دکمه‌های «قلم جدید/ورود کالا» فقط برای نقش‌های مدیریتی (Admin/Manager)
/// نمایان‌اند — همان گیت سمت سرور، این‌بار در لایهٔ تجربهٔ کاربری.
/// </remarks>
public partial class ConsumablesViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;
    private readonly ITokenManager _tokens;

    /// <summary>عنوان صفحه در هدر</summary>
    public override string Title => "لوازم مصرفی درمانگاه";

    /// <summary>
    /// آیا کاربر می‌تواند قلم تعریف و کالا وارد کند؟ — Admin/Manager؛
    /// همان مجوزی که سرور روی POST consumables/items و consumables/inventory
    /// اعمال می‌کند، اینجا فقط برای پنهان‌کردن دکمه‌ها بازتابی شده است.
    /// </summary>
    public bool CanManage =>
        _tokens.IsAdmin || _tokens.Roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);

    /// <summary>بچ‌های اقلام مصرفی — هر ردیف یک ورود کالا</summary>
    [ObservableProperty]
    private ObservableCollection<ConsumableBatchRow> _batches = new();

    /// <summary>جستجوی قلم — پالایش زندهٔ سمت کلاینت روی نام</summary>
    [ObservableProperty]
    private string? _search;

    /// <summary>فقط بچ‌های زیر نقطه سفارش</summary>
    [ObservableProperty]
    private bool _lowStockOnly;

    // ─── چیپ‌های خلاصه — از کل دادهٔ بارگذاری‌شده محاسبه می‌شوند ───

    /// <summary>شمار اقلام تعریف‌شدهٔ مصرفی (بچ‌دار و بی‌موجودی با هم)</summary>
    [ObservableProperty]
    private int _itemsCount;

    /// <summary>شمار بچ‌های زیر نقطه سفارش</summary>
    [ObservableProperty]
    private int _lowStockCount;

    /// <summary>شمار بچ‌های نزدیک انقضا (۹۰ روز)</summary>
    [ObservableProperty]
    private int _expiringCount;

    /// <summary>ارزش فروش کل انبار مصرفی — قالب‌بندی‌شده با واحد پول</summary>
    [ObservableProperty]
    private string _totalValueText = "0";

    /// <summary>آخرین خطای صفحه — درون‌صفحه‌ای نمایش می‌یابد</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public ConsumablesViewModel(IApiClient api, ISnackbarService snackbar, ITokenManager tokens)
    {
        _api = api;
        _snackbar = snackbar;
        _tokens = tokens;
        _ = LoadAsync();
    }

    /// <summary>
    /// بارگذاری بچ‌های اقلام مصرفی از GET consumables/inventory —
    /// نوع «مصرفی» سمت سرور قفل شده است و دارو هرگز در این فهرست نمی‌آید.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var endpoint = "consumables/inventory" + (LowStockOnly ? "?lowStock=true" : "");
            var response = await _api.GetAsync<InventoryResponse>(endpoint);
            if (response?.Data is null)
            {
                ErrorMessage = _api.LastError ?? "دریافت فهرست اقلام مصرفی ناموفق بود";
                _snackbar.Error(ErrorMessage);
                return;
            }

            Batches = new ObservableCollection<ConsumableBatchRow>(response.Data
                .Select(MapRow));

            RefreshSummary();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>نگاشت DTO سرور به ردیف نمایشی</summary>
    private static ConsumableBatchRow MapRow(InventoryDto dto) => new(
        dto.Id, dto.MedicationName, dto.BatchNumber, dto.Quantity, dto.ReorderLevel,
        dto.ExpiryDateShamsi, dto.SellPrice, dto.PurchasePrice,
        dto.IsLowStock, dto.IsExpiringSoon);

    /// <summary>
    /// به‌روزرسانی چیپ‌های خلاصه — اقلام یکتا (تجمیع بچ‌ها به قلم)، کمبود،
    /// انقضا و ارزش کل انبار؛ همیشه از کل فهرست (بی‌توجه به جستجو) تا
    /// چیپ‌ها با تایپ کاربر نپرند.
    /// </summary>
    private void RefreshSummary()
    {
        ItemsCount = Batches.Select(b => b.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        LowStockCount = Batches.Count(b => b.IsLowStock);
        ExpiringCount = Batches.Count(b => b.IsExpiringSoon);
        TotalValueText = Money.Format(Batches.Sum(b => b.StockValue));
    }

    /// <summary>
    /// پالایش زندهٔ فهرست بر اساس جستجو — نام قلم/بچ؛ بدون رفت‌وبرگشت به
    /// سرور (۲۰۰ ردیف سقف سرور است و پالایش کلایندی فوری است)؛ با هر تایپ،
    /// اتصال VisibleBatches از نو ارزیابی می‌شود.
    /// </summary>
    partial void OnSearchChanged(string? value)
    {
        OnPropertyChanged(nameof(VisibleBatches));
    }

    /// <summary>
    /// فهرست نمایان پس از جستجو — DataGrid به همین می‌بایند؛ تهی/خالی یعنی
    /// همهٔ ردیف‌ها.
    /// </summary>
    public IEnumerable<ConsumableBatchRow> VisibleBatches
    {
        get
        {
            var term = Search?.Trim();
            if (string.IsNullOrWhiteSpace(term))
                return Batches;
            return Batches.Where(b =>
                b.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (b.BatchNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
    }

    /// <summary>تغییر سوییچ کمبود → بارگذاری مجدد با پارامتر سرور</summary>
    partial void OnLowStockOnlyChanged(bool value) => _ = LoadAsync();

    // ═════════════════ دستورات مدیریتی — فقط نقش‌های Admin/Manager ═════════════════

    /// <summary>
    /// تعریف قلم مصرفی جدید — همان فرم مشترک چرخهٔ ۳۹ با پیش‌فرضِ «لوازم
    /// مصرفی»؛ پس از ثبت موفق، فهرست تازه می‌شود.
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

    /// <summary>
    /// ورود کالا (خرید) — فرم مشترک با پیش‌فرض فیلتر نوع «فقط لوازم مصرفی»
    /// تا جستجوی قلم در انبار بزرگ بین داروها گم نشود.
    /// </summary>
    [RelayCommand]
    private void StockIn()
    {
        var vm = new StockInFormViewModel(_api, defaultKindIndex: 2);
        var window = new StockInFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>دریافت خروجی موجودی — همان شکل InventoryListDto سرور</summary>
    public sealed class InventoryResponse
    {
        public bool IsSuccess { get; set; }
        public List<InventoryDto>? Data { get; set; }
    }

    /// <summary>DTO بچ انبار — خروجی GET consumables/inventory</summary>
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

        // چرخهٔ ۴۱ — قیمت‌های افزایشی (صفحهٔ مستقل مصرفی؛ داروخانه نادیده می‌گیرد)
        public decimal SellPrice { get; set; }
        public decimal PurchasePrice { get; set; }
    }
}
