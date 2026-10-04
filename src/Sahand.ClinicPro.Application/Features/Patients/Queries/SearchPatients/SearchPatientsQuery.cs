using MediatR;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;

/// <summary>
/// پرس‌وجوی جستجوی سریع بیماران — برای فیلد جستجوی سراسری WPF
/// </summary>
/// <param name="SearchTerm">عبارت جستجو در نام و کد ملی و موبایل و پرونده</param>
/// <param name="MaxResults">حداکثر نتایج — پیش‌فرض ۲۰</param>
public record SearchPatientsQuery(string SearchTerm, int MaxResults = 20)
    : IRequest<Result<List<PatientListDto>>>;
