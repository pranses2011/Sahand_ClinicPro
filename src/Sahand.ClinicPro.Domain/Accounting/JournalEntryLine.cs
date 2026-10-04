namespace Sahand.ClinicPro.Domain.Entities.Accounting;

/// <summary>
/// سطر سند حسابداری - یک حساب با بدهکار یا بستانکار
/// </summary>
public class JournalEntryLine : AuditableEntity, ISoftDeletable
{
    /// <summary>شناسه سند</summary>
    public long EntryId { get; set; }

    /// <summary>شماره سطر</summary>
    public int LineNumber { get; set; }

    /// <summary>شناسه حساب</summary>
    public long AccountId { get; set; }

    /// <summary>مبلغ بدهکار</summary>
    public decimal Debit { get; set; } = 0m;

    /// <summary>مبلغ بستانکار</summary>
    public decimal Credit { get; set; } = 0m;

    /// <summary>شرح سطر</summary>
    public string? Description { get; set; }

    /// <summary>شناسه مرکز هزینه</summary>
    public long? CostCenterId { get; set; }

    /// <summary>سند والد</summary>
    public JournalEntry? Entry { get; set; }

    /// <summary>حساب این سطر</summary>
    public AccountingAccount? Account { get; set; }

    /// <summary>مرکز هزینه</summary>
    public CostCenter? CostCenter { get; set; }
}
