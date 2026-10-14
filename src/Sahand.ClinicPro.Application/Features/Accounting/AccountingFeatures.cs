using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sahand.ClinicPro.Application.Common.Helpers;
using Sahand.ClinicPro.Application.DTOs.Accounting;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Entities.Accounting;
using Sahand.ClinicPro.Domain.Enums;
using Sahand.ClinicPro.Domain.Exceptions;
using Sahand.ClinicPro.Domain.Interfaces;

namespace Sahand.ClinicPro.Application.Features.Accounting;

/// <summary>
/// پرس‌وجوی درخت سرفصل‌ها چهارسطحی
/// </summary>
public record GetChartOfAccountsQuery : IRequest<Result<List<AccountNodeDto>>>;

/// <summary>گرداننده درخت سرفصل‌ها — از گروه‌های سطح یک</summary>
public class GetChartOfAccountsQueryHandler
    : IRequestHandler<GetChartOfAccountsQuery, Result<List<AccountNodeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetChartOfAccountsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو و ساخت درخت</summary>
    public async Task<Result<List<AccountNodeDto>>> Handle(GetChartOfAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _unitOfWork.Repository<AccountingAccount>()
            .QueryableNoTracking.OrderBy(a => a.AccountCode)
            .ToListAsync(cancellationToken);

        var roots = all.Where(a => a.ParentAccountId == null)
            .Select(a => BuildNode(a, all))
            .ToList();

        return Result<List<AccountNodeDto>>.Success(roots);
    }

    /// <summary>ساخت گره درخت با فرزندان</summary>
    private static AccountNodeDto BuildNode(AccountingAccount account,
        List<AccountingAccount> all)
    {
        return new AccountNodeDto(
            account.Id, account.AccountCode, account.AccountName,
            account.AccountType, account.Level, account.IsDetail,
            account.CurrentBalance,
            all.Where(c => c.ParentAccountId == account.Id)
                .Select(c => BuildNode(c, all))
                .ToList());
    }
}

/// <summary>
/// فرمان ثبت سند حسابداری دستی — با الزام قانون طلایی توازن
/// </summary>
public record CreateJournalEntryCommand(CreateJournalEntryDto Dto) : IRequest<Result<long>>;

/// <summary>اعتبارسنج سند دستی</summary>
public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    /// <summary>قواعد اعتبارسنجی</summary>
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Dto.Description).NotEmpty().WithMessage("شرح سند الزامی است");
        RuleFor(x => x.Dto.Lines).NotEmpty().WithMessage("سند باید حداقل دو سطر داشته باشد");
        RuleForEach(x => x.Dto.Lines).SetValidator(new JournalLineDtoValidator());
    }
}

/// <summary>اعتبارسنج سطر سند</summary>
public class JournalLineDtoValidator : AbstractValidator<JournalLineDto>
{
    /// <summary>قواعد اعتبارسنجی سطر</summary>
    public JournalLineDtoValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("انتخاب حساب الزامی است");
        RuleFor(x => x.Debit).GreaterThanOrEqualTo(0).WithMessage("بدهکار نمی‌تواند منفی باشد");
        RuleFor(x => x.Credit).GreaterThanOrEqualTo(0).WithMessage("بستانکار نمی‌تواند منفی باشد");
        RuleFor(x => x).Must(l => l.Debit > 0 || l.Credit > 0)
            .WithMessage("هر سطر باید بدهکار یا بستانکار داشته باشد");
    }
}

