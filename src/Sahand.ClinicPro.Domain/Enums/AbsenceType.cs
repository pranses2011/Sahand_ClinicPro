namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع غیبت پزشک
/// </summary>
public enum AbsenceType : byte
{
    /// <summary>مرخصی</summary>
    Vacation = 1,
    /// <summary>استعلاجی</summary>
    SickLeave = 2,
    /// <summary>کنگره</summary>
    Conference = 3,
    /// <summary>سایر</summary>
    Other = 4,
}
