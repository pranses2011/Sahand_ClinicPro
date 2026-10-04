namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// طبیعت حساب
/// </summary>
public enum AccountNature : byte
{
    /// <summary>بدهکار</summary>
    Debit = 1,
    /// <summary>بستانکار</summary>
    Credit = 2,
}
