using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.People;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CompletePatientIdentity;

/// <summary>
/// گرداننده تکمیل هویت — کد ملی با رقم کنترلی و موبایل با الگو اعتبارسنجی
/// می‌شود؛ در صورت وجود پروندهٔ اصلی با همان کد ملی، هیچ تغییری اعمال
/// نمی‌شود و نتیجهٔ Duplicate (همراه شناسه/نام پروندهٔ اصلی) برمی‌گردد
/// تا کلاینت پیشنهاد ادغام بدهد و هیچ سابقه‌ای گم نشود.
/// </summary>
public class CompletePatientIdentityCommandHandler
    : IRequestHandler<CompletePatientIdentityCommand, Result<CompleteIdentityResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CompletePatientIdentityCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>اجرای فرمان تکمیل هویت</summary>
    public async Task<Result<CompleteIdentityResultDto>> Handle(CompletePatientIdentityCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var repo = _unitOfWork.Repository<Patient>();

        var patient = await repo.QueryableTracking
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result<CompleteIdentityResultDto>.Failure(Error.NotFound("بیمار", request.PatientId));
        if (!patient.IsUnidentified)
            return Result<CompleteIdentityResultDto>.Failure(
                new Error("Patient.AlreadyIdentified", "هویت این پرونده قبلاً تکمیل شده است"));

        var nationalCode = (dto.NationalCode ?? string.Empty).Trim();
        if (!NationalCodeValidator.IsValid(nationalCode))
            return Result<CompleteIdentityResultDto>.Failure(
                new Error("Patient.InvalidNationalCode", NationalCodeValidator.InvalidMessage));

        var mobile = (dto.Mobile ?? string.Empty).Trim();
        if (!Regex.IsMatch(mobile, @"^09\d{9}$"))
            return Result<CompleteIdentityResultDto>.Failure(
                new Error("Patient.InvalidMobile", "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود"));

        // پروندهٔ اصلی با همین کد ملی از قبل وجود دارد؟ → پیشنهاد ادغام، بدون تغییر
        var existing = await repo.QueryableNoTracking
            .FirstOrDefaultAsync(p => p.NationalCode == nationalCode && p.Id != patient.Id,
                cancellationToken);
        if (existing is not null)
        {
            return Result<CompleteIdentityResultDto>.Success(new CompleteIdentityResultDto
            {
                Status = "Duplicate",
                PatientId = patient.Id,
                DuplicatePatientId = existing.Id,
                DuplicatePatientName = $"{existing.FirstName} {existing.LastName}"
            });
        }

        patient.FirstName = dto.FirstName.Trim();
        patient.LastName = dto.LastName.Trim();
        patient.NationalCode = nationalCode;
        patient.Mobile = mobile;
        patient.Gender = dto.Gender == 2 ? Gender.Female : Gender.Male;
        patient.IsUnidentified = false;

        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<CompleteIdentityResultDto>.Success(new CompleteIdentityResultDto
        {
            Status = "Completed",
            PatientId = patient.Id
        });
    }
}
