namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع تراکنش داروخانه
/// </summary>
public enum PharmacyTransactionType : byte
{
    /// <summary>ورود موجودی</summary>
    StockIn = 1,
    /// <summary>خروج موجودی</summary>
    StockOut = 2,
    /// <summary>برگشت</summary>
    Return = 3,
    /// <summary>اصلاح</summary>
    Adjustment = 4,
    /// <summary>تحویل نسخه</summary>
    Dispense = 5,
}
