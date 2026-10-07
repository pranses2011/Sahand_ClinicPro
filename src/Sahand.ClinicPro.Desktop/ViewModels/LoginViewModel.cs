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
}
