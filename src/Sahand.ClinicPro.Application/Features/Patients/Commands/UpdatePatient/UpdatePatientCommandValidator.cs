using FluentValidation;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// اعتبارسنج ویرایش بیمار — کد ملی با الگوریتم رقم کنترلی (ضد جعل)
/// و یکتایی به‌استثنای خود بیمار
/// </summary>
public class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public UpdatePatientCommandValidator(IPatientRepository patientRepository)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("شناسه بیمار نامعتبر است");

        RuleFor(x => x.Dto.NationalCode)
            .NotEmpty().WithMessage("کد ملی الزامی است")
            .Length(10).WithMessage("کد ملی باید دقیقاً ۱۰ رقم باشد")
            .Must(NationalCodeValidator.IsValid).WithMessage(NationalCodeValidator.InvalidMessage)
            .MustAsync(async (cmd, code, ct) =>
                !await patientRepository.ExistsByNationalCodeAsync(code, cmd.Id, ct))
                .WithMessage("بیمار دیگری با این کد ملی قبلاً ثبت شده است");

        RuleFor(x => x.Dto.FirstName)
            .NotEmpty().WithMessage("نام الزامی است")
            .MaximumLength(50).WithMessage("نام حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.LastName)
            .NotEmpty().WithMessage("نام خانوادگی الزامی است")
            .MaximumLength(50).WithMessage("نام خانوادگی حداکثر ۵۰ کاراکتر است");

        RuleFor(x => x.Dto.Mobile)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches("^09\\d{9}$").WithMessage("شماره موبایل معتبر نیست — الگوی 09XXXXXXXXX");

        RuleFor(x => x.Dto.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.Email))
            .WithMessage("ایمیل معتبر نیست");
    }
}
