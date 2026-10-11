using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ردیف فهرست ورود کالای دسته‌جمعی — چرخهٔ ۴۵.
/// هر ردیف یک «بچ» کامل است: قلم + شماره بچ + تعداد + قیمت خرید/فروش + انقضا؛
/// دکمهٔ ثبت همه، ردیف‌ها را پشت‌سرهم به سرور می‌فرستد و نتیجهٔ هر ردیف را جدا گزارش می‌کند.
/// </summary>
public sealed class StockInBatchLine
{
    /// <summary>قلم دارو/مصرفی</summary>
    public MedicationOption Item { get; }

    /// <summary>شماره بچ — اختیاری</summary>
    public string? BatchNumber { get; }

    /// <summary>تعداد ورودی</summary>
    public decimal Quantity { get; }

    /// <summary>قیمت خرید هر واحد (ریال)</summary>
    public decimal PurchasePrice { get; }

    /// <summary>قیمت فروش هر واحد (ریال)</summary>
    public decimal SellPrice { get; }

    /// <summary>تاریخ انقضای شمسی</summary>
    public string ExpiryShamsi { get; }

    /// <summary>متن نمایشی قلم — نام + فرم/توان (برای ستون فهرست)</summary>
    public string ItemDisplay =>
        $"{Item.Name}{(string.IsNullOrWhiteSpace(Item.Form) ? "" : $" ({Item.Form})")}";

    /// <summary>جمع خرید این ردیف (تعداد × قیمت خرید)</summary>
    public decimal PurchaseTotal => Quantity * PurchasePrice;

