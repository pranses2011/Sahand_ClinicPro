using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Scheduling;

/// <summary>
/// اتاق ویزیت - ظرفیت فیزیکی درمانگاه
/// </summary>
public class ExaminationRoom : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره اتاق - یکتا</summary>
    public string? RoomNumber { get; set; }

    /// <summary>نام اتاق</summary>
    public string? RoomName { get; set; }

    /// <summary>شناسه بخش</summary>
    public long? DepartmentId { get; set; }

    /// <summary>طبقه</summary>
    public int? Floor { get; set; }

    /// <summary>گنجایش</summary>
    public int Capacity { get; set; } = 1;

    /// <summary>نوع اتاق</summary>
    public RoomType RoomType { get; set; }

    /// <summary>تجهیزات داخل اتاق</summary>
    public string? Equipment { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>بخش</summary>
    public Department? Department { get; set; }

    /// <summary>نوبت‌های این اتاق</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
