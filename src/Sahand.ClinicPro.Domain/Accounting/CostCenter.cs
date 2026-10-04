namespace Sahand.ClinicPro.Domain.Entities.Accounting;

/// <summary>
/// مرکز هزینه - تخصیص هزینه‌ها به بخش‌ها
/// </summary>
public class CostCenter : AuditableEntity, ISoftDeletable
{
    /// <summary>نام مرکز هزینه</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتا</summary>
    public string? Code { get; set; }

    /// <summary>شناسه بخش مرتبط</summary>
    public long? DepartmentId { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>بخش مرتبط</summary>
    public Department? Department { get; set; }
}
