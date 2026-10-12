# -*- coding: utf-8 -*-
"""گام‌های ۱۸ و ۱۹ — حسابداری دوبل + بیمه و مطالبات"""
import os

ROOT = os.path.join(os.path.dirname(__file__), "..")
APP = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.Application"))
API = os.path.abspath(os.path.join(ROOT, "src", "Sahand.ClinicPro.API"))


def write(rel, content, base=APP):
    full = os.path.join(base, rel)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    with open(full, "w", encoding="utf-8-sig", newline="\n") as f:
        f.write(content.strip() + "\n")
    print(f"  + {rel}")


# ═══════════ گام ۱۸: حسابداری ═══════════
write("DTOs/Accounting/AccountingDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Accounting;

/// <summary>سرفصل حساب در نمایش درختی</summary>
public record AccountNodeDto(
    long Id,
    string AccountCode,
    string AccountName,
    AccountType Type,
    AccountLevel Level,
    bool IsDetail,
    decimal CurrentBalance,
    List<AccountNodeDto> Children);

/// <summary>قلم سند حسابداری برای ثبت</summary>
public record JournalLineDto(long AccountId, decimal Debit, decimal Credit, string? Description);

/// <summary>درخواست ثبت سند دستی</summary>
public record CreateJournalEntryDto(
    string? Description,
    List<JournalLineDto> Lines,
    JournalReferenceType ReferenceType = JournalReferenceType.Manual,
    long? ReferenceId = null);

/// <summary>ردیف فهرست اسناد</summary>
public record JournalEntryListDto(
    long Id,
    string? EntryNumber,
    string DateShamsi,
    string? Description,
    decimal TotalDebit,
    decimal TotalCredit,
    JournalEntryStatus Status);
""")

write("Features/Accounting/AccountingFeatures.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Accounting;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Exceptions;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Accounting;

/// <summary>
/// پرس‌وجوی درخت سرفصل‌ها چهارسطحی
/// </summary>
public record GetChartOfAccountsQuery : IRequest<Result<List<AccountNodeDto>>>;

/// <summary>گرداننده درخت سرفصل‌ها — از گروه‌های سطح یک</summary>
public class GetChartOfAccountsQueryHandler
    : IRequestHandler<GetChartOfAccountsQuery, Result<List<AccountNodeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetChartOfAccountsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو و ساخت درخت</summary>
    public async Task<Result<List<AccountNodeDto>>> Handle(GetChartOfAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _unitOfWork.Repository<AccountingAccount>()
            .QueryableNoTracking.OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var roots = all.Where(a => a.ParentAccountId == null)
            .Select(a => BuildNode(a, all))
            .ToList();

        return Result<List<AccountNodeDto>>.Success(roots);
    }

    /// <summary>ساخت گره درخت با فرزندان</summary>
    private static AccountNodeDto BuildNode(AccountingAccount account,
        List<AccountingAccount> all)
    {
        return new AccountNodeDto(
            account.Id, account.AccountCode, account.AccountName,
            account.AccountType, account.Level, account.IsDetail,
            account.CurrentBalance,
            all.Where(c => c.ParentAccountId == account.Id)
                .Select(c => BuildNode(c, all))
                .ToList());
    }
}

/// <summary>
/// فرمان ثبت سند حسابداری دستی — با الزام قانون طلایی توازن
/// </summary>
public record CreateJournalEntryCommand(CreateJournalEntryDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج سند دستی</summary>
public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Dto.Description).NotEmpty().WithMessage("شرح سند الزامی است");
        RuleFor(x => x.Dto.Lines).NotEmpty().WithMessage("سند باید حداقل دو سطر داشته باشد");
        RuleForEach(x => x.Dto.Lines).SetValidator(new JournalLineDtoValidator());
    }
}

/// <summary>اعتبارسنج سطر سند</summary>
public class JournalLineDtoValidator : AbstractValidator<JournalLineDto>
{
    /// <summary>قواعد اعتبارسنجی سطر</summary>
    public JournalLineDtoValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("انتخاب حساب الزامی است");
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0).WithMessage("بدهکار نمی‌تواند منفی باشد");
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0).WithMessage("بستانکار نمی‌تواند منفی باشد");
        RuleFor(x => x).Must(l => l.Debit > 0 || l.Credit > 0)
            .WithMessage("هر سطر باید بدهکار یا بستانکار داشته باشد");
    }
}

/// <summary>گرداننده ثبت سند دستی — توازن بدهکار و بستانکار الزامی است</summary>
public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateJournalEntryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت سند</summary>
    public async Task<Result<long>> Handle(CreateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);

        // قانون طلایی حسابداری دوبل — سند نامتوازن هرگز ذخیره نمی‌شود
        if (totalDebit != totalCredit)
            throw new UnbalancedJournalEntryException(totalDebit, totalCredit);

        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _unitOfWork.Repository<AccountingAccount>()
            .QueryableNoTracking
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        // همه حساب‌ها باید موجود و قابل سند شدن باشند
        if (accounts.Count != accountIds.Count)
            return Result<long>.Failure(new Error("INVALID_ACCOUNT", "یک یا چند حساب انتخابی یافت نشد"));
        if (accounts.Any(a => !a.IsDetail))
            return Result<long>.Failure(new Error("NOT_DETAIL_ACCOUNT",
                "سند فقط روی حساب‌های تفصیلی قابل ثبت است"));

        var now = DateTime.UtcNow;
        var entry = new JournalEntry
        {
            EntryDate = DateOnly.FromDateTime(now),
            EntryDateShamsi = PersianDateHelper.ToShamsi(now),
            Description = dto.Description,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Status = JournalEntryStatus.Draft
        };

        var lineNumber = 1;
        foreach (var line in dto.Lines)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                LineNumber = lineNumber++,
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Description = line.Description
            });
        }

        await _unitOfWork.Repository<JournalEntry>().AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(entry.Id);
    }
}

/// <summary>
/// پرس‌وجوی فهرست اسناد
/// </summary>
public record GetJournalEntriesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<List<JournalEntryListDto>>>;

/// <summary>گرداننده فهرست اسناد</summary>
public class GetJournalEntriesQueryHandler
    : IRequestHandler<GetJournalEntriesQuery, Result<List<JournalEntryListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetJournalEntriesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<JournalEntryListDto>>> Handle(GetJournalEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<JournalEntry>()
            .QueryableNoTracking
            .OrderByDescending(e => e.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new JournalEntryListDto(
                e.Id, e.EntryNumber,
                e.EntryDateShamsi ?? string.Empty,
                e.Description, e.TotalDebit, e.TotalCredit, e.Status))
            .ToListAsync(cancellationToken);

        return Result<List<JournalEntryListDto>>.Success(items);
    }
}
""")

