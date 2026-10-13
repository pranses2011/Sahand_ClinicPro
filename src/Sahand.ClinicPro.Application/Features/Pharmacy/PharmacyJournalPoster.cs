using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Pharmacy;

/// <summary>
/// سندساز حسابداری گردش انبار — چرخهٔ ۶۷.
/// </summary>
/// <remarks>
/// ═══ گزارش مالک (ریشهٔ ساخت این کلاس) ═══
/// «فکر میکنم عملیات مالی ایراد داره چون در تب سود و زیان از صفحه حسابداری،
/// مبلغ هزینه‌ها رو صفر مینویسه در حالی که برای خدمات درمانی اقلام مصرفی
/// استفاده میشه».
///
/// ریشه: فرمان «مصرف اقلام» (ConsumeStock) فقط تراکنش انبار (PharmacyTransaction)
/// ثبت می‌کرد و هرگز به دفاتر حسابداری نمی‌رسید ⇒ هیچ سطر هزینه‌ای در
/// JournalEntryLine نمی‌نشست ⇒ صورت سود و زیان (که از ماندهٔ حساب‌های هزینه
/// محاسبه می‌شود) همیشه هزینهٔ صفر نشان می‌داد. فرمان «ورود کالا» (StockIn)
/// هم بدون سند بود ⇒ حساب داراییِ موجودی انبار (1104) هرگز بدهکار نمی‌شد.
///
/// ═══ الگوی حسابداری (مبنای بهای تمام‌شده) ═══
///   • ورود کالا:      بدهکار 1104 موجودی انبار / بستانکار 2104 بدهی تأمین‌کننده
///                     (خرید نسیه — تأمین‌کننده مشخص) یا 1101 صندوق (خرید نقدی)؛
///   • مصرف در خدمت:   بدهکار 5102 هزینهٔ خرید دارو و مواد / بستانکار 1104
///                     موجودی انبار — به «بهای خرید» (قیمت تمام‌شده) نه قیمت فروش.
///
/// ═══ سازگاری و ایمنی ═══
///   • اگر کدینگ حسابداری آماده نباشد (نصب قدیمی بدون سیدر تازه)، سند «بی‌صدا»
///     ساخته نمی‌شود و فقط هشدار لاگ می‌شود — همان الگوی RecordPaymentCommand؛
///   • ثبت سند هرگز جریان اصلی (ذخیرهٔ مصرف/ورود) را نمی‌شکند؛ خطای سند فقط لاگ می‌شود.
/// </remarks>
public static class PharmacyJournalPoster
{
    /// <summary>کد حساب موجودی انبار — دارایی جاری</summary>
    public const string InventoryAccountCode = "1104";

    /// <summary>کد حساب صندوق — خرید نقدی از این بستانکار می‌شود</summary>
    public const string CashAccountCode = "1101";

    /// <summary>کد حساب بدهی تأمین‌کنندگان — خرید نسیه (چرخهٔ ۶۷ به کدینگ افزوده شد)</summary>
    public const string SupplierPayableCode = "2104";

    /// <summary>کد حساب هزینهٔ خرید دارو و مواد</summary>
    public const string ConsumableExpenseCode = "5102";

    /// <summary>
    /// سند «مصرف اقلام در پذیرش» — بدهکار هزینه / بستانکار موجودی انبار.
    /// بهای تمام‌شده (قیمت خرید بچ) مبنای مبلغ است؛ ارقام گردنشده نگه داشته
    /// می‌شوند تا دفاتر دقیق بمانند.
    /// </summary>
    /// <param name="unitOfWork">واحد کار جاری — سند در همان تراکنشِ ذخیرهٔ اصلی می‌نشیند</param>
    /// <param name="logger">لاگر برای هشدار بی‌کدینگی (سند اختیاری است)</param>
    /// <param name="receptionId">شناسهٔ پذیرش مصرف‌کننده — مرجع سند</param>
    /// <param name="receptionNumber">شمارهٔ نمایشی پذیرش برای شرح سند</param>
    /// <param name="cost">جمع بهای تمام‌شدهٔ اقلام مصرفی (قیمت خرید × تعداد)</param>
    /// <param name="ct">توکن لغو</param>
    public static async Task PostConsumptionAsync(IUnitOfWork unitOfWork, ILogger logger,
        long receptionId, string receptionNumber, decimal cost, CancellationToken ct)
    {
        // مبلغ صفر = سند بی‌معنا — ثبت نمی‌شود (جمع‌های صفر دفتر را شلوغ می‌کنند)
        if (cost <= 0)
            return;

        try
        {
            var expense = await FindAccountAsync(unitOfWork, ConsumableExpenseCode, ct);
            var inventory = await FindAccountAsync(unitOfWork, InventoryAccountCode, ct);
            if (expense is null || inventory is null)
            {
                logger.LogWarning("کدینگ حسابداری آماده نیست (نیاز: {Exp}=5102 و {Inv}=1104) — سند هزینهٔ مصرف پذیرش {Id} تولید نشد",
                    ConsumableExpenseCode, InventoryAccountCode, receptionId);
                return;
            }

            var entry = BuildEntry(
                $"مصرف اقلام مصرفی در پذیرش {receptionNumber}",
                JournalReferenceType.Inventory, receptionId, cost,
                expense, inventory,
                "هزینهٔ اقلام مصرفی خدمات", "کاهش موجودی انبار (مصرف)");

            await unitOfWork.Repository<JournalEntry>().AddAsync(entry, ct);
        }
        catch (Exception ex)
        {
            // خطای سند نباید جریان مصرف را برگرداند — مصرف انبار واقعیت دارد؛
            // سند حسابداری در بدترین حالت جا می‌ماند و لاگ برای پیگیری می‌ماند
            logger.LogWarning(ex, "ساخت سند حسابداری مصرف پذیرش {Id} ناموفق بود — مصرف ثبت شد ولی دفتر ناقص است", receptionId);
        }
    }

