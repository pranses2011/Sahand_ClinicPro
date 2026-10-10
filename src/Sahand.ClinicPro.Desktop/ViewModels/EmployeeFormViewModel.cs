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
/// ═══ چرخهٔ بیست‌وششم (گزارش هجدهم مالک) ═══ پایهٔ FormViewModelBase برای
/// ردیابی تغییرات ذخیره‌نشده — پیش از بستن فرم (ESC یا دکمهٔ بستن) تأیید
/// فارسی خود برنامه پرسیده می‌شود.
/// </summary>
public partial class EmployeeFormViewModel : FormViewModelBase
{
    private readonly IApiClient _api;

    /// <summary>
    /// شناسهٔ پرسنل در حال ویرایش — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// «در لیست پرسنل امکان ویرایش پرسنل وجود ندارد». تهی = حالت ثبت جدید؛
    /// مقدار‌دار = حالت ویرایش — فرم با داده‌های فعلی پرسنل پر و ذخیره
    /// به PUT hr/employees/{id} می‌رود.
    /// </summary>
    private readonly long? _editEmployeeId;

    // ═══ چرخهٔ چهل‌ودوم (گزارش مالک): «در ویرایش پرسنل، سمت و دپارتمان رو
    // اصلاح نمیکنه» — ریشه: فرم ویرایش در نبودِ مقدار در فهرست مرجع (فهرست
    // خالیِ پیش از بذر چرخهٔ ۴۲ یا سمتِ حذف‌شده)، متنی در کامبو نداشت و ذخیره
    // بدون تغییر، تهی ارسال و مقدار فعلی را «پاک» می‌کرد؛ حل سمت/دپارتمان
    // اکنون مقدار اصل را می‌شناسد و در همین وضعیت، همان را نگه می‌دارد.

    /// <summary>شناسهٔ سمتِ فعلی پرسنل هنگام باز شدن فرم ویرایش — تهی یعنی سمت ندارد</summary>
    private long? _originalPositionId;

    /// <summary>نام سمتِ فعلی پرسنل هنگام باز شدن فرم ویرایش — برای تطبیق متن کامبو</summary>
    private string? _originalPositionName;

    /// <summary>شناسهٔ دپارتمان فعلی پرسنل هنگام باز شدن فرم ویرایش — تهی یعنی بخش ندارد</summary>
    private long? _originalDepartmentId;

    /// <summary>نام دپارتمان فعلی پرسنل هنگام باز شدن فرم ویرایش — برای تطبیق متن کامبو</summary>
    private string? _originalDepartmentName;

    /// <summary>عنوان فرم — «ثبت پرسنل جدید» یا «ویرایش پرسنل»</summary>
    public string FormTitle => _editEmployeeId is null ? "ثبت پرسنل جدید" : "ویرایش پرسنل";

    /// <summary>متن دکمهٔ ذخیره — «ثبت» یا «ذخیرهٔ تغییرات»</summary>
    public string SaveButtonText => _editEmployeeId is null ? "ثبت پرسنل" : "ذخیرهٔ تغییرات";

    /// <summary>عنوان فرم برای پیام تأیید تغییرات ذخیره‌نشده — چرخه ۲۶</summary>
    public override string UnsavedChangesTitle => FormTitle;

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

    /// <summary>سازندهٔ حالت ثبت — چرخه هجدهم</summary>
    public EmployeeFormViewModel(IApiClient api)
        : this(api, editEmployeeId: null)
    {
    }

    /// <summary>
    /// سازندهٔ حالت ویرایش — چرخهٔ بیست‌وششم (گزارش هجدهم مالک):
    /// با شناسهٔ پرسنل، فرم پس از بارگذاری لیست‌های مرجع با داده‌های
    /// فعلی پرسنل پر می‌شود تا همان فرم به‌عنوان ویرایش باز شود.
    /// </summary>
    public EmployeeFormViewModel(IApiClient api, long? editEmployeeId)
    {
        _api = api;
        _editEmployeeId = editEmployeeId;
        // چرخه ۲۴: بارگذاری فهرست‌های مرجع سمت/دپارتمان برای ComboBoxها —
        // در حالت ویرایش، پس از آن داده‌های پرسنل هم بارگذاری می‌شود تا
        // اندیکس سمت/دپارتمان فعلی از فهرستِ ازپیش‌بارگذاری‌شده انتخاب شود
        _ = InitializeAsync();
    }

