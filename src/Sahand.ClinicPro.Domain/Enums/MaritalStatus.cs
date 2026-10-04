namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// وضعیت تأهل
/// </summary>
public enum MaritalStatus : byte
{
    /// <summary>مجرد</summary>
    Single = 1,
    /// <summary>متأهل</summary>
    Married = 2,
    /// <summary>بیوه</summary>
    Widowed = 3,
    /// <summary>مطلقه</summary>
    Divorced = 4,
}
