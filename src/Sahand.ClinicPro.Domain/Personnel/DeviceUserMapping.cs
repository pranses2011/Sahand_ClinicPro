namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// نگاشت شناسه ثبت‌شده روی دستگاه (EnrollNumber) به پرسنل درمانگاه
/// هر کارت/اثر انگشت روی دستگاه یک شناسه عددی دارد که به کارمند متصل می‌شود
/// </summary>
public class DeviceUserMapping : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه دستگاه</summary>
    public long DeviceId { get; set; }

    /// <summary>شناسه ثبت‌شده روی دستگاه — عدد یا رشته بسته به برند</summary>
    public string EnrollNumber { get; set; } = null!;

    /// <summary>شناسه پرسنل درمانگاه</summary>
    public long EmployeeId { get; set; }

    /// <summary>روش تأیید هویت ترجیحی روی دستگاه</summary>
    public byte? PreferredVerifyMode { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>تاریخ آخرین تأیید تطبیق</summary>
    public DateTime? LastVerifiedAt { get; set; }

    /// <summary>دستگاه مبدأ</summary>
    public AttendanceDevice? Device { get; set; }

    /// <summary>پرسنل مقصد</summary>
    public Employee? Employee { get; set; }
}
