using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// مطالبه بیمه - جمع ثبت‌شده برای ارسال به بیمهگر
/// </summary>
public class InsuranceClaim : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره مطالبه - یکتا</summary>
    public string? ClaimNumber { get; set; }

    /// <summary>شناسه بیمه</summary>
    public long InsuranceCompanyId { get; set; }

    /// <summary>شروع دوره مطالبه</summary>
    public DateOnly? PeriodStart { get; set; }

    /// <summary>پایان دوره مطالبه</summary>
    public DateOnly? PeriodEnd { get; set; }

    /// <summary>مبلغ کل مطالبه</summary>
    public decimal TotalAmount { get; set; } = 0m;

    /// <summary>مبلغ تأییدشده</summary>
    public decimal ApprovedAmount { get; set; } = 0m;

    /// <summary>مبلغ ردشده</summary>
    public decimal RejectedAmount { get; set; } = 0m;

    /// <summary>مبلغ دریافتی</summary>
    public decimal ReceivedAmount { get; set; } = 0m;

    /// <summary>وضعیت مطالبه</summary>
    public ClaimStatus Status { get; set; }

    /// <summary>زمان ارسال</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>بیمهگر</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }
}
