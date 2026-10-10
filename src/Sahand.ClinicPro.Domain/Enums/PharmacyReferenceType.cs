namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منشأ تراکنش داروخانه
/// </summary>
public enum PharmacyReferenceType : byte
{
    /// <summary>نسخه</summary>
    Prescription = 1,
    /// <summary>سفارش خرید</summary>
    PurchaseOrder = 2,
    /// <summary>انبارگردانی</summary>
    StockTaking = 3,
    /// <summary>دستی</summary>
    Manual = 4,
    /// <summary>
    /// پذیرش — چرخهٔ سی‌ونه‌م (گزارش مالک): مصرف لوازم درمانگاه (سرنگ/سرم/
    /// گاز و…) در پذیرش خدمتی؛ ReferenceId همان شناسهٔ Reception است تا
    /// ردیابی «چه چیزی برای کدام پذیرش مصرف شد» کامل باشد.
    /// </summary>
    Reception = 5,
}
