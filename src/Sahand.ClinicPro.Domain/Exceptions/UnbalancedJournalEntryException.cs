namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// نقض قانون طلایی حسابداری دوبل — در هر سند جمع بدهکار باید با جمع بستانکار برابر باشد
/// </summary>
public class UnbalancedJournalEntryException : DomainException
{
    public UnbalancedJournalEntryException(decimal totalDebit, decimal totalCredit)
        : base($"سند حسابداری نامتوازن است — جمع بدهکار {totalDebit:N0} و جمع بستانکار {totalCredit:N0} ریال", "UNBALANCED_JOURNAL")
    {
    }
}
