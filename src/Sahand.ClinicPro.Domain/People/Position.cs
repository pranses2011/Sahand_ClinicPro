namespace Sahand.ClinicPro.Domain.Entities.People;

/// <summary>
/// سمت شغلی - عنوان‌های سازمانی با دامنه حقوق
/// </summary>
public class Position : AuditableEntity, ISoftDeletable
{
    /// <summary>عنوان سمت</summary>
    public string Title { get; set; } = null!;

    /// <summary>کد یکتای سمت</summary>
    public string? Code { get; set; }

    /// <summary>شناسه بخش مرتبط</summary>
    public long? DepartmentId { get; set; }

    /// <summary>توضیح مسئولیت‌ها</summary>
    public string? Description { get; set; }

    /// <summary>حداقل حقوق</summary>
    public decimal MinSalary { get; set; } = 0m;

    /// <summary>حداکثر حقوق</summary>
    public decimal MaxSalary { get; set; } = 0m;

    /// <summary>بخش مرتبط</summary>
    public Department? Department { get; set; }

    /// <summary>کارمندان با این سمت</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
