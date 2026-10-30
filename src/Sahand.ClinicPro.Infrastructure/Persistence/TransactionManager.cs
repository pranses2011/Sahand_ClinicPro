using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;
using Sahand.ClinicPro.Infrastructure.Persistence;

namespace Sahand.ClinicPro.Infrastructure.Persistence;

/// <summary>
/// مدیر تراکنش — پیچیدن DbContext.Database در تراکنش برای TransactionBehavior
/// </summary>
public class TransactionManager : ITransactionManager
{
    private readonly ClinicProDbContext _context;
    private readonly ILogger<TransactionManager> _logger;
    private IDbContextTransaction? _transaction;
    private int _depth;
    private bool _rollbackOnly;

    /// <summary>سازنده با تزریق کانتکست و لاگر</summary>
    public TransactionManager(ClinicProDbContext context, ILogger<TransactionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>آغاز تراکنش ریشه یا پیوستن فرمان تو‌در‌تو به همان تراکنش.</summary>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            if (_rollbackOnly)
                throw new InvalidOperationException("تراکنش به‌دلیل شکست فرمان داخلی باید به عقب برگردد.");
            _depth++;
            _logger.LogDebug("فرمان داخلی به تراکنش جاری پیوست؛ عمق {Depth}", _depth);
            return;
        }
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _depth = 1;
        _rollbackOnly = false;
    }

    /// <summary>فقط فرمان ریشه می‌تواند تراکنش مشترک را تثبیت کند.</summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        if (_depth > 1)
        {
            _depth--;
            return;
        }
        if (_rollbackOnly)
        {
            // SQL Server با MARS از savepoint پشتیبانی نمی‌کند. شکست داخلی
            // کل واحد کار را عقب می‌برد، حتی اگر گردانندهٔ بیرونی آن را گرفته باشد.
            await RollbackAsync(CancellationToken.None);
            throw new InvalidOperationException("تراکنش به‌دلیل شکست فرمان داخلی به عقب برگشت؛ عملیات ثبت نشد.");
        }
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
        _depth = 0;
    }

    /// <summary>در شکست داخلی، تراکنش ریشه برای عقب‌گرد کامل علامت‌گذاری می‌شود.</summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        if (_depth > 1)
        {
            _depth--;
            _rollbackOnly = true;
            return;
        }
        var transaction = _transaction;
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction = null;
            _depth = 0;
            _rollbackOnly = false;
            await transaction.DisposeAsync();
        }
    }
}