/// <summary>گرداننده ثبت سند دستی — توازن بدهکار و بستانکار الزامی است</summary>
public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public CreateJournalEntryCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای ثبت سند</summary>
    public async Task<Result<long>> Handle(CreateJournalEntryCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);

        // قانون طلایی حسابداری دوبل — سند نامتوازن هرگز ذخیره نمی‌شود
        if (totalDebit != totalCredit)
            throw new UnbalancedJournalEntryException(totalDebit, totalCredit);

        var accountIds = dto.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _unitOfWork.Repository<AccountingAccount>()
            .QueryableNoTracking
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        // همه حساب‌ها باید موجود و قابل سند شدن باشند
        if (accounts.Count != accountIds.Count)
            return Result<long>.Failure(new Error("INVALID_ACCOUNT", "یک یا چند حساب انتخابی یافت نشد"));
        if (accounts.Any(a => !a.IsDetail))
            return Result<long>.Failure(new Error("NOT_DETAIL_ACCOUNT",
                "سند فقط روی حساب‌های تفصیلی قابل ثبت است"));

        var now = DateTime.UtcNow;
        var entry = new JournalEntry
        {
            EntryDate = DateOnly.FromDateTime(now),
            EntryDateShamsi = PersianDateHelper.ToShamsi(now),
            Description = dto.Description,
            ReferenceType = dto.ReferenceType,
            ReferenceId = dto.ReferenceId,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            // ═══ چرخهٔ چهل‌وهفتم — اسناد از ابتدا «تأیید‌شده» ثبت می‌شوند ═══
            // پیشتر هر سند جدید Draft (پیش‌نویس) بود و چون هیچ گردش‌کار تأییدی
            // در برنامه وجود ندارد، همهٔ اسناد برای همیشه «پیش‌نویس» می‌ماندند و
            // نشان وضعیت در دفتر روزنامهٔ دسکتاپ گمراه‌کننده بود. گزارش‌های
            // تراز/دفتر کل/سود و زیان از ابتدا همهٔ اسناد (با هر وضعیت) را
            // می‌خوانند پس این تغییر رفتار گزارش‌ها را عوض نمی‌کند.
            Status = JournalEntryStatus.Approved
        };

        var lineNumber = 1;
        foreach (var line in dto.Lines)
        {
            entry.Lines.Add(new JournalEntryLine
            {
                LineNumber = lineNumber++,
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Description = line.Description
            });
        }

        await _unitOfWork.Repository<JournalEntry>().AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<long>.Success(entry.Id);
    }
}

/// <summary>
/// پرس‌وجوی فهرست اسناد
/// </summary>
public record GetJournalEntriesQuery(int Page = 1, int PageSize = 20) : IRequest<Result<List<JournalEntryListDto>>>;

