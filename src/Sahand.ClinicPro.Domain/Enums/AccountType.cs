namespace Sahand.ClinicPro.Domain.Enums;

/// <summary>
/// نوع حساب (طبیعت)
/// </summary>
public enum AccountType : byte
{
    /// <summary>دارایی</summary>
    Asset = 1,
    /// <summary>بدهکاری</summary>
    Liability = 2,
    /// <summary>حقوق صاحبان سهام</summary>
    Equity = 3,
    /// <summary>درآمد</summary>
    Revenue = 4,
    /// <summary>هزینه</summary>
    Expense = 5,
}
