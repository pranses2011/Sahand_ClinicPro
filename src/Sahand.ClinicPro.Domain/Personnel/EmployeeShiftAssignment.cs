using Sahand.ClinicPro.Domain.Entities.Scheduling;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Personnel;

/// <summary>
/// تخصیص شیفت به پرسنل — با الگوی تکرار (ثابت/هفتگی/روزهای منتخب)
/// در هر تاریخ، جدیدترین تخصیصِ دارای اولویت بالاتر برنده است
/// </summary>
public class EmployeeShiftAssignment : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پرسنل</summary>
    public long EmployeeId { get; set; }

    /// <summary>شناسه شیفت</summary>
    public long ShiftId { get; set; }

    /// <summary>شروع اعتبار تخصیص (میلادی)</summary>
    public DateOnly FromDate { get; set; }

    /// <summary>پایان اعتبار — تهی یعنی بی‌پایان</summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>الگوی تکرار</summary>
    public ShiftAssignmentRecurrence Recurrence { get; set; } = ShiftAssignmentRecurrence.Fixed;

    /// <summary>بیت‌ماسک روزهای هفته فعال — بیت۰=شنبه تا بیت۶=جمعه (فقط برای Weekly/CustomDays)</summary>
    public int WeekDaysMask { get; set; } = 0b01111111;

    /// <summary>اولویت در تعارض تخصیص‌ها — بزرگ‌تر برنده</summary>
    public int Priority { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>یادداشت</summary>
    public string? Notes { get; set; }

    /// <summary>پرسنل</summary>
    public Employee? Employee { get; set; }

    /// <summary>شیفت</summary>
    public Shift? Shift { get; set; }

    /// <summary>آیا این تخصیص در تاریخ داده‌شده فعال است؟</summary>
    public bool IsEffectiveOn(DateOnly date)
    {
        if (!IsActive || date < FromDate || (ToDate is not null && date > ToDate))
            return false;
        return Recurrence == ShiftAssignmentRecurrence.Fixed
            || (WeekDaysMask & (1 << PersianDayOfWeek(date))) != 0;
    }

    /// <summary>روز هفته ایرانیِ تاریخ — ۰=شنبه تا ۶=جمعه</summary>
    public static int PersianDayOfWeek(DateOnly date) => ((int)date.DayOfWeek + 1) % 7;
}
