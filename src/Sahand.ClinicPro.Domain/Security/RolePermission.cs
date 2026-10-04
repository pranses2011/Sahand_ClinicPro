namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// رابطه چند‌به‌چند نقش و دسترسی - کلید مرکب
/// </summary>
public class RolePermission
{
    /// <summary>شناسه نقش</summary>
    public long RoleId { get; set; }

    /// <summary>شناسه دسترسی</summary>
    public long PermissionId { get; set; }

    /// <summary>نقش مرتبط</summary>
    public Role? Role { get; set; }

    /// <summary>دسترسی مرتبط</summary>
    public Permission? Permission { get; set; }
}
