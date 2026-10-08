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

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد درخواست بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>سازنده — editTarget تهی یعنی ثبت جدید</summary>
    public TariffFormViewModel(IApiClient api, TariffRow? editTarget)
    {
        _api = api;
        _editTarget = editTarget;

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
    }

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

        IsBusy = true;
        ErrorMessage = string.Empty;

        var body = new
        {
            serviceName = ServiceName.Trim(),
            serviceCode = string.IsNullOrWhiteSpace(ServiceCode) ? null : ServiceCode.Trim(),
            price = Price,
            insuranceShare = InsuranceShare,
            patientShare = PatientShare,
            isActive = IsActive
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
