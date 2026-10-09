using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;

namespace Sahand.ClinicPro.Infrastructure.Persistence.Seeders;

/// <summary>
/// بذر سرفصل‌های حسابداری — ساختار چهارسطحی پایه (کدینگ ملی مبنا)
/// ═══ چرخه سی‌ویکم (گزارش مالک): «هیچ‌کدام از درآمدها و موارد مالی ذخیره
/// نمیشن و نمایش داده نمیشن» ═══ ریشهٔ پنهان: پیش‌تر فقط ۵ سرفصل گروهِ ریشه
/// (دارایی/بدهی/حقوق صاحبان سهام/درآمد/هزینه) بذر می‌شد و سپس اگر «هیچ» حسابی
/// نبود؛ ولی موتورهای سندسازِ خودکار (فاکتور/پرداخت) به حساب‌های تفصیلی
/// «1101 صندوق»، «1103 مطالبات بیمار» و «4101 درآمد خدمات» ارجاع می‌دهند که
/// هرگز وجود نداشتند → هر سند خودکار با هشدارِ «کدینگ حسابداری آماده نیست»
/// به‌صورت خاموش رها می‌شد و هیچ ردیفی در دفاتر/گزارشات حسابداری نمی‌نشست.
/// اکنون: (الف) الگوی «همه‌چیز هست؟ رد شو» حذف شد و به‌جایش upsert برحسب کد
/// است تا نصب‌های موجود حساب‌های تازه را هم بگیرند؛ (ب) کدینگ تفصیلی استاندارد
/// درمانگاهی (صندوق/بانک/مطالبات/بدهی بیمه/درآمدها/هزینه‌ها) بذر می‌شود و
/// اسناد خودکار فاکتور و پرداخت از این پس واقعاً تولید خواهند شد.
/// </summary>
public class AccountingAccountSeeder : IDataSeeder
{
    private readonly ClinicProDbContext _context;

    /// <summary>ترتیب اجرای بذر</summary>
    public int Order => 50;

    /// <summary>سازنده با تزریق کانتکست</summary>
    public AccountingAccountSeeder(ClinicProDbContext context) => _context = context;

    /// <summary>
    /// ایجاد/تکمیل ساختار سرفصل‌ها — ردیف‌های موجود دست‌نخورده می‌مانند؛
    /// فقط حساب‌های غایب افزوده می‌شوند (سازگار با کدینگِ شخصی‌سازی‌شدهٔ درمانگاه)
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // کدینگ استاندارد درمانگاهی — گروه‌های ریشه + حساب‌های تفصیلیِ ارجاع‌شده
        // در موتورهای سندساز خودکار:
        //   1101 صندوق (دریافت نقدی/کارتی — RecordPayment)
        //   1103 مطالبات بیمار (بدهکار فاکتور — CreateInvoice/RecordPayment)
        //   4101 درآمد خدمات درمانی (بستانکار فاکتور — CreateInvoice)
        var chart = new (string Code, string Name, AccountType Type, AccountNature Nature, bool IsDetail)[]
        {
            // ─── گروه‌های ریشه (چهارسطحی ملی) ───
            ("1", "دارایی‌ها", AccountType.Asset, AccountNature.Debit, false),
            ("11", "دارایی‌های جاری", AccountType.Asset, AccountNature.Debit, false),
            ("1101", "صندوق", AccountType.Asset, AccountNature.Debit, true),
            ("1102", "بانک", AccountType.Asset, AccountNature.Debit, true),
            ("1103", "مطالبات از بیماران", AccountType.Asset, AccountNature.Debit, true),
            ("1104", "موجودی دارو و مواد مصرفی", AccountType.Asset, AccountNature.Debit, true),

            ("2", "بدهی‌ها", AccountType.Liability, AccountNature.Credit, false),
            ("21", "بدهی‌های جاری", AccountType.Liability, AccountNature.Credit, false),
            ("2101", "بدهی به بیمه‌ها", AccountType.Liability, AccountNature.Credit, true),
            ("2102", "بدهی به پرسنل (حقوق پرداختنی)", AccountType.Liability, AccountNature.Credit, true),
            ("2103", "پیش‌دریافت از بیماران", AccountType.Liability, AccountNature.Credit, true),

            ("3", "حقوق صاحبان سهام", AccountType.Equity, AccountNature.Credit, false),

            ("4", "درآمدها", AccountType.Revenue, AccountNature.Credit, false),
            ("41", "درآمد عملیاتی", AccountType.Revenue, AccountNature.Credit, false),
            ("4101", "درآمد خدمات درمانی", AccountType.Revenue, AccountNature.Credit, true),
            ("4102", "درآمد فروش دارو", AccountType.Revenue, AccountNature.Credit, true),
            ("4103", "درآمد سایر خدمات", AccountType.Revenue, AccountNature.Credit, true),

            ("5", "هزینه‌ها", AccountType.Expense, AccountNature.Debit, false),
            ("51", "هزینه‌های عملیاتی", AccountType.Expense, AccountNature.Debit, false),
            ("5101", "هزینه حقوق و دستمزد پرسنل", AccountType.Expense, AccountNature.Debit, true),
            ("5102", "هزینه خرید دارو و مواد", AccountType.Expense, AccountNature.Debit, true),
            ("5103", "هزینه‌های عمومی درمانگاه", AccountType.Expense, AccountNature.Debit, true),
        };

        // upsert برحسب کد — حساب‌های موجود (حتی دستی/شخصی‌سازی‌شده) بازنویسی نمی‌شوند
        var existing = await _context.AccountingAccounts
            .Where(a => !a.IsDeleted)
            .Select(a => a.AccountCode)
            .ToListAsync(cancellationToken);
        var existingCodes = existing.ToHashSet();

        var addedAny = false;
        foreach (var (code, name, type, nature, isDetail) in chart)
        {
            if (existingCodes.Contains(code))
                continue; // این کد قبلاً هست — دست نمی‌زنیم

            _context.AccountingAccounts.Add(new AccountingAccount
            {
                AccountCode = code,
                AccountName = name,
                AccountType = type,
                Level = isDetail ? AccountLevel.Detail : AccountLevel.Group,
                Nature = nature,
                IsDetail = isDetail,
                IsActive = true
            });
            addedAny = true;
        }

        // ذخیرهٔ مستقل — بذرهای بعدی نباید منتظر کسی باشند که Save بزند
        // (همان درس چرخهٔ سی‌ویکم از PrintTemplateSeeder)
        if (addedAny)
            await _context.SaveChangesAsync(cancellationToken);
    }
}
