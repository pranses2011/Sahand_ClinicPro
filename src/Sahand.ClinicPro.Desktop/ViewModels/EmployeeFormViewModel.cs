using System.Collections.ObjectModel;
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

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — سمت/دپارتمان + فیلدهای
    // تکمیلی فرم پرسنل مطابق اسکرین‌شات: کد پرسنلی، تاریخ استخدام، تحصیلات،
    // رشتهٔ تحصیلی و تماس اضطراری. پیش‌تر departmentId/positionId تهی
    // هاردکد ارسال می‌شد و لیست پرسنل سمت/دپارتمان را همیشه «—» نشان می‌داد.

    /// <summary>گزینهٔ مرجع سمت/دپارتمان</summary>
    public sealed class LookupOption
    {
        /// <summary>شناسه</summary>
        public long Id { get; init; }

        /// <summary>نام نمایشی</summary>
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>فهرست دپارتمان‌ها — از lookups/departments</summary>
    public ObservableCollection<LookupOption> Departments { get; } = new();

    /// <summary>فهرست سمت‌ها — از lookups/positions</summary>
    public ObservableCollection<LookupOption> Positions { get; } = new();

    /// <summary>ایندکس دپارتمان انتخابی — -۱ یعنی هیچ</summary>
    [ObservableProperty]
    private int _departmentIndex = -1;

    /// <summary>ایندکس سمت انتخابی — -۱ یعنی هیچ</summary>
    [ObservableProperty]
    private int _positionIndex = -1;

    /// <summary>متن تایپی دپارتمان — ComboBox ویرایش‌پذیر؛ مقدار تازه = ساخت خودکار</summary>
    [ObservableProperty]
    private string? _departmentText;

    /// <summary>متن تایپی سمت — ComboBox ویرایش‌پذیر؛ مقدار تازه = ساخت خودکار</summary>
    [ObservableProperty]
    private string? _positionText;

    /// <summary>کد پرسنلی — اختیاری</summary>
    [ObservableProperty]
    private string? _personnelCode;

    /// <summary>تاریخ استخدام شمسی — مثل ۱۴۰۳/۰۵/۱۲</summary>
    [ObservableProperty]
    private string? _hireDateShamsi;

    /// <summary>ایندکس تحصیلات — ۰ دیپلم/سیکل/کاردانی/کارشناسی/کارشناسی ارشد/دکتری</summary>
    [ObservableProperty]
    private int _educationLevelIndex;

    /// <summary>فهرست مقاطع تحصیلی فارسی</summary>
    public string[] EducationLevels { get; } =
        { "دیپلم", "فوق دیپلم", "کاردانی", "کارشناسی", "کارشناسی ارشد", "دکتری" };

    /// <summary>رشتهٔ تحصیلی</summary>
    [ObservableProperty]
    private string? _fieldOfStudy;

    /// <summary>نام فرد رابط در شرایط اضطراری</summary>
    [ObservableProperty]
    private string? _emergencyContact;

    /// <summary>شمارهٔ تماس اضطراری</summary>
    [ObservableProperty]
    private string? _emergencyPhone;

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
    public EmployeeFormViewModel(IApiClient api)
    {
        _api = api;
        // چرخه ۲۴: بارگذاری فهرست‌های مرجع سمت/دپارتمان برای ComboBoxها
        _ = LoadLookupsAsync();
    }

    /// <summary>
    /// بارگذاری فهرست‌های مرجع سمت/دپارتمان — چرخه بیست‌وچهارم؛
    /// خطا بی‌صدا مقادیر خالی می‌گذارد (فرم بدون این فیلدها هم کار می‌کند)
    /// </summary>
    private async Task LoadLookupsAsync()
    {
        var departments = await _api.GetAsync<LookupResponse>("lookups/departments");
        foreach (var d in departments?.Data ?? new List<LookupOption>())
            Departments.Add(d);

        var positions = await _api.GetAsync<LookupResponse>("lookups/positions");
        foreach (var p in positions?.Data ?? new List<LookupOption>())
            Positions.Add(p);
    }

    /// <summary>پاسخ فهرست مرجع — پاکت استاندارد ApiResponse</summary>
    private sealed class LookupResponse
    {
        public bool IsSuccess { get; set; }
        public List<LookupOption>? Data { get; set; }
    }

    /// <summary>
    /// تبدیل انتخاب/متن ComboBox سمت یا دپارتمان به شناسه — اگر متن تایپی
    /// تازه باشد (با هیچ مورد موجود منطبق نیست) ابتدا ساخته و شناسه‌اش
    /// برگردانده می‌شود؛ «—» یا تهی = بدون مقدار (چرخه ۲۴)
    /// </summary>
    /// <param name="items">فهرست گزینه‌های موجود</param>
    /// <param name="selectedIndex">ایندکس انتخابی</param>
    /// <param name="text">متن تایپی</param>
    /// <param name="createEndpoint">اندپوینت ساخت مقدار تازه</param>
    private async Task<long?> ResolveLookupIdAsync(
        ObservableCollection<LookupOption> items, int selectedIndex,
        string? text, string createEndpoint)
    {
        // مورد انتخابی از فهرست؟
        if (selectedIndex >= 0 && selectedIndex < items.Count)
            return items[selectedIndex].Id;

        // متن تایپی تازه؟ (نه خالی و نه نشان‌گر «بدون مقدار»)
        var name = text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name == "—")
            return null;

        // مقدار تازه — ساخت از طریق سرور (Admin/Manager)
        var response = await _api.PostAsync<CreateLookupResponse>(createEndpoint,
            new { name });
        return response?.Data is > 0 ? response.Data : null;
    }

    /// <summary>پاسخ ساخت مرجع — شناسهٔ رکورد ساخته‌شده/موجود</summary>
    private sealed class CreateLookupResponse
    {
        public bool IsSuccess { get; set; }
        public long? Data { get; set; }
    }

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

        // ═══ چرخه ۲۴: حل سمت/دپارتمان — انتخابی از فهرست یا متن تایپی تازه
        // (سمت/دپارتمان تازه در سرور ساخته می‌شود) ═══
        var positionId = await ResolveLookupIdAsync(Positions, PositionIndex, PositionText, "lookups/positions");
        var departmentId = await ResolveLookupIdAsync(Departments, DepartmentIndex, DepartmentText, "lookups/departments");

        // تاریخ استخدام شمسی → میلادی (خالی = امروز)
        DateOnly? hireDate = null;
        var hireText = Helpers.Money.PersianDigitsToLatin(HireDateShamsi?.Trim() ?? "");
        if (!string.IsNullOrWhiteSpace(hireText) &&
            Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper.ToGregorian(hireText) is { } g)
            hireDate = DateOnly.FromDateTime(g);

        var body = new
        {
            firstName = FirstName.Trim(),
            lastName = LastName.Trim(),
            nationalCode = NationalCode.Trim(),
            mobile = Mobile.Trim(),
            departmentId,
            positionId,
            employmentType = EmploymentTypeIndex + 1,
            baseSalary,
            commissionPercent,
            // چرخه ۲۴ — فیلدهای تکمیلی مطابق اسکرین‌شات
            personnelCode = string.IsNullOrWhiteSpace(PersonnelCode) ? null : PersonnelCode.Trim(),
            hireDate,
            educationLevel = EducationLevelIndex >= 0 && EducationLevelIndex < 6
                ? EducationLevelIndex + 1 : (int?)null,
            fieldOfStudy = string.IsNullOrWhiteSpace(FieldOfStudy) ? null : FieldOfStudy.Trim(),
            emergencyContact = string.IsNullOrWhiteSpace(EmergencyContact) ? null : EmergencyContact.Trim(),
            emergencyPhone = string.IsNullOrWhiteSpace(EmergencyPhone) ? null : EmergencyPhone.Trim()
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
