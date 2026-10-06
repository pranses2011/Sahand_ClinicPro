using Sahand.ClinicPro.Desktop.Services;
using Sahand.ClinicPro.Desktop.ViewModels;
using Xunit;

namespace Sahand.ClinicPro.Desktop.UnitTests;

/// <summary>
/// تست‌های جریان ورود و تغییر رمز اجباری — چرخه چهاردهم (گزارش پنجم مالک):
/// پرچم mustChangePassword سرور باید ورود موفق را به پنجره تغییر رمز هدایت کند
/// نه مستقیم به پنجره اصلی — قول قدم ۷ راهنمای SQL.
/// </summary>
public class LoginFlowTests
{
    private static (LoginViewModel Vm, FakeApiClient Api) Create()
    {
        var api = new FakeApiClient();
        return (new LoginViewModel(api), api);
    }

    [Fact]
    public async Task Login_WithoutPasswordChangeFlag_RaisesLoginSucceeded()
    {
        var (vm, api) = Create();
        api.FakeMustChangePassword = false;
        api.FakeLoginSucceeds = true;

        var succeeded = 0;
        var changeRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.PasswordChangeRequired += (_, _) => changeRequired++;

        vm.Username = "admin";
        vm.Password = "Admin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(1, succeeded);
        Assert.Equal(0, changeRequired);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task Login_WithPasswordChangeFlag_RaisesPasswordChangeRequired()
    {
        var (vm, api) = Create();
        api.FakeMustChangePassword = true;
        api.FakeLoginSucceeds = true;

        var succeeded = 0;
        var changeRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.PasswordChangeRequired += (_, _) => changeRequired++;

        vm.Username = "admin";
        vm.Password = "Admin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        // ورود «نیمه‌کاره» مانده — تا تغییر رمز پنجره اصلی باز نمی‌شود
        Assert.Equal(0, succeeded);
        Assert.Equal(1, changeRequired);
    }

    [Fact]
    public async Task Login_Failure_ShowsServerErrorWithoutEvents()
    {
        var (vm, api) = Create();
        api.FakeLoginSucceeds = false;
        api.FakeError = "نام کاربری یا رمز عبور نادرست است";

        var succeeded = 0;
        var changeRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.PasswordChangeRequired += (_, _) => changeRequired++;

        vm.Username = "admin";
        vm.Password = "wrong";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(0, succeeded);
        Assert.Equal(0, changeRequired);
        Assert.True(vm.HasError);
        Assert.Equal("نام کاربری یا رمز عبور نادرست است", vm.ErrorMessage);
    }
}

/// <summary>
/// تست‌های ViewModel پنجره تغییر رمز اجباری
/// </summary>
public class ChangePasswordViewModelTests
{
    private static (ChangePasswordViewModel Vm, FakeApiClient Api) Create()
    {
        var api = new FakeApiClient();
        return (new ChangePasswordViewModel(api), api);
    }

    [Fact]
    public async Task Change_MismatchedConfirm_ShowsErrorWithoutCallingApi()
    {
        var (vm, api) = Create();
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "New@12345";
        vm.ConfirmPassword = "Different@12345";

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Equal("تکرار رمز جدید با خودِ رمز یکسان نیست", vm.ErrorMessage);
        // خطای محلی — هیچ رفت‌وبرگشتی به سرور نبوده
        Assert.DoesNotContain(api.Calls, c => c.Endpoint == "auth/change-password");
    }

    [Fact]
    public async Task Change_Success_RaisesChangeSucceeded()
    {
        var (vm, api) = Create();
        api.NextBoolResult = true;
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "New@12345";
        vm.ConfirmPassword = "New@12345";

        var succeeded = 0;
        vm.ChangeSucceeded += (_, _) => succeeded++;

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal(1, succeeded);
        Assert.False(vm.HasError);
        Assert.Contains(api.Calls, c => c.Method == "POST-bool" && c.Endpoint == "auth/change-password");
    }

    [Fact]
    public async Task Change_ServerRejects_ShowsPersianError()
    {
        var (vm, api) = Create();
        api.NextBoolResult = false;
        api.FakeError = "رمز عبور جدید نباید با رمزهای اخیر یکسان باشد";
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "New@12345";
        vm.ConfirmPassword = "New@12345";

        var succeeded = 0;
        vm.ChangeSucceeded += (_, _) => succeeded++;

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal(0, succeeded);
        Assert.True(vm.HasError);
        Assert.Equal("رمز عبور جدید نباید با رمزهای اخیر یکسان باشد", vm.ErrorMessage);
    }

    [Fact]
    public void Change_ShortPassword_DisablesCommand()
    {
        var (vm, _) = Create();
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "short";
        vm.ConfirmPassword = "short";

        Assert.False(vm.ChangePasswordCommand.CanExecute(null));
    }
}
