namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// شدت تداخل دارویی
/// </summary>
public enum DrugInteractionSeverity : byte
{
    /// <summary>خفیف</summary>
    Minor = 1,
    /// <summary>متوسط</summary>
    Moderate = 2,
    /// <summary>شدید</summary>
    Major = 3,
    /// <summary>ممنوع مصرف</summary>
    Contraindicated = 4,
}
