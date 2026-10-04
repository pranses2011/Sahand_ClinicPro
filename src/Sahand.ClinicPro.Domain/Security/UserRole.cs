using System;

namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// رابطه چند‌به‌چند کاربر و نقش - کلید مرکب
/// </summary>
public class UserRole
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>شناسه نقش</summary>
    public long RoleId { get; set; }

    /// <summary>زمان تخصیص نقش</summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>شناسه کاربر تخصیص‌دهنده</summary>
    public long? AssignedBy { get; set; }

    /// <summary>کاربر مرتبط</summary>
    public User? User { get; set; }

    /// <summary>نقش مرتبط</summary>
    public Role? Role { get; set; }
}
