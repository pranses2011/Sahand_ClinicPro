using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>اعتبارسنج فرمان فراموشی رمز — ورودی خالی بی‌معناست</summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("نام کاربری یا ایمیل خود را وارد کنید")
            .MaximumLength(256).WithMessage("نام کاربری یا ایمیل بیش از حد بلند است");
    }
}
