using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.Views;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>ردیف درخواست آزمایش</summary>
/// <remarks>
/// چرخهٔ ۶۷: پراپرتی <see cref="CanPrint"/> تعیین می‌کند دکمهٔ «چاپ نتیجه»
/// برای این ردیف نمایش داده شود یا نه (وضعیت ۳=در جریان / ۴=نتیجه آماده).
/// </remarks>
public record LabOrderRow(
    long Id,
    string? Number,
    string PatientName,
    string DoctorName,
    string Date,
    string StatusText,
    int ItemsCount,
    int Status = 0)
{
    /// <summary>آیا دکمهٔ «چاپ نتیجه» برای این ردیف نمایش داده شود؟
    /// چرخهٔ ۶۷ (گزارش مالک: «امکان چاپ هم داشته باشه») — وضعیت ۳ (در جریان)
    /// و ۴ (نتیجه آماده) یعنی دست‌کم نتیجه‌ای ثبت شده که چاپش معنا دارد.</summary>
    public bool CanPrint => Status is 3 or 4;
}

/// <summary>قلم آزمایش قابل ویرایش نتیجه</summary>
public partial class LabItemRow : ObservableObject
{
    /// <summary>شناسه قلم</summary>
    public long Id { get; init; }

    /// <summary>نام آزمایش</summary>
    public string TestName { get; init; } = string.Empty;

    /// <summary>واحد</summary>
    public string? Unit { get; init; }

    /// <summary>بازه طبیعی</summary>
    public string? NormalRange { get; init; }

    /// <summary>
    /// بازهٔ مرجع «مخصوص همین بیمار» (سن/جنسیت) — چرخهٔ ۶۹ (فاز ۳ / F-28)؛
    /// سرور با موتور واحد محاسبه می‌کند و فرم اختصاصی ثبت نتیجه رنگی نشانش می‌دهد.
    /// </summary>
    public string? PatientRangeText { get; init; }

    /// <summary>وضعیت فعلی قلم</summary>
    public string StatusText { get; init; } = string.Empty;

    /// <summary>
    /// فلگ تفسیر آزمایشگاهی سرور — چرخهٔ ۶۹ (F-28): تهی/L/H/LL/HH؛
    /// رنگ ردیف جدول از همین ساخته می‌شود (سبز طبیعی/زرد غیرطبیعی/قرمز بحرانی).
    /// </summary>
    public string? AbnormalFlag { get; init; }

    /// <summary>
    /// متن فارسی وضعیت نتیجه — چرخهٔ ۶۹: بج رنگی کنار نتیجه («طبیعی»، «بالاتر از
    /// حد»، «پایین‌تر از حد»، «بحرانی بالا»، «بحرانی پایین»).
    /// </summary>
    public string FlagText => AbnormalFlag switch
    {
        "H" => "بالاتر از حد",
        "L" => "پایین‌تر از حد",
        "HH" => "بحرانی بالا",
        "LL" => "بحرانی پایین",
        _ => string.Empty
    };

    /// <summary>رنگ بج فلگ — قرمز بحرانی / نارنجی غیرطبیعی / سبز طبیعی</summary>
    public string FlagBrush => AbnormalFlag switch
    {
        "HH" or "LL" => "#E53935",
        "H" or "L" => "#FB8C00",
        _ => "#43A047"
    };

    /// <summary>رنگ پس‌زمینهٔ ردیف — از فلگ تفسیر (چرخهٔ ۶۹ / گزارش مالک)</summary>
    public string RowBrush => AbnormalFlag switch
    {
        "HH" or "LL" => "#FFF5F5",   // قرمز بسیار روشن — بحرانی
        "H" or "L" => "#FFF8E1",     // کهربایی بسیار روشن — غیرطبیعی
        _ => "#FFFFFF"               // سفید — طبیعی/بدون تفسیر
    };

    /// <summary>نتیجه متنی</summary>
    [ObservableProperty]
    private string? _result;

    /// <summary>نتیجه عددی</summary>
    [ObservableProperty]
    private string? _resultNumeric;

    /// <summary>توضیح</summary>
    [ObservableProperty]
    private string? _comment;
}

