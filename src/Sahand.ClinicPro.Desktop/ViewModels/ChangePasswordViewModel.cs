using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel تغییر رمز عبور الزامی — چرخه چهاردهم (گزارش پنجم مالک).
/// راهنمای SQL قدم ۷ قول داده بود برنامه بعد از ورود اولِ admin
/// (بذرافشان با MustChangePassword=true) «بلافاصله و دورزدنی‌ناپذیر»
/// تغییر رمز را بخواهد؛ پیش‌تر این جریان در دسکتاپ وجود نداشت و
/// کاربر بدون تغییر رمز مستقیم وارد پنجره اصلی می‌شد.
/// فرمان به POST auth/change-password می‌رود (توکنِ همان ورود پیوست می‌شود)
/// و سرور پس از موفقیت پرچم کاربر را پایین می‌آورد.
/// </summary>
public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IApiClient _api;

    /// <summary>
    /// سازنده با تزریق کلاینت API
    /// </summary>
    public ChangePasswordViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadPasswordPolicyAsync();
    }

    /// <summary>نام کاربریِ واردشده — فقط برای نمایش در سربرگ پنجره</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>رمز عبور فعلی — کاربر همان لحظه در صفحه ورود تایپش کرده است</summary>
    [ObservableProperty]
    private string _currentPassword = string.Empty;

    /// <summary>رمز عبور جدید — ارزیابی قطعی مطابق سیاست جاری سرور</summary>
    [ObservableProperty]
    private string _newPassword = string.Empty;

    /// <summary>تکرار رمز عبور جدید — باید با خود رمز یکسان باشد</summary>
    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    /// <summary>پیام خطای فارسی برای نمایش در پنجره</summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>نشانگر مشغول‌بودن — نوار انتظار و قفل دکمه</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>رویداد تغییر موفق رمز — پنجره اصلی باز می‌شود</summary>
    public event EventHandler? ChangeSucceeded;

    /// <summary>
    /// رویداد الزام فعال‌سازی لایسنس — چرخه هفدهم (گزارش یازدهم مالک):
    /// رمز با موفقیت عوض شد اما لایسنس فعال نیست؛ پیش از پوستهٔ اصلی باید
    /// فعال‌سازی انجام شود — مسیر پنجرهٔ اصلی از اینجا هم مثل ورود گارد می‌شود
    /// تا دورزدنی نماند.
    /// </summary>
    public event EventHandler? LicenseActivationRequired;

    /// <summary>آیا پیام خطا نمایش داده شود</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// دکمه ثبت رمز جدید
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ChangePasswordAsync()
    {
        // بررسی محلی هم‌خوانی تکرار — قبل از رفت‌وبرگشت به سرور
        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "تکرار رمز جدید با خودِ رمز یکسان نیست";
            OnPropertyChanged(nameof(HasError));
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(HasError));
        ChangePasswordCommand.NotifyCanExecuteChanged();

        // گزارش هفتم مالک: سرور ChangePasswordRequestDto سه فیلد اجباری دارد و
        // ConfirmPassword آن [Required] + [Compare(NewPassword)] است — ارسالِ
        // فقط دو فیلد با «اطلاعات ارسالی نامعتبر است» (400 ModelState) رد می‌شد.
        // چرخه هفدهم (گزارش یازدهم): نویسه‌های نامرئی جهت (که در کادرهای ماسک
        // دیده نمی‌شوند) پیش از ارسال پاک می‌شوند تا رمزِ تایپ‌شده با رمزِ
        // رسیده به سرور یکسان بماند
        var ok = await _api.PostAsync("auth/change-password", new
        {
            currentPassword = InputSanitizer.StripDirectionMarks(CurrentPassword),
            newPassword = InputSanitizer.StripDirectionMarks(NewPassword),
            confirmPassword = InputSanitizer.StripDirectionMarks(ConfirmPassword)
        });

        IsBusy = false;
        ChangePasswordCommand.NotifyCanExecuteChanged();

        if (ok)
        {
            // گیت لایسنس — چرخه هفدهم (گزارش یازدهم): پاسخ صریحِ «لایسنس نامعتبر»
            // فعال‌سازی را الزامی می‌کند؛ وضعیت نامعلوم (قطعی شبکه) مانع ادامه نمی‌شود
            var verdict = await LicenseGate.CheckAsync(_api);
            if (verdict == LicenseGate.Verdict.Invalid)
            {
                LicenseActivationRequired?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ChangeSucceeded?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            ErrorMessage = _api.LastError ?? "تغییر رمز عبور ناموفق بود";
            OnPropertyChanged(nameof(HasError));
        }
    }

    [ObservableProperty]
    private string _passwordPolicyHint = "رمز جدید باید با سیاست امنیتی درمانگاه سازگار باشد و با رمزهای اخیر یکسان نباشد.";

    private async Task LoadPasswordPolicyAsync()
    {
        var response = await _api.GetAsync<PasswordPolicyResponse>("users/password-policy");
        if (response?.Data is not { } policy) return;
        var requirements = new List<string> { $"حداقل {policy.MinPasswordLength} نویسه" };
        if (policy.RequireMixedCase) requirements.Add("حروف کوچک و بزرگ");
        if (policy.RequireDigit) requirements.Add("عدد");
        if (policy.RequireSpecialChar) requirements.Add("نویسهٔ ویژه");
        requirements.Add("متفاوت با رمزهای اخیر");
        PasswordPolicyHint = string.Join("، ", requirements);
    }

    private sealed class PasswordPolicyResponse
    {
        public bool IsSuccess { get; set; }
        public PasswordPolicyDto? Data { get; set; }
    }

    private sealed class PasswordPolicyDto
    {
        public int MinPasswordLength { get; set; }
        public bool RequireMixedCase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecialChar { get; set; }
    }

    /// <summary>
    /// امکان ثبت — هر سه کادر پر باشد؛ تمام محدودیت‌های پویا در سرور بررسی
    /// می‌شوند تا حداقل‌طول یا الزام غیرفعال‌شده از تنظیمات نقض نشود
    /// </summary>
    private bool CanChange() =>
        !IsBusy
        && !string.IsNullOrEmpty(CurrentPassword)
        && !string.IsNullOrEmpty(NewPassword)
        && !string.IsNullOrEmpty(ConfirmPassword);

    /// <summary>به‌روزرسانی امکان دکمه با تغییر کادرها</summary>
    partial void OnCurrentPasswordChanged(string value) => ChangePasswordCommand.NotifyCanExecuteChanged();

    /// <summary>به‌روزرسانی امکان دکمه با تغییر کادرها</summary>
    partial void OnNewPasswordChanged(string value) => ChangePasswordCommand.NotifyCanExecuteChanged();

    /// <summary>به‌روزرسانی امکان دکمه با تغییر کادرها</summary>
    partial void OnConfirmPasswordChanged(string value) => ChangePasswordCommand.NotifyCanExecuteChanged();
}
