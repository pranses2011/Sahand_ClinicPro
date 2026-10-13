namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// منشأ سند حسابداری
/// </summary>
public enum JournalReferenceType : byte
{
    /// <summary>فاکتور</summary>
    Invoice = 1,
    /// <summary>پرداخت</summary>
    Payment = 2,
    /// <summary>هزینه</summary>
    Expense = 3,
    /// <summary>کارانه</summary>
    DoctorPayment = 4,
    /// <summary>دستی</summary>
    Manual = 5,
    /// <summary>گردش انبار — ورود کالا یا مصرف اقلام (چرخهٔ ۶۷: هزینهٔ اقلام مصرفی
    /// از این پس در دفاتر می‌نشیند و صورت سود و زیان هزینهٔ واقعی نشان می‌دهد)</summary>
    Inventory = 6,
}
