using System.ComponentModel.DataAnnotations;

namespace Sahand.ClinicPro.Application.DTOs.Auth;

/// <summary>
/// درخواست تمدید توکن با توکن تازه‌سازی
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>توکن تازه‌سازی دریافتی هنگام ورود</summary>
    [Required(ErrorMessage = "توکن تازه‌سازی الزامی است")]
    public string RefreshToken { get; set; } = string.Empty;
}
