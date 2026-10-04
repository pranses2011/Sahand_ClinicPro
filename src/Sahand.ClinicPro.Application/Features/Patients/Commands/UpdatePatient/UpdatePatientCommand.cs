using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;

/// <summary>
/// فرمان ویرایش اطلاعات بیمار
/// </summary>
/// <param name="Id">شناسه بیمار</param>
/// <param name="Dto">داده‌های ویرایش</param>
public record UpdatePatientCommand(long Id, UpdatePatientDto Dto) : IRequest<Result>;
