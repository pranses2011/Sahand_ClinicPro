using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel پنجره ویرایش اطلاعات حقوقی پرسنل — چرخه دهم
/// حقوق پایه + درصد از سود خدمات؛ ذخیره از طریق
/// PUT hr/employees/{id}/salary (فرمان UpdateEmployeeSalaryCommand موجود)
/// </summary>
public partial class EmployeeSalaryEditViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly long _employeeId;

    /// <summary>نام کامل پرسنل — برای سربرگ پنجره</summary>
    [ObservableProperty]
    private string _employeeName = string.Empty;

    /// <summary>شناسنامه پرسنل — بخش/سمت</summary>
    [ObservableProperty]
    private string _employeeInfo = string.Empty;

    /// <summary>حقوق پایه ماهانه (ریال) — متن قابل تبدیل به عدد</summary>
    [ObservableProperty]
    private string _baseSalary = "0";

    /// <summary>درصد حقوق از سود خدمات انجامی خود پرسنل (۰ تا ۱۰۰)</summary>
    [ObservableProperty]
    private string _commissionPercent = "0";

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد بستن پنجره</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق — فراخواننده فهرست را بازخوانی می‌کند</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده — شناسه و شناسنامه پرسنل از ردیف فهرست</summary>
    public EmployeeSalaryEditViewModel(IApiClient api, long employeeId,
        string employeeName, string employeeInfo,
        decimal baseSalary, decimal commissionPercent)
    {
        _api = api;
        _employeeId = employeeId;
        _employeeName = employeeName;
        _employeeInfo = employeeInfo;
        _baseSalary = baseSalary.ToString("0");
        _commissionPercent = commissionPercent.ToString("0.##");
    }

    /// <summary>ذخیره اطلاعات حقوقی — PUT hr/employees/{id}/salary</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // ═══ چرخهٔ بیست‌وهشتم — پارس امن ارقام فارسی/جداکننده (هم‌کلاس رفع
        // «حق ویزیت عددی نامعتبر است» پزشک — قانون ۵ مالک: خطای هم‌نوع همه‌جا)
        if (!Helpers.Money.TryParseRialDigits(BaseSalary, out var baseSalary))
        {
            ErrorMessage = "حقوق پایه عددی نامعتبر است";
            return;
        }
        // درصد — اعشار هم مجاز؛ ارقام فارسی نرمال می‌شوند
        var commissionLatin = Helpers.Money.PersianDigitsToLatin(CommissionPercent?.Trim() ?? "").Replace("٫", ".");
        if (!decimal.TryParse(commissionLatin, System.Globalization.CultureInfo.InvariantCulture, out var commissionPercent)
            || commissionPercent is < 0 or > 100)
        {
            ErrorMessage = "درصد حقوق از سود خدمات باید عددی بین ۰ تا ۱۰۰ باشد";
            return;
        }

        IsBusy = true;
        var ok = await _api.PutAsync($"hr/employees/{_employeeId}/salary", new
        {
            baseSalary,
            commissionPercent
        });
        IsBusy = false;

        if (ok) SaveSucceeded?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ذخیره اطلاعات حقوقی ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