/// <summary>گرداننده فهرست اسناد</summary>
public class GetJournalEntriesQueryHandler
    : IRequestHandler<GetJournalEntriesQuery, Result<List<JournalEntryListDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده با تزریق واحد کار</summary>
    public GetJournalEntriesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرای پرس‌وجو</summary>
    public async Task<Result<List<JournalEntryListDto>>> Handle(GetJournalEntriesQuery request,
        CancellationToken cancellationToken)
    {
        // ═══ چرخهٔ ۷۵ — ترمیم نمایش «فاکتور شماره —» در اسناد تاریخی ═══
        // گزارش مالک: «در لیست اسناد، شماره فاکتور رو نمیندازه و به جاش خط
        // تیره میندازه (مثلاً: فاکتور شماره - سعید نجفی)». اسنادِ پیش از این
        // چرخه با شمارهٔ تهی ثبت شده‌اند (ریشه در CreateInvoiceCommand شرح
        // داده شد و برای ثبت‌های تازه رفع شده)؛ برای اسناد قدیمی، شمارهٔ
        // واقعی فاکتور از ReferenceId در همان لحظهٔ نمایش بازخوانی و جایگزین
        // الگوی شکسته می‌شود — بدون بازنویسی دیتابیس.
        var brokenIds = new List<long>(); // (مستندسازی الگو — شناسه‌ها در ادامه جمع می‌شوند)

        var query = _unitOfWork.Repository<JournalEntry>()
            .QueryableNoTracking
            .OrderByDescending(e => e.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);

        // نخست شناسهٔ فاکتورهای درگیرِ اسنادِ صفحهٔ جاری (برای یک پرس‌وجوی جمعی)
        var refs = await query
            .Where(e => e.ReferenceType == Domain.Enums.JournalReferenceType.Invoice && e.ReferenceId != null)
            .Select(e => new { e.Id, e.ReferenceId, e.Description })
            .ToListAsync(cancellationToken);

        // الگوی شکسته = توضیح «فاکتور شماره …» که بعد از «شماره» فاصله و بعدش
        // خط تیره/— است (شمارهٔ تهی). این اسناد در یک پرس‌وجو شمارهٔ واقعی می‌گیرند.
        var broken = refs.Where(r =>
            !string.IsNullOrEmpty(r.Description) &&
            r.Description.Contains("فاکتور شماره", StringComparison.OrdinalIgnoreCase) &&
            (r.Description.Contains("فاکتور شماره -", StringComparison.OrdinalIgnoreCase) ||
             r.Description.Contains("فاکتور شماره —", StringComparison.OrdinalIgnoreCase) ||
             r.Description.Contains("فاکتور شماره ‑", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        // نگاشت شناسهٔ سند ← شمارهٔ فاکتور واقعی (کلید = Id سند تا در فهرست
        // نهایی مستقیماً قابل استفاده باشد)
        var invoiceNumberByEntryId = new Dictionary<long, string>();
        if (broken.Count > 0)
        {
            var invoiceIds = broken.Where(b => b.ReferenceId.HasValue)
                .Select(b => b.ReferenceId!.Value).Distinct().ToList();
            var numberById = (await _unitOfWork.Repository<Domain.Entities.Billing.Invoice>()
                    .QueryableNoTracking
                    .Where(i => invoiceIds.Contains(i.Id))
                    .Select(i => new { i.Id, i.InvoiceNumber })
                    .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.InvoiceNumber);
            foreach (var b in broken)
                if (b.ReferenceId.HasValue && numberById.TryGetValue(b.ReferenceId.Value, out var n)
                    && !string.IsNullOrWhiteSpace(n))
                    invoiceNumberByEntryId[b.Id] = n!;
        }

        var items = await _unitOfWork.Repository<JournalEntry>()
            .QueryableNoTracking
            .OrderByDescending(e => e.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new JournalEntryListDto(
                e.Id, e.EntryNumber,
                e.EntryDateShamsi ?? string.Empty,
                e.Description, e.TotalDebit, e.TotalCredit, e.Status))
            .ToListAsync(cancellationToken);

        // جایگزینی الگوی شکسته با شمارهٔ واقعی — فقط روی ردیف‌های درگیر همین صفحه؛
        // DTO رکورد جایگزین‌پذیر است (with-expression) چون ویژگی‌های record فقط-خواندنی‌اند
        var healed = new List<JournalEntryListDto>();
        foreach (var dto in items)
        {
            if (invoiceNumberByEntryId.TryGetValue(dto.Id, out var number))
            {
                healed.Add(dto with
                {
                    Description = dto.Description!
                        .Replace("فاکتور شماره -", $"فاکتور شماره {number}", StringComparison.OrdinalIgnoreCase)
                        .Replace("فاکتور شماره —", $"فاکتور شماره {number}", StringComparison.OrdinalIgnoreCase)
                        .Replace("فاکتور شماره ‑", $"فاکتور شماره {number}", StringComparison.OrdinalIgnoreCase)
                });
            }
            else
            {
                healed.Add(dto);
            }
        }

        return Result<List<JournalEntryListDto>>.Success(healed);
    }
}

/* ═══════════ گزارش‌های گام U9 — دفتر کل و تراز و سود و زیان ═══════════ */

/// <summary>ردیف دفتر کل</summary>
public record LedgerRowDto(
    string DateShamsi,
    string? EntryNumber,
    string? Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string BalanceType);

/// <summary>پرس‌وجوی دفتر کل یک حساب — تب ۲ فرم ۱۶</summary>
public record GetGeneralLedgerQuery(long AccountId, string? FromDateShamsi = null, string? ToDateShamsi = null)
    : IRequest<Result<List<LedgerRowDto>>>;

/// <summary>گرداننده دفتر کل</summary>
public class GetGeneralLedgerQueryHandler
    : IRequestHandler<GetGeneralLedgerQuery, Result<List<LedgerRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetGeneralLedgerQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — گردش حساب با مانده تراکمی</summary>
    public async Task<Result<List<LedgerRowDto>>> Handle(GetGeneralLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _unitOfWork.Repository<AccountingAccount>().QueryableNoTracking
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);
        if (account is null)
            return Result<List<LedgerRowDto>>.Failure(Error.NotFound("حساب", request.AccountId));

        var from = PersianDateHelper.ToGregorian(request.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(request.ToDateShamsi);

        var lines = await _unitOfWork.Repository<JournalEntryLine>().QueryableNoTracking
            .Where(l => l.AccountId == request.AccountId)
            .Include(l => l.Entry)
            .ToListAsync(cancellationToken);

        var ordered = lines
            .Where(l => l.Entry != null &&
                        (from is null || l.Entry.EntryDate >= DateOnly.FromDateTime(from.Value)) &&
                        (to is null || l.Entry.EntryDate <= DateOnly.FromDateTime(to.Value)))
            .OrderBy(l => l.Entry!.EntryDate)
            .ToList();

        var balance = account.OpeningBalance;
        var natureSign = account.Nature == AccountNature.Credit ? -1 : 1;
        var rows = new List<LedgerRowDto> { new(
            account.OpeningBalance == 0 ? "—" : "مانده اول دوره", null, "مانده افتتاحیه",
            0, 0, account.OpeningBalance,
            account.OpeningBalance >= 0 ? "بدهکار" : "بستانکار") };

        foreach (var line in ordered)
        {
            balance += natureSign * (line.Debit - line.Credit);
            rows.Add(new LedgerRowDto(
                PersianDateHelper.ToShamsi(line.Entry!.EntryDate.ToDateTime(TimeOnly.MinValue)),
                line.Entry.EntryNumber,
                line.Description ?? line.Entry.Description,
                line.Debit, line.Credit, balance,
                balance >= 0 ? "بدهکار" : "بستانکار"));
        }

        return Result<List<LedgerRowDto>>.Success(rows);
    }
}

/// <summary>ردیف تراز آزمایشی</summary>
public record TrialBalanceRowDto(
    long AccountId,
    string AccountCode,
    string AccountName,
    decimal OpeningBalance,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal ClosingBalance,
    string BalanceType);

/// <summary>پرس‌وجوی تراز آزمایشی — تب ۴ فرم ۱۶</summary>
public record GetTrialBalanceQuery(string? FromDateShamsi = null, string? ToDateShamsi = null)
    : IRequest<Result<List<TrialBalanceRowDto>>>;

/// <summary>گرداننده تراز</summary>
public class GetTrialBalanceQueryHandler
    : IRequestHandler<GetTrialBalanceQuery, Result<List<TrialBalanceRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetTrialBalanceQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — فقط حساب‌های تفصیلی</summary>
    public async Task<Result<List<TrialBalanceRowDto>>> Handle(GetTrialBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var from = PersianDateHelper.ToGregorian(request.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(request.ToDateShamsi);

        var accounts = await _unitOfWork.Repository<AccountingAccount>().QueryableNoTracking
            .Where(a => a.IsDetail && a.IsActive)
            .ToListAsync(cancellationToken);

        var lines = await _unitOfWork.Repository<JournalEntryLine>().QueryableNoTracking
            .Include(l => l.Entry)
            .ToListAsync(cancellationToken);

        var rows = new List<TrialBalanceRowDto>();
        foreach (var account in accounts)
        {
            var accountLines = lines.Where(l =>
                l.AccountId == account.Id &&
                l.Entry != null &&
                (from is null || l.Entry.EntryDate >= DateOnly.FromDateTime(from.Value)) &&
                (to is null || l.Entry.EntryDate <= DateOnly.FromDateTime(to.Value))).ToList();

            var debit = accountLines.Sum(l => l.Debit);
            var credit = accountLines.Sum(l => l.Credit);
            var natureSign = account.Nature == AccountNature.Credit ? -1 : 1;
            var closing = account.OpeningBalance + natureSign * (debit - credit);

            if (account.OpeningBalance == 0 && debit == 0 && credit == 0) continue;

            rows.Add(new TrialBalanceRowDto(
                account.Id, account.AccountCode, account.AccountName,
                account.OpeningBalance, debit, credit, closing,
                closing >= 0 ? "بدهکار" : "بستانکار"));
        }

        return Result<List<TrialBalanceRowDto>>.Success(rows.OrderBy(r => r.AccountCode).ToList());
    }
}

/// <summary>
/// پرس‌وجوی جزئیات کامل یک سند — چرخهٔ چهل‌وهفتم (گزارش مالک):
/// «در تب دفتر روزنامه، با دابل کلیک بر روی یک سند، جزئیات کاملش رو
/// بهمون نشون بده» — سربرگ سند + همهٔ سطرها با عنوان حساب.
/// </summary>
public record GetJournalEntryDetailQuery(long EntryId) : IRequest<Result<JournalEntryDetailDto>>;

/// <summary>ردیف سطر سند در دیالوگ جزئیات — عنوان کامل حساب با کد</summary>
public record JournalEntryLineDto(
    int LineNumber,
    string AccountCode,
    string AccountName,
    string? Description,
    decimal Debit,
    decimal Credit);

/// <summary>دی‌تی‌او جزئیات کامل سند</summary>
public record JournalEntryDetailDto(
    long Id,
    string? EntryNumber,
    string DateShamsi,
    string Description,
    decimal TotalDebit,
    decimal TotalCredit,
    int Status,
    List<JournalEntryLineDto> Lines);

/// <summary>گردانندهٔ جزئیات سند — سطرها با عنوان حساب از راه اتصال ناوبری</summary>
public class GetJournalEntryDetailQueryHandler
    : IRequestHandler<GetJournalEntryDetailQuery, Result<JournalEntryDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetJournalEntryDetailQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — سند + سطرهای مرتب با حساب</summary>
    public async Task<Result<JournalEntryDetailDto>> Handle(GetJournalEntryDetailQuery request,
        CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.Repository<JournalEntry>().QueryableNoTracking
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.EntryId, cancellationToken);
        if (entry is null)
            return Result<JournalEntryDetailDto>.Failure(Error.NotFound("سند حسابداری", request.EntryId));

        // حساب هر سطر یک‌جا واکشی می‌شود (کد + عنوان برای نمایش)
        var accountIds = entry.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _unitOfWork.Repository<AccountingAccount>().QueryableNoTracking
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var dto = new JournalEntryDetailDto(
            entry.Id,
            entry.EntryNumber,
            entry.EntryDateShamsi ?? string.Empty,
            // شرح سند نامهربان به تهی — پارامتر DTO ناتهی است (هشدار CS8604 چرخهٔ ۴۷)
            entry.Description ?? string.Empty,
            entry.TotalDebit,
            entry.TotalCredit,
            (int)entry.Status,
            entry.Lines.OrderBy(l => l.LineNumber)
                .Select(l =>
                {
                    // حساب سطر — در نبود (حذف‌شده) نشان جایگزین چاپ می‌شود
                    accounts.TryGetValue(l.AccountId, out var account);
                    return new JournalEntryLineDto(
                        l.LineNumber,
                        account?.AccountCode ?? "؟",
                        account?.AccountName ?? "حساب حذف‌شده",
                        l.Description,
                        l.Debit,
                        l.Credit);
                })
                .ToList());

        return Result<JournalEntryDetailDto>.Success(dto);
    }
}

