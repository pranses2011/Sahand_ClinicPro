namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع نظرسنجی
/// </summary>
public enum SurveyType : byte
{
    /// <summary>رضایت</summary>
    Satisfaction = 1,
    /// <summary>خدمات</summary>
    Service = 2,
    /// <summary>پزشک</summary>
    Doctor = 3,
}
