namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// سطح حساب چهارسطحی
/// </summary>
public enum AccountLevel : byte
{
    /// <summary>گروه</summary>
    Group = 1,
    /// <summary>کل</summary>
    Ledger = 2,
    /// <summary>معین</summary>
    Subsidiary = 3,
    /// <summary>تفصیلی</summary>
    Detail = 4,
}
