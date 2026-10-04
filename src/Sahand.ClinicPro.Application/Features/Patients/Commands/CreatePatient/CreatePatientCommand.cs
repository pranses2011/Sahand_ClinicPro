using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;

/// <summary>
/// فرمان ثبت بیمار جدید — شماره پرونده خودکار و رویداد پیامک خوش‌آمدگویی
/// </summary>
/// <param name="Dto">داده‌های بیمار جدید</param>
public record CreatePatientCommand(CreatePatientDto Dto) : IRequest<Result<PatientListDto>>;
