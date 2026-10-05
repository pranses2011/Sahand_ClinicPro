namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نسبت خویشاوند در سابقه خانوادگی — گام R2
/// </summary>
public enum FamilyRelative : byte
{
    /// <summary>پدر</summary>
    Father = 1,
    /// <summary>مادر</summary>
    Mother = 2,
    /// <summary>برادر</summary>
    Brother = 3,
    /// <summary>خواهر</summary>
    Sister = 4,
    /// <summary>پسر</summary>
    Son = 5,
    /// <summary>دختر</summary>
    Daughter = 6,
    /// <summary>پدربزرگ</summary>
    Grandfather = 7,
    /// <summary>مادربزرگ</summary>
    Grandmother = 8,
    /// <summary>عمو/دایی</summary>
    Uncle = 9,
    /// <summary>عمه/خاله</summary>
    Aunt = 10,
    /// <summary>پسرعمو/پسرخاله و مشابه</summary>
    Cousin = 11,
    /// <summary>سایر بستگان</summary>
    Other = 12
}
