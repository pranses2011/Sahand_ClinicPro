namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// استعلام بیمه - پاسخ آنلاین بیمه در لحظه پذیرش
/// </summary>
public class InsuranceInquiry : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه بیمه</summary>
    public long InsuranceCompanyId { get; set; }

    /// <summary>زمان استعلام</summary>
    public DateTime? InquiryDate { get; set; }

    /// <summary>پاسخ خام API بیمه</summary>
    public string? Response { get; set; }

    /// <summary>بیمه معتبر است</summary>
    public bool IsValid { get; set; } = false;

    /// <summary>کد تأیید دریافتی</summary>
    public string? ApprovalCode { get; set; }

    /// <summary>اعتبار تا</summary>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }

    /// <summary>بیمه</summary>
    public InsuranceCompany? InsuranceCompany { get; set; }
}