/// <summary>ردیف سود و زیان</summary>
public record IncomeStatementRowDto(string Title, decimal Amount, bool IsHeader, bool IsTotal);

/// <summary>پرس‌وجوی صورت سود و زیان — تب ۵ فرم ۱۶</summary>
public record GetIncomeStatementQuery(string? FromDateShamsi = null, string? ToDateShamsi = null)
    : IRequest<Result<List<IncomeStatementRowDto>>>;

/// <summary>گرداننده سود و زیان</summary>
public class GetIncomeStatementQueryHandler
    : IRequestHandler<GetIncomeStatementQuery, Result<List<IncomeStatementRowDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public GetIncomeStatementQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا — درآمد منهای هزینه = سود خالص</summary>
    public async Task<Result<List<IncomeStatementRowDto>>> Handle(GetIncomeStatementQuery request,
        CancellationToken cancellationToken)
    {
        var from = PersianDateHelper.ToGregorian(request.FromDateShamsi);
        var to = PersianDateHelper.ToGregorian(request.ToDateShamsi);

        var accounts = await _unitOfWork.Repository<AccountingAccount>().QueryableNoTracking
            .Where(a => a.IsDetail && a.IsActive &&
                        (a.AccountType == AccountType.Revenue || a.AccountType == AccountType.Expense))
            .ToListAsync(cancellationToken);

        var lines = await _unitOfWork.Repository<JournalEntryLine>().QueryableNoTracking
            .Include(l => l.Entry)
            .ToListAsync(cancellationToken);

        decimal AmountOf(long accountId)
        {
            var account = accounts.First(a => a.Id == accountId);
            var accountLines = lines.Where(l =>
                l.AccountId == accountId && l.Entry != null &&
                (from is null || l.Entry.EntryDate >= DateOnly.FromDateTime(from.Value)) &&
                (to is null || l.Entry.EntryDate <= DateOnly.FromDateTime(to.Value)));
            // ═══ چرخهٔ چهل‌وهفتم (گزارش مالک) — رفع علامت معکوس ═══
            // «فکر میکنم عملیات مالی ایراد داره چون در تب سود و زیان از صفحه
            // حسابداری، مبالغ رو زیان خالص میندازه و هیچ سودی نیست».
            // ماندهٔ حسابداریِ درست با «ماهیت» حساب تعیین می‌شود:
            //   درآمد (ماهیت بستانکار، natureSign=−۱): مانده = بستانکار−بدهکار
            //     = −(Σد−Σب) ⇒ natureSign × (Σد−Σب) با علامت − ✓
            //   هزینه (ماهیت بدهکار، natureSign=+۱): مانده = بدهکار−بستانکار
            //     = +(Σد−Σب) ⇒ natureSign × (Σد−Σب) با علامت + ✓
            // فرمول پیشین «−natureSign × …» دقیقاً هر دو را وارونه می‌کرد:
            // درآمد و هزینه هر دو منفی می‌شدند و «سود خالص = Σ(−درآمد) − Σ(−هزینه)
            // = هزینه − درآمد» همیشه زیان نشان می‌داد (یا سودِ کاذب در کلینیک
            // پر هزینه). حذف علامت منفی، هر دو ردیف و جمع نهایی را اصلاح می‌کند.
            // هم‌خانواده‌ها بازبینی شدند: دفتر کل و تراز (natureSign × (د−ب))
            // از ابتدا درست‌اند — تنها صورت سود و زیان معیوب بود.
            var natureSign = account.Nature == AccountNature.Credit ? -1 : 1;
            return natureSign * accountLines.Sum(l => l.Debit - l.Credit);
        }

        var rows = new List<IncomeStatementRowDto>();
        var incomes = accounts.Where(a => a.AccountType == AccountType.Revenue).ToList();
        var expenses = accounts.Where(a => a.AccountType == AccountType.Expense).ToList();

        if (incomes.Count > 0)
        {
            rows.Add(new IncomeStatementRowDto("درآمدها", 0, true, false));
            foreach (var income in incomes)
            {
                var amount = AmountOf(income.Id);
                if (amount != 0)
                    rows.Add(new IncomeStatementRowDto($"درآمد {income.AccountName}", amount, false, false));
            }
            var totalIncome = incomes.Sum(i => AmountOf(i.Id));
            rows.Add(new IncomeStatementRowDto("جمع درآمدها", totalIncome, false, true));
        }

        if (expenses.Count > 0)
        {
            rows.Add(new IncomeStatementRowDto("هزینه‌ها", 0, true, false));
            foreach (var expense in expenses)
            {
                var amount = AmountOf(expense.Id);
                if (amount != 0)
                    rows.Add(new IncomeStatementRowDto($"هزینه {expense.AccountName}", amount, false, false));
            }
            var totalExpense = expenses.Sum(e => AmountOf(e.Id));
            rows.Add(new IncomeStatementRowDto("جمع هزینه‌ها", totalExpense, false, true));
        }

        var net = incomes.Sum(i => AmountOf(i.Id)) - expenses.Sum(e => AmountOf(e.Id));
        rows.Add(new IncomeStatementRowDto(net >= 0 ? "سود خالص" : "زیان خالص", net, false, true));

        return Result<List<IncomeStatementRowDto>>.Success(rows);
    }
}

