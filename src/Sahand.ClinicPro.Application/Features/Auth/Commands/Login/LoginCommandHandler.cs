using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Login;

/// <summary>
/// گرداننده ورود — احراز هویت از IIdentityService و ساخت پاسخ توکن
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<LoginCommandHandler> _logger;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public LoginCommandHandler(IIdentityService identityService, ILogger<LoginCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>اجرای فرمان ورود</summary>
    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.AuthenticateAsync(
            request.UsernameOrEmail, request.Password, request.IpAddress, request.DeviceInfo,
            cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("ورود ناموفق برای {Username} از {Ip}",
                request.UsernameOrEmail, request.IpAddress ?? "نامشخص");
            return Result<LoginResponseDto>.Failure(
                new Error("LOGIN_FAILED", result.Error ?? "نام کاربری یا رمز عبور نادرست است"));
        }

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn,
            Username = result.Username!,
            DisplayName = result.DisplayName!,
            Roles = result.Roles,
            Permissions = result.Permissions,
            MustChangePassword = result.MustChangePassword
        });
    }
}
