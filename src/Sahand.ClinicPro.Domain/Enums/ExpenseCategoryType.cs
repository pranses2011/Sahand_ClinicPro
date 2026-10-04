namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// دسته هزینه
/// </summary>
public enum ExpenseCategoryType : byte
{
    /// <summary>خرید</summary>
    Purchase = 1,
    /// <summary>اداری</summary>
    Administrative = 2,
    /// <summary>قبوض</summary>
    Utility = 3,
    /// <summary>حقوق</summary>
    Salary = 4,
    /// <summary>اجاره</summary>
    Rent = 5,
    /// <summary>تعمیرات</summary>
    Maintenance = 6,
    /// <summary>سایر</summary>
    Other = 7,
}
