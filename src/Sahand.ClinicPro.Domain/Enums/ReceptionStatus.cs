namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت پذیرش
/// </summary>
public enum ReceptionStatus : byte
{
    /// <summary>پذیرش شده</summary>
    Admitted = 1,
    /// <summary>در صف</summary>
    InQueue = 2,
    /// <summary>در حال ویزیت</summary>
    InVisit = 3,
    /// <summary>ویزیت شده</summary>
    Visited = 4,
    /// <summary>ترخیص</summary>
    Discharged = 5,
    /// <summary>لغو</summary>
    Cancelled = 6,
}
