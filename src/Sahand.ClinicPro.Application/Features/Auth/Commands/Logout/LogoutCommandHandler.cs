using MediatR;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;

/// <summary>
/// گرداننده خروج — ابطال توکن تامه‌سازی در دیتابیس
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;

    /// <summary>سازنده با تزریق سرویس احراز هویت</summary>
    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>اجرای فرمان خروج</summary>
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _identityService.RevokeTokenAsync(request.RefreshToken, cancellationToken);
        return Result.Success();
    }
}
