using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Billing;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Insurance.Fractionment;

/// <summary>پرس‌وجوی محاسبه تسویه بیمه یک فاکتور (چرخه ۱۳، درخواست ۲-۱)</summary>
public record CalculateFractionmentQuery(long InvoiceId) : IRequest<Result<FractionmentResultDto>>;

/// <summary>دستور اعمال تسویه روی اقلام فاکتور</summary>
public record ApplyFractionmentCommand(long InvoiceId) : IRequest<Result<FractionmentResultDto>>;

/// <summary>ردیف تسویه یک قلم</summary>
public record FractionmentLineDto(
    long ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal ItemTotal,
    decimal Franchise,
    decimal InsuranceShare,
    decimal PatientShare,
    string RuleType,
    string RuleSource);

/// <summary>نتیجه تسویه کامل</summary>
public record FractionmentResultDto(
    long InvoiceId,
    string InvoiceCode,
    string InsuranceName,
    string PatientName,
    string DateShamsi,
    List<FractionmentLineDto> Lines,
    decimal Subtotal,
    decimal TotalFranchise,
    decimal TotalInsuranceShare,
    decimal TotalPatientShare);

/// <summary>
/// موتور تسویه حساب بیمه — سهم بیمه/بیمار به‌ازای هر قلم طبق قرارداد:
/// ۱) قاعده اختصاصی (بیمه+خدمت) ۲) قاعده عمومی بیمه ۳) تعرفه جاری بیمه
/// ۴) درصد پوشش پیش‌فرض شرکت — با فرانشیز و سقف قلم
/// </summary>
public class FractionmentCalculator
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public FractionmentCalculator(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>محاسبه تسویه یک فاکتور — با گزینه ثبت روی اقلام</summary>
    public async Task<Result<FractionmentResultDto>> CalculateAsync(long invoiceId, bool persist)
    {
        var invoiceQuery = _unitOfWork.Repository<Invoice>().QueryableTracking
            .Include(i => i.Items.Where(it => !it.IsDeleted))
            .Include(i => i.Patient)
            .Where(i => i.Id == invoiceId && !i.IsDeleted);
        var invoice = await invoiceQuery.FirstOrDefaultAsync();
        if (invoice is null)
            return Result<FractionmentResultDto>.Failure(
                new Error("INVOICE_NOT_FOUND", $"فاکتور با شناسه {invoiceId} یافت نشد"));

        if (invoice.InsuranceCompanyId is null)
            return Result<FractionmentResultDto>.Failure(
                new Error("NO_INSURANCE", "این فاکتور بیمه‌ای نیست — بیمه‌گر ندارد"));

        var companyId = invoice.InsuranceCompanyId.Value;

        var company = await _unitOfWork.Repository<InsuranceCompany>().QueryableNoTracking
            .FirstOrDefaultAsync(c => c.Id == companyId);

        // همه قواعد فعال این بیمه — اختصاصی و عمومی
        var rules = await _unitOfWork.Repository<InsuranceFractionmentRule>().QueryableNoTracking
            .Where(r => r.InsuranceCompanyId == companyId && r.IsActive && !r.IsDeleted)
            .ToListAsync();
        var specific = rules.Where(r => r.ServiceId is not null)
            .ToDictionary(r => r.ServiceId!.Value);
        var general = rules.FirstOrDefault(r => r.ServiceId is null);

        // تعرفه جاری بیمه برای خدمات (fallback سوم)
        var tariffs = await _unitOfWork.Repository<Tariff>().QueryableNoTracking
            .Where(t => t.InsuranceCompanyId == companyId && t.IsActive && !t.IsDeleted &&
                        (t.ValidTo == null || t.ValidTo >= DateOnly.FromDateTime(DateTime.UtcNow)) &&
                        (t.ValidFrom == null || t.ValidFrom <= DateOnly.FromDateTime(DateTime.UtcNow)))
            .OrderByDescending(t => t.Year)
            .ToListAsync();
        var tariffByService = tariffs
            .GroupBy(t => t.ServiceId)
            .ToDictionary(g => g.Key, g => g.First());

        var lines = new List<FractionmentLineDto>();
        foreach (var item in invoice.Items)
        {
            var itemTotal = item.TotalPrice > 0 ? item.TotalPrice : item.Quantity * item.UnitPrice;
            string ruleSource;
            FractionmentKind kind;

            if (item.ServiceId is not null && specific.TryGetValue(item.ServiceId.Value, out var spec))
            {
                (kind, ruleSource) = ApplyRule(spec, itemTotal, out var franchise, out var insuranceShare);
                ruleSource = $"قاعده اختصاصی خدمت — {ruleSource}";
                AddLine(item, itemTotal, franchise, insuranceShare, kind, ruleSource, lines, persist);
            }
            else if (general is not null)
            {
                (kind, ruleSource) = ApplyRule(general, itemTotal, out var franchise2, out var insuranceShare2);
                ruleSource = $"قاعده عمومی بیمه — {ruleSource}";
                AddLine(item, itemTotal, franchise2, insuranceShare2, kind, ruleSource, lines, persist);
            }
            else if (item.ServiceId is not null && tariffByService.TryGetValue(item.ServiceId.Value, out var tariff))
            {
                var insuranceShare3 = Math.Round(itemTotal * tariff.InsuranceShare / 100m);
                AddLine(item, itemTotal, 0m, insuranceShare3,
                    FractionmentKind.Percent, $"تعرفه {tariff.Year}", lines, persist);
            }
            else if (company is not null)
            {
                var percent = company.DefaultCoveragePercent > 0
                    ? company.DefaultCoveragePercent
                    : (company.DefaultInsuranceShare > 0 ? company.DefaultInsuranceShare : 70m);
                var insuranceShare4 = Math.Round(itemTotal * percent / 100m);
                AddLine(item, itemTotal, 0m, insuranceShare4,
                    FractionmentKind.Percent, $"پیش‌فرض قرارداد ({percent:0.#}٪)", lines, persist);
            }
            else
            {
                var insuranceShare5 = Math.Round(itemTotal * 70m / 100m);
                AddLine(item, itemTotal, 0m, insuranceShare5,
                    FractionmentKind.Percent, "پیش‌فرض ۷۰٪", lines, persist);
            }
        }

        if (persist && lines.Count > 0)
            await _unitOfWork.SaveEntitiesAsync();

        var result = new FractionmentResultDto(
            invoice.Id,
            invoice.InvoiceNumber ?? $"INV-{invoice.Id}",
            company?.Name ?? "بیمه",
            invoice.Patient is not null
                ? $"{invoice.Patient.FirstName} {invoice.Patient.LastName}".Trim()
                : "—",
            Application.Common.Helpers.PersianDateHelper.ToShamsi(invoice.InvoiceDate),
            lines,
            lines.Sum(l => l.ItemTotal),
            lines.Sum(l => l.Franchise),
            lines.Sum(l => l.InsuranceShare),
            lines.Sum(l => l.PatientShare));

        return Result<FractionmentResultDto>.Success(result);
    }

    private enum FractionmentKind { Percent, Nominal }

    /// <summary>اعمال قاعده روی مبلغ قلم — با فرانشیز و سقف</summary>
    private static (FractionmentKind Kind, string Source) ApplyRule(
        InsuranceFractionmentRule rule, decimal itemTotal,
        out decimal franchise, out decimal insuranceShare)
    {
        franchise = Math.Min(rule.FranchiseAmount, itemTotal);
        var afterFranchise = itemTotal - franchise;

        if (rule.RuleType == FractionmentRuleType.Nominal)
        {
            // اسمی: بیمه مبلغ ثابت می‌پردازد — حداکثر تا مبلغ قلم پس از فرانشیز
            insuranceShare = Math.Min(rule.NominalAmount, afterFranchise);
            return (FractionmentKind.Nominal, "اسمی — قرارداد خدمت‌به‌خدمت");
        }

        insuranceShare = Math.Round(afterFranchise * rule.InsurancePercent / 100m);
        if (rule.PerItemCeiling > 0)
            insuranceShare = Math.Min(insuranceShare, rule.PerItemCeiling);
        insuranceShare = Math.Min(insuranceShare, afterFranchise);
        return (FractionmentKind.Percent, $"درصدی {rule.InsurancePercent:0.#}٪");
    }

    /// <summary>افزودن ردیف و در صورت persist ثبت روی قلم</summary>
    private static void AddLine(InvoiceItem item, decimal itemTotal, decimal franchise,
        decimal insuranceShare, FractionmentKind kind, string source,
        List<FractionmentLineDto> lines, bool persist)
    {
        var patientShare = itemTotal - insuranceShare;
        if (patientShare < 0) patientShare = 0m;

        if (persist)
        {
            item.InsuranceShare = insuranceShare;
            item.PatientShare = patientShare;
            item.InsurancePercent = itemTotal > 0
                ? Math.Round(insuranceShare * 100m / itemTotal, 2)
                : 0m;
        }

        lines.Add(new FractionmentLineDto(
            item.Id,
            item.Description ?? item.Service?.ServiceName ?? "خدمت",
            item.Quantity,
            item.UnitPrice,
            itemTotal,
            franchise,
            insuranceShare,
            patientShare,
            kind == FractionmentKind.Nominal ? "اسمی" : "درصدی",
            source));
    }
}

