namespace Sahand.ClinicPro.Domain.Entities.Accounting;

/// <summary>
/// سال مالی - دوره حسابداری
/// </summary>
public class FiscalYear : AuditableEntity, ISoftDeletable
{
    /// <summary>سال مالی - یکتا</summary>
    public int Year { get; set; }

    /// <summary>شروع سال مالی</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>پایان سال مالی</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>بسته شده</summary>
    public bool IsClosed { get; set; } = false;

    /// <summary>سال مالی جاری</summary>
    public bool IsActive { get; set; } = false;

    /// <summary>اسناد این سال مالی</summary>
    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
