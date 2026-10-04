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
            Status = JournalEntryStatus.Draft
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

        return Result<List<JournalEntryListDto>>.Success(items);
    }
}
