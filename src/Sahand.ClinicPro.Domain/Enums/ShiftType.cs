namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع شیفت
/// </summary>
public enum ShiftType : byte
{
    /// <summary>صبح</summary>
    Morning = 1,
    /// <summary>عصر</summary>
    Evening = 2,
    /// <summary>شب</summary>
    Night = 3,
    /// <summary>چرخشی</summary>
    Rotating = 4,
}
