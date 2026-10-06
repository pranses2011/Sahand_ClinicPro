using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// قاعده تسویه بیمه (Fractionment) — چرخه ۱۳ (درخواست ۲-۱)
/// قرارداد سهم بیمه/بیمار به‌ازای هر خدمت: درصدی یا اسمی + فرانشیز
/// اولویت: قاعده اختصاصی (بیمه+خدمت) ← قاعده عمومی بیمه ← پیش‌فرض شرکت
/// </summary>
public class InsuranceFractionmentRule : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه شرکت بیمه</summary>
    public long InsuranceCompanyId { get; set; }

    /// <summary>شناسه خدمت — تهی یعنی قاعده عمومی همه خدمات این بیمه</summary>
    public long? ServiceId { get; set; }

    /// <summary>نوع قاعده — درصدی/اسمی</summary>
    public FractionmentRuleType RuleType { get; set; } = FractionmentRuleType.Percentage;

    /// <summary>درصد سهم بیمه وقتی RuleType=Percentage</summary>
    public decimal InsurancePercent { get; set; } = 70m;

    /// <summary>مبلغ اسمی سهم بیمه وقتی RuleType=Nominal (ریال به‌ازای هر بار خدمت)</summary>
    public decimal NominalAmount { get; set; } = 0m;

    /// <summary>فرانشیز ثابت بیمار (ریال) — قبل از تقسیم کسر می‌شود</summary>
    public decimal FranchiseAmount { get; set; } = 0m;

    /// <summary>سقف پرداخت بیمه به‌ازای هر قلم (ریال) — صفر یعنی بدون سقف</summary>
    public decimal PerItemCeiling { get; set; } = 0m;

    /// <summary>یادداشت قرارداد</summary>
    public string? Notes { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>شرکت بیمه</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }

    /// <summary>خدمت — در صورت قاعده اختصاصی</summary>
    public Service? Service { get; set; }
}