write("Controllers/AccountingController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Accounting;
using Sahand.ClinicPro.Application.Features.Accounting;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر حسابداری دوبل — درخت سرفصل و اسناد
/// مسیرها: /api/v1/accounting
/// </summary>
[ApiController]
[Route("api/v1/accounting")]
[Authorize]
public class AccountingController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public AccountingController(IMediator mediator) => _mediator = mediator;

    /// <summary>درخت سرفصل‌های چهارسطحی — گروه و کل و معین و تفصیلی</summary>
    [HttpGet("accounts")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetChart()
    {
        var result = await _mediator.Send(new GetChartOfAccountsQuery());
        return ToResponse(result);
    }

    /// <summary>فهرست اسناد حسابداری</summary>
    [HttpGet("entries")]
    [HasPermission("accounting.view")]
    public async Task<IActionResult> GetEntries([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetJournalEntriesQuery(page, pageSize));
        return ToResponse(result);
    }

    /// <summary>ثبت سند دستی — توازن بدهکار و بستانکار الزامی</summary>
    [HttpPost("entries")]
    [HasPermission("accounting.create")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateJournalEntryDto dto)
    {
        var result = await _mediator.Send(new CreateJournalEntryCommand(dto));
        if (result.IsSuccess)
            return CreatedResponse(result, "GetEntries", new { }, "سند حسابداری ثبت شد");
        return ToResponse(result);
    }
}
""", API)

# ═══════════ گام ۱۹: بیمه ═══════════
write("DTOs/Insurance/InsuranceDtos.cs", """
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Application.DTOs.Insurance;

/// <summary>ردیف فهرست مطالبات</summary>
public record ClaimListDto(
    long Id,
    string? ClaimNumber,
    string InsuranceName,
    string? PeriodStartShamsi,
    string? PeriodEndShamsi,
    decimal TotalAmount,
    decimal ApprovedAmount,
    decimal ReceivedAmount,
    ClaimStatus Status);

/// <summary>درخواست ثبت مطالبه دوره‌ای</summary>
public record SubmitClaimDto(
    long InsuranceCompanyId,
    string? PeriodStartShamsi,
    string? PeriodEndShamsi);

/// <summary>درخواست استعلام بیمه</summary>
public record InsuranceInquiryDto(
    long PatientId,
    long InsuranceCompanyId,
    string? InsuranceNumber);
""")

write("Features/Insurance/InsuranceFeatures.cs", """
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Insurance;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Insurance;

/// <summary>
/// فرمان ثبت مطالبه دوره‌ای بیمه — جمع اقلام بیمه‌ای دوره
/// </summary>
public record SubmitClaimCommand(SubmitClaimDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج مطالبه</summary>
public class SubmitClaimCommandValidator : AbstractValidator<SubmitClaimCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public SubmitClaimCommandValidator()
    {
        RuleFor(x => x.Dto.InsuranceCompanyId).GreaterThan(0).WithMessage("انتخاب بیمه الزامی است");
        RuleFor(x => x.Dto.PeriodStartShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi).WithMessage("تاریخ شروع شمسی نامعتبر است");
        RuleFor(x => x.Dto.PeriodEndShamsi)
            .Must(ShamsiValidationExtensions.IsValidShamsi).WithMessage("تاریخ پایان شمسی نامعتبر است");
    }
}

/// <summary>گرداننده ثبت مطالبه — جمع سهم بیمه فاکتورهای دوره</summary>
public class SubmitClaimCommandHandler : IRequestHandler<SubmitClaimCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public SubmitClaimCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت مطالبه</summary>
    public async Task<Result<long>> Handle(SubmitClaimCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var insurance = await _unitOfWork.Repository<InsuranceCompany>()
            .GetByIdAsync(dto.InsuranceCompanyId, cancellationToken);
        if (insurance is null)
            return Result<long>.Failure(Error.NotFound("بیمه", dto.InsuranceCompanyId));

        var start = PersianDateHelper.ToGregorian(dto.PeriodStartShamsi) ?? DateTime.UtcNow.AddDays(-30);
        var end = PersianDateHelper.ToGregorian(dto.PeriodEndShamsi) ?? DateTime.UtcNow;

        // جمع سهم بیمه از اقلام فاکتورهای دوره
        var total = await _unitOfWork.Repository<Invoice>()
            .QueryableNoTracking
            .Where(i => i.InsuranceCompanyId == dto.InsuranceCompanyId
                     && i.InvoiceDate >= start && i.InvoiceDate <= end
                     && i.Status != InvoiceStatus.Cancelled)
            .SumAsync(i => (decimal?)i.InsuranceDiscount, cancellationToken) ?? 0m;

        if (total <= 0)
            return Result<long>.Failure(new Error("EMPTY_PERIOD",
                "در این دوره مبلغ بیمه‌ای برای مطالبه وجود ندارد"));

        var claim = new InsuranceClaim
        {
            InsuranceCompanyId = dto.InsuranceCompanyId,
            PeriodStart = DateOnly.FromDateTime(start),
            PeriodEnd = DateOnly.FromDateTime(end),
            TotalAmount = total,
            Status = ClaimStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<InsuranceClaim>().AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(claim.Id);
    }
}

/// <summary>
/// پرس‌وجوی فهرست مطالبات
/// </summary>
public record GetClaimsQuery(long? InsuranceCompanyId = null) : IRequest<Result<List<ClaimListDto>>>;

/// <summary>گرداننده فهرست مطالبات</summary>
public class GetClaimsQueryHandler : IRequestHandler<GetClaimsQuery, Result<List<ClaimListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetClaimsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<ClaimListDto>>> Handle(GetClaimsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<InsuranceClaim> query = _unitOfWork.Repository<InsuranceClaim>()
            .QueryableNoTracking.Include(c => c.InsuranceCompany);

        if (request.InsuranceCompanyId.HasValue)
            query = query.Where(c => c.InsuranceCompanyId == request.InsuranceCompanyId.Value);

        var items = await query
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new ClaimListDto(
                c.Id, c.ClaimNumber,
                c.InsuranceCompany != null ? c.InsuranceCompany.Name : "",
                c.PeriodStart.HasValue ? PersianDateHelper.ToShamsi(c.PeriodStart.Value.ToDateTime(TimeOnly.MinValue)) : null,
                c.PeriodEnd.HasValue ? PersianDateHelper.ToShamsi(c.PeriodEnd.Value.ToDateTime(TimeOnly.MinValue)) : null,
                c.TotalAmount, c.ApprovedAmount, c.ReceivedAmount, c.Status))
            .ToListAsync(cancellationToken);

        return Result<List<ClaimListDto>>.Success(items);
    }
}

