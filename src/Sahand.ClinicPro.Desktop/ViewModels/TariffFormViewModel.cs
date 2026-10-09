using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم خدمت/تعرفه (ثبت/ویرایش) — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «در صفحه بیمه، بتوان تعرفه جدید تعریف کرد و یا تعرفه‌های موجود را ویرایش کرد».
/// اگر EditTarget تهی باشد ثبت جدید است؛ در غیر این صورت ویرایش همان ردیف.
/// سهم بیمه/بیمار درصدی است (۰-۱۰۰) و در سرور روی ردیف تعرفهٔ آزادِ سال جاری اعمال می‌شود.
/// </summary>
public partial class TariffFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>ردیف در حال ویرایش — تهی یعنی ثبت جدید</summary>
    private readonly TariffRow? _editTarget;

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — تعرفهٔ به‌تفکیک بیمه ═══
    // «تعرفه‌ها برای بیمه‌ها متفاوت می‌تواند باشد و برای آزاد هم تعرفه تعریف
    // بشود» — این فرم روی «هدف» مشخص ذخیره می‌کند: تهی = تعرفهٔ آزاد (بدون
    // بیمه)، مقدار = تعرفهٔ اختصاصی همان بیمه. برچسب هدف هم در فرم دیده می‌شود.

    /// <summary>شناسهٔ هدف تعرفه — تهی = آزاد</summary>
    private readonly long? _insuranceCompanyId;

    /// <summary>عنوان پنجره</summary>
    [ObservableProperty]
    private string _header = "خدمت / تعرفهٔ جدید";

    /// <summary>نام خدمت</summary>
    [ObservableProperty]
    private string _serviceName = string.Empty;

    /// <summary>کد خدمت</summary>
    [ObservableProperty]
    private string? _serviceCode;

    /// <summary>قیمت تعرفه‌ای (با واحد پول برنامه)</summary>
    [ObservableProperty]
    private decimal _price;

    /// <summary>درصد سهم بیمه (۰-۱۰۰)</summary>
    [ObservableProperty]
    private decimal _insuranceShare;

    /// <summary>درصد سهم بیمار (۰-۱۰۰)</summary>
    [ObservableProperty]
    private decimal _patientShare;

    /// <summary>فعال باشد؟ (فقط در ویرایش)</summary>
    [ObservableProperty]
    private bool _isActive = true;

    /// <summary>
    /// آیا هدف این تعرفه «آزاد» است؟ — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «برای آزاد (بدون بیمه) تمامی مبالغ بصورت پیش‌فرض سهم بیمار بکن چون از
    /// اسمش مشخص هست و بیمه‌ای در کار نیست». تعرفهٔ آزاد همیشه ۱۰۰٪ سهم
    /// بیمار و ۰٪ سهم بیمه است؛ فیلدهای درصد در فرم غیرفعال می‌شوند.
    /// </summary>
    public bool IsFreeTarget => _insuranceCompanyId is null;

    /// <summary>
    /// آیا هدف این تعرفه یک «بیمه» است؟ (وارون IsFreeTarget) — فیلدهای درصد
    /// سهم فقط برای تعرفهٔ بیمه‌ای فعال‌اند؛ برای آزاد غیرفعال می‌شوند
    /// </summary>
    public bool IsInsuranceTarget => _insuranceCompanyId is not null;

    /// <summary>
    /// متن راهنمای بخش سهم‌ها — برای هدف آزاد توضیح قاعدهٔ ۱۰۰٪ و برای بیمه
    /// راهنمای عادی درصدها
    /// </summary>
    public string ShareHint => IsFreeTarget
        ? "تعرفهٔ آزاد (بدون بیمه): تمام مبلغ به‌صورت پیش‌فرض سهم بیمار است (۱۰۰٪) — بیمه‌ای در کار نیست"
        : "درصد سهم بیمه و بیمار (۰ تا ۱۰۰) — مجموع دو سهم باید ۱۰۰ باشد";

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده — editTarget تهی یعنی ثبت جدید؛ چرخه ۲۴: targetInsuranceId
    /// هدف تعرفه (تهی = آزاد) و targetLabel برچسب نمایشی همان هدف</summary>
    public TariffFormViewModel(IApiClient api, TariffRow? editTarget,
        long? targetInsuranceId = null, string targetLabel = "آزاد (بدون بیمه)")
    {
        _api = api;
        _editTarget = editTarget;
        _insuranceCompanyId = targetInsuranceId;
        TargetLabel = targetLabel;

        if (editTarget is not null)
        {
            Header = $"ویرایش تعرفه — {editTarget.ServiceName}";
            ServiceName = editTarget.ServiceName;
            ServiceCode = editTarget.Code == "—" ? null : editTarget.Code;
            Price = editTarget.Price;
            InsuranceShare = editTarget.InsuranceShare;
            PatientShare = editTarget.PatientShare;
            IsActive = editTarget.IsActive;
        }
        else
        {
            Header = $"خدمت / تعرفهٔ جدید — {targetLabel}";
        }

        // ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ برای هدف آزاد، سهم‌ها
        // به‌صورت پیش‌فرض ۱۰۰٪ بیمار / ۰٪ بیمه است و در ویرایش هم مقادیر
        // بیمه‌ای قبلی به همین قاعدهٔ استاندارد بازتنظیم می‌شود
        if (IsFreeTarget)
        {
            InsuranceShare = 0;
            PatientShare = 100;
        }
    }

    /// <summary>برچسب هدف تعرفه — آزاد یا نام بیمه (نمایش در فرم)</summary>
    [ObservableProperty]
    private string _targetLabel = "آزاد (بدون بیمه)";

    /// <summary>ذخیره — POST برای جدید / PUT برای ویرایش</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // اعتبارسنجی سمت کلاینت — پیام فارسی روشن
        if (string.IsNullOrWhiteSpace(ServiceName))
        {
            ErrorMessage = "نام خدمت الزامی است";
            return;
        }
        if (InsuranceShare is < 0 or > 100 || PatientShare is < 0 or > 100)
        {
            ErrorMessage = "درصد سهم بیمه و بیمار باید بین ۰ تا ۱۰۰ باشد";
            return;
        }
        if (Price < 0)
        {
            ErrorMessage = "قیمت نمی‌تواند منفی باشد";
            return;
        }

        // ═══ چرخهٔ بیست‌وششم ═══ تعرفهٔ آزاد همیشه ۱۰۰٪ بیمار است — بیمه‌ای
        // وجود ندارد؛ مقادیر ارسالی صرفاً از حالت فرم خوانده نمی‌شود تا با
        // غیرفعال‌بودن فیلدها هم سازگار و قطعی باشد
        var insuranceShare = IsFreeTarget ? 0 : InsuranceShare;
        var patientShare = IsFreeTarget ? 100 : PatientShare;

        IsBusy = true;
        ErrorMessage = string.Empty;

        var body = new
        {
            serviceName = ServiceName.Trim(),
            serviceCode = string.IsNullOrWhiteSpace(ServiceCode) ? null : ServiceCode.Trim(),
            price = Price,
            insuranceShare,
            patientShare,
            isActive = IsActive,
            // چرخه ۲۴: هدف تعرفه — تهی = آزاد، مقدار = بیمهٔ منتخب
            insuranceCompanyId = _insuranceCompanyId
        };

        bool ok;
        if (_editTarget is null)
            ok = await _api.PostAsync("lookups/services", body);
        else
            ok = await _api.PutAsync($"lookups/services/{_editTarget.Id}", body);

        IsBusy = false;
        if (ok) CloseRequested?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