/// <summary>درخواست ثبت حساب جدید — تب ۳ فرم ۱۶</summary>
public record CreateAccountRequest(
    string AccountCode,
    string AccountName,
    int AccountType,
    int Level,
    int Nature,
    long? ParentAccountId,
    decimal OpeningBalance);

/// <summary>فرمان ثبت حساب</summary>
public record CreateAccountCommand(CreateAccountRequest Request) : IRequest<Result<long>>;

/// <summary>گرداننده ثبت حساب</summary>
public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<long>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>سازنده</summary>
    public CreateAccountCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>اجرا</summary>
    public async Task<Result<long>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        if (string.IsNullOrWhiteSpace(dto.AccountCode))
            return Result<long>.Failure(new Error("ACCOUNT.CODE", "کد حساب الزامی است"));

        var exists = await _unitOfWork.Repository<AccountingAccount>().QueryableNoTracking
            .AnyAsync(a => a.AccountCode == dto.AccountCode, cancellationToken);
        if (exists)
            return Result<long>.Failure(new Error("ACCOUNT.DUPLICATE", "این کد حساب قبلاً ثبت شده است"));

        var account = new AccountingAccount
        {
            AccountCode = dto.AccountCode.Trim(),
            AccountName = dto.AccountName.Trim(),
            AccountType = (AccountType)dto.AccountType,
            Level = (AccountLevel)dto.Level,
            Nature = (AccountNature)dto.Nature,
            ParentAccountId = dto.ParentAccountId,
            IsDetail = dto.Level == 4,
            OpeningBalance = dto.OpeningBalance,
            CurrentBalance = dto.OpeningBalance
        };
        await _unitOfWork.Repository<AccountingAccount>().AddAsync(account, cancellationToken);
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);
        return Result<long>.Success(account.Id);
    }
}
