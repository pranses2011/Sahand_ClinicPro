using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel فرم ثبت/ویرایش کاربر — گام U13 طبق فرم ۲۴:
/// تب ۱ اطلاعات کاربری + تب ۲ نقش‌ها و دسترسی‌ها + سنجه قدرت رمز ۵ سطحی
/// </summary>
public partial class UserFormViewModel : ViewModelBase
{
    private readonly IApiClient _api;
    private readonly ISnackbarService _snackbar;

    /// <summary>شناسه کاربر در حال ویرایش — تهی یعنی ثبت جدید</summary>
    [ObservableProperty]
    private long? _editUserId;

    /// <summary>عنوان فرم — ثبت یا ویرایش</summary>
    [ObservableProperty]
    private string _formTitle = "ثبت کاربر جدید";

    /// <summary>نام کاربری</summary>
    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary>نام کامل کاربر — چرخه بیست‌وچهارم (گزارش هفدهم مالک):
    /// «فیلد نام کامل در ثبت/ویرایش کاربر وجود ندارد» — در سرور به رکورد
    /// پرسنل متصل می‌شود و در لیست کاربران نمایش داده می‌شود</summary>
    [ObservableProperty]
    private string? _fullName;

    /// <summary>ایمیل</summary>
    [ObservableProperty]
    private string? _email;

    /// <summary>موبایل</summary>
    [ObservableProperty]
    private string? _phone;

    /// <summary>رمز عبور</summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary>تکرار رمز عبور</summary>
    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    /// <summary>نمایش رمز</summary>
    [ObservableProperty]
    private bool _showPassword;

    /// <summary>الزام تغییر رمز در اولین ورود</summary>
    [ObservableProperty]
    private bool _mustChangePassword = true;

    /// <summary>احراز هویت دومرحله‌ای</summary>
    [ObservableProperty]
    private bool _twoFactorEnabled;

    /// <summary>وضعیت فعال</summary>
    [ObservableProperty]
    private bool _isActive = true;

    /// <summary>پیام خطای یکتایی نام کاربری — اعتبارسنجی برخط</summary>
    [ObservableProperty]
    private string? _usernameError;

    /// <summary>نقش‌های موجود برای چندانتخابی</summary>
    public ObservableCollection<RoleCheckItem> Roles { get; } = new();

    /// <summary>عنوان — فرم پنجره‌ای بدون صفحه ناوبری</summary>
    public override string Title => FormTitle;

    /// <summary>سازنده</summary>
    public UserFormViewModel(IApiClient api, ISnackbarService snackbar)
    {
        _api = api;
        _snackbar = snackbar;
        _ = LoadRolesAsync();
    }

    /// <summary>آیا حالت ویرایش است — رمز و نام کاربری غیرقابل تغییر</summary>
    public bool IsEdit => EditUserId is not null;

    /* ─── سنجه قدرت رمز ۵ سطحی — طبق فرم ۲۴ ─── */

    /// <summary>امتیاز قدرت ۰ تا ۵</summary>
    public int StrengthScore
    {
        get
        {
            if (string.IsNullOrEmpty(Password)) return 0;
            var score = 0;
            if (Password.Length >= 8) score++;
            if (Password.Length >= 12) score++;
            if (Password.Any(char.IsUpper) && Password.Any(char.IsLower)) score++;
            if (Password.Any(char.IsDigit)) score++;
            if (Password.Any(c => "!@#$%^&*()-_=+[]{};:,.?/\\|~`\"'".Contains(c))) score++;
            return Math.Min(score, 5);
        }
    }

    /// <summary>متن فارسی سطح قدرت</summary>
    public string StrengthText => StrengthScore switch
    {
        0 => "رمز عبور را وارد کنید",
        1 => "خیلی ضعیف",
        2 => "ضعیف",
        3 => "متوسط",
        4 => "قوی",
        _ => "خیلی قوی"
    };

    /// <summary>رنگ نوار قدرت</summary>
    public string StrengthColor => StrengthScore switch
    {
        0 => "#B0BEC5", 1 => "#C62828", 2 => "#E65100",
        3 => "#F9A825", 4 => "#7CB342", _ => "#2E7D32"
    };

    /// <summary>درصد نوار پیشرفت</summary>
    public double StrengthPercent => StrengthScore * 20.0;

    /// <summary>بازخورد تغییر رمز — تازه‌سازی سنجه</summary>
    partial void OnPasswordChanged(string value)
    {
        OnPropertyChanged(nameof(StrengthScore));
        OnPropertyChanged(nameof(StrengthText));
        OnPropertyChanged(nameof(StrengthColor));
        OnPropertyChanged(nameof(StrengthPercent));
    }

    /* ─── بارگذاری داده ─── */

    /// <summary>بارگذاری نقش‌ها برای چک‌باکس‌ها</summary>
    private async Task LoadRolesAsync()
    {
        var roles = await _api.GetAsync<RolesResponse>("roles");
        Roles.Clear();
        foreach (var r in roles?.Data ?? new List<RoleItemDto>())
            Roles.Add(new RoleCheckItem { Id = r.Id, Name = r.Name, Description = r.Description, IsChecked = false });
    }

