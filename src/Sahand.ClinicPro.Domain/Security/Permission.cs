namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// دسترسی - مجوز اتمی بر اساس ماژول و عملیات
/// </summary>
public class Permission : AuditableEntity, ISoftDeletable
{
    /// <summary>نام یکتای دسترسی مثل patients.create</summary>
    public string Name { get; set; } = null!;

    /// <summary>دسته‌بندی</summary>
    public string? Category { get; set; }

    /// <summary>نام ماژول</summary>
    public string? Module { get; set; }

    /// <summary>نوع عملیات مثل view یا create</summary>
    public string? Action { get; set; }

    /// <summary>توضیح فارسی دسترسی</summary>
    public string? Description { get; set; }

    /// <summary>نقش‌های دارای این دسترسی</summary>
    public ICollection<RolePermission> Roles { get; set; } = new List<RolePermission>();
}
