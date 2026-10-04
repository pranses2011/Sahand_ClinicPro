namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// بخش سازمانی درمانگاه - ساختار درختی
/// </summary>
public class Department : AuditableEntity, ISoftDeletable
{
    /// <summary>نام بخش</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتای بخش</summary>
    public string? Code { get; set; }

    /// <summary>شناسه کارمند مدیر بخش</summary>
    public long? ManagerEmployeeId { get; set; }

    /// <summary>شناسه بخش والد</summary>
    public long? ParentDepartmentId { get; set; }

    /// <summary>طبقه</summary>
    public int? Floor { get; set; }

    /// <summary>تلفن بخش</summary>
    public string? Phone { get; set; }

    /// <summary>بخش والد</summary>
    public Department? Parent { get; set; }

    /// <summary>بخش‌های فرزند</summary>
    public ICollection<Department> Children { get; set; } = new List<Department>();

    /// <summary>کارمندان این بخش</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>اتاق‌های این بخش</summary>
    public ICollection<ExaminationRoom> Rooms { get; set; } = new List<ExaminationRoom>();
}
