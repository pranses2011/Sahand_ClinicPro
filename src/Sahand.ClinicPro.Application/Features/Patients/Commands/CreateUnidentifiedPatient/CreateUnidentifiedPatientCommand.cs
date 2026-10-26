using MediatR;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Application.DTOs.Patients;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreateUnidentifiedPatient;

/// <summary>
/// فرمان ساخت پروندهٔ موقت بیمار ناشناس (اورژانسی) — بدون کد ملی،
/// با کد موقت یکتای سیستمی؛ کد ملی برای ثبت عادی همچنان الزامی است
/// </summary>
public record CreateUnidentifiedPatientCommand(CreateUnidentifiedPatientDto Dto)
    : IRequest<Result<PatientListDto>>;
