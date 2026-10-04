using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.Login;

/// <summary>
/// اعتبارسنج فرمان ورود — با پیام‌های فارسی
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>قواعد اعتبارسنجی ورود</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("نام کاربری الزامی است")
            .MaximumLength(100).WithMessage("نام کاربری حداکثر ۱۰۰ کاراکتر است");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است")
            .MaximumLength(128).WithMessage("رمز عبور حداکثر ۱۲۸ کاراکتر است");
    }
}
