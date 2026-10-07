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
// عمدتاً عمومی برای همه کاربران واردشده (ممیزی چرخه ۱۲): فهرست‌های مرجع فقط-خواندنی (تخصص/بیمه/دارو/خدمت) — خواندن برای همه فرم‌ها لازم است
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

    // ═══ چرخه بیست‌ویکم (گزارش پانزدهم مالک): مدیریت اتاق‌ها ═══
    // «در تعریف پزشک فیلد اتاق برای انتخاب هست ولی جایی برای تعریف اتاق و اسم
    //  اتاق نیست» — سه endpoint زیر برای پنجرهٔ مدیریت اتاق در فرم پزشک افزوده شدند.
    // نوشتن دادهٔ مرجع نیازمند نقش مدیریتی است؛ خواندن برای همه باقی می‌ماند.

    /// <summary>فهرست کامل اتاق‌ها شامل غیرفعال — پنجرهٔ مدیریت اتاق‌ها</summary>
    [HttpGet("rooms/manage")]
    public async Task<IActionResult> GetRoomsForManage()
    {
        var result = await _mediator.Send(new GetRoomsForManageQuery());
        return ToResponse(result);
    }

    /// <summary>ساخت اتاق معاینه جدید</summary>
    [HttpPost("rooms")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var result = await _mediator.Send(
            new CreateRoomCommand(request.RoomNumber, request.RoomName, request.Floor));
        return ToResponse(result);
    }

    /// <summary>ویرایش اتاق معاینه</summary>
    [HttpPut("rooms/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateRoom(long id, [FromBody] CreateRoomRequest request)
    {
        var result = await _mediator.Send(
            new UpdateRoomCommand(id, request.RoomNumber, request.RoomName, request.Floor, request.IsActive));
        return ToResponse(result);
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) اتاق</summary>
    [HttpDelete("rooms/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeactivateRoom(long id)
    {
        var result = await _mediator.Send(new DeactivateRoomCommand(id));
        return ToResponse(result);
    }

    /// <summary>بدنهٔ درخواست ساخت/ویرایش اتاق</summary>
    public sealed class CreateRoomRequest
    {
        /// <summary>شماره اتاق — یکتا</summary>
        public string RoomNumber { get; set; } = string.Empty;

        /// <summary>نام نمایشی اتاق</summary>
        public string? RoomName { get; set; }

        /// <summary>طبقه</summary>
        public int? Floor { get; set; }

        /// <summary>وضعیت فعال — فقط در ویرایش معنا دارد</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>فهرست پرسنل فعال — برای انتخاب در فرم پزشک</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var result = await _mediator.Send(new GetEmployeesQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست خدمات فعال با قیمت تعرفه — برای اقلام فاکتور</summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices([FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetServicesQuery(search));
        return ToResponse(result);
    }
}
