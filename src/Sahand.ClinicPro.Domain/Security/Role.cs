namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// نقش - دسته‌بندی دسترسی کاربران
/// </summary>
public class Role : AuditableEntity, ISoftDeletable
{
    /// <summary>نام نقش - یکتا</summary>
    public string Name { get; set; } = null!;

    /// <summary>نام نرمال‌شده برای جستجو</summary>
    public string? NormalizedName { get; set; }

    /// <summary>توضیح نقش</summary>
    public string? Description { get; set; }

    /// <summary>نقش سیستمی غیرقابل حذف</summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>کاربران این نقش</summary>
    public ICollection<UserRole> Users { get; set; } = new List<UserRole>();

    /// <summary>دسترسی‌های این نقش</summary>
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}
