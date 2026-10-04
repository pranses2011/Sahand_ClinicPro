using MediatR;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// گرداننده تغییر رمز عبور — با تاریخچه و سیاست امنیتی
/// </summary>
public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    /// <summary>سازنده با تزریق سرویس‌ها</summary>
    public ChangePasswordCommandHandler(IIdentityService identityService,
        ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    /// <summary>اجرای فرمان تغییر رمز</summary>
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUser.UserId
            ?? throw new Domain.Exceptions.NotFoundException("کاربر", "فعلی");

        return await _identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);
    }
}
