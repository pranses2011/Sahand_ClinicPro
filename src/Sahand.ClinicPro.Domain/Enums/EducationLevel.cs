namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// سطح تحصیلات
/// </summary>
public enum EducationLevel : byte
{
    /// <summary>بی‌سواد</summary>
    Illiterate = 1,
    /// <summary>ابتدایی</summary>
    Elementary = 2,
    /// <summary>سیکل</summary>
    MiddleSchool = 3,
    /// <summary>دیپلم</summary>
    HighSchool = 4,
    /// <summary>کاردانی</summary>
    Associate = 5,
    /// <summary>کارشناسی</summary>
    Bachelor = 6,
    /// <summary>کارشناسی ارشد</summary>
    Master = 7,
    /// <summary>دکتری</summary>
    Phd = 8,
}
