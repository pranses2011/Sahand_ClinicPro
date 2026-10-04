namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع عملیات حسابرسی
/// </summary>
public enum AuditActionType : byte
{
    /// <summary>ایجاد</summary>
    Create = 1,
    /// <summary>ویرایش</summary>
    Update = 2,
    /// <summary>حذف</summary>
    Delete = 3,
    /// <summary>مشاهده</summary>
    View = 4,
    /// <summary>ورود</summary>
    Login = 5,
    /// <summary>خروج</summary>
    Logout = 6,
    /// <summary>چاپ</summary>
    Print = 7,
    /// <summary>خروجی</summary>
    Export = 8,
}
