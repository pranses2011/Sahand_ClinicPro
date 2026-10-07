using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

using Sahand.ClinicPro.Desktop.Helpers;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم ثبت پرسنل — چرخه هجدهم (گزارش دوازدهم):
/// عکس پروفایل همزمان با ثبت انتخاب می‌شود و پس از موفقیتِ ثبت (گرفتن شناسهٔ
/// پرسنل از پاسخ API) در Photos/Employees ذخیره و مسیرش در دیتابیس ثبت می‌شود.
/// </summary>
public partial class EmployeeFormViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>نام</summary>
    [ObservableProperty]
    private string _firstName = string.Empty;

    /// <summary>نام خانوادگی</summary>
    [ObservableProperty]
    private string _lastName = string.Empty;

    /// <summary>کد ملی</summary>
    [ObservableProperty]
    private string _nationalCode = string.Empty;

    /// <summary>
    /// متن اعتبارسنجی زندهٔ کد ملی — چرخه بیستم (گزارش چهاردهم مالک):
    /// «اعتبارسنجی کد ملی زنده باشد و هنگام نوشتن زیرش با متن نشان داده شود»
    /// </summary>
    [ObservableProperty]
    private string _nationalCodeValidationText = string.Empty;

    /// <summary>رنگ متن اعتبارسنجی — سبز معتبر / قرمز نامعتبر</summary>
    [ObservableProperty]
    private string _nationalCodeValidationColor = "#C62828";

    /// <summary>
    /// بازخورد زندهٔ کد ملی هنگام تایپ — تعداد رقم‌های باقی‌مانده،
    /// درستی رقم کنترلی و خطای کاراکتر غیرعددی زیر فیلد نشان داده می‌شود
    /// </summary>
    partial void OnNationalCodeChanged(string value)
    {
        var code = value?.Trim() ?? string.Empty;
        if (code.Length == 0)
        {
            NationalCodeValidationText = string.Empty;
            return;
        }

        // نرمال‌سازی ارقام فارسی/عربی برای سنجش
        var latin = Helpers.Money.PersianDigitsToLatin(code);

        if (latin.Any(ch => !char.IsDigit(ch)))
        {
            NationalCodeValidationColor = "#C62828";
            NationalCodeValidationText = "❌ کد ملی فقط باید رقم باشد";
            return;
        }

        if (latin.Length < 10)
        {
            NationalCodeValidationColor = "#F9A825";
            NationalCodeValidationText = $"⏳ {10 - latin.Length} رقم دیگر وارد کنید ({latin.Length} از ۱۰)";
            return;
        }

        var valid = Validators.IsValidNationalCode(latin);
        NationalCodeValidationColor = valid ? "#2E7D32" : "#C62828";
        NationalCodeValidationText = valid
            ? "✅ کد ملی معتبر است (رقم کنترلی درست)"
            : "❌ رقم کنترلی کد ملی درست نیست — کد را بازبینی کنید";
    }

    /// <summary>موبایل</summary>
    [ObservableProperty]
    private string _mobile = string.Empty;

    /// <summary>حقوق پایه ماهانه (ریال) — متن قابل تبدیل به عدد</summary>
    [ObservableProperty]
    private string _baseSalary = "0";

    /// <summary>درصد حقوق از سود خدمات انجامی خود پرسنل (۰ تا ۱۰۰)</summary>
    [ObservableProperty]
    private string _commissionPercent = "0";

    /// <summary>نمایه نوع استخدام — ۰ تمام‌وقت</summary>
    [ObservableProperty]
    private int _employmentTypeIndex;

    /// <summary>پیام خطا</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>در حال ذخیره</summary>
    [ObservableProperty]
    private bool _isBusy;

    /* ─── عکس پروفایل — چرخه هجدهم ─── */

    /// <summary>بایت‌های عکس انتخاب‌شده — پیش از ثبت در حافظه</summary>
    [ObservableProperty]
    private byte[]? _pendingPhotoBytes;

    /// <summary>نام فایل عکس انتخاب‌شده</summary>
    [ObservableProperty]
    private string? _pendingPhotoFileName;

    /// <summary>پیش‌نمایش عکس انتخاب‌شده</summary>
    [ObservableProperty]
    private System.Windows.Media.ImageSource? _photoPreview;

    /// <summary>آیا عکسی انتخاب شده</summary>
    public bool HasPendingPhoto => PendingPhotoBytes is { Length: > 0 };

    /// <summary>رویداد بستن</summary>
    public event EventHandler? CloseRequested;

    /// <summary>رویداد ذخیره موفق</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>سازنده با کلاینت API</summary>
    public EmployeeFormViewModel(IApiClient api) => _api = api;

    /// <summary>انتخاب عکس پروفایل — JPG/PNG تا ۵ مگابایت</summary>
    [RelayCommand]
    private void PickPhoto()
    {
        var picked = PhotoTools.PickPhoto("انتخاب عکس پرسنل");
        if (picked is null) return;
        (PendingPhotoBytes, PendingPhotoFileName) = picked.Value;
        PhotoPreview = PhotoTools.ToImage(PendingPhotoBytes);
        OnPropertyChanged(nameof(HasPendingPhoto));
    }

    /// <summary>بزرگ‌نمایی عکس انتخاب‌شده</summary>
    [RelayCommand]
    private void ViewPhoto()
        => PhotoTools.ShowLarge(PendingPhotoBytes, "عکس پرسنل");

    /// <summary>حذف انتخاب عکس</summary>
    [RelayCommand]
    private void ClearPhoto()
    {
        PendingPhotoBytes = null;
        PendingPhotoFileName = null;
        PhotoPreview = null;
        OnPropertyChanged(nameof(HasPendingPhoto));
    }

    /// <summary>ثبت پرسنل</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
            || string.IsNullOrWhiteSpace(NationalCode) || string.IsNullOrWhiteSpace(Mobile))
        {
            ErrorMessage = "همه فیلدهای الزامی را تکمیل کنید";
            return;
        }

        // چرخه ۱۹ (گزارش سیزدهم مالک): صحت کد ملی با رقم کنترلی سنجیده می‌شود نه فقط طول
        if (!Validators.IsValidNationalCode(NationalCode))
        {
            ErrorMessage = "کد ملی معتبر نیست — باید ۱۰ رقم با رقم کنترلی درست باشد";
            return;
        }
        if (!Validators.IsValidMobile(Mobile))
        {
            ErrorMessage = "فرمت موبایل: ۰۹XXXXXXXXX";
            return;
        }

        // ═══ اطلاعات حقوقی چرخه نهم — حقوق پایه + درصد از سود خدمات ═══
        if (!decimal.TryParse(BaseSalary.Trim(), out var baseSalary) || baseSalary < 0)
        {
            ErrorMessage = "حقوق پایه عددی نامعتبر است";
            return;
        }
        if (!decimal.TryParse(CommissionPercent.Trim(), out var commissionPercent)
            || commissionPercent is < 0 or > 100)
        {
            ErrorMessage = "درصد حقوق از سود خدمات باید عددی بین ۰ تا ۱۰۰ باشد";
            return;
        }

        IsBusy = true;
        var body = new
        {
            firstName = FirstName.Trim(),
            lastName = LastName.Trim(),
            nationalCode = NationalCode.Trim(),
            mobile = Mobile.Trim(),
            departmentId = (long?)null,
            positionId = (long?)null,
            employmentType = EmploymentTypeIndex + 1,
            baseSalary,
            commissionPercent
        };

        // پاسخ تایپ‌شده — شناسهٔ پرسنلِ تازه‌ثبت‌شده برای عکس لازم است
        var response = await _api.PostAsync<CreateEmployeeResponse>("hr/employees", body);
        var employeeId = response?.Data;
        if (employeeId is null or 0)
        {
            IsBusy = false;
            ErrorMessage = _api.LastError ?? "ثبت پرسنل ناموفق بود";
            return;
        }

        // عکس انتخاب‌شده — پس از ثبت، آپلود روی شناسهٔ جدید
        if (PendingPhotoBytes is { Length: > 0 })
        {
            var photoOk = await PhotoTools.UploadEmployeePhotoAsync(
                _api, employeeId.Value, PendingPhotoBytes,
                PendingPhotoFileName ?? "photo.jpg");
            if (!photoOk)
                ErrorMessage = "پرسنل ثبت شد اما ذخیرهٔ عکس ناموفق بود — از فهرست پرسنل دوباره عکس بگذارید";
        }

        IsBusy = false;
        SaveSucceeded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>انصراف</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>پاسخ ثبت پرسنل — پاکت استاندارد با شناسه</summary>
    private sealed class CreateEmployeeResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }
}
