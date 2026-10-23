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

    /* ═══════════ گیت لایسنس — چرخه هفدهم (گزارش یازدهم مالک) ═══════════
       پیش‌تر ورود موفق بدون هیچ بررسی لایسنسی مستقیم پنجرهٔ اصلی را باز می‌کرد؛
       اکنون پاسخ صریحِ «لایسنس نامعتبر» باید رویداد الزام فعال‌سازی بدهد نه ورود موفق */

    [Fact]
    public async Task Login_WithInvalidLicense_RaisesLicenseActivationRequired_NotLoginSucceeded()
    {
        var (vm, api) = Create();
        api.FakeMustChangePassword = false;
        api.FakeLoginSucceeds = true;
        api.CannedResponses["license/status"] = new { Data = new { IsValid = false } };

        var succeeded = 0;
        var activationRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.LicenseActivationRequired += (_, _) => activationRequired++;

        vm.Username = "admin";
        vm.Password = "Admin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(0, succeeded);
        Assert.Equal(1, activationRequired);
        Assert.True(vm.LicenseNeedsActivation);
        Assert.Contains(api.Calls, c => c.Method == "GET" && c.Endpoint == "license/status");
    }

    [Fact]
    public async Task Login_WithValidLicense_RaisesLoginSucceeded()
    {
        var (vm, api) = Create();
        api.FakeMustChangePassword = false;
        api.FakeLoginSucceeds = true;
        api.CannedResponses["license/status"] = new { Data = new { IsValid = true } };

        var succeeded = 0;
        var activationRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.LicenseActivationRequired += (_, _) => activationRequired++;

        vm.Username = "admin";
        vm.Password = "Admin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(1, succeeded);
        Assert.Equal(0, activationRequired);
        Assert.False(vm.LicenseNeedsActivation);
    }

    [Fact]
    public async Task Login_WhenLicenseStatusUnreachable_FailsOpen()
    {
        // قطعی شبکه / خطای سرور ⇒ پاسخ null ⇒ وضعیت «نامعلوم» ⇒ ورود مجاز
        // (درمانگاه نباید بر اثر اختلال لحظه‌ای سرور لایسنس از کار بیفتد)
        var (vm, api) = Create();
        api.FakeMustChangePassword = false;
        api.FakeLoginSucceeds = true;
        // هیچ پاسخ آماده‌ای برای license/status تنظیم نمی‌شود → null

        var succeeded = 0;
        var activationRequired = 0;
        vm.LoginSucceeded += (_, _) => succeeded++;
        vm.LicenseActivationRequired += (_, _) => activationRequired++;

        vm.Username = "admin";
        vm.Password = "Admin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(1, succeeded);
        Assert.Equal(0, activationRequired);
    }

    [Fact]
    public async Task Login_SanitizesInvisibleDirectionMarks_FromCredentials()
    {
        // نویسه‌های نامرئی جهت (RLM/LRM/…) که کیبورد فارسی ویندوز می‌تواند تزریق کند،
        // باید پیش از ارسال حذف شوند تا «رمز درست» روی سرور نادرست دیده نشود
        var (vm, api) = Create();
        api.FakeLoginSucceeds = true;

        vm.Username = " ad\u200Fmin ";
        vm.Password = "Ad\u200Fmin@123";
        await vm.LoginCommand.ExecuteAsync(null);

        Assert.NotNull(api.LastLoginCall);
        Assert.Equal("admin", api.LastLoginCall!.Value.Username);
        Assert.Equal("Admin@123", api.LastLoginCall!.Value.Password);
    }
}

/// <summary>
/// تست‌های پاک‌ساز ورودی — چرخه هفدهم (گزارش یازدهم مالک)
/// </summary>
public class InputSanitizerTests
{
    [Theory]
    [InlineData("Admin@123", "Admin@123")]          // سالم — دست‌نخورده
    [InlineData("Ad\u200Fmin@123", "Admin@123")]    // RLM
    [InlineData("Ad\u200Emin@123", "Admin@123")]    // LRM
    [InlineData("\u061CAdmin@123", "Admin@123")]    // Arabic Letter Mark
    [InlineData("\u202EAdmin@123", "Admin@123")]    // RLO
    [InlineData("Admin@123\u2066", "Admin@123")]    // LRI
    [InlineData("Ad\uFEFFmin@123", "Admin@123")]    // BOM
    [InlineData("", "")]
    public void StripDirectionMarks_RemovesInvisibleBidiControls(string input, string expected)
        => Assert.Equal(expected, Sahand.ClinicPro.Desktop.Services.InputSanitizer.StripDirectionMarks(input));

    [Fact]
    public void StripDirectionMarks_Null_ReturnsEmpty()
        => Assert.Equal(string.Empty,
            Sahand.ClinicPro.Desktop.Services.InputSanitizer.StripDirectionMarks(null));
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
    public async Task Change_Payload_IncludesConfirmPassword()
    {
        // گزارش هفتم مالک: سرور ChangePasswordRequestDto سه فیلد اجباری دارد و
        // نبودِ confirmPassword با 400 «اطلاعات ارسالی نامعتبر است» رد می‌شد —
        // این گارد مانع بازگشت همان ریشه است
        var (vm, api) = Create();
        api.NextBoolResult = true;
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "New@12345";
        vm.ConfirmPassword = "New@12345";

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.NotNull(api.LastBody);
        var payload = System.Text.Json.JsonSerializer.Serialize(api.LastBody);
        Assert.Contains("\"currentPassword\":\"Old@12345\"", payload);
        Assert.Contains("\"newPassword\":\"New@12345\"", payload);
        Assert.Contains("\"confirmPassword\":\"New@12345\"", payload);
    }

    [Fact]
    public void Change_ShortPassword_DoesNotDisableCommand()
    {
        // چرخهٔ ۷۹: حداقل طول ثابتِ ۸ نویسه از ViewModel برداشته شد؛ سیاست رمز
        // کاملاً از تنظیمات سرور («users/password-policy») می‌آید و ممکن است
        // ۶، ۸ یا ۱۲ نویسه باشد. پس کلاینت نباید با عدد ثابت، دکمه را قفل کند
        // (این تست همان گاردِ برگشت ناخواستهٔ حداقل ثابت است).
        var (vm, _) = Create();
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "short";
        vm.ConfirmPassword = "short";

        Assert.True(vm.ChangePasswordCommand.CanExecute(null),
            "ارزیابی طول رمز فقط وظیفهٔ سرور است، نه عدد ثابت در کلاینت");
    }

    [Fact]
    public void Change_EmptyPassword_DisablesCommand()
    {
        // گارد اصلی که باید بماند: کادر خالی ⇒ دکمهٔ ثبت قفل
        var (vm, _) = Create();
        vm.CurrentPassword = "Old@12345";
        vm.NewPassword = "";
        vm.ConfirmPassword = "";

        Assert.False(vm.ChangePasswordCommand.CanExecute(null));
    }
}
