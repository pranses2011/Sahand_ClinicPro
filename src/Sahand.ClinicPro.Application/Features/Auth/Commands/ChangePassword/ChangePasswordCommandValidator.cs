using FluentValidation;
using Sahand.ClinicPro.Application.Common.Helpers;

namespace Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;

/// <summary>
/// اعتبارسنج فرمان تغییر رمز عبور
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("رمز عبور فعلی الزامی است");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("رمز عبور جدید الزامی است")
            .NotEqual(x => x.CurrentPassword).WithMessage("رمز جدید نباید با رمز فعلی یکسان باشد");
    }
}
