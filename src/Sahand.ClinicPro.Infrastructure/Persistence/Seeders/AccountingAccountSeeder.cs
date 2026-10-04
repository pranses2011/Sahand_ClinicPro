using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر سرفصل‌های حسابداری — ساختار چهارسطحی پایه (کدینگ ملی مبنا)
/// </summary>
public class AccountingAccountSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 50;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public AccountingAccountSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>ایجاد ساختار پایه سرفصل‌ها</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.AccountingAccounts.AnyAsync(cancellationToken)) return;

        var roots = new (string Code, string Name, AccountType Type, AccountNature Nature)[]
        {
            ("1", "دارایی‌ها", AccountType.Asset, AccountNature.Debit),
            ("2", "بدهی‌ها", AccountType.Liability, AccountNature.Credit),
            ("3", "حقوق صاحبان سهام", AccountType.Equity, AccountNature.Credit),
            ("4", "درآمدها", AccountType.Revenue, AccountNature.Credit),
            ("5", "هزینه‌ها", AccountType.Expense, AccountNature.Debit),
        };

        foreach (var (code, name, type, nature) in roots)
        {
            _context.AccountingAccounts.Add(new AccountingAccount
            {
                AccountCode = code,
                AccountName = name,
                AccountType = type,
                Level = AccountLevel.Group,
                Nature = nature,
                IsDetail = false,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
