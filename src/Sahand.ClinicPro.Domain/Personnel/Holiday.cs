using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// تقویم تعطیلات — روزهای غیرکاری رسمی و سازمانی
/// در روز تعطیل، غیبت پرسنلِ بدون پوشش آن‌چنان penalize نمی‌شود
/// </summary>
public class Holiday : AuditableEntity
{
    /// <summary>تاریخ تعطیلی (میلادی)</summary>
    public DateOnly Date { get; set; }

    /// <summary>عنوان — مثلاً «۲۲ بهمن، پیروزی انقلاب»</summary>
    public string Title { get; set; } = null!;

    /// <summary>نوع تعطیلی</summary>
    public HolidayType HolidayType { get; set; } = HolidayType.Official;

    /// <summary>تکرار سالانه — تعطیلات قمری/شمسی ثابت</summary>
    public bool IsRecurring { get; set; }

    /// <summary>ساعت شروع نیمه‌کاری برای تعطیلات سازمانی</summary>
    public TimeOnly? PartialStartTime { get; set; }

    /// <summary>ساعت پایان نیمه‌کاری</summary>
    public TimeOnly? PartialEndTime { get; set; }

    /// <summary>پرسنل پوشش‌دهنده در تعطیل (On-Call)</summary>
    public ICollection<long> OnCallEmployeeIds { get; set; } = new List<long>();
}