    /// <summary>
    /// سند «ورود کالا به انبار» — بدهکار موجودی انبار / بستانکار بدهی تأمین‌کننده
    /// (نسیه) یا صندوق (نقدی). مبلغ = تعداد × بهای خرید.
    /// </summary>
    /// <param name="unitOfWork">واحد کار جاری</param>
    /// <param name="logger">لاگر برای هشدار بی‌کدینگی</param>
    /// <param name="inventoryId">شناسهٔ بچ/ردیف انبار ایجادشده — مرجع سند</param>
    /// <param name="itemName">نام قلم برای شرح سند</param>
    /// <param name="quantity">تعداد ورودی</param>
    /// <param name="purchasePrice">بهای خرید واحد</param>
    /// <param name="supplierId">شناسهٔ تأمین‌کننده — تهی یعنی خرید نقدی</param>
    /// <param name="ct">توکن لغو</param>
    public static async Task PostStockInAsync(IUnitOfWork unitOfWork, ILogger logger,
        long inventoryId, string itemName, decimal quantity, decimal purchasePrice,
        long? supplierId, CancellationToken ct)
    {
        var amount = quantity * purchasePrice;
        if (amount <= 0)
            return; // ورود رایگان/بدون بهای خرید — سند بی‌معنا

        try
        {
            var inventory = await FindAccountAsync(unitOfWork, InventoryAccountCode, ct);
            // خرید نسیه ⇒ بدهی تأمین‌کننده؛ خرید نقدی/بدون تأمین‌کننده ⇒ صندوق
            var counterAccount = supplierId.HasValue
                ? await FindAccountAsync(unitOfWork, SupplierPayableCode, ct)
                : await FindAccountAsync(unitOfWork, CashAccountCode, ct);

            if (inventory is null || counterAccount is null)
            {
                logger.LogWarning(
                    "کدینگ حسابداری آماده نیست (نیاز: {InvCode}=1104 و {PayCode}=2104/1101) — سند ورود کالای بچ {BatchId} تولید نشد",
                    InventoryAccountCode, supplierId.HasValue ? SupplierPayableCode : CashAccountCode, inventoryId);
                return;
            }

            var entry = BuildEntry(
                $"ورود کالا به انبار — {itemName}",
                JournalReferenceType.Inventory, inventoryId, amount,
                inventory, counterAccount,
                "افزایش موجودی انبار", supplierId.HasValue ? "بدهی به تأمین‌کننده (نسیه)" : "خرید نقدی");

            await unitOfWork.Repository<JournalEntry>().AddAsync(entry, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ساخت سند حسابداری ورود کالای بچ {Id} ناموفق بود — ورود ثبت شد ولی دفتر ناقص است", inventoryId);
        }
    }

    /// <summary>خواندن حساب فعالِ غیرحذف‌شده با کد — بدون ردیابی (فقط برای ارجاع سطر سند)</summary>
    private static async Task<AccountingAccount?> FindAccountAsync(IUnitOfWork unitOfWork, string code, CancellationToken ct)
        => await unitOfWork.Repository<AccountingAccount>()
            .QueryableNoTracking
            .FirstOrDefaultAsync(a => a.AccountCode == code && !a.IsDeleted, ct);

    /// <summary>
    /// ساخت سند دوبلِ متوازنِ تأییدشده با دو سطر — هم‌الگوی GenerateReceiptJournalAsync
    /// (RecordPaymentCommand): تاریخ جاری، شماره سند خودکار (سرور در ذخیره می‌سازد)،
    /// وضعیت Approved تا از ابتدا در دفاتر بیاید.
    /// </summary>
    private static JournalEntry BuildEntry(string description, JournalReferenceType referenceType,
        long referenceId, decimal amount, AccountingAccount debitAccount, AccountingAccount creditAccount,
        string debitLineNote, string creditLineNote) => new()
    {
        EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
        EntryDateShamsi = PersianDateHelper.ToShamsi(DateTime.UtcNow),
        Description = description,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        TotalDebit = amount,
        TotalCredit = amount,
        Status = JournalEntryStatus.Approved,
        Lines =
        {
            new JournalEntryLine
            {
                LineNumber = 1,
                AccountId = debitAccount.Id,
                Debit = amount,
                Credit = 0,
                Description = debitLineNote
            },
            new JournalEntryLine
            {
                LineNumber = 2,
                AccountId = creditAccount.Id,
                Debit = 0,
                Credit = amount,
                Description = creditLineNote
            }
        }
    };
}
