namespace Sahand.ClinicPro.Domain.Entities.Security;

/// <summary>
/// دسترسی ماژولی کاربر - سطح دسترسی دقیق‌تر از نقش
/// </summary>
public class UserModulePermission : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه کاربر</summary>
    public long UserId { get; set; }

    /// <summary>شناسه ماژول</summary>
    public long ModuleId { get; set; }

    /// <summary>مجوز مشاهده</summary>
    public bool CanView { get; set; } = false;

    /// <summary>مجوز افزودن</summary>
    public bool CanAdd { get; set; } = false;

    /// <summary>مجوز ویرایش</summary>
    public bool CanEdit { get; set; } = false;

    /// <summary>مجوز حذف</summary>
    public bool CanDelete { get; set; } = false;

    /// <summary>مجوز چاپ</summary>
    public bool CanPrint { get; set; } = false;

    /// <summary>مجوز خروجی</summary>
    public bool CanExport { get; set; } = false;

    /// <summary>کاربر</summary>
    public User? User { get; set; }

    /// <summary>ماژول</summary>
    public SystemModule? Module { get; set; }
}
