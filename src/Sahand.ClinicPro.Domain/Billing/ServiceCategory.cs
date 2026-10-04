namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// دسته‌بندی خدمات - ساختار درختی
/// </summary>
public class ServiceCategory : AuditableEntity, ISoftDeletable
{
    /// <summary>نام دسته</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتا</summary>
    public string? Code { get; set; }

    /// <summary>شناسه دسته والد</summary>
    public long? ParentCategoryId { get; set; }

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>دسته والد</summary>
    public ServiceCategory? Parent { get; set; }

    /// <summary>دسته‌های فرزند</summary>
    public ICollection<ServiceCategory> Children { get; set; } = new List<ServiceCategory>();

    /// <summary>خدمات این دسته</summary>
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
