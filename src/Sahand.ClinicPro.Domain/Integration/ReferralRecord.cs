using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Integration;

/// <summary>
/// ارجاع - داخلی یا بین‌مرکزی با کد ارجاع
/// </summary>
public class ReferralRecord : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه بیمار</summary>
    public long PatientId { get; set; }

    /// <summary>شناسه پزشک ارجاع‌دهنده</summary>
    public long? ReferringDoctorId { get; set; }

    /// <summary>کد ارجاع - یکتا</summary>
    public string? ReferralCode { get; set; }

    /// <summary>نوع ارجاع</summary>
    public ReferralType ReferralType { get; set; }

    /// <summary>نوع مقصد ارجاع</summary>
    public string? DestinationType { get; set; }

    /// <summary>دلیل ارجاع</summary>
    public string? Reason { get; set; }

    /// <summary>وضعیت ارجاع</summary>
    public ReferralStatus Status { get; set; }

    /// <summary>معتبر تا</summary>
    public DateOnly? ValidUntil { get; set; }

    /// <summary>بیمار</summary>
    public Patient? Patient { get; set; }
}