/// <summary>
/// ViewModel آزمایشگاه — درخواست‌ها با فیلتر وضعیت
/// </summary>
public partial class LabViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>عنوان صفحه</summary>
    public override string Title => "آزمایشگاه";

    /// <summary>درخواست‌ها</summary>
    [ObservableProperty]
    private ObservableCollection<LabOrderRow> _orders = new();

    /// <summary>درخواست انتخاب‌شده</summary>
    [ObservableProperty]
    private LabOrderRow? _selectedOrder;

    /// <summary>نمایه وضعیت فیلتر — صفر یعنی همه</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    /// <summary>برچسب وضعیت‌ها</summary>
    public string[] StatusLabels { get; } =
        { "سفارش‌شده", "نمونه‌گیری‌شده", "در جریان", "نتیجه آماده" };

    /// <summary>پزشکان — برای فرم درخواست</summary>
    public ObservableCollection<DoctorOption> Doctors { get; } = new();

    /// <summary>سازنده با تزریق کلاینت API و سرویس توست (چرخهٔ ۶۷ — چاپ نتیجه)</summary>
    public LabViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = InitializeAsync();
    }

    /// <summary>بارگذاری پزشکان و درخواست‌ها</summary>
    private async Task InitializeAsync()
    {
        var page = await _api.GetAsync<AppointmentsViewModel.DoctorsPage>("doctors?page=1&pageSize=200");
        if (page?.Data is not null)
            foreach (var d in page.Data)
                Doctors.Add(new DoctorOption(d.Id, d.FullName, d.SpecialtyName, d.ConsultationFee));

        await LoadAsync();
    }

    /// <summary>بارگذاری درخواست‌ها</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var endpoint = "lab/orders" +
                       (StatusFilterIndex > 0 ? $"?status={StatusFilterIndex}" : string.Empty);

        var response = await _api.GetAsync<LabOrdersResponse>(endpoint);

        if (response?.Data is not null)
        {
            Orders = new ObservableCollection<LabOrderRow>(response.Data.Select(o => new LabOrderRow(
                o.Id, o.OrderNumber, o.PatientName, o.DoctorName, o.DateShamsi,
                StatusTextOf(o.Status), o.ItemsCount, o.Status)));
        }
        else
        {
            ErrorMessage = _api.LastError ?? "دریافت درخواست‌ها ناموفق بود";
        }

        IsBusy = false;
    }

    /// <summary>متن فارسی وضعیت</summary>
    private static string StatusTextOf(int status) => status switch
    {
        1 => "سفارش‌شده",
        2 => "نمونه‌گیری‌شده",
        3 => "در جریان",
        4 => "نتیجه آماده",
        _ => "نامشخص"
    };

    /// <summary>ثبت درخواست آزمایش جدید</summary>
    [RelayCommand]
    private void NewOrder()
    {
        var vm = new LabOrderFormViewModel(_api, Doctors);
        var window = new LabOrderFormWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>ثبت نتیجه درخواست انتخاب‌شده</summary>
    [RelayCommand]
    private void RecordResult()
    {
        if (SelectedOrder is null) return;

        // چرخهٔ ۶۹: توست هم تزریق می‌شود تا هشدارهای فرم (انتخاب قلم و…) دیده شوند
        var vm = new LabResultViewModel(_api, SelectedOrder.Id, _snackbar);
        var window = new LabResultWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
        _ = LoadAsync();
    }

    /// <summary>
    /// چاپ برگهٔ نتیجهٔ یک درخواست — چرخهٔ ۶۷ (گزارش مالک: «لیست آزمایشگاه
    /// ... امکان چاپ هم داشته باشه»). جزئیات درخواست (بیمار/پزشک/اقلام با
    /// نتایج ثبت‌شده) از سرور خوانده می‌شود، سند فارسی راست‌چین ساخته می‌شود
    /// و از دروازهٔ PrintFlow می‌گذرد (پیش‌نمایش اختیاری طبق تنظیمات چاپ).
    /// </summary>
    [RelayCommand]
    private async Task PrintResultAsync(LabOrderRow? row)
    {
        if (row is null) return;

        var response = await _api.GetAsync<LabResultViewModel.LabOrderDetailEnvelope>($"lab/orders/{row.Id}");
        var detail = response?.Data;
        if (detail is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت جزئیات درخواست برای چاپ ناموفق بود");
            return;
        }

        var printed = Services.LabResultPrinter.Print(detail, row.Id);
        if (printed)
            _snackbar.Success($"برگهٔ نتیجهٔ «{row.Number ?? row.Id.ToString()}» به چاپگر ارسال شد");
    }

    /// <summary>
    /// چاپ برچسب نمونه‌های یک درخواست — چرخهٔ ۶۹ (فاز ۳ / F-02 نقشه راه ai-review)
    /// «برچسب نمونه»: برای هر قلم، برچسب کوچکی با QR یکتای «درخواست + قلم» +
    /// نام بیمار + نام آزمایش چاپ می‌شود که روی ظرف نمونه می‌چسبد؛ اسکن آن در
    /// آزمایشگاه، قلم درست را پیدا می‌کند — خطای جابه‌جایی نمونه صفر می‌شود.
    /// </summary>
    [RelayCommand]
    private async Task PrintSpecimenLabelsAsync(LabOrderRow? row)
    {
        if (row is null) return;

        var response = await _api.GetAsync<LabResultViewModel.LabOrderDetailEnvelope>($"lab/orders/{row.Id}");
        var detail = response?.Data;
        if (detail is null)
        {
            _snackbar.Error(_api.LastError ?? "دریافت جزئیات درخواست برای برچسب ناموفق بود");
            return;
        }

        if (detail.Items is null || detail.Items.Count == 0)
        {
            _snackbar.Warning("این درخواست قلمی برای چاپ برچسب ندارد");
            return;
        }

        var printed = Services.SpecimenLabelPrinter.Print(detail, row.Id);
        if (printed)
            _snackbar.Success($"برچسب نمونه‌های «{row.Number ?? row.Id.ToString()}» به چاپگر ارسال شد");
    }

    /// <summary>پاسخ درخواست‌ها</summary>
    public sealed class LabOrdersResponse
    {
        public bool IsSuccess { get; set; }
        public List<LabOrderDto>? Data { get; set; }
    }

    /// <summary>DTO درخواست</summary>
    public sealed class LabOrderDto
    {
        public long Id { get; set; }
        public string? OrderNumber { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string DateShamsi { get; set; } = string.Empty;
        public int Status { get; set; }
        public int ItemsCount { get; set; }
    }

    /* ── کاتالوگ تست‌ها — تب ۵ گام U10 + بازطراحی چرخهٔ ۷۲ ── */

    /* ═══ چرخهٔ ۷۲ (گزارش مالک) — کاتالوگ کامل با بازه‌های مرجع ═══
       «بازه‌های مرجع و حدود بحرانی هر آزمایش رو بطور کامل و دقیق تکمیل کن…
       در صورت نیاز بتونم اصلاحشون بکنم» — ردیف کاتالوگ از ۴ فیلد نمایشی
       به همهٔ ستون‌های ساخت‌یافتهٔ بازه + حدود بحرانی ارتقا یافت و از
       endpoint جدید lab/tests/catalog تغذیه می‌شود؛ دکمهٔ ویرایش هر ردیف
       فرم LabTestEditWindow را برای مالک باز می‌کند. */

    /// <summary>
    /// آیتم کاتالوگ تست — نسخهٔ کامل چرخهٔ ۷۲ با بازه‌های ساخت‌یافته.
    /// فیلدهای عددی تهی‌پذیرند (آزمایش کیفی بدون بازه)؛ رشته‌های نمایشی
    /// بازه برای ستون «بازهٔ مرجع» جدول ساخته می‌شوند (RangeMale/Female/...).
    /// </summary>
    public record TestCatalogRow(
        long Id,
        string Name,
        string? TestNameEn,
        string? TestCode,
        string? Category,
        string? SampleType,
        string? Unit,
        decimal Price,
        int? TurnAroundTime,
        string? NormalRangeMale,
        string? NormalRangeFemale,
        decimal? RefMinAdultMale, decimal? RefMaxAdultMale,
        decimal? RefMinAdultFemale, decimal? RefMaxAdultFemale,
        decimal? RefMinChild, decimal? RefMaxChild, int? ChildAgeMaxYears,
        decimal? CriticalLow, decimal? CriticalHigh,
        bool IsActive)
    {
        /// <summary>متن بازهٔ مردان — «۱۳٫۵ تا ۱۷٫۵» یا «—»</summary>
        public string RangeMaleText => FormatRange(RefMinAdultMale, RefMaxAdultMale);

        /// <summary>متن بازهٔ زنان</summary>
        public string RangeFemaleText => FormatRange(RefMinAdultFemale, RefMaxAdultFemale);

        /// <summary>متن بازهٔ کودک + سن در پرانتز</summary>
        public string RangeChildText
        {
            get
            {
                var txt = FormatRange(RefMinChild, RefMaxChild);
                if (txt == "—" || ChildAgeMaxYears is not { } age)
                    return txt;
                return $"{txt} (تا {Helpers.Money.ToPersianDigits(age.ToString())} سال)";
            }
        }

        /// <summary>متن حدود بحرانی — «کمتر از ۲٫۵ یا بیشتر از ۶٫۲»</summary>
        public string CriticalText
        {
            get
            {
                // مقادیر محلی — تحلیل جریان کامپایلر با متغیرهای الگوی سوئیچ امن نیست
                if (CriticalLow.HasValue && CriticalHigh.HasValue)
                    return $"<{FaNum(CriticalLow.Value)} یا >{FaNum(CriticalHigh.Value)}";
                if (CriticalLow.HasValue)
                    return $"<{FaNum(CriticalLow.Value)}";
                if (CriticalHigh.HasValue)
                    return $">{FaNum(CriticalHigh.Value)}";
                return "—";
            }
        }

        /// <summary>رنگ ستون بحرانی — قرمز اگر حد ثبت شده باشد</summary>
        public string CriticalBrush => CriticalLow.HasValue || CriticalHigh.HasValue ? "#C62828" : "#90A4AE";

        /// <summary>قالب‌بندی یک بازهٔ تهی‌پذیر با ارقام فارسی</summary>
        private static string FormatRange(decimal? min, decimal? max) => (min, max) switch
        {
            (null, null) => "—",
            ({ } lo, { } hi) => $"{Helpers.Money.ToPersianDigits(lo.ToString("0.####"))} تا {Helpers.Money.ToPersianDigits(hi.ToString("0.####"))}",
            ({ } lo, null) => $"از {Helpers.Money.ToPersianDigits(lo.ToString("0.####"))}",
            (null, { } hi) => $"تا {Helpers.Money.ToPersianDigits(hi.ToString("0.####"))}"
        };

        /// <summary>عدد با ارقام فارسی و حداکثر ۴ رقم اعشار — برای متن بحرانی</summary>
        private static string FaNum(decimal value)
            => Helpers.Money.ToPersianDigits(value.ToString("0.####"));
    }

    /// <summary>کاتالوگ کامل تست‌ها (با بازه‌ها و حدود بحرانی)</summary>
    [ObservableProperty]
    private ObservableCollection<TestCatalogRow> _testCatalog = new();

    /// <summary>عبارت جستجوی کاتالوگ — فارسی/انگلیسی/کد</summary>
    [ObservableProperty]
    private string? _catalogSearch;

    /// <summary>شمار تست‌های دارای بازهٔ ساخت‌یافته — نشان سربرگ کاتالوگ</summary>
    [ObservableProperty]
    private string _catalogSummaryText = "—";

    /// <summary>
    /// بارگذاری کاتالوگ کامل تست‌ها — چرخهٔ ۷۲ از endpoint جدید با همهٔ
    /// بازه‌های مرجع و حدود بحرانی؛ خلاصهٔ «چند تست بازهٔ ثبت‌شده دارد» هم
    /// برای نشان سربرگ ساخته می‌شود.
    /// </summary>
    public async Task LoadCatalogAsync()
    {
        var query = "lab/tests/catalog" +
                    (string.IsNullOrWhiteSpace(CatalogSearch) ? "" : $"?search={Uri.EscapeDataString(CatalogSearch.Trim())}");
        var response = await _api.GetAsync<CatalogResponse>(query);
        if (response?.Data is null) return;

        TestCatalog = new ObservableCollection<TestCatalogRow>(response.Data.Select(MapCatalogRow));

        // نشان خلاصه — چند تست بازهٔ ساخت‌یافته/حد بحرانی دارد
        var withRanges = TestCatalog.Count(t => t.RefMinAdultMale.HasValue || t.RefMaxAdultMale.HasValue
                                                || t.RefMinAdultFemale.HasValue || t.RefMaxAdultFemale.HasValue);
        var withCritical = TestCatalog.Count(t => t.CriticalLow.HasValue || t.CriticalHigh.HasValue);
        CatalogSummaryText =
            $"{Helpers.Money.ToPersianDigits(TestCatalog.Count.ToString())} آزمایش — " +
            $"{Helpers.Money.ToPersianDigits(withRanges.ToString())} بازهٔ مرجع — " +
            $"{Helpers.Money.ToPersianDigits(withCritical.ToString())} حد بحرانی";
    }

    /// <summary>نگاشت DTO سرور به ردیف نمایش کاتالوگ</summary>
    private static TestCatalogRow MapCatalogRow(CatalogItem t) => new(
        t.Id, t.Name, t.TestNameEn, t.TestCode, t.Category, t.SampleType,
        t.Unit, t.Price, t.TurnAroundTime,
        t.NormalRangeMale, t.NormalRangeFemale,
        t.RefMinAdultMale, t.RefMaxAdultMale,
        t.RefMinAdultFemale, t.RefMaxAdultFemale,
        t.RefMinChild, t.RefMaxChild, t.ChildAgeMaxYears,
        t.CriticalLow, t.CriticalHigh,
        t.IsActive);

    /// <summary>جستجوی کاتالوگ — با تغییر متن، فهرست تازه‌سازی می‌شود</summary>
    [RelayCommand]
    private async Task SearchCatalogAsync() => await LoadCatalogAsync();

    /// <summary>
    /// ویرایش یک ردیف کاتالوگ — چرخهٔ ۷۲: فرم ویرایش بازه‌های مرجع/حدود
    /// بحرانی برای مالک باز می‌شود؛ پس از ذخیرهٔ موفق کاتالوگ تازه‌سازی
    /// می‌شود تا ستون‌های بازه همان لحظه به‌روز دیده شوند.
    /// </summary>
    [RelayCommand]
    private void EditTest(TestCatalogRow? row)
    {
        if (row is null) return;

        var vm = new LabTestEditViewModel(_api, row);
        var window = new Views.LabTestEditWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        vm.SaveSucceeded += (_, _) => _snackbar.Success($"بازه‌های «{row.Name}» ذخیره شد");
        window.ShowDialog();

        // اگر ذخیره‌ای بود، پشت‌صحنهٔ کاتالوگ تازه شود
        if (vm.WasSaved)
            _ = LoadCatalogAsync();
    }

    /// <summary>پاسخ کاتالوگ آزمایش‌ها — هم‌قالب LabTestCatalogDto سرور (چرخهٔ ۷۲)</summary>
    public sealed class CatalogResponse
    {
        public bool IsSuccess { get; set; }
        public List<CatalogItem>? Data { get; set; }
    }

    /// <summary>آیتم آزمایش کامل — هر ۹ ستون بازه + حدود بحرانی</summary>
    public sealed class CatalogItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TestNameEn { get; set; }
        public string? TestCode { get; set; }
        public string? Category { get; set; }
        public string? SampleType { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public int? TurnAroundTime { get; set; }
        public string? NormalRangeMale { get; set; }
        public string? NormalRangeFemale { get; set; }
        public decimal? RefMinAdultMale { get; set; }
        public decimal? RefMaxAdultMale { get; set; }
        public decimal? RefMinAdultFemale { get; set; }
        public decimal? RefMaxAdultFemale { get; set; }
        public decimal? RefMinChild { get; set; }
        public decimal? RefMaxChild { get; set; }
        public int? ChildAgeMaxYears { get; set; }
        public decimal? CriticalLow { get; set; }
        public decimal? CriticalHigh { get; set; }
        public bool IsActive { get; set; }
    }
}
