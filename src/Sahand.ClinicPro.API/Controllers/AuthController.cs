using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;
using Sahand.ClinicPro.Application.Features.Auth.Commands.Login;
using Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;
using Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

namespace Sahand.ClinicPro.API.Controllers.Auth;

/// <summary>
/// کنترلر احراز هویت — ورود و تمدید توکن و خروج و تغییر رمز
/// مسیرها: /api/v1/auth
/// محدودیت نرخ: ۵ درخواست بر دقیقه برای ورود
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>سازنده با تزریق میانجی و دستیاب HttpContext</summary>
    public AuthController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// ورود به سامانه و دریافت توکن‌ها
    /// </summary>
    /// <remarks>پنج تلاش ناموفق پیاپی حساب را ۳۰ دقیقه قفل می‌کند</remarks>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();

        var result = await _mediator.Send(new LoginCommand(
            request.UsernameOrEmail, request.Password, ip, device));

        return ToResponse(result, "ورود با موفقیت انجام شد");
    }

    /// <summary>
    /// تمدید توکن دسترسی با توکن تازه‌سازی — همراه چرخش توکن
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken, ip));
        return ToResponse(result, "توکن با موفقیت تمدید شد");
    }

    /// <summary>
    /// خروج و ابطال توکن تازه‌سازی
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken));
        return ToResponse(Domain.Common.Result.Success(), "خروج با موفقیت انجام شد");
    }

    /// <summary>
    /// تغییر رمز عبور کاربر واردشده فعلی
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(
            request.CurrentPassword, request.NewPassword));
        return ToResponse(result, "رمز عبور با موفقیت تغییر کرد");
    }
}
