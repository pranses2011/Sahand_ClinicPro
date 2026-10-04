namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع استخدام
/// </summary>
public enum EmploymentType : byte
{
    /// <summary>تمام‌وقت</summary>
    FullTime = 1,
    /// <summary>پاره‌وقت</summary>
    PartTime = 2,
    /// <summary>پیمانی</summary>
    Contractor = 3,
    /// <summary>کارآموز</summary>
    Intern = 4,
}
