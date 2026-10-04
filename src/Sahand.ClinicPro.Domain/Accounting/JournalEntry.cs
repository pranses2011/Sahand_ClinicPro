using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Accounting;

/// <summary>
/// سند حسابداری دوبل - خلاصه تراکنش مالی
/// </summary>
public class JournalEntry : AuditableEntity, ISoftDeletable
{
    /// <summary>شماره سند - یکتا</summary>
    public string? EntryNumber { get; set; }

    /// <summary>تاریخ سند</summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>تاریخ شمسی سند</summary>
    public string? EntryDateShamsi { get; set; }

    /// <summary>شرح سند</summary>
    public string? Description { get; set; }

    /// <summary>منشأ سند - فاکتور یا پرداخت یا دستی</summary>
    public JournalReferenceType ReferenceType { get; set; }

    /// <summary>شناسه رکورد منشأ</summary>
    public long? ReferenceId { get; set; }

    /// <summary>جمع بدهکار</summary>
    public decimal TotalDebit { get; set; } = 0m;

    /// <summary>جمع بستانکار</summary>
    public decimal TotalCredit { get; set; } = 0m;

    /// <summary>وضعیت سند</summary>
    public JournalEntryStatus Status { get; set; } = Domain.Enums.JournalEntryStatus.Draft;

    /// <summary>شناسه سال مالی</summary>
    public long? FiscalYearId { get; set; }

    /// <summary>شناسه تأییدکننده</summary>
    public long? ApprovedBy { get; set; }

    /// <summary>زمان تأیید</summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>سال مالی</summary>
    public FiscalYear? FiscalYear { get; set; }

    /// <summary>سطرهای سند</summary>
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}
