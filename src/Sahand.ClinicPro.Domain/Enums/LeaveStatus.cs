namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت مرخصی
/// </summary>
public enum LeaveStatus : byte
{
    /// <summary>در انتظار</summary>
    Pending = 1,
    /// <summary>تأیید شده</summary>
    Approved = 2,
    /// <summary>رد شده</summary>
    Rejected = 3,
    /// <summary>لغو</summary>
    Cancelled = 4,
}
