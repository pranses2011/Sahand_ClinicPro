using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Features.Lookups;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر داده‌های مرجع — گزینه‌های انتخابی فرم‌های کلاینت
/// مسیرها: /api/v1/lookups
/// </summary>
/// <remarks>
/// تصمیم معماری: این endpointها فقط داده مرجع فقط‌خواندنی هستند و در چند ماژول
/// (بیماران، پذیرش، ویزیت، نسخه، آزمایشگاه) مشترک‌اند؛ بنابراین به‌جای مجوز
/// ماژول‌محور تنها احراز هویت الزامی است.
/// </remarks>
[ApiController]
[Route("api/v1/lookups")]
[Authorize]
public class LookupsController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public LookupsController(IMediator mediator) => _mediator = mediator;

    /// <summary>فهرست شرکت‌های بیمه فعال</summary>
    [HttpGet("insurance-companies")]
    public async Task<IActionResult> GetInsuranceCompanies()
    {
        var result = await _mediator.Send(new GetInsuranceCompaniesQuery());
        return ToResponse(result);
    }

    /// <summary>جستجوی تشخیص‌های ICD-10</summary>
    /// <param name="search">عبارت جستجو در کد یا شرح</param>
    /// <param name="take">حداکثر تعداد نتیجه — پیش‌فرض ۳۰</param>
    [HttpGet("diagnoses")]
    public async Task<IActionResult> GetDiagnoses([FromQuery] string? search,
        [FromQuery] int take = 30)
    {
        var result = await _mediator.Send(new GetDiagnosesQuery(search, take));
        return ToResponse(result);
    }

    /// <summary>جستجوی داروها</summary>
    /// <param name="search">عبارت جستجو در نام دارو</param>
    /// <param name="take">حداکثر تعداد نتیجه — پیش‌فرض ۳۰</param>
    [HttpGet("medications")]
    public async Task<IActionResult> GetMedications([FromQuery] string? search,
        [FromQuery] int take = 30)
    {
        var result = await _mediator.Send(new GetMedicationsQuery(search, take));
        return ToResponse(result);
    }

    /// <summary>فهرست آزمایش‌های فعال</summary>
    /// <param name="search">فیلتر نام — اختیاری</param>
    [HttpGet("lab-tests")]
    public async Task<IActionResult> GetLabTests([FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetLabTestsQuery(search));
        return ToResponse(result);
    }

    /// <summary>فهرست تخصص‌های فعال — با گزینه فقط تخصص‌های اصلی</summary>
    [HttpGet("specialties")]
    public async Task<IActionResult> GetSpecialties([FromQuery] bool onlyParents = false)
    {
        var result = await _mediator.Send(new GetSpecialtiesQuery(onlyParents));
        return ToResponse(result);
    }

    /// <summary>فهرست اتاق‌های معاینه فعال</summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var result = await _mediator.Send(new GetRoomsQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست پرسنل فعال — برای انتخاب در فرم پزشک</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var result = await _mediator.Send(new GetEmployeesQuery());
        return ToResponse(result);
    }
}
