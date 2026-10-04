using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Domain.Entities.Accounting;

/// <summary>
/// سرفصل حساب - چهارسطحی: گروه و کل و معین و تفصیلی
/// </summary>
/// <remarks>
/// قانون طلایی: در هر سند جمع بدهکار برابر جمع بستانکار است
/// سند حسابداری خودکار برای هر فاکتور و پرداخت تولید می‌شود
/// </remarks>
public class AccountingAccount : AuditableEntity, ISoftDeletable
{
    /// <summary>کد حساب - یکتا</summary>
    public string AccountCode { get; set; } = null!;

    /// <summary>نام حساب</summary>
    public string AccountName { get; set; } = null!;

    /// <summary>نوع حساب - دارایی یا بدهی یا درآمد</summary>
    public AccountType AccountType { get; set; }

    /// <summary>شناسه حساب والد</summary>
    public long? ParentAccountId { get; set; }

    /// <summary>سطح حساب</summary>
    public AccountLevel Level { get; set; }

    /// <summary>قابل سند شدن</summary>
    public bool IsDetail { get; set; } = false;

    /// <summary>طبیعت حساب</summary>
    public AccountNature Nature { get; set; }

    /// <summary>تراز افتتاحیه</summary>
    public decimal OpeningBalance { get; set; } = 0m;

    /// <summary>تراز جاری</summary>
    public decimal CurrentBalance { get; set; } = 0m;

    /// <summary>فعال</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>حساب والد</summary>
    public AccountingAccount? Parent { get; set; }

    /// <summary>حساب‌های فرزند</summary>
    public ICollection<AccountingAccount> Children { get; set; } = new List<AccountingAccount>();

    /// <summary>سطرهای سند این حساب</summary>
    public ICollection<JournalEntryLine> JournalLines { get; set; } = new List<JournalEntryLine>();
}
