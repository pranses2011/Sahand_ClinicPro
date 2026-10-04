namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع شرکت بیمه
/// </summary>
public enum InsuranceType : byte
{
    /// <summary>عمومی - تأمین اجتماعی</summary>
    Public = 1,
    /// <summary>سلامت</summary>
    Salamat = 2,
    /// <summary>ارتش</summary>
    Artesh = 3,
    /// <summary>خصوصی</summary>
    Private = 4,
    /// <summary>تکمیلی</summary>
    Supplementary = 5,
}
