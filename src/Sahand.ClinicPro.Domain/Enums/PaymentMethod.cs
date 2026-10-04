namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// روش پرداخت
/// </summary>
public enum PaymentMethod : byte
{
    /// <summary>نقدی</summary>
    Cash = 1,
    /// <summary>کارت‌خوان</summary>
    Card = 2,
    /// <summary>آنلاین</summary>
    Online = 3,
    /// <summary>چک</summary>
    Cheque = 4,
    /// <summary>انتقال بانکی</summary>
    BankTransfer = 5,
    /// <summary>بیمه</summary>
    Insurance = 6,
}
