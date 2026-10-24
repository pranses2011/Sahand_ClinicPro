using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// گرداننده فرمان فراموشی رمز — منطق کامل در <see cref="IIdentityService"/>
/// است (جستجوی حساب، تولید رمز موقت، ارسال ایمیل و ثبت تغییر).
/// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    /// <summary>سازنده با تزریق سرویس احراز هویت و ثبت‌گر</summary>
    public ForgotPasswordCommandHandler(IIdentityService identityService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>اجرای فرمان — پیام موفقیت یکسان برای همهٔ ورودی‌ها</summary>
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.SendPasswordRecoveryAsync(
            request.UsernameOrEmail, cancellationToken);

        if (result.IsFailure)
            _logger.LogWarning("بازیابی رمز ناموفق بود برای {Input}: {Message}",
                request.UsernameOrEmail, result.Error.Message);

        return result;
    }
}
