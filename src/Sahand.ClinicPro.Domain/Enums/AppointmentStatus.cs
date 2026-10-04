namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت نوبت
/// </summary>
public enum AppointmentStatus : byte
{
    /// <summary>رزرو شده</summary>
    Booked = 1,
    /// <summary>تأیید شده</summary>
    Confirmed = 2,
    /// <summary>حاضر</summary>
    Arrived = 3,
    /// <summary>در حال ویزیت</summary>
    InProgress = 4,
    /// <summary>تکمیل شده</summary>
    Completed = 5,
    /// <summary>لغو شده</summary>
    Cancelled = 6,
    /// <summary>عدم مراجعه</summary>
    NoShow = 7,
}
