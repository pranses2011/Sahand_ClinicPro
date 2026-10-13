namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع سابقه بیمار
/// </summary>
public enum HistoryType : byte
{
    /// <summary>جراحی</summary>
    Surgical = 1,
    /// <summary>بیماری</summary>
    Medical = 2,
    /// <summary>آلرژی</summary>
    Allergy = 3,
    /// <summary>خانوادگی</summary>
    Family = 4,
    /// <summary>بستری</summary>
    Hospitalization = 5,
    /// <summary>دارویی</summary>
    Medication = 6,
    /// <summary>
    /// یادداشت سریع — چرخهٔ ۶۹ (فاز ۳ / F-17 «یادداشت سریع»): ثبت خلاصهٔ
    /// مهم بدون ساخت ویزیت کامل (تماس تلفنی، توصیهٔ تلفنی، یادآور پیگیری…)
    /// </summary>
    Note = 7,
}