    /// <summary>بارگذاری کاربر برای ویرایش — پر کردن فیلدها</summary>
    public async Task LoadForEdit(long userId)
    {
        EditUserId = userId;
        FormTitle = $"ویرایش کاربر #{userId}";
        OnPropertyChanged(nameof(IsEdit));

        var response = await _api.GetAsync<UsersResponse>($"users?page=1&pageSize=1&search=");
        // ویرایش برخط فهرست — یافتن از همان پاسخ
        var users = response?.Data?.Items ?? new List<UserDto>();
        var user = users.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            // تلاش با بارگذاری کامل — سرویس فهرست فیلترشده
            var all = await _api.GetAsync<UsersResponse>("users?page=1&pageSize=500");
            user = (all?.Data?.Items ?? new List<UserDto>()).FirstOrDefault(u => u.Id == userId);
        }
        if (user is null) { _snackbar.Error("کاربر یافت نشد"); return; }

        Username = user.Username;
        FullName = user.FullName;
        Email = user.Email;
        IsActive = user.IsActive;
        MustChangePassword = user.MustChangePassword;

        foreach (var role in Roles)
            role.IsChecked = user.Roles?.Contains(role.Name) == true;
    }

    /* ─── ذخیره ─── */

    /// <summary>ذخیره کاربر — اعتبارسنجی کامل سپس ثبت/ویرایش</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // اعتبارسنجی سمت کاربر — مطابق سیاست
        if (!IsEdit && Username.Trim().Length < 3)
        { ErrorMessage = "نام کاربری حداقل ۳ کاراکتر است"; return; }

        // نام کامل — الزامی برای شناسایی انسانی کاربر (گزارش هفدهم مالک)
        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length < 3)
        { ErrorMessage = "نام کامل کاربر را وارد کنید"; return; }

        if (!IsEdit)
        {
            if (Password.Length < 8)
            { ErrorMessage = "رمز عبور حداقل ۸ کاراکتر است"; return; }
            if (!Password.Any(char.IsUpper) || !Password.Any(char.IsLower) ||
                !Password.Any(char.IsDigit) || !Password.Any(c => "!@#$%^&*()-_=+[]{};:,.?/\\|~`\"'".Contains(c)))
            { ErrorMessage = "رمز باید شامل حروف بزرگ و کوچک، عدد و علامت باشد"; return; }
            if (Password != ConfirmPassword)
            { ErrorMessage = "تکرار رمز با رمز عبور یکسان نیست"; return; }
        }

        var roleIds = Roles.Where(r => r.IsChecked).Select(r => r.Id).ToList();

        // چرخه بیست‌وچهارم: نام کامل هم ارسال می‌شود تا سرور کاربر را به
        // پرسنل (Employee) متصل کند — ریشهٔ «نام کامل در لیست خالی بود»
        var ok = IsEdit
            ? await _api.PutAsync($"users/{EditUserId}", new
            {
                email = Email, phone = Phone, fullName = FullName?.Trim(), roleIds,
                mustChangePassword = MustChangePassword,
                twoFactorEnabled = TwoFactorEnabled, isActive = IsActive
            })
            : await _api.PostAsync("users", new
            {
                username = Username.Trim(), email = Email, password = Password,
                fullName = FullName?.Trim(),
                phone = Phone, roleIds, mustChangePassword = MustChangePassword,
                twoFactorEnabled = TwoFactorEnabled, isActive = IsActive
            });

        if (ok)
        {
            _snackbar.Success(IsEdit ? "کاربر به‌روزرسانی شد" : "کاربر ایجاد شد");
            SaveSucceeded?.Invoke(this, EventArgs.Empty);
        }
        else
            ErrorMessage = _api.LastError ?? "ذخیره ناموفق بود";
    }

    /// <summary>بررسی یکتایی نام کاربری — با تأخیر بعد از توقف تایپ</summary>
    [RelayCommand]
    private async Task CheckUsernameAsync()
    {
        if (IsEdit || Username.Trim().Length < 3) return;
        var response = await _api.GetAsync<CheckResponse>(
            $"users/check-username?username={Uri.EscapeDataString(Username.Trim())}");
        UsernameError = response?.Data?.Available != true
            ? "این نام کاربری قبلاً استفاده شده" : null;
    }

    /// <summary>رویداد موفقیت ذخیره — بستن فرم</summary>
    public event EventHandler? SaveSucceeded;

    /// <summary>درخواست بستن فرم</summary>
    public event EventHandler? CloseRequested;

    /// <summary>انصراف — بستن بدون ذخیره</summary>
    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /* ─── پاسخ‌ها ─── */

    private sealed class RolesResponse
    {
        public bool IsSuccess { get; set; }
        public List<RoleItemDto>? Data { get; set; }
    }

    private sealed class RoleItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private sealed class UsersResponse
    {
        public bool IsSuccess { get; set; }
        public UsersPage? Data { get; set; }
    }

    private sealed class UsersPage
    {
        public List<UserDto> Items { get; set; } = new();
    }

    private sealed class UserDto
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public List<string>? Roles { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
    }

    private sealed class CheckResponse
    {
        public bool IsSuccess { get; set; }
        public CheckData? Data { get; set; }
    }

    private sealed class CheckData
    {
        public bool Available { get; set; }
    }
}

/// <summary>آیتم نقش قابل انتخاب در فرم کاربر</summary>
public partial class RoleCheckItem : ObservableObject
{
    /// <summary>شناسه نقش</summary>
    public long Id { get; init; }

    /// <summary>نام نقش</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>توضیح نقش</summary>
    public string? Description { get; init; }

    /// <summary>انتخاب‌شده</summary>
    [ObservableProperty]
    private bool _isChecked;
}
