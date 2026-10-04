using FluentValidation;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;

/// <summary>
/// اعتبارسنج ثبت بیمار — کد ملی با رقم کنترلی و یکتایی غیرهمگام
/// </summary>
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    /// <summary>قواعد اعتبارسنجی با پیام فارسی</summary>
    public CreatePatientCommandValidator(IPatientRepository patientRepository)
    {
        RuleFor(x => x.Dto.NationalCode)
            .NotEmpty().WithMessage("کد ملی الزامی است")
            .Length(10).WithMessage("کد ملی باید دقیقاً ۱۰ رقم باشد")
            .Must(NationalCodeValidator.IsValid).WithMessage(NationalCodeValidator.InvalidMessage)
            .MustAsync(async (code, ct) => !await patientRepository.ExistsByNationalCodeAsync(code, ct))
                .WithMessage("بیماری با این کد ملی قبلاً ثبت شده است");

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

        RuleFor(x => x.Dto.BirthDateShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi)
            .When(x => !string.IsNullOrEmpty(x.Dto.BirthDateShamsi))
            .WithMessage("تاریخ تولد شمسی نامعتبر است — الگوی 1403/07/12");
    }
}
