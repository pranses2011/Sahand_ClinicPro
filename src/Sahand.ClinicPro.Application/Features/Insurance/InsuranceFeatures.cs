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
