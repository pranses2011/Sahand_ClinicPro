using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Billing;

/// <summary>
/// هزینه - پرداخت‌های جاری درمانگاه
/// </summary>
public class Expense : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره سند هزینه - یکتا</summary>
    public string? ExpenseNumber { get; set; }

    /// <summary>دسته هزینه</summary>
    public ExpenseCategoryType Category { get; set; }

    /// <summary>تاریخ هزینه</summary>
    public DateOnly? ExpenseDate { get; set; }

    /// <summary>مبلغ به ریال</summary>
    public decimal Amount { get; set; } = 0m;

    /// <summary>توضیحات</summary>
    public string? Description { get; set; }

    /// <summary>روش پرداخت</summary>
    public PaymentMethod Method { get; set; }

    /// <summary>تصویر رسید</summary>
    public byte[]? ReceiptImage { get; set; }

    /// <summary>شناسه تأییدکننده</summary>
    public long? ApprovedBy { get; set; }
}
