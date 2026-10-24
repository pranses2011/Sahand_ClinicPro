namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// بدنهٔ درخواست «فراموشی رمز» — چرخهٔ ۸۲ (گزارش مالک).
/// </summary>
/// <remarks>
/// کاربر نام کاربری یا ایمیل خود را می‌دهد و سرور رمز موقت را به ایمیل
/// ثبت‌شدهٔ همان حساب می‌فرستد؛ هیچ رمزی در پاسخ HTTP برگردانده نمی‌شود.
/// </remarks>
public class ForgotPasswordRequestDto
{
    /// <summary>نام کاربری یا ایمیل ثبت‌شده در سامانه</summary>
    public string UsernameOrEmail { get; set; } = string.Empty;
}
