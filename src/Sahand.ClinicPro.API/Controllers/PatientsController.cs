using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Patients;
using Sahand.ClinicPro.Application.Features.Patients.Commands.CreatePatient;
using Sahand.ClinicPro.Application.Features.Patients.Commands.DeletePatient;
using Sahand.ClinicPro.Application.Features.Patients.Commands.UpdatePatient;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetAllPatients;
using Sahand.ClinicPro.Application.Features.Patients.Queries.GetPatientById;
using Sahand.ClinicPro.Application.Features.Patients.Queries.SearchPatients;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر بیماران — مدیریت پرونده بیماران
/// مسیرها: /api/v1/patients
/// </summary>
[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public PatientsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// فهرست صفحه‌بندی‌شده بیماران
    /// </summary>
    /// <param name="page">شماره صفحه — پیش‌فرض ۱</param>
    /// <param name="pageSize">اندازه صفحه — حداکثر ۱۰۰</param>
    /// <param name="search">جستجو در نام و کد ملی و موبایل و پرونده</param>
    [HttpGet]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _mediator.Send(new GetAllPatientsQuery(
            new PagedRequest { PageNumber = page, PageSize = pageSize, Search = search }));
        return ToResponse(result);
    }

    /// <summary>
    /// جزئیات کامل یک بیمار
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    [HttpGet("{id:long}")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>
    /// جستجوی سریع بیماران — برای تکمیل خودکار
    /// </summary>
    /// <param name="term">عبارت جستجو</param>
    [HttpGet("search")]
    [HasPermission("patients.view")]
    public async Task<IActionResult> Search([FromQuery] string term)
    {
        var result = await _mediator.Send(new SearchPatientsQuery(term));
        return ToResponse(result);
    }

    /// <summary>
    /// ثبت بیمار جدید — شماره پرونده خودکار
    /// </summary>
    /// <param name="dto">داده‌های بیمار جدید</param>
    [HttpPost]
    [HasPermission("patients.create")]
    [ProducesResponseType(typeof(ApiResponse<PatientListDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var result = await _mediator.Send(new CreatePatientCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value.Id },
                $"بیمار با شماره پرونده {result.Value.MedicalRecordNo} ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// ویرایش اطلاعات بیمار
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    /// <param name="dto">داده‌های ویرایش</param>
    [HttpPut("{id:long}")]
    [HasPermission("patients.edit")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePatientDto dto)
    {
        var result = await _mediator.Send(new UpdatePatientCommand(id, dto));
        return ToResponse(result, "اطلاعات بیمار به‌روزرسانی شد");
    }

    /// <summary>
    /// حذف نرم بیمار — رکورد فیزیکی حذف نمی‌شود
    /// </summary>
    /// <param name="id">شناسه بیمار</param>
    [HttpDelete("{id:long}")]
    [HasPermission("patients.delete")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _mediator.Send(new DeletePatientCommand(id));
        return ToResponse(result, "بیمار حذف شد");
    }
}
