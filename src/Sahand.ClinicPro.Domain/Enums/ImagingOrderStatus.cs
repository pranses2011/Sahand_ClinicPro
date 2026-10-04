namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت درخواست تصویربرداری
/// </summary>
public enum ImagingOrderStatus : byte
{
    /// <summary>ثبت شده</summary>
    Ordered = 1,
    /// <summary>زمان‌بندی شده</summary>
    Scheduled = 2,
    /// <summary>انجام شده</summary>
    Performed = 3,
    /// <summary>گزارش شده</summary>
    Reported = 4,
    /// <summary>لغو</summary>
    Cancelled = 5,
}