    /// <summary>
    /// بارگذاری لیست‌های مرجع و سپس (در حالت ویرایش) داده‌های پرسنل —
    /// ترتیب مهم است: ابتدا فهرست‌ها تا انتخاب اندیکس صحیح ممکن باشد.
    /// چرخه ۲۶: پس از پایان بارگذاری فرم «تمیز» علامت می‌خورد تا ردیابی
    /// تغییرات کاربر (نه پرشدن برنامه‌ای فرم) از همین نقطه آغاز شود.
    /// </summary>
    private async Task InitializeAsync()
    {
        BeginInit();
        await LoadLookupsAsync();
        if (_editEmployeeId is not null)
            await LoadForEditAsync(_editEmployeeId.Value);
        MarkClean();
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
    /// حل انتخاب/متن ComboBox سمت یا دپارتمان به شناسه — چرخهٔ چهل‌ودوم:
    /// زنجیرهٔ پنج‌مرحله‌ای که سه عارضهٔ قدیمی را می‌بندد:
    ///   ۱) انتخاب از فهرست → شناسهٔ همان گزینه
    ///   ۲) متن تهی/«—» → تهی (پاک‌کردن آگاهانهٔ کاربر — در ویرایش، متن از
    ///      ابتدا با نام فعلی پر شده است پس این حالت فقط با پاک‌کردن دستی رخ می‌دهد)
    ///   ۳) متن برابر نامِ فعلی → شناسهٔ فعلی، بدون رفت‌وبرگشت سرور (حفاظت
    ///      در برابر 403 کاربر غیرمدیر و رفع «هر ویرایش مقدار را صفر می‌کرد»)
    ///   ۴) متن منطبق با گزینهٔ موجود فهرست → شناسهٔ همان (جستجوی کلایندی)
    ///   ۵) مقدار تازه → ساخت سروری؛ شکست ساخت دیگر بی‌صدا تهی نمی‌شود بلکه
    ///      ذخیره با پیام فارسی متوقف می‌شود تا کاربر بداند چه شده است
    /// </summary>
    /// <param name="items">فهرست گزینه‌های موجود</param>
    /// <param name="selectedIndex">ایندکس انتخابی</param>
    /// <param name="text">متن تایپی</param>
    /// <param name="createEndpoint">اندپوینت ساخت مقدار تازه</param>
    /// <param name="originalId">شناسهٔ مقدار فعلی پرسنل (ویرایش) یا تهی (ثبت جدید)</param>
    /// <param name="originalName">نام مقدار فعلی پرسنل یا تهی</param>
    /// <param name="fieldLabel">برچسب فارسی فیلد برای پیام خطا (سمت/دپارتمان)</param>
    /// <returns>شناسهٔ حل‌شده + در صورت شکست ساخت، پیام خطای فارسی</returns>
    private async Task<(long? Id, string? Error)> ResolveLookupIdAsync(
        ObservableCollection<LookupOption> items, int selectedIndex, string? text,
        string createEndpoint, long? originalId, string? originalName, string fieldLabel)
    {
        // ۱) مورد انتخابی از فهرست؟
        if (selectedIndex >= 0 && selectedIndex < items.Count)
            return (items[selectedIndex].Id, null);

        // ۲) متن تهی؟ (خالی یا نشان‌گر «بدون مقدار») — پاک‌کردن آگاهانه
        var name = text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name == "—")
            return (null, null);

        // ۳) همان نام فعلی؟ — حفظ مقدار جاری بدون هیچ تماس سروری؛ کاربر
        //    فیلد را دست نزده است (متن اولیهٔ ویرایش) یا همان را دوباره تایپ کرده
        if (originalId is not null
            && string.Equals(name, originalName, StringComparison.OrdinalIgnoreCase))
            return (originalId, null);

        // ۴) منطبق با گزینهٔ موجود فهرست؟ — جستجوی کلایندی، بدون رفت‌وبرگشت
        var match = items.FirstOrDefault(i =>
            string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            return (match.Id, null);

        // ۵) مقدار تازه — ساخت از طریق سرور (اندپوینت‌های ساخت نقش
        //    Admin/Manager را می‌خواهند؛ کاربر بدون نقش با پیام راهنما متوقف می‌شود)
        var response = await _api.PostAsync<CreateLookupResponse>(createEndpoint,
            new { name });
        if (response?.Data is > 0)
            return (response.Data, null);

        return (null,
            $"ذخیرهٔ {fieldLabel} جدید («{name}») ناموفق بود: {_api.LastError ?? "خطای ناشناخته"} — " +
            "اگر نقش شما مدیر نیست، یکی از گزینه‌های موجود فهرست را انتخاب کنید");
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

    /// <summary>
    /// بارگذاری داده‌های فعلی پرسنل برای ویرایش — چرخهٔ بیست‌وششم
    /// (گزارش هجدهم مالک): GET hr/employees/{id} و پُرکردن همهٔ فیلدها؛
    /// سمت/دپارتمان از روی شناسه در فهرست مرجع انتخاب می‌شود و حقوق پایه
    /// به رسم نمایشی (تومان/ریال کاربر) تبدیل می‌شود.
    /// </summary>
    private async Task LoadForEditAsync(long employeeId)
    {
        var response = await _api.GetAsync<EmployeeDetailResponse>($"hr/employees/{employeeId}");
        var d = response?.Data;
        if (d is null)
        {
            ErrorMessage = _api.LastError ?? "دریافت اطلاعات پرسنل ناموفق بود";
            return;
        }

        FirstName = d.FirstName ?? string.Empty;
        LastName = d.LastName ?? string.Empty;
        NationalCode = d.NationalCode ?? string.Empty;
        Mobile = d.Mobile ?? string.Empty;
        PersonnelCode = d.PersonnelCode;
        FieldOfStudy = d.FieldOfStudy;
        EmergencyContact = d.EmergencyContact;
        EmergencyPhone = d.EmergencyPhone;

        // نوع استخدام و مقطع تحصیلی — اندیس صفرمبنه از مقدار عددی یک‌مبنای سرور
        EmploymentTypeIndex = Math.Max(0, (int)d.EmploymentType - 1);
        EducationLevelIndex = d.EducationLevel is null ? -1 : (int)d.EducationLevel - 1;

        // حقوق — ریال خالص در VM (کادر مبلغ با MoneyRial خودش نمایشی می‌کند)
        // ═══ چرخهٔ بیست‌وهشتم — قرارداد MoneyRial: VM همیشه ریال خالص با
        // ارقام لاتین است؛ پیش‌تر ToDisplay در VM می‌نشست و در حالت تومان
        // کادر مبلغ دوباره تقسیم می‌کرد (نمایش ۱۰ برابر کوچک‌تر) ═══
        BaseSalary = d.BaseSalary.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        CommissionPercent = d.CommissionPercent.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        // تاریخ استخدام میلادی → شمسی متن
        if (d.HireDate is { } hd)
        {
            HireDateShamsi = Sahand.ClinicPro.Application.Common.Helpers.PersianDateHelper
                .ToShamsi(hd);
        }

        // انتخاب سمت/دپارتمان فعلی از فهرست مرجع ازپیش‌بارگذاری‌شده؛ چرخهٔ ۴۲:
        // در نبودِ اندیس (فهرست خالی یا مقدارِ حذف‌شده)، متن کامبو با «نام
        // فعلی» پر می‌شود و مقدارهای اصل برای حلِ مقاوم ذخیره می‌شوند — ذخیرهٔ
        // بدون تغییر دیگر مقدار فعلی را پاک نمی‌کند و کاربر هم می‌بیند چه بوده
        _originalPositionId = d.PositionId;
        _originalPositionName = d.PositionName;
        _originalDepartmentId = d.DepartmentId;
        _originalDepartmentName = d.DepartmentName;
        PositionIndex = FindLookupIndex(Positions, d.PositionId);
        DepartmentIndex = FindLookupIndex(Departments, d.DepartmentId);
        if (PositionIndex < 0 && !string.IsNullOrWhiteSpace(d.PositionName))
            PositionText = d.PositionName;
        if (DepartmentIndex < 0 && !string.IsNullOrWhiteSpace(d.DepartmentName))
            DepartmentText = d.DepartmentName;
    }

    /// <summary>
    /// یافتن اندیس گزینهٔ مرجع با شناسهٔ داده‌شده — -۱ یعنی در فهرست نیست
    /// (مثلاً سمت حذف‌شده یا نبود مرجع)؛ ساختار فشرده برای خوانایی فرم
    /// </summary>
    private static int FindLookupIndex(System.Collections.ObjectModel.ObservableCollection<LookupOption> items, long? id)
        => id is null
            ? -1
            : System.Linq.Enumerable.ToList(items).FindIndex(p => p.Id == id.Value);

    /// <summary>پاسخ جزئیات پرسنل برای ویرایش — پاکت استاندارد</summary>
    private sealed class EmployeeDetailResponse
    {
        public bool IsSuccess { get; set; }
        public EmployeeDetailDto? Data { get; set; }
    }

    /// <summary>مدل جزئیات پرسنل — هم‌نام DTO سرور</summary>
    private sealed class EmployeeDetailDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalCode { get; set; }
        public string? Mobile { get; set; }
        public long? DepartmentId { get; set; }
        public long? PositionId { get; set; }
        // چرخهٔ ۴۲: نام‌های سمت/دپارتمان برای پرکردن متن کامبو در نبودِ اندیس
        public string? PositionName { get; set; }
        public string? DepartmentName { get; set; }
        public int EmploymentType { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal CommissionPercent { get; set; }
        public string? PersonnelCode { get; set; }
        public DateTime? HireDate { get; set; }
        public int? EducationLevel { get; set; }
        public string? FieldOfStudy { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public bool HasPhoto { get; set; }
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
        // ═══ چرخهٔ بیست‌وهشتم — پارس امن ارقام فارسی/جداکننده (هم‌کلاس رفع
        // «حق ویزیت عددی نامعتبر است» پزشک — قانون ۵ مالک) ═══
        if (!Helpers.Money.TryParseRialDigits(BaseSalary, out var baseSalary))
        {
            ErrorMessage = "حقوق پایه عددی نامعتبر است";
            return;
        }
        var commissionLatin = Helpers.Money.PersianDigitsToLatin(CommissionPercent?.Trim() ?? "").Replace("٫", ".");
        if (!decimal.TryParse(commissionLatin, System.Globalization.CultureInfo.InvariantCulture, out var commissionPercent)
            || commissionPercent is < 0 or > 100)
        {
            ErrorMessage = "درصد حقوق از سود خدمات باید عددی بین ۰ تا ۱۰۰ باشد";
            return;
        }

        IsBusy = true;

        // ═══ چرخهٔ ۴۲: حل سمت/دپارتمان با زنجیرهٔ مقاوم — انتخاب فهرستی، حفظ
        // مقدار فعلی، تطبیق کلایندی نام، و ساخت سروریِ مقدار تازه؛ شکستِ ساخت
        // ذخیره را با پیام فارسی متوقف می‌کند تا مقدار بی‌صدا پاک نشود ═══
        var (positionId, positionError) = await ResolveLookupIdAsync(
            Positions, PositionIndex, PositionText, "lookups/positions",
            _originalPositionId, _originalPositionName, "سمت");
        if (positionError is not null)
        {
            IsBusy = false;
            ErrorMessage = positionError;
            return;
        }
        var (departmentId, departmentError) = await ResolveLookupIdAsync(
            Departments, DepartmentIndex, DepartmentText, "lookups/departments",
            _originalDepartmentId, _originalDepartmentName, "دپارتمان");
        if (departmentError is not null)
        {
            IsBusy = false;
            ErrorMessage = departmentError;
            return;
        }

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

        // ═══ چرخهٔ بیست‌وششم — دو مسیر ذخیره ═══
        // ثبت جدید: POST با پاسخ شناسهٔ تازه (برای آپلود عکس)؛
        // ویرایش: PUT روی شناسهٔ موجود (بدون پاسخ داده — فقط موفق/ناموفق)
        if (_editEmployeeId is { } editId)
        {
            var ok = await _api.PutAsync($"hr/employees/{editId}", body);
            if (!ok)
            {
                IsBusy = false;
                ErrorMessage = _api.LastError ?? "ذخیرهٔ تغییرات ناموفق بود";
                return;
            }

            // عکس تازه‌انتخاب‌شده هم روی همین پرسنل آپلود می‌شود
            if (PendingPhotoBytes is { Length: > 0 })
            {
                var photoOk = await PhotoTools.UploadEmployeePhotoAsync(
                    _api, editId, PendingPhotoBytes, PendingPhotoFileName ?? "photo.jpg");
                if (photoOk)
                {
                    // چرخهٔ سی‌وهفتم: ابطال کش بندانگشتی — فهرست پرسنل پس از بستن فرم
                    // بازخوانی می‌شود و باید نسخهٔ تازه را دانلود کند نه کهنه را
                    PhotoTools.InvalidateThumbnail(PhotoTools.EmployeeThumbnailKey(editId));
                }
                else
                    ErrorMessage = "تغییرات ذخیره شد اما ذخیرهٔ عکس ناموفق بود";
            }

            IsBusy = false;
            MarkClean(); // ذخیرهٔ موفق — فرم دیگر «کثیف» نیست؛ بستن بی‌تأیید
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
            return;
        }

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
            if (photoOk)
            {
                // چرخهٔ سی‌وهفتم: ابطال کش بندانگشتی پرسنل تازه (اگر از قبل عکس داشته باشد)
                PhotoTools.InvalidateThumbnail(PhotoTools.EmployeeThumbnailKey(employeeId.Value));
            }
            else
                ErrorMessage = "پرسنل ثبت شد اما ذخیرهٔ عکس ناموفق بود — از فهرست پرسنل دوباره عکس بگذارید";
        }

        IsBusy = false;
        MarkClean(); // ذخیرهٔ موفق — فرم دیگر «کثیف» نیست؛ بستن بی‌تأیید
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
