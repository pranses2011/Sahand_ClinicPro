using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sahand.ClinicPro.Desktop.Services;

namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// ViewModel ورود — فراخوانی ورود API و مدیریت پیام خطا
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _api;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>مرا به خاطر بسپار — فقط نام کاربری ذخیره می‌شود نه رمز</summary>
    [ObservableProperty]
    private bool _rememberMe;

    /// <summary>آیا دکمه فعال‌سازی لایسنس نمایش داده شود — وقتی لایسنس فعال نیست</summary>
    [ObservableProperty]
    private bool _licenseNeedsActivation;

    /// <summary>رویداد ورود موفق — پنجره را عوض می‌کند</summary>
    public event EventHandler? LoginSucceeded;

    /// <summary>
    /// رویداد الزام تغییر رمز — سرور اعلام کرده رمز باید عوض شود (چرخه چهاردهم، گزارش پنجم مالک).
    /// راهنمای SQL قدم ۷ قول داده بود برنامه بعد از ورود اولِ admin (بذرافشان با
    /// MustChangePassword=true) «بلافاصله و دورزدنی‌ناپذیر» تغییر رمز بخواهد؛
    /// پیش‌تر این جریان در دسکتاپ وجود نداشت و پرچم سرور نادیده گرفته می‌شد
    /// </summary>
    public event EventHandler? PasswordChangeRequired;

    /// <summary>رویداد درخواست فعال‌سازی لایسنس</summary>
    public event EventHandler? LicenseActivationRequested;

    /// <summary>
    /// رویداد الزام فعال‌سازی لایسنس — چرخه هفدهم (گزارش یازدهم مالک):
    /// ورود به سرور موفق بوده اما لایسنس فعال نیست؛ تا فعال‌سازی، پوستهٔ اصلی
    /// باز نمی‌شود. پیش‌تر هیچ بررسی لایسنسی در جریان ورود وجود نداشت و با
    /// رمز درست بدون لایسنس هم وارد برنامه می‌شدند.
    /// </summary>
    public event EventHandler? LicenseActivationRequired;

    /// <summary>سازنده با تزریق کلاینت API</summary>
    public LoginViewModel(IApiClient api) => _api = api;

    /// <summary>آیا پیام خطا نمایش داده شود</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>دکمه ورود</summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();

        var ok = await _api.LoginAsync(
            InputSanitizer.StripDirectionMarks(Username).Trim(),
            InputSanitizer.StripDirectionMarks(Password));

        IsBusy = false;
        LoginCommand.NotifyCanExecuteChanged();

        if (ok)
        {
            // سرور تغییر رمز را الزامی کرده؟ — تا تغییرِ رمز، ورود کامل نمی‌شود
            if (_api.LastLoginMustChangePassword)
            {
                PasswordChangeRequired?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // گیت لایسنس — چرخه هفدهم (گزارش یازدهم): پاسخ «لایسنس نامعتبر»
                // یعنی پیش از پوستهٔ اصلی باید فعال‌سازی انجام شود؛ وضعیت
                // «نامعلوم» (قطعی شبکه) اما ورود را نمی‌بندد تا درمانگاه بر اثر
                // اختلال لحظه‌ای سرور از کار نیفتد
                var verdict = await LicenseGate.CheckAsync(_api);
                LicenseNeedsActivation = verdict == LicenseGate.Verdict.Invalid;
                if (verdict == LicenseGate.Verdict.Invalid)
                {
                    LicenseActivationRequired?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    LoginSucceeded?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        else
        {
            ErrorMessage = _api.LastError ?? "ورود ناموفق بود";
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>امکان ورود — نام کاربری و رمز پر شده باشند</summary>
    private bool CanLogin() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrEmpty(Password);

    /// <summary>به‌روزرسانی امکان دکمه با تغییر فیلدها</summary>
    partial void OnUsernameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// نمایش پیام راهنما روی صفحهٔ ورود — چرخه هفدهم (گزارش یازدهم):
    /// پنجره فعال‌سازی بدون نتیجه بسته شد و کاربر به صفحهٔ ورود بازگشته؛
    /// HasError محاسباتی است و باید پس از ErrorMessage دستی اطلاع‌رسانی شود
    /// </summary>
    public void ShowLoginError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(HasError));
    }

    /// <summary>فعال‌سازی لایسنس — رویداد به پنجره برای باز کردن دیالوگ فعال‌سازی</summary>
    [RelayCommand]
    private void ActivateLicense()
    {
        LicenseActivationRequested?.Invoke(this, EventArgs.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════
    // چرخهٔ ۸۲ (گزارش مالک) — «فراموشی رمز» صفحهٔ ورود
    // «در صفحهٔ ورود، فراموشی رمز کار نمی‌کند؛ رمز را به ایمیل کاربر بفرست»
    // ═══════════════════════════════════════════════════════════════════
    // پیش‌تر «فراموشی رمز؟» فقط یک TextBlock با ToolTip بود («با مدیر تماس
    // بگیرید»). اکنون یک پنل درون‌کارتی باز می‌شود: کاربر نام کاربری یا ایمیل
    // می‌دهد، سامانه رمز موقت را به ایمیل ثبت‌شده می‌فرستد و پاسخ همیشه یکسان
    // است (ضد افشای وجود حساب).

    /// <summary>پنل «فراموشی رمز» باز است؟</summary>
    [ObservableProperty]
    private bool _isForgotOpen;

    /// <summary>ورودی بازیابی — نام کاربری یا ایمیل</summary>
    [ObservableProperty]
    private string _forgotInput = string.Empty;

    /// <summary>پیام نتیجهٔ ارسال نامهٔ بازیابی</summary>
    [ObservableProperty]
    private string _forgotMessage = string.Empty;

    /// <summary>رنگ پیام بازیابی — سبز موفق، قرمز خطا</summary>
    [ObservableProperty]
    private string _forgotMessageColor = "#2E7D32";

    /// <summary>نمایش پیام نتیجه؟</summary>
    public bool HasForgotMessage => !string.IsNullOrEmpty(ForgotMessage);

    /// <summary>فرم ورود دیده شود؟ — هنگام باز بودن پنل بازیابی پنهان می‌شود</summary>
    public bool IsLoginFormVisible => !IsForgotOpen;

    /// <summary>اطلاع‌رسانی به رابط — پنل بازیابی و فرم ورود جای یکدیگر را می‌گیرند</summary>
    partial void OnIsForgotOpenChanged(bool value) => OnPropertyChanged(nameof(IsLoginFormVisible));

    /// <summary>باز کردن پنل — ورودی با نام کاربریِ فعلی فرم پیش‌پر می‌شود</summary>
    [RelayCommand]
    private void OpenForgot()
    {
        ForgotInput = Username;
        ForgotMessage = string.Empty;
        OnPropertyChanged(nameof(HasForgotMessage));
        IsForgotOpen = true;
    }

    /// <summary>بستن پنل و بازگشت به فرم ورود</summary>
    [RelayCommand]
    private void CloseForgot()
    {
        IsForgotOpen = false;
        ForgotMessage = string.Empty;
        OnPropertyChanged(nameof(HasForgotMessage));
    }

    /// <summary>
    /// ارسال درخواست بازیابی — فراخوانی ناشناس (کاربر هنوز وارد نشده است).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendForgot))]
    private async Task SendForgotAsync()
    {
        IsBusy = true;
        ForgotMessage = string.Empty;

        var ok = await _api.PostAnonymousAsync("auth/forgot-password",
            new { usernameOrEmail = ForgotInput.Trim() });

        IsBusy = false;
        if (ok)
        {
            ForgotMessageColor = "#2E7D32";
            ForgotMessage = "اگر حسابی با این مشخصات وجود داشته باشد، رمز موقت به ایمیل ثبت‌شدهٔ آن ارسال شد. "
                + "پس از دریافت، با همان رمز وارد شوید و رمز تازه بگذارید (پوشهٔ هرزنامه را هم ببینید).";
        }
        else
        {
            ForgotMessageColor = "#C62828";
            ForgotMessage = _api.LastError ?? "ارسال نامهٔ بازیابی ناموفق بود — با مدیر سامانه تماس بگیرید";
        }

        OnPropertyChanged(nameof(HasForgotMessage));
        SendForgotCommand.NotifyCanExecuteChanged();
    }

    /// <summary>امکان ارسال — ورودی پر و درخواست در جریان نباشد</summary>
    private bool CanSendForgot() => !IsBusy && !string.IsNullOrWhiteSpace(ForgotInput);

    /// <summary>به‌روزرسانی امکان دکمهٔ ارسال با تغییر ورودی</summary>
    partial void OnForgotInputChanged(string value) => SendForgotCommand.NotifyCanExecuteChanged();
}
