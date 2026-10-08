using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.Common.Models;
using Sahand.ClinicPro.Application.DTOs.Insurance;
using Sahand.ClinicPro.Application.Features.Insurance;
using Sahand.ClinicPro.Application.Features.Insurance.Fractionment;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر بیمه — مطالبات، استعلامات و تسویه حساب (چرخه ۱۳، درخواست ۲-۱)
/// مسیرها: /api/v1/insurance
/// </summary>
[ApiController]
[Route("api/v1/insurance")]
[Authorize]
public class InsuranceController : ApiBaseController
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق میانجی و واحد کار</summary>
    public InsuranceController(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>فهرست مطالبات بیمه</summary>
    /// <param name="companyId">فیلتر شرکت بیمه</param>
    [HttpGet("claims")]
    [HasPermission("insurance.view")]
    public async Task<IActionResult> GetClaims([FromQuery] long? companyId)
    {
        var result = await _mediator.Send(new GetClaimsQuery(companyId));
        return ToResponse(result);
    }

    /// <summary>ثبت مطالبه دوره‌ای — جمع خودکار اقلام بیمه‌ای دوره</summary>
    [HttpPost("claims")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> SubmitClaim([FromBody] SubmitClaimDto dto)
    {
        var result = await _mediator.Send(new SubmitClaimCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetClaims", new { }, "مطالبه ثبت و ارسال شد");
        return ToResponse(result);
    }

    /// <summary>ثبت استعلام بیمه — اتصال برخط در گام ۲۴</summary>
    [HttpPost("inquiries")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> RecordInquiry([FromBody] InsuranceInquiryDto dto)
    {
        var result = await _mediator.Send(new RecordInquiryCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetClaims", new { }, "استعلام ثبت شد");
        return ToResponse(result);
    }

    /* ═══════════ تسویه حساب بیمه — چرخه ۱۳ (درخواست ۲-۱) ═══════════ */

    /// <summary>
    /// محاسبه تسویه فاکتور بیمه‌ای — تفکیک سهم بیمه/بیمار هر قلم بدون ذخیره
    /// </summary>
    /// <param name="invoiceId">شناسه فاکتور بیمه‌ای</param>
    [HttpGet("fractionment/{invoiceId:long}")]
    [HasPermission("insurance.view")]
    public async Task<IActionResult> CalculateFractionment(long invoiceId)
    {
        var result = await _mediator.Send(new CalculateFractionmentQuery(invoiceId));
        return ToResponse(result);
    }

    /// <summary>
    /// اعمال تسویه روی فاکتور — ثبت سهم بیمه/بیمار روی اقلام + صورتحساب طبقه‌بندی‌شده
    /// </summary>
    /// <param name="invoiceId">شناسه فاکتور بیمه‌ای</param>
    [HttpPost("fractionment/{invoiceId:long}/apply")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> ApplyFractionment(long invoiceId)
    {
        var result = await _mediator.Send(new ApplyFractionmentCommand(invoiceId));
        return ToResponse(result, "تسویه بیمه روی اقلام فاکتور اعمال شد");
    }

    /// <summary>فهرست قواعد تسویه — با فیلتر بیمه</summary>
    /// <param name="companyId">فیلتر شرکت بیمه</param>
    [HttpGet("fractionment-rules")]
    [HasPermission("insurance.view")]
    public async Task<IActionResult> GetRules([FromQuery] long? companyId)
    {
        var query = _unitOfWork.Repository<InsuranceFractionmentRule>().QueryableNoTracking
            .Where(r => !r.IsDeleted && r.IsActive);
        if (companyId is { } cid)
            query = query.Where(r => r.InsuranceCompanyId == cid);

        var rules = await query
            .OrderBy(r => r.InsuranceCompanyId).ThenBy(r => r.ServiceId)
            .Select(r => new FractionmentRuleDto(
                r.Id, r.InsuranceCompanyId, r.ServiceId,
                r.ServiceId != null ? r.Service!.ServiceName : "همه خدمات",
                r.RuleType, r.InsurancePercent, r.NominalAmount,
                r.FranchiseAmount, r.PerItemCeiling, r.Notes))
            .ToListAsync();

        return Ok(ApiResponse<List<FractionmentRuleDto>>.Success(rules));
    }

    /// <summary>ثبت/ویرایش قاعده تسویه — شناسه صفر یعنی ثبت جدید</summary>
    /// <param name="dto">داده قاعده</param>
    [HttpPost("fractionment-rules")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> SaveRule([FromBody] SaveFractionmentRuleDto dto)
    {
        if (dto.InsurancePercent is < 0 or > 100)
            return BadRequest(ApiResponse<object>.Failure("درصد بیمه باید بین ۰ تا ۱۰۰ باشد"));

        // چرخه بیست‌وسوم رفع CS8600: نتیجهٔ جستجو تهی‌پذیر است — متغیر تهی‌پذیر
        InsuranceFractionmentRule? rule;
        if (dto.Id == 0)
        {
            rule = new InsuranceFractionmentRule();
            await _unitOfWork.Repository<InsuranceFractionmentRule>().AddAsync(rule);
        }
        else
        {
            rule = await _unitOfWork.Repository<InsuranceFractionmentRule>().QueryableTracking
                .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted);
            if (rule is null)
                return NotFound(ApiResponse<object>.Failure("قاعده یافت نشد"));
        }

        rule.InsuranceCompanyId = dto.InsuranceCompanyId;
        rule.ServiceId = dto.ServiceId;
        rule.RuleType = dto.RuleType;
        rule.InsurancePercent = dto.InsurancePercent;
        rule.NominalAmount = dto.NominalAmount;
        rule.FranchiseAmount = dto.FranchiseAmount;
        rule.PerItemCeiling = dto.PerItemCeiling;
        rule.Notes = dto.Notes?.Trim();
        rule.IsActive = true;

        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.Success(
            new { rule.Id }, dto.Id == 0 ? "قاعده تسویه ثبت شد" : "قاعده تسویه به‌روزرسانی شد"));
    }

    /// <summary>حذف نرم قاعده تسویه</summary>
    /// <param name="id">شناسه قاعده</param>
    [HttpDelete("fractionment-rules/{id:long}")]
    [HasPermission("insurance.create")]
    public async Task<IActionResult> DeleteRule(long id)
    {
        var rule = await _unitOfWork.Repository<InsuranceFractionmentRule>().QueryableTracking
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        if (rule is null)
            return NotFound(ApiResponse<object>.Failure("قاعده یافت نشد"));

        rule.IsDeleted = true;
        await _unitOfWork.SaveEntitiesAsync();
        return Ok(ApiResponse<object>.SuccessMessage("قاعده تسویه حذف شد"));
    }

    // ═══ چرخه بیست‌وسوم (گزارش شانزدهم مالک): مدیریت شرکت‌های بیمه ═══
    // «در صفحه بیمه، بتوان بیمه جدید تعریف کرد و یا بیمه‌های موجود را ویرایش کرد»
    // — چهار endpoint زیر به پنجرهٔ مدیریت بیمه‌های صفحهٔ بیمه سرویس می‌دهند.
    // نوشتن دادهٔ مرجع نیازمند نقش مدیریتی است؛ خواندن برای همه باقی می‌ماند.

    /// <summary>فهرست کامل بیمه‌ها شامل غیرفعال — پنجرهٔ مدیریت بیمه‌ها</summary>
    [HttpGet("companies/manage")]
    [HasPermission("insurance.view")]
    public async Task<IActionResult> GetCompaniesForManage()
    {
        var result = await _mediator.Send(
            new Application.Features.Lookups.GetInsuranceCompaniesForManageQuery());
        return ToResponse(result);
    }

    /// <summary>ساخت شرکت بیمه جدید</summary>
    /// <param name="request">داده بیمه (نام/کد/نوع/درصد پوشش)</param>
    [HttpPost("companies")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateCompany([FromBody] SaveInsuranceCompanyRequest request)
    {
        var result = await _mediator.Send(new Application.Features.Lookups.CreateInsuranceCompanyCommand(
            request.Name, request.Code, request.Type, request.CoveragePercent, request.IsActive));
        return ToResponse(result, "شرکت بیمه ثبت شد");
    }

    /// <summary>ویرایش شرکت بیمه</summary>
    /// <param name="id">شناسه بیمه</param>
    /// <param name="request">داده بیمه</param>
    [HttpPut("companies/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateCompany(long id, [FromBody] SaveInsuranceCompanyRequest request)
    {
        var result = await _mediator.Send(new Application.Features.Lookups.UpdateInsuranceCompanyCommand(
            id, request.Name, request.Code, request.Type, request.CoveragePercent, request.IsActive));
        return ToResponse(result, "شرکت بیمه به‌روزرسانی شد");
    }

    /// <summary>غیرفعال‌سازی (حذف نرم) بیمه — سوابق مالی حفظ می‌شوند</summary>
    /// <param name="id">شناسه بیمه</param>
    [HttpDelete("companies/{id:long}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeactivateCompany(long id)
    {
        var result = await _mediator.Send(
            new Application.Features.Lookups.DeactivateInsuranceCompanyCommand(id));
        return ToResponse(result, "شرکت بیمه غیرفعال شد");
    }
}

/// <summary>بدنهٔ درخواست ساخت/ویرایش شرکت بیمه — چرخه بیست‌وسوم</summary>
public sealed class SaveInsuranceCompanyRequest
{
    /// <summary>نام شرکت بیمه — یکتا</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>کد اختصاری بیمه</summary>
    public string? Code { get; set; }

    /// <summary>نوع بیمه — ۱ عمومی، ۲ سلامت، ۳ ارتش، ۴ خصوصی، ۵ تکمیلی</summary>
    public int Type { get; set; } = 4;

    /// <summary>درصد پوشش پیش‌فرض (۰ تا ۱۰۰)</summary>
    public decimal CoveragePercent { get; set; } = 70m;

    /// <summary>وضعیت فعال — در ویرایش معنا دارد</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>ردیف قاعده تسویه</summary>
public record FractionmentRuleDto(
    long Id,
    long InsuranceCompanyId,
    long? ServiceId,
    string ServiceName,
    FractionmentRuleType RuleType,
    decimal InsurancePercent,
    decimal NominalAmount,
    decimal FranchiseAmount,
    decimal PerItemCeiling,
    string? Notes);

/// <summary>ثبت/ویرایش قاعده تسویه</summary>
public record SaveFractionmentRuleDto(
    long Id,
    long InsuranceCompanyId,
    long? ServiceId,
    FractionmentRuleType RuleType,
    decimal InsurancePercent,
    decimal NominalAmount,
    decimal FranchiseAmount,
    decimal PerItemCeiling,
    string? Notes);
