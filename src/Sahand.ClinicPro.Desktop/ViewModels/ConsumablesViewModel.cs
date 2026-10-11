using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Helpers;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف «قلم مصرفی» — چرخهٔ ۴۳ بازطراحی: هر ردیف یک قلم (نه بچ) با جمع
/// موجودی همهٔ بچ‌ها، قیمت‌های آخرین خرید و نزدیک‌ترین انقضا؛ قلم‌های
/// تعریف‌شدهٔ بدون ورود کالا هم با موجودی صفر دیده می‌شوند تا کاتالوگ
/// مصرفی‌های درمانگاه همیشه کامل نمایش یابد (گزارش مالک: «لیست اقلام
/// مصرفی نمایش داده نمیشه» — ریشه: فهرست قبلی بچ‌محور بود و قلمِ بدون
/// بچ هرگز در آن نمی‌آمد).
/// </summary>
/// <param name="Id">شناسهٔ قلم (Medication)</param>
/// <param name="Name">نام قلم (فارسی یا ژنریک)</param>
/// <param name="Quantity">جمع موجودی همهٔ بچ‌های قلم</param>
/// <param name="ReorderLevel">بیشینهٔ نقطهٔ سفارش بچ‌های قلم (صفر = بدون تنظیم)</param>
/// <param name="ExpiryShamsi">نزدیک‌ترین تاریخ انقضا شمسی — تهی یعنی بدون بچ/بدون انقضا</param>
/// <param name="SellPrice">قیمت فروش آخرین بچ (ریال سرور)</param>
/// <param name="PurchasePrice">قیمت خرید آخرین بچ (ریال سرور)</param>
/// <param name="IsLowStock">جمع موجودی زیر نقطهٔ سفارش است؟</param>
/// <param name="IsExpiringSoon">نزدیک‌ترین انقضا در ۹۰ روز آینده است؟</param>
/// <param name="BatchCount">شمار بچ‌های ثبت‌شدهٔ این قلم</param>
public record ConsumableBatchRow(
    long Id, string Name, decimal Quantity, decimal ReorderLevel,
    string? ExpiryShamsi, decimal SellPrice, decimal PurchasePrice,
    bool IsLowStock, bool IsExpiringSoon, int BatchCount)
{
    /// <summary>متن وضعیت قلم — همان واژگان تب موجودی داروخانه</summary>
    public string StatusText => IsLowStock ? "کمبود موجودی" : IsExpiringSoon ? "نزدیک انقضا" : "نرمال";

    /// <summary>ارزش فروش این قلم (جمع موجودی × قیمت فروش) — برای جمع ارزش انبار</summary>
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

            // ═══ چرخهٔ ۴۸ (گزارش مالک: «بعد از ثبت و ورود اقلام جدید، در لیست
            //      نشان داده نمی‌شود و حتی دکمهٔ به‌روزرسانی هم بی‌اثر است») ═══
            // جدول به پراپرتی محاسباتیِ VisibleBatches بایند است نه خودِ Batches؛
            // تعویض نمونهٔ Batches تنها اعلانِ همان نام را منتشر می‌کند و اتصالِ
            // ارزیابی‌شدهٔ گرید همچنان به مجموعهٔ قدیمیِ (زیر پالایش قبلی) اشاره
            // می‌کرد — جستجو تنها راهِ پنهانی دوباره‌ارزیابی بود. اعلان صریحِ
            // VisibleBatches پس از هر بارگذاری، گرید را به دادهٔ تازه وصل می‌کند.
            OnPropertyChanged(nameof(VisibleBatches));

            RefreshSummary();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>نگاشت DTO قلم‌محور سرور به ردیف نمایشی — چرخهٔ ۴۳</summary>
    private static ConsumableBatchRow MapRow(InventoryDto dto) => new(
        dto.ItemId, dto.MedicationName, dto.TotalQuantity, dto.ReorderLevel,
        dto.NearestExpiryShamsi, dto.SellPrice, dto.PurchasePrice,
        dto.IsLowStock, dto.IsExpiringSoon, dto.BatchCount);

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
    /// پالایش زندهٔ فهرست بر اساس جستجو — نام قلم؛ بدون رفت‌وبرگشت به
    /// سرور (۲۰۰ ردیف سقف سرور است و پالایش کلایندی فوری است)؛ با هر تایپ،
    /// اتصال VisibleBatches از نو ارزیابی می‌شود. چرخهٔ ۴۳: ردیف‌ها قلم‌اند
    /// نه بچ، پس ستون بچ در جستجو معنا ندارد.
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
                b.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
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
        var vm = new PharmacyItemFormViewModel(_api, isConsumableContext: true);
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
        var vm = new StockInFormViewModel(_api, isConsumableContext: true);
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

    /// <summary>DTO قلم انبار — خروجی قلم‌محور GET consumables/inventory (چرخهٔ ۴۳)</summary>
    public sealed class InventoryDto
    {
        public long ItemId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public string? NearestExpiryShamsi { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpiringSoon { get; set; }

        // چرخهٔ ۴۱ — قیمت‌های افزایشی؛ چرخهٔ ۴۳ — قیمت‌های «آخرین بچ» قلم
        public decimal SellPrice { get; set; }
        public decimal PurchasePrice { get; set; }

        // چرخهٔ ۴۳ — شمار بچ‌های ثبت‌شدهٔ قلم
        public int BatchCount { get; set; }
    }
}
