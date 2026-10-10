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

    /// <summary>جستجوی داروها و اقلام انبار</summary>
    /// <param name="search">عبارت جستجو در نام دارو/قلم</param>
    /// <param name="take">حداکثر تعداد نتیجه — پیش‌فرض ۳۰</param>
    /// <param name="itemKind">فیلتر نوع قلم — چرخهٔ ۳۹: ۱ دارو، ۲ لوازم مصرفی، تهی هر دو</param>
    [HttpGet("medications")]
    public async Task<IActionResult> GetMedications([FromQuery] string? search,
        [FromQuery] int take = 30,
        [FromQuery] Sahand.ClinicPro.Domain.Enums.MedicationItemKind? itemKind = null)
    {
        var result = await _mediator.Send(new GetMedicationsQuery(search, take, itemKind));
        return ToResponse(result);
    }

    // ═══ چرخهٔ سی‌ونه‌م (گزارش مالک) — ثبت قلم جدید انبار ═══
    // «خرید و انبار و قیمت‌های این لوازم هم باید باشه» — تا این چرخه کاتالوگ
    // اقلام فقط از بذر اولیه پر می‌شد و ساخت قلم (دارو یا لوازم مصرفی) از
    // خود برنامه ممکن نبود؛ اندپوینت زیر برای پنجرهٔ «افزودن قلم» داروخانه
    // است. نوشتن دادهٔ مرجع نیازمند نقش مدیریتی است (هم‌سبک اتاق‌ها).

    /// <summary>
    /// ثبت قلم جدید انبار داروخانه — دارو یا لوازم مصرفی (سرنگ/سرم/گاز و…)
    /// </summary>
    /// <param name="request">مشخصات قلم — نام فارسی/ژنریک، نوع و مشخصات اختیاری</param>
    [HttpPost("medications")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateMedication([FromBody] CreatePharmacyItemCommand request)
    {
        var result = await _mediator.Send(request);
        if (result.IsSuccess)
            return CreatedResponse(result, nameof(GetMedications), new { }, "قلم انبار ثبت شد");
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

    // ═══ چرخه بیست‌وچهارم (گزارش هفدهم مالک) — سمت/دپارتمان پرسنل ═══
    // «در تعریف پرسنل سمت و چند فیلد دیگر وجود ندارد» و لیست پرسنل سمت و
    // دپارتمان را نشان نمی‌داد — این endpointها فهرست‌های مرجع را عرضه و
    // ساخت سریع مقدار تازه را ممکن می‌کنند تا فرم پرسنل قابل تکمیل باشد.

    /// <summary>فهرست دپارتمان‌های فعال — ComboBox فرم پرسنل</summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var result = await _mediator.Send(new GetDepartmentsQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست سمت‌های فعال — ComboBox فرم پرسنل</summary>
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions()
    {
        var result = await _mediator.Send(new GetPositionsQuery());
        return ToResponse(result);
    }

    /// <summary>ساخت دپارتمان جدید (یا بازگشت شناسهٔ موجود هم‌نام)</summary>
    [HttpPost("departments")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateLookupRequest request)
    {
        var result = await _mediator.Send(new CreateDepartmentIfMissingCommand(request.Name));
        return ToResponse(result);
    }

    /// <summary>ساخت سمت جدید (یا بازگشت شناسهٔ موجود هم‌عنوان)</summary>
    [HttpPost("positions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreatePosition([FromBody] CreateLookupRequest request)
    {
        var result = await _mediator.Send(new CreatePositionIfMissingCommand(request.Name));
        return ToResponse(result);
    }

    /// <summary>بدنهٔ ساخت مرجع سمت/دپارتمان — نام یا عنوان</summary>
    public sealed class CreateLookupRequest
    {
        /// <summary>نام دپارتمان یا عنوان سمت</summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>فهرست خدمات فعال با قیمت تعرفه — اقلام فاکتور؛ چرخه ۲۴:
    /// پارامتر insuranceCompanyId تعرفهٔ بیمهٔ منتخب را برمی‌گرداند (تهی = آزاد)</summary>
    [HttpGet("services")]
    public async Task<IActionResult> GetServices([FromQuery] string? search,
        [FromQuery] long? insuranceCompanyId)
    {
        var result = await _mediator.Send(new GetServicesQuery(search, insuranceCompanyId));
        return ToResponse(result);
    }

    // ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک): مدیریت تعرفه‌ها ═══
    // «در صفحه بیمه، بتوان تعرفه جدید تعریف کرد و یا تعرفه‌های موجود را ویرایش کرد»
    // — چهار endpoint زیر به پنجرهٔ مدیریت تعرفه‌های صفحهٔ بیمه سرویس می‌دهند.
    // نوشتن دادهٔ مرجع نیازمند نقش مدیریتی است؛ خواندن برای همه باقی می‌ماند.

    /// <summary>فهرست کامل خدمت‌های تعرفه‌دار شامل غیرفعال — پنجرهٔ مدیریت تعرفه‌ها؛
    /// چرخه ۲۴: پارامتر insuranceCompanyId هدف تعرفه (تهی = آزاد)</summary>
    [HttpGet("services/manage")]
    public async Task<IActionResult> GetServicesForManage([FromQuery] long? insuranceCompanyId)
    {
        var result = await _mediator.Send(new GetServicesTariffsForManageQuery(insuranceCompanyId));
        return ToResponse(result);
    }

    /// <summary>ساخت خدمت تعرفه‌دار جدید — خدمت + ردیف تعرفهٔ هدف منتخب سال جاری
    /// (چرخه ۲۴: insuranceCompanyId تهی = تعرفهٔ آزاد، مقدار = تعرفهٔ بیمه)</summary>
    /// <param name="request">داده خدمت/تعرفه</param>
    [HttpPost("services")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateService([FromBody] SaveServiceTariffRequest request)
    {
        var result = await _mediator.Send(new CreateServiceTariffCommand(
            request.ServiceName, request.ServiceCode, request.Price,
            request.InsuranceShare, request.PatientShare, request.InsuranceCompanyId));
        return ToResponse(result, "خدمت/تعرفه ثبت شد");
    }

    /// <summary>ویرایش خدمت تعرفه‌دار — تعرفهٔ هدف منتخب سال جاری upsert می‌شود</summary>
    /// <param name="id">شناسه خدمت</param>
    /// <param name="request">داده خدمت/تعرفه</param>
    [HttpPut("services/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateService(long id, [FromBody] SaveServiceTariffRequest request)
    {
        var result = await _mediator.Send(new UpdateServiceTariffCommand(
            id, request.ServiceName, request.ServiceCode, request.Price,
            request.InsuranceShare, request.PatientShare, request.IsActive,
            request.InsuranceCompanyId));
        return ToResponse(result, "خدمت/تعرفه به‌روزرسانی شد");
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) خدمت — سوابق فاکتور حفظ می‌شوند</summary>
    /// <param name="id">شناسه خدمت</param>
    [HttpDelete("services/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeactivateService(long id)
    {
        var result = await _mediator.Send(new DeactivateServiceCommand(id));
        return ToResponse(result, "خدمت غیرفعال شد");
    }
}

/// <summary>بدنهٔ درخواست ساخت/ویرایش خدمت تعرفه‌دار — چرخه بیست‌وسوم</summary>
public sealed class SaveServiceTariffRequest
{
    /// <summary>نام خدمت</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>کد خدمت — یکتا اگر داده شود</summary>
    public string? ServiceCode { get; set; }

    /// <summary>قیمت تعرفه‌ای</summary>
    public decimal Price { get; set; }

    /// <summary>درصد سهم بیمه (۰-۱۰۰)</summary>
    public decimal InsuranceShare { get; set; }

    /// <summary>درصد سهم بیمار (۰-۱۰۰)</summary>
    public decimal PatientShare { get; set; }

    /// <summary>وضعیت فعال — در ویرایش معنا دارد</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>هدف تعرفه — چرخه ۲۴: تهی = تعرفهٔ آزاد (بدون بیمه)، مقدار = بیمهٔ منتخب</summary>
    public long? InsuranceCompanyId { get; set; }
}
