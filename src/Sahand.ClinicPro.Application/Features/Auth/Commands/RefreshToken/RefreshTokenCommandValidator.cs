using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// اعتبارسنج فرمان تمدید توکن
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("توکن تازه‌سازی الزامی است");
    }
}
