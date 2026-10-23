using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست تغییر رمز عبور توسط کاربر واردشده
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>رمز عبور فعلی</summary>
    [Required(ErrorMessage = "رمز عبور فعلی الزامی است")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>رمز عبور جدید — سیاست امنیتی پویا در سرویس هویت اعمال می‌شود</summary>
    [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>تکرار رمز عبور جدید</summary>
    [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
    [Compare(nameof(NewPassword), ErrorMessage = "تکرار رمز عبور با رمز جدید یکسان نیست")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
