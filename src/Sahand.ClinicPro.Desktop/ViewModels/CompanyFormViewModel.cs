using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم شرکت بیمه (ثبت/ویرایش) — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
/// «در صفحه بیمه، بتوان بیمه جدید تعریف کرد و یا بیمه‌های موجود را ویرایش کرد».
/// اگر EditTarget تهی باشد ثبت جدید است؛ در غیر این صورت ویرایش همان ردیف.
/// </summary>
public partial class CompanyFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>ردیف در حال ویرایش — تهی یعنی ثبت جدید</summary>
    private readonly CompanyRow? _editTarget;

    /// <summary>عنوان پنجره</summary>
    [ObservableProperty]
    private string _header = "شرکت بیمهٔ جدید";

    /// <summary>نام شرکت بیمه</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>کد اختصاری بیمه</summary>
    [ObservableProperty]
    private string? _code;

    /// <summary>نمایه نوع بیمه در کشو — ۰ عمومی، ۱ سلامت، ۲ ارتش، ۳ خصوصی، ۴ تکمیلی</summary>
    [ObservableProperty]
    private int _typeIndex = 3;

    /// <summary>گزینه‌های نوع بیمه — متن فارسی و مقدار عددی متناظر</summary>
    public static readonly (string Text, int Value)[] TypeOptions =
    {
        ("عمومی (تأمین اجتماعی)", 1),
        ("سلامت", 2),
        ("نیروهای مسلح", 3),
        ("خصوصی", 4),
        ("تکمیلی", 5)
    };

    /// <summary>درصد پوشش پیش‌فرض (۰ تا ۱۰۰)</summary>
    [ObservableProperty]
    private decimal _coveragePercent = 70m;

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
    public CompanyFormViewModel(IApiClient api, CompanyRow? editTarget)
    {
        _api = api;
        _editTarget = editTarget;

        if (editTarget is not null)
        {
            Header = $"ویرایش بیمه — {editTarget.Name}";
            Name = editTarget.Name;
            Code = editTarget.Code;
            CoveragePercent = editTarget.CoveragePercent;
            IsActive = editTarget.IsActive;
            // یافتن نمایهٔ گزینهٔ متناظر با نوع فعلی
            var idx = Array.FindIndex(TypeOptions, t => t.Value == editTarget.Type);
            TypeIndex = idx >= 0 ? idx : 3;
        }
    }

    /// <summary>ذخیره — POST برای جدید / PUT برای ویرایش</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        // اعتبارسنجی سمت کلاینت — پیام فارسی روشن
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "نام شرکت بیمه الزامی است";
            return;
        }
        if (CoveragePercent is < 0 or > 100)
        {
            ErrorMessage = "درصد پوشش باید بین ۰ تا ۱۰۰ باشد";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var body = new
        {
            name = Name.Trim(),
            code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
            type = TypeOptions[TypeIndex].Value,
            coveragePercent = CoveragePercent,
            isActive = IsActive
        };

        bool ok;
        if (_editTarget is null)
            ok = await _api.PostAsync("insurance/companies", body);
        else
            ok = await _api.PutAsync($"insurance/companies/{_editTarget.Id}", body);

        IsBusy = false;
        if (ok) CloseRequested?.Invoke(this, EventArgs.Empty);
        else ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