/// <summary>گرداننده محاسبه</summary>
public class CalculateFractionmentQueryHandler
    : IRequestHandler<CalculateFractionmentQuery, Result<FractionmentResultDto>>
{
    private readonly FractionmentCalculator _calculator;

    /// <summary>سازنده</summary>
    public CalculateFractionmentQueryHandler(FractionmentCalculator calculator) => _calculator = calculator;

    /// <summary>اجرا</summary>
    public Task<Result<FractionmentResultDto>> Handle(CalculateFractionmentQuery request,
        CancellationToken cancellationToken)
        => _calculator.CalculateAsync(request.InvoiceId, persist: false);
}

/// <summary>گرداننده اعمال</summary>
public class ApplyFractionmentCommandHandler
    : IRequestHandler<ApplyFractionmentCommand, Result<FractionmentResultDto>>
{
    private readonly FractionmentCalculator _calculator;

    /// <summary>سازنده</summary>
    public ApplyFractionmentCommandHandler(FractionmentCalculator calculator) => _calculator = calculator;

    /// <summary>اجرا</summary>
    public Task<Result<FractionmentResultDto>> Handle(ApplyFractionmentCommand request,
        CancellationToken cancellationToken)
        => _calculator.CalculateAsync(request.InvoiceId, persist: true);
}
