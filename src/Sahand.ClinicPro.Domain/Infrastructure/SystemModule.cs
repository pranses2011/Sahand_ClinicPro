namespace Sahand.ClinicPro.Domain.Entities.Infrastructure;

/// <summary>
/// ماژول سیستم - فهرست ماژول‌ها با وضعیت فعال‌سازی لایسنس
/// </summary>
public class SystemModule : AuditableEntity, ISoftDeletable
{
    /// <summary>نام ماژول</summary>
    public string Name { get; set; } = null!;

    /// <summary>کد یکتای ماژول</summary>
    public string? Code { get; set; }

    /// <summary>دسته ماژول</summary>
    public string? Category { get; set; }

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>آیکون</summary>
    public string? IconPath { get; set; }

    /// <summary>ترتیب نمایش</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>ماژول اصلی - بدون نیاز به لایسنس</summary>
    public bool IsCore { get; set; } = false;

    /// <summary>فعال‌شده با لایسنس</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>نسخه ماژول</summary>
    public string? Version { get; set; }

    /// <summary>دسترسی‌های کاربر به این ماژول</summary>
    public ICollection<UserModulePermission> UserPermissions { get; set; } = new List<UserModulePermission>();
}
