using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Doctors;
using Sahand.ClinicPro.Application.Features.Doctors.Commands.CreateDoctor;
using Sahand.ClinicPro.Application.Features.Doctors.Commands.DeleteDoctor;
using Sahand.ClinicPro.Application.Features.Doctors.Commands.UpdateDoctor;
using Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctorById;
using Sahand.ClinicPro.Application.Features.Doctors.Queries.GetDoctors;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پزشکان — مدیریت اطلاعات پزشکان و تخصص‌ها
/// مسیرها: /api/v1/doctors
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
[Authorize]
public class DoctorsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public DoctorsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// فهرست پزشکان با فیلتر تخصص
    /// </summary>
    /// <param name="page">شماره صفحه</param>
    /// <param name="pageSize">اندازه صفحه</param>
    /// <param name="search">جستجو در نام و کد نظام پزشکی</param>
    /// <param name="specialtyId">فیلتر تخصص</param>
    /// <param name="onlyActive">فقط فعال‌ها — پیش‌فرض بله</param>
    [HttpGet]
    [HasPermission("doctors.view")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] long? specialtyId = null,
        [FromQuery] bool onlyActive = true)
    {
        var result = await _mediator.Send(new GetDoctorsQuery(
            new PagedRequest { PageNumber = page, PageSize = pageSize, Search = search },
            specialtyId, onlyActive));
        return ToResponse(result);
    }

    /// <summary>
    /// جزئیات یک پزشک
    /// </summary>
    /// <param name="id">شناسه پزشک</param>
    [HttpGet("{id:long}")]
    [HasPermission("doctors.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetDoctorByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>
    /// ثبت پزشک جدید
    /// </summary>
    /// <param name="dto">داده‌های پزشک</param>
    [HttpPost]
    [HasPermission("doctors.create")]
    public async Task<IActionResult> Create([FromBody] SaveDoctorDto dto)
    {
        var result = await _mediator.Send(new CreateDoctorCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetById), new { id = result.Value }, "پزشک ثبت شد");
        return ToResponse(result);
    }

    /// <summary>
    /// ویرایش پزشک
    /// </summary>
    /// <param name="id">شناسه پزشک</param>
    /// <param name="dto">داده‌های ویرایش</param>
    [HttpPut("{id:long}")]
    [HasPermission("doctors.edit")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveDoctorDto dto)
    {
        var result = await _mediator.Send(new UpdateDoctorCommand(id, dto));
        return ToResponse(result, "اطلاعات پزشک به‌روزرسانی شد");
    }

    /// <summary>
    /// حذف نرم پزشک
    /// </summary>
    /// <param name="id">شناسه پزشک</param>
    [HttpDelete("{id:long}")]
    [HasPermission("doctors.delete")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _mediator.Send(new DeleteDoctorCommand(id));
        return ToResponse(result, "پزشک حذف شد");
    }
}
