using MediatR;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// پرس‌وجوی دریافت جزئیات کامل بیمار
/// </summary>
/// <param name="Id">شناسه بیمار</param>
public record GetPatientByIdQuery(long Id) : IRequest<Result<PatientDetailDto>>;
