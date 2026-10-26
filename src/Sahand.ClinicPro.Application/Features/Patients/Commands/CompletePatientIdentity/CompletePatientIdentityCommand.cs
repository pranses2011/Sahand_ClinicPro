using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CompletePatientIdentity;

/// <summary>
/// فرمان تکمیل هویت بیمار ناشناس — ثبت مشخصات واقعی روی پروندهٔ موقت؛
/// اگر کد ملی از قبل پروندهٔ اصلی داشته باشد، نتیجه Duplicate برمی‌گردد
/// تا کاربر دربارهٔ ادغام تصمیم بگیرد (خطا نیست)
/// </summary>
public record CompletePatientIdentityCommand(long PatientId, CompletePatientIdentityDto Dto)
    : IRequest<Result<CompleteIdentityResultDto>>;
