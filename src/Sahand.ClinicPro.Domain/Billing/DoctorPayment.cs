using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// کارانه پزشک - تسویه دوره‌ای سهم پزشک از ویزیت‌ها
/// </summary>
public class DoctorPayment : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>شروع دوره</summary>
    public DateOnly? PeriodStart { get; set; }

    /// <summary>پایان دوره</summary>
    public DateOnly? PeriodEnd { get; set; }

    /// <summary>تعداد ویزیت‌های دوره</summary>
    public int TotalVisits { get; set; } = 0;

    /// <summary>مبلغ ناخالص</summary>
    public decimal GrossAmount { get; set; } = 0m;

    /// <summary>کسورات</summary>
    public decimal DeductionAmount { get; set; } = 0m;

    /// <summary>مبلغ خالص قابل پرداخت</summary>
    public decimal NetAmount { get; set; } = 0m;

    /// <summary>زمان پرداخت</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>وضعیت کارانه</summary>
    public DoctorPaymentStatus Status { get; set; }

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }
}
