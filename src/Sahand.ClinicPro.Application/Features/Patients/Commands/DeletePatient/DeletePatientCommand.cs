using MediatR;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;

/// <summary>
/// فرمان حذف نرم بیمار — رکورد فیزیکی حذف نمی‌شود
/// </summary>
/// <param name="Id">شناسه بیمار</param>
public record DeletePatientCommand(long Id) : IRequest<Result>;
