using MediatR;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;

/// <summary>
/// پرس‌وجوی فهرست صفحه‌بندی‌شده بیماران
/// </summary>
/// <param name="Page">پارامترهای صفحه‌بندی و مرتب‌سازی و جستجو</param>
public record GetAllPatientsQuery(PagedRequest? Page = null) : IRequest<Result<PagedResponse<PatientListDto>>>;
