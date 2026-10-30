using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Domain.Common;
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Visits;
using Sahand.ClinicPro.Application.Features.Visits.Commands.SaveVisit;
using Sahand.ClinicPro.Application.Features.Visits.Queries;
using Sahand.ClinicPro.Application.Features.Visits.GetVisitQueue;
using Sahand.ClinicPro.Application.Features.Visits.CallNextPatient;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر ویزیت — پرونده بالینی با الگوی SOAP
/// مسیرها: /api/v1/visits
/// </summary>
[ApiController]
[Route("api/v1/visits")]
[Authorize]
public class VisitsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public VisitsController(IMediator mediator) => _mediator = mediator;

    /// <summary>تاریخچه ویزیت‌های یک بیمار</summary>
    /// <param name="patientId">شناسه بیمار</param>
    [HttpGet("patient/{patientId:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetByPatient(long patientId)
    {
        var result = await _mediator.Send(new GetPatientVisitsQuery(patientId));
        return ToResponse(result);
    }

    /// <summary>جزئیات کامل یک ویزیت</summary>
    /// <param name="id">شناسه ویزیت</param>
    [HttpGet("{id:long}")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _mediator.Send(new GetVisitByIdQuery(id));
        return ToResponse(result);
    }

    /// <summary>ذخیره یا تکمیل ویزیت — ذخیره تدریجی SOAP پشتیبانی می‌شود</summary>
    /// <param name="dto">داده ویزیت</param>
    [HttpPost]
    [HasPermission("visits.create")]
    public async Task<IActionResult> Save([FromBody] SaveVisitDto dto)
    {
        try
        {
            var result = await _mediator.Send(new SaveVisitCommand(dto));
            if (result.IsFailure && result.Error.Code == "VISIT.CONFLICT") return Conflict(ApiResponse<object>.Failure(result.Error.Message, 409));
            if (result.IsFailure) return ToResponse(result);
            return CreatedResponse(Result<long>.Success(result.Value.Id), nameof(GetById), new { id = result.Value.Id }, "ویزیت ذخیره شد");
        }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.Failure("ویزیت همزمان تغییر کرد؛ اطلاعات را دوباره بارگذاری کنید.", 409)); }
    }

    /// <summary>ذخیره با دریافت نسخهٔ دقیق برای ویرایش بعدی.</summary>
    [HttpPost("save-versioned")]
    [HasPermission("visits.create")]
    public async Task<IActionResult> SaveVersioned([FromBody] SaveVisitDto dto)
    {
        try
        {
            var result = await _mediator.Send(new SaveVisitCommand(dto));
            if (result.IsFailure && result.Error.Code == "VISIT.CONFLICT") return Conflict(ApiResponse<object>.Failure(result.Error.Message, 409));
            if (result.IsFailure) return ToResponse(result);
            return CreatedResponse(result, nameof(GetById), new { id = result.Value.Id }, "ویزیت ذخیره شد");
        }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.Failure("ویزیت همزمان تغییر کرد؛ اطلاعات را دوباره بارگذاری کنید.", 409)); }
    }

    /// <summary>صف ویزیت امروز — فعلی + در انتظار + انجام‌شده</summary>
    /// <param name="doctorId">فیلتر پزشک — تهی یعنی همه</param>
    [HttpGet("queue")]
    [HasPermission("visits.view")]
    public async Task<IActionResult> GetQueue([FromQuery] long? doctorId)
    {
        var result = await _mediator.Send(new GetVisitQueueQuery(doctorId));
        return ToResponse(result);
    }

    /// <summary>فراخوان بیمار بعدی از صف — ایجاد ویزیت در جریان</summary>
    [HttpPost("call-next")]
    [HasPermission("visits.create")]
    public async Task<IActionResult> CallNext([FromQuery] long? doctorId)
    {
        var result = await _mediator.Send(new CallNextPatientCommand(doctorId));
        return ToResponse(result, "بیمار بعدی فراخوانی شد");
    }

    /// <summary>
    /// ═══ چرخهٔ ۷۵ — فراخوان «همان بیمارِ ردیف انتخاب‌شده» ═══
    /// گزارش مالک: «وقتی از لیست بیماران در صف انتظار روی دکمه فراخوان کلیک
    /// میکنم خطای 'بیماری در صف انتظار نیست' میده» — ریشه: دکمهٔ ردیفی به
    /// فرمان کور call-next وصل بود که بیمارِ ردیف را نمی‌دید. این اندپوینت
    /// همان پذیرش را فراخوان می‌کند (ایدمپوتنت — دوباره‌زدن روی بیمارِ روی‌میز
    /// همان ویزیت را برمی‌گرداند).
    /// </summary>
    /// <param name="receptionId">شناسه پذیرشِ ردیف صف</param>
    [HttpPost("call/{receptionId:long}")]
    [HasPermission("visits.create")]
    public async Task<IActionResult> CallPatient(long receptionId)
    {
        var result = await _mediator.Send(
            new Application.Features.Visits.CallPatient.CallPatientCommand(receptionId));
        return ToResponse(result, "بیمار فراخوانی شد");
    }
}
