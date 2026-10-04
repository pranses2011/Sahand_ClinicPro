namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع ویزیت
/// </summary>
public enum VisitType : byte
{
    /// <summary>اولیه</summary>
    Initial = 1,
    /// <summary>پیگیری</summary>
    FollowUp = 2,
    /// <summary>اورژانس</summary>
    Emergency = 3,
    /// <summary>مشاوره</summary>
    Consultation = 4,
}
