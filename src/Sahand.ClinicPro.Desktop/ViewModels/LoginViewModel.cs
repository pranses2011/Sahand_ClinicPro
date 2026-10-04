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

    /// <summary>رویداد ورود موفق — پنجره را عوض می‌کند</summary>
    public event EventHandler? LoginSucceeded;

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

        var ok = await _api.LoginAsync(Username.Trim(), Password);

        IsBusy = false;
        LoginCommand.NotifyCanExecuteChanged();

        if (ok)
        {
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
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
}