    /// <summary>سازنده با همهٔ داده‌های بچ</summary>
    public StockInBatchLine(MedicationOption item, string? batchNumber, decimal quantity,
        decimal purchasePrice, decimal sellPrice, string expiryShamsi)
    {
        Item = item;
        BatchNumber = batchNumber;
        Quantity = quantity;
        PurchasePrice = purchasePrice;
        SellPrice = sellPrice;
        ExpiryShamsi = expiryShamsi;
    }
}

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
/// ═══ چرخهٔ چهل‌وپنجم (گزارش مالک) — ورود کالای دسته‌جمعی ═══
/// «در اقلام مصرفی و داروها ورود کالا حالت دسته جمعی هم داشته باشه که
/// همه کالاها رو بنویسیم و در آخر ثبت بکنیم» — فرم حالا «فهرستی» کار
/// می‌کند: قلم را جستجو و مشخصات بچ را پر می‌کنید → «افزودن به فهرست» →
/// ردیف به جدول می‌نشیند و فرم برای قلم بعدی خالی می‌شود → در پایان
/// «ثبت همه» ردیف‌ها را پشت‌سرهم ثبت می‌کند و نتیجهٔ هر ردیف را جدا
/// گزارش می‌دهد (موفق‌ها از فهرست کنده می‌شوند؛ ناموفق‌ها برای اصلاح
/// می‌مانند). رفتار تک‌ردیفی قبلی هم حفظ شد: اگر فهرست خالی باشد و
/// فرم پُر باشد، ثبت همان یک ردیف است.
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

    /* ═══ چرخهٔ ۴۵ — حالت دسته‌جمعی ═══ */

    /// <summary>ردیف‌های فهرست‌شده برای ثبت گروهی</summary>
    public ObservableCollection<StockInBatchLine> BatchLines { get; } = new();

    /// <summary>آیا ردیفی در فهرست هست؟ — نمایش جدول و متن دکمهٔ ثبت</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaveButtonText))]
    private bool _hasBatchLines;

    /// <summary>متن خلاصهٔ فهرست — شمار ردیف و جمع خرید</summary>
    public string BatchSummaryText => BatchLines.Count == 0
        ? string.Empty
        : $"{Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(BatchLines.Count.ToString())} قلم در فهرست — جمع خرید {Sahand.ClinicPro.Desktop.Helpers.Money.Format(BatchLines.Sum(l => l.PurchaseTotal))}";

    /// <summary>متن دکمهٔ ثبت — تک‌ردیفی یا گروهی</summary>
    public string SaveButtonText => HasBatchLines
        ? $"ثبت همه ({Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(BatchLines.Count.ToString())} قلم)"
        : "ثبت موجودی";

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>عنوان فرم — بافت‌دار (مصرفی/دارو)</summary>
    public string FormTitle => _isConsumableContext
        ? "ورود کالای مصرفی (خرید) — تک‌ردیفی یا دسته‌جمعی"
        : "ورود دارو به انبار (خرید) — تک‌ردیفی یا دسته‌جمعی";

    /// <summary>راهنمای بافت‌دار زیر عنوان</summary>
    public string FormHint => _isConsumableContext
        ? "قلم را جستجو و مشخصات بچ را وارد کنید، با «افزودن به فهرست» ردیف بسازید و همه را یک‌جا ثبت کنید — موجودی فوراً در صفحهٔ لوازم مصرفی دیده می‌شود."
        : "دارو را جستجو و مشخصات بچ را وارد کنید، با «افزودن به فهرست» ردیف بسازید و همه را یک‌جا ثبت کنید — موجودی فوراً در تب «موجودی» داروخانه دیده می‌شود.";

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

    /// <summary>
    /// اعتبارسنجی فرم جاری (بدون پیام) — false یعنی فرم برای ردیف‌سازی
    /// آماده نیست؛ پیام خطا در فراخواننده تنظیم می‌شود تا «افزودن» و «ثبت»
    /// از یک منبع صدق کنند.
    /// </summary>
    private bool TryBuildLine(out StockInBatchLine? line, out string error)
    {
        line = null;
        if (SelectedMedication is null)
        {
            error = "ابتدا قلم را از جستجو انتخاب کنید";
            return false;
        }
        if (Quantity <= 0)
        {
            error = "تعداد باید بزرگ‌تر از صفر باشد";
            return false;
        }
        if (SellPrice < 0 || PurchasePrice < 0)
        {
            error = "قیمت‌ها نمی‌توانند منفی باشند";
            return false;
        }
        if (string.IsNullOrWhiteSpace(ExpiryDateShamsi) || ExpiryDateShamsi.Trim().Length != 10)
        {
            error = "تاریخ انقضا شمسی الزامی است (مثال 1405/06/31)";
            return false;
        }

        error = string.Empty;
        line = new StockInBatchLine(
            SelectedMedication,
            string.IsNullOrWhiteSpace(BatchNumber) ? null : BatchNumber.Trim(),
            Quantity,
            PurchasePrice,
            SellPrice,
            ExpiryDateShamsi.Trim());
        return true;
    }

    /// <summary>
    /// افزودن ردیف جاری به فهرست دسته‌جمعی — چرخهٔ ۴۵.
    /// ردیفِ همسان (قلم + بچ + انقضا) ادغام می‌شود (تعداد و قیمت‌ها جمع) تا
    /// فهرست تمیز بماند؛ پس از افزودن، فیلدهای بچ برای قلم بعدی خالی می‌شوند.
    /// </summary>
    [RelayCommand]
    private void AddLine()
    {
        ErrorMessage = string.Empty;
        if (!TryBuildLine(out var built, out var error))
        {
            ErrorMessage = error;
            return;
        }
        // اعتبارسنجی پاس شده — ردیف غیرتهی است
        var line = built!;

        // ادغام با ردیف همسان — همان قلم با همان بچ و انقضا
        var twin = BatchLines.FirstOrDefault(l =>
            l.Item.Id == line.Item.Id
            && string.Equals(l.BatchNumber, line.BatchNumber, StringComparison.OrdinalIgnoreCase)
            && l.ExpiryShamsi == line.ExpiryShamsi);
        if (twin is not null)
        {
            BatchLines.Remove(twin);
            line = new StockInBatchLine(twin.Item, twin.BatchNumber,
                twin.Quantity + line.Quantity,
                line.PurchasePrice, line.SellPrice, line.ExpiryShamsi);
        }

        BatchLines.Add(line);
        RefreshBatchState();

        // فرم برای قلم بعدی آماده — جستجو و نتایج هم پاک شود تا انتخابِ کهنه خطا نکند
        SelectedMedication = null;
        MedicationResults.Clear();
        MedicationSearchTerm = string.Empty;
        BatchNumber = null;
        Quantity = 1;
        PurchasePrice = 0;
        SellPrice = 0;
        ExpiryDateShamsi = null;
    }

    /// <summary>حذف یک ردیف از فهرست دسته‌جمعی</summary>
    [RelayCommand]
    private void RemoveLine(StockInBatchLine? line)
    {
        if (line is null) return;
        BatchLines.Remove(line);
        RefreshBatchState();
    }

    /// <summary>خالی‌کردن کل فهرست دسته‌جمعی (با تأیید فارسی خود برنامه)</summary>
    [RelayCommand]
    private void ClearLines()
    {
        if (BatchLines.Count == 0) return;
        var confirmed = Views.AppDialog.Confirm("خالی‌کردن فهرست",
            $"هر {Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(BatchLines.Count.ToString())} ردیف فهرستِ ثبت‌نشده حذف شود؟",
            Views.AppDialogIcon.Warning) == true;
        if (!confirmed) return;

        BatchLines.Clear();
        RefreshBatchState();
    }

    /// <summary>نوسازی وضعیت فهرست (پرچم + متن‌های وابسته)</summary>
    private void RefreshBatchState()
    {
        HasBatchLines = BatchLines.Count > 0;
        OnPropertyChanged(nameof(BatchSummaryText));
    }

    /// <summary>
    /// ثبت همه — چرخهٔ ۴۵: ردیف‌ها پشت‌سرهم به سرور می‌روند؛ موفق‌ها از
    /// فهرست کنده می‌شوند و ناموفق‌ها با پیام فارسی می‌مانند. اگر فهرست خالی
    /// است و فرم پُر است، همان رفتار تک‌ردیفی قبل از این چرخه اجرا می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // فهرست خالی + فرم پُر = جریان تک‌ردیفیِ سابق (سازگاری رفتار)
        if (BatchLines.Count == 0)
        {
            if (!TryBuildLine(out var single, out var singleError))
            {
                ErrorMessage = singleError;
                return;
            }
            BatchLines.Add(single!);
            RefreshBatchState();
        }

        IsBusy = true;
        var endpoint = _isConsumableContext ? "consumables/inventory" : "pharmacy/inventory";
        var succeeded = 0;
        var failures = new List<string>();

        // ثبت ترتیبی — هر ردیف مستقل است؛ شکست یکی مانع بقیه نمی‌شود
        foreach (var line in BatchLines.ToList())
        {
            var body = new
            {
                medicationId = line.Item.Id,
                batchNumber = line.BatchNumber,
                quantity = line.Quantity,
                purchasePrice = line.PurchasePrice,
                sellPrice = line.SellPrice,
                expiryDateShamsi = line.ExpiryShamsi,
                supplierId = (long?)null
            };
            var ok = await _api.PostAsync(endpoint, body);
            if (ok)
            {
                succeeded++;
                BatchLines.Remove(line);
            }
            else
            {
                failures.Add($"«{line.ItemDisplay}»: {_api.LastError ?? "خطای ناشناخته"}");
            }
        }

        RefreshBatchState();
        IsBusy = false;

        if (failures.Count == 0)
        {
            // همه موفق — بستن فرم و خبر به صفحهٔ والد برای نوسازی
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
            return;
        }

        // گزارش ناموفق‌ها — موفق‌ها ثبت شده‌اند و از فهرست کنده شده‌اند.
        // فرم باز می‌ماند تا ردیف‌های ناموفق اصلاح و دوباره ثبت شوند؛ صفحهٔ
        // والد پس از بسته‌شدن دیالوگ به‌هرحال نوسازی می‌شود (ShowDialog → LoadAsync)
        // پس در این مسیر SaveSucceeded شلیک نمی‌شود — آن رویداد یعنی «همه ثبت شد؛
        // ببند» و بستنِ پنجره، ردیف‌های ناموفقِ قابل‌اصلاح را از دست کاربر می‌برد.
        var summary = succeeded > 0
            ? $"{Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(succeeded.ToString())} ردیف ثبت شد؛ {Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(failures.Count.ToString())} ردیف ناموفق:"
            : "هیچ ردیفی ثبت نشد:";
        ErrorMessage = summary + "\n• " + string.Join("\n• ", failures);
    }

    /// <summary>انصراف — اگر ردیف ثبت‌نشده در فهرست هست، تأیید بگیرد (دیالوگ فارسی)</summary>
    [RelayCommand]
    private void Cancel()
    {
        if (BatchLines.Count > 0)
        {
            var confirmed = Views.AppDialog.Confirm("خروج از فرم ورود کالا",
                $"{Sahand.ClinicPro.Desktop.Helpers.Money.ToPersianDigits(BatchLines.Count.ToString())} ردیفِ هنوزثبت‌نشده در فهرست دارید — بدون ثبت خارج می‌شوید؟",
                Views.AppDialogIcon.Warning) == true;
            if (!confirmed) return;
        }
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

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
