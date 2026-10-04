namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// برنامه کاری هفتگی پزشک - مبنای تولید اسلات‌های نوبت
/// </summary>
public class DoctorSchedule : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه پزشک</summary>
    public long DoctorId { get; set; }

    /// <summary>روز هفته - 0=شنبه تا 6=جمعه</summary>
    public byte DayOfWeek { get; set; }

    /// <summary>ساعت شروع</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>ساعت پایان</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>شروع استراحت</summary>
    public TimeOnly? BreakStart { get; set; }

    /// <summary>پایان استراحت</summary>
    public TimeOnly? BreakEnd { get; set; }

    /// <summary>شناسه اتاق</summary>
    public long? RoomId { get; set; }

    /// <summary>حداکثر بیمار در این برنامه</summary>
    public int? MaxPatients { get; set; }

    /// <summary>مدت هر اسلات به دقیقه</summary>
    public int SlotDuration { get; set; } = 15;

    /// <summary>معتبر از تاریخ</summary>
    public DateOnly? ValidFrom { get; set; }

    /// <summary>معتبر تا تاریخ</summary>
    public DateOnly? ValidTo { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>پزشک</summary>
    public Doctor? Doctor { get; set; }

    /// <summary>اتاق</summary>
    public ExaminationRoom? Room { get; set; }
}
