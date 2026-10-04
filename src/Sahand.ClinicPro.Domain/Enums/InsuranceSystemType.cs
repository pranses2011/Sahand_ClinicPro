namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// سامانه نسخه الکترونیک بیمه
/// </summary>
public enum InsuranceSystemType : byte
{
    /// <summary>سلامت</summary>
    Salamat = 1,
    /// <summary>تأمین اجتماعی</summary>
    Tamin = 2,
    /// <summary>مسلح</summary>
    Mosallah = 3,
    /// <summary>سایر</summary>
    Other = 4,
}
