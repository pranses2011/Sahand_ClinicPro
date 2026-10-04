namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت ویزیت
/// </summary>
public enum VisitStatus : byte
{
    /// <summary>شروع شده</summary>
    Started = 1,
    /// <summary>در حال انجام</summary>
    InProgress = 2,
    /// <summary>تکمیل شده</summary>
    Completed = 3,
    /// <summary>ذخیره موقت</summary>
    Draft = 4,
}
