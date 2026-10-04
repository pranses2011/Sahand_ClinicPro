using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// گرداننده تمدید توکن — چرخش توکن تامه‌سازی و صدور جفت جدید
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public RefreshTokenCommandHandler(IIdentityService identityService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>اجرای فرمان تمدید</summary>
    public async Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.RefreshTokenAsync(
            request.RefreshToken, request.IpAddress, cancellationToken);

        if (!result.Succeeded)
            return Result<LoginResponseDto>.Failure(
                new Error("REFRESH_FAILED", result.Error ?? "توکن تازه‌سازی نامعتبر است"));

        return Result<LoginResponseDto>.Success(new LoginResponseDto
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            ExpiresIn = result.ExpiresIn
        });
    }
}
