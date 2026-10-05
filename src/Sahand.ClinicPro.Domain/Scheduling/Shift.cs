using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// شیفت کاری - الگوی زمانی کار پرسنل
/// </summary>
public class Shift : AuditableEntity, ISoftDeletable
{
    /// <summary>نام شیفت</summary>
    public string ShiftName { get; set; } = null!;

    /// <summary>کد یکتای شیفت</summary>
    public string? ShiftCode { get; set; }

    /// <summary>ساعت شروع</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>ساعت پایان</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>نوع شیفت</summary>
    public ShiftType ShiftType { get; set; }

    /// <summary>شیفت شب</summary>
    public bool IsNightShift { get; set; } = false;

    /// <summary>تحمل تأخیر (دقیقه) — ورود تا این اندازه دیر، تأخیر شمرده نمی‌شود</summary>
    public int LateGraceMinutes { get; set; } = 10;

    /// <summary>تحمل تعجیل (دقیقه) — خروج تا این اندازه زود، تعجیل شمرده نمی‌شود</summary>
    public int EarlyLeaveGraceMinutes { get; set; } = 10;

    /// <summary>حداقل فاصله دو ضربه معتبر (دقیقه) — حذف ضربه‌های تصادفی متوالی</summary>
    public int MinMinutesBetweenPunches { get; set; } = 5;

    /// <summary>استراحت کسرشونده از کارکرد (دقیقه)</summary>
    public int BreakMinutes { get; set; } = 0;

    /// <summary>شروع اضافه‌کاری دقیقه‌هایی پس از پایان شیفت — صفر یعنی بلافاصله</summary>
    public int OvertimeStartAfterMinutes { get; set; } = 15;

    /// <summary>رنگ تقویمی شیفت — هگز، مثل #42A5F5</summary>
    public string? Color { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>حضورهای این شیفت</summary>
    public ICollection<EmployeeAttendance> Attendances { get; set; } = new List<EmployeeAttendance>();

    /// <summary>تخصیص‌های این شیفت به پرسنل</summary>
    public ICollection<EmployeeShiftAssignment> Assignments { get; set; } = new List<EmployeeShiftAssignment>();

    /// <summary>طول شیفت بر حسب دقیقه — در شیفت شب از نیمه‌شب عبور می‌کند؛ شروع و پایان برابر یعنی شبانه‌روزی</summary>
    public int DurationMinutes
    {
        get
        {
            var start = StartTime.ToTimeSpan();
            var end = EndTime.ToTimeSpan();

            // شروع و پایان یکسان — شیفت شبانه‌روزی ۲۴ ساعته
            if (start == end)
                return 24 * 60;

            return end > start
                ? (int)(end - start).TotalMinutes
                : (int)(TimeSpan.FromDays(1) - start + end).TotalMinutes;
        }
    }
}
