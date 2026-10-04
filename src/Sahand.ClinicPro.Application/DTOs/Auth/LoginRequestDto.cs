using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست ورود به سامانه — با نام کاربری یا ایمیل
/// محدودیت نرخ: ۵ درخواست بر دقیقه و قفل ۳۰ دقیقه‌ای بعد از ۵ شکست
/// </summary>
public class LoginRequestDto
{
    /// <summary>نام کاربری یا ایمیل</summary>
    [Required(ErrorMessage = "نام کاربری الزامی است")]
    [MaxLength(100, ErrorMessage = "نام کاربری حداکثر ۱۰۰ کاراکتر است")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>رمز عبور</summary>
    [Required(ErrorMessage = "رمز عبور الزامی است")]
    public string Password { get; set; } = string.Empty;
}
