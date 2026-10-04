using FluentValidation;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// اعتبارسنج ویرایش بیمار
/// </summary>
public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("شناسه بیمار نامعتبر است");

        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("نام الزامی است")
            .MaximumLength(50).WithMessage("نام حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است")
            .MaximumLength(50).WithMessage("نام خانوادگی حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست");
    }
}