/// <summary>
/// فرمان ثبت استعلام بیمه — پاسخ پنل بیمه در گام ۲۴ به آن متصل می‌شود
/// </summary>
public record RecordInquiryCommand(InsuranceInquiryDto Dto) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت استعلام</summary>
public class RecordInquiryCommandHandler : IRequestHandler<RecordInquiryCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public RecordInquiryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت استعلام — فعلاً ثبت محلی</summary>
    public async Task<Result<long>> Handle(RecordInquiryCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var inquiry = new InsuranceInquiry
        {
            PatientId = dto.PatientId,
            InsuranceCompanyId = dto.InsuranceCompanyId,
            InquiryDate = DateTime.UtcNow,
            IsValid = false,
            Response = "استعلام برخط در گام ۲۴ به سامانه بیمه متصل می‌شود"
        };

        await _unitOfWork.Repository<InsuranceInquiry>().AddAsync(inquiry, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(inquiry.Id);
    }
}
""")

write("Controllers/InsuranceController.cs", """
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sahand.ClinicPro.API.Filters;
using Sahand.ClinicPro.Application.DTOs.Insurance;
using Sahand.ClinicPro.Application.Features.Insurance;

namespace Sahand.ClinicPro.API.Controllers;

/// <summary>
/// کنترلر بیمه — مطالبات و استعلامات
/// مسیرها: /api/v1/insurance
/// </summary>
[ApiController]
[Route("api/v1/insurance")]
[Authorize]
public class InsuranceController : ApiBaseController
{
    private readonly IMediator _mediator;

    /// <summary>سازنده با تزریق میانجی</summary>
    public InsuranceController(IMediator mediator) => _mediator = mediator;

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
}
""", API)

print("✅ گام‌های ۱۸ و ۱۹ — حسابداری + بیمه کامل شد")
