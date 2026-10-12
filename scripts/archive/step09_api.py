# -*- coding: utf-8 -*-
"""گام ۹ — لایه API: میدلور خطا + فیلتر دسترسی + کنترلرهای Auth/Patients/Doctors + تنظیم Program.cs"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content):
    full = os.path.join(API, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ میدلور مدیریت خطا ═══════════
write("Middleware/ExceptionHandlingMiddleware.cs", """
using System.Text.Json;
using Sahand.ClinicPro.Application.Common.Exceptions;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.API.Middleware;

/// <summary>
/// میدلور مدیریت خطا — همه استثناها را به پاسخ استاندارد فارسی تبدیل می‌کند
/// جزئیات خطای غیرمنتظر برای کاربر پنهان و در لاگ ثبت می‌شود
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>سازنده با تزریق وابستگی‌ها</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>فراخوانی میدلور</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>تبدیل استثنا به پاسخ استاندارد</summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var (statusCode, message, errors) = exception switch
        {
            AppValidationException validation => (StatusCodes.Status400BadRequest,
                "اطلاعات ارسالی نامعتبر است",
                validation.Errors.SelectMany(kv => kv.Value).ToList()),

            NotFoundException notFound => (StatusCodes.Status404NotFound,
                notFound.Message, new List<string>()),

            BusinessRuleException business => (StatusCodes.Status400BadRequest,
                business.Message, new List<string>()),

            ConflictException conflict => (StatusCodes.Status409Conflict,
                conflict.Message, new List<string>()),

            UnauthorizedAccessException => (StatusCodes.Status403Forbidden,
                "دسترسی لازم برای این عملیات را ندارید", new List<string>()),

            OperationCanceledException => (StatusCodes.Status499ClientClosedRequest,
                "درخواست توسط کلاینت لغو شد", new List<string>()),

            _ => (StatusCodes.Status500InternalServerError,
                "خطای غیرمنتظره در سرور رخ داد — با پشتیبانی تماس بگیرید", new List<string>())
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "خطای غیرمنتظره — TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning("خطای {ExceptionType} — پیام: {Message} — TraceId: {TraceId}",
                exception.GetType().Name, exception.Message, traceId);
        }

        var response = new
        {
            isSuccess = false,
            statusCode,
            message,
            errors,
            traceId,
            data = (object?)null,
            elapsedMs = 0
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
""")

# ═══════════ فیلتر دسترسی مبتنی بر Permission ═══════════
write("Filters/PermissionAuthorization.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Sahand.ClinicPro.API.Filters;

/// <summary>
/// نیاز دسترسی — بررسی ادعای permission در توکن
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>نام دسترسی مثل patients.view</summary>
    public string Permission { get; }

    /// <summary>ساخت نیاز با نام دسترسی</summary>
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// گرداننده بررسی دسترسی — نقش ادمین همه دسترسی‌ها را دارد
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>بررسی ادعای permission یا نقش Admin</summary>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// تأمین‌کننده سیاست دسترسی — سیاست‌های PERMISSION:x را پویا می‌سازد
/// </summary>
public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    /// <summary>پیشوند شناسایی سیاست‌های دسترسی</summary>
    public const string PolicyPrefix = "PERMISSION:";

    /// <summary>سازنده با گزینه‌های سیاست</summary>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    /// <summary>ساخت سیاست برای هر دسترسی به‌صورت پویا</summary>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// صفت اعمال دسترسی روی اکشن یا کنترلر — مثل [HasPermission(\"patients.view\")]
/// </summary>
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>ساخت صفت با نام دسترسی</summary>
    public HasPermissionAttribute(string permission)
        : base($"{PermissionPolicyProvider.PolicyPrefix}{permission}") { }
}
""")

# ═══════════ کنترلر پایه ═══════════
write("Controllers/ApiBaseController.cs", """
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر پایه همه کنترلرها — تبدیل Result دامنه به ApiResponse استاندارد
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiBaseController : ControllerBase
{
    /// <summary>تبدیل نتیجه دامنه به پاسخ HTTP</summary>
    protected IActionResult ToResponse<T>(Result<T> result, string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<T>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return Ok(ApiResponse<T>.Success(result.Value!,
            successMessage ?? "عملیات با موفقیت انجام شد"));
    }

    /// <summary>تبدیل نتیجه بدون داده به پاسخ HTTP</summary>
    protected IActionResult ToResponse(Result result, string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return Ok(ApiResponse<object>.SuccessMessage(successMessage ?? "عملیات با موفقیت انجام شد"));
    }

    /// <summary>پاسخ ساخته‌شده موفق — کد 201 با مکان منبع</summary>
    protected IActionResult CreatedResponse<T>(Result<T> result, string actionName, object routeValues,
        string? successMessage = null)
    {
        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<T>.Failure(result.Error.Message,
                StatusCodes.Status400BadRequest,
                new List<string> { result.Error.Code }));
        }

        return CreatedAtAction(actionName, routeValues,
            ApiResponse<T>.Success(result.Value!, successMessage ?? "با موفقیت ایجاد شد"));
    }
}
""")

# ═══════════ کنترلر احراز هویت ═══════════
write("Controllers/AuthController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Sahand.ClinicPro.API.Controllers;
using Sahand.ClinicPro.Application.DTOs.Auth;
using Sahand.ClinicPro.Application.Features.Auth.Commands.ChangePassword;
using Sahand.ClinicPro.Application.Features.Auth.Commands.Login;
using Sahand.ClinicPro.Application.Features.Auth.Commands.Logout;
using Sahand.ClinicPro.Application.Features.Auth.Commands.RefreshToken;

namespace Sahand.ClinicPro.API.Controllers.Auth;

/// <summary>
/// کنترلر احراز هویت — ورود و تمدید توکن و خروج و تغییر رمز
/// مسیرها: /api/v1/auth
/// محدودیت نرخ: ۵ درخواست بر دقیقه برای ورود
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>سازنده با تزریق میانجی و دستیاب HttpContext</summary>
    public AuthController(IMediator mediator, IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// ورود به سامانه و دریافت توکن‌ها
    /// </summary>
    /// <remarks>پنج تلاش ناموفق پیاپی حساب را ۳۰ دقیقه قفل می‌کند</remarks>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var device = Request.Headers.UserAgent.ToString();

        var result = await _mediator.Send(new LoginCommand(
            request.UsernameOrEmail, request.Password, ip, device));

        return ToResponse(result, "ورود با موفقیت انجام شد");
    }

    /// <summary>
    /// تمدید توکن دسترسی با توکن تازه‌سازی — همراه چرخش توکن
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken, ip));
        return ToResponse(result, "توکن با موفقیت تمدید شد");
    }

    /// <summary>
    /// خروج و ابطال توکن تازه‌سازی
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken));
        return ToResponse(Domain.Common.Result.Success(), "خروج با موفقیت انجام شد");
    }

    /// <summary>
    /// تغییر رمز عبور کاربر واردشده فعلی
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(
            request.CurrentPassword, request.NewPassword));
        return ToResponse(result, "رمز عبور با موفقیت تغییر کرد");
    }
}
""")

# ═══════════ کنترلر بیماران ═══════════
write("Controllers/PatientsController.cs", """
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
""")

# ═══════════ کنترلر پزشکان ═══════════
write("Controllers/DoctorsController.cs", """
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
""")

print("✅ گام ۹ — بخش ۱ (میدلور + فیلتر + کنترلرها) کامل شد")
