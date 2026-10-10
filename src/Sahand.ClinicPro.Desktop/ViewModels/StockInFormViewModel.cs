using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم ورود موجودی — بچ دارو/قلم با تاریخ انقضا
/// </summary>
/// <remarks>
/// ═══ چرخهٔ چهل‌وسوم (گزارش مالک) ═══
/// «چون اقلام مصرفی دیگه از داروخانه مستقل شده، در… ورود کالا نباید نوع
/// قلم وجود داشته باشه. همچنین از داروخانه هم حذف شود چون فقط دارو در
/// اونجا ثبت میشه» — کمبوی «نوع قلم» حذف و جستجوی قلم به بافت فراخواننده
/// قفل شد: صفحهٔ لوازم مصرفی فقط قلم‌های مصرفی را جست‌وجو می‌کند و
/// داروخانه فقط داروها (سمت سرور هم با ExpectedItemKind رد می‌شود).
///
/// «در ورود اقلام هم خطای غیر منتظره میده» — دو ریشهٔ مرتبط رفع شد:
/// ۱) نتایج جستجو نام‌های خالی نشان می‌دادند (DTO مشترک نسخه‌نویسی فیلدهای
///    nameFa/nameEn را از پاسخ lookups/medications انتظار داشت در حالی که
///    سرور id/name/form/strength می‌فرستد — نام هیچ‌وقت بایند نمی‌شد!)؛
///    اکنون DTO محلیِ هم‌شکل پاسخ سرور؛
/// ۲) سمت سرور: گارد وجود/نوع قلم + اتصال ناوبری تراکنش (کامیت سرور این چرخه).
/// </remarks>
public partial class StockInFormViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly bool _isConsumableContext;

    /// <summary>جستجوی دارو/قلم</summary>
    [ObservableProperty]
    private string _medicationSearchTerm = string.Empty;

    /// <summary>نتایج جستجو</summary>
    [ObservableProperty]
    private ObservableCollection<MedicationOption> _medicationResults = new();

    /// <summary>داروی انتخاب‌شده</summary>
    [ObservableProperty]
    private MedicationOption? _selectedMedication;

    /// <summary>شماره بچ</summary>
    [ObservableProperty]
    private string? _batchNumber;

    /// <summary>تعداد</summary>
    [ObservableProperty]
    private decimal _quantity = 1;

    /// <summary>قیمت خرید به ریال</summary>
    [ObservableProperty]
    private decimal _purchasePrice;

    /// <summary>قیمت فروش به ریال</summary>
    [ObservableProperty]
    private decimal _sellPrice;

    /// <summary>تاریخ انقضا شمسی</summary>
    [ObservableProperty]
    private string? _expiryDateShamsi;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>عنوان فرم — بافت‌دار (مصرفی/دارو)</summary>
    public string FormTitle => _isConsumableContext
        ? "ورود کالای مصرفی (خرید)"
        : "ورود دارو به انبار (خرید)";

    /// <summary>راهنمای بافت‌دار زیر عنوان</summary>
    public string FormHint => _isConsumableContext
        ? "قلم مصرفی را جستجو کنید و بچ/تعداد/قیمت را ثبت کنید — موجودی فوراً در صفحهٔ لوازم مصرفی دیده می‌شود."
        : "دارو را جستجو کنید و بچ/تعداد/قیمت را ثبت کنید — موجودی فوراً در تب «موجودی» داروخانه دیده می‌شود.";

    /// <summary>
    /// سازنده با کلاینت API و بافت فراخواننده
    /// </summary>
    /// <param name="api">کلاینت HTTP برنامه</param>
    /// <param name="isConsumableContext">
    /// true = از صفحهٔ «لوازم مصرفی» (جستجو فقط بین مصرفی‌ها و ثبت در مسیر
    /// مستقل consumables)؛ false = از داروخانه (فقط داروها و مسیر pharmacy)
    /// </param>
    public StockInFormViewModel(IApiClient api, bool isConsumableContext = true)
    {
        _api = api;
        _isConsumableContext = isConsumableContext;
    }

    /// <summary>
    /// جستجوی دارو/قلم — چرخهٔ ۴۳: فیلتر نوع از بافت فرم می‌آید (۱ دارو،
    /// ۲ مصرفی) و DTO محلی هم‌شکل پاسخ واقعی سرور (id/name/form/strength).
    /// </summary>
    [RelayCommand]
    private async Task SearchMedicationAsync()
    {
        if (string.IsNullOrWhiteSpace(MedicationSearchTerm) || MedicationSearchTerm.Trim().Length < 2)
            return;

        var kindParam = _isConsumableContext ? "&itemKind=2" : "&itemKind=1";
        var response = await _api.GetAsync<MedicationsLookupResponse>(
            $"lookups/medications?search={Uri.EscapeDataString(MedicationSearchTerm.Trim())}&take=25{kindParam}");

        MedicationResults = response?.Data is not null
            ? new ObservableCollection<MedicationOption>(response.Data.Select(m =>
                new MedicationOption(m.Id, m.Name, m.Form, m.Strength)))
            : new ObservableCollection<MedicationOption>();
    }

    /// <summary>ثبت ورود موجودی</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedMedication is null)
        {
            ErrorMessage = "ابتدا قلم را از جستجو انتخاب کنید";
            return;
        }
        if (Quantity <= 0)
        {
            ErrorMessage = "تعداد باید بزرگ‌تر از صفر باشد";
            return;
        }
        if (SellPrice < 0 || PurchasePrice < 0)
        {
            ErrorMessage = "قیمت‌ها نمی‌توانند منفی باشند";
            return;
        }
        if (string.IsNullOrWhiteSpace(ExpiryDateShamsi) || ExpiryDateShamsi.Trim().Length != 10)
        {
            ErrorMessage = "تاریخ انقضا شمسی الزامی است (مثال 1405/06/31)";
            return;
        }

        IsBusy = true;
        var body = new
        {
            medicationId = SelectedMedication.Id,
            batchNumber = string.IsNullOrWhiteSpace(BatchNumber) ? null : BatchNumber.Trim(),
            quantity = Quantity,
            purchasePrice = PurchasePrice,
            sellPrice = SellPrice,
            expiryDateShamsi = ExpiryDateShamsi.Trim(),
            supplierId = (long?)null
        };

        // ═══ چرخهٔ ۴۳ — مسیر بر اساس بافت فرم ═══
        // مصرفی‌ها: مسیر مستقل api/v1/consumables/inventory (بدون گیت لایسنس
        // داروخانه؛ نوع «مصرفی» سمت سرور قفل است) — داروخانه: مسیر
        // pharmacy/inventory (نوع «دارو» قفل است). هر دو به همان فرمان می‌رسند.
        var endpoint = _isConsumableContext ? "consumables/inventory" : "pharmacy/inventory";
        var ok = await _api.PostAsync(endpoint, body);
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ثبت موجودی ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    // ═══ DTOهای سبک محلی — هم‌شکل پاسخ واقعی سرور (چرخهٔ ۴۳) ═══
    // پیش‌تر از MedicationsResponse نسخه‌نویسی (nameFa/nameEn/…) استفاده می‌شد
    // که پاسخ lookups/medications آن شکل را ندارد (سرور id/name/form/strength/
    // isNarcotic/itemKind/kindText می‌فرستد) → نام‌ها هرگز بایند نمی‌شدند و
    // نتایج جستجو «خالی» دیده می‌شدند.

    /// <summary>پاسخ فهرست اقلام — پاکت استاندارد ApiResponse</summary>
    public sealed class MedicationsLookupResponse
    {
        public bool IsSuccess { get; set; }
        public List<MedicationLookupDto>? Data { get; set; }
    }

    /// <summary>ردیف قلم از GET lookups/medications — هم‌شکل MedicationOptionDto سرور</summary>
    public sealed class MedicationLookupDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Form { get; set; }
        public string? Strength { get; set; }
    }
}
// توجه: MedicationOption از قبل در سطح همین namespace تعریف شده
// (PrescriptionFormViewModel.cs) و همین‌جا بازاستفاده می‌شود.
