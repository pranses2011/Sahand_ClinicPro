namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع تراکنش سپاس
/// </summary>
public enum SepasTransactionType : byte
{
    /// <summary>ثبت ویزیت</summary>
    EncounterRegistration = 1,
    /// <summary>به‌روزرسانی ویزیت</summary>
    EncounterUpdate = 2,
    /// <summary>ثبت بیمار</summary>
    PatientRegistration = 3,
}
