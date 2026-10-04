namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع اتاق
/// </summary>
public enum RoomType : byte
{
    /// <summary>معاینه</summary>
    Examination = 1,
    /// <summary>عمل</summary>
    Procedure = 2,
    /// <summary>مشاوره</summary>
    Consultation = 3,
    /// <summary>سونوگرافی</summary>
    Ultrasound = 4,
    /// <summary>آزمایشگاه</summary>
    Laboratory = 5,
    /// <summary>سایر</summary>
    Other = 6,
}
