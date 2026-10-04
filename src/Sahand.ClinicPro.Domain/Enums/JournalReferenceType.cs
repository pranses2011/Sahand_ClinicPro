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
}
