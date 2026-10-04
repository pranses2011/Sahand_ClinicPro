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

    /// <summary>سازنده با تزریق کانتکست و لاگر</summary>
    public TransactionManager(ClinicProDbContext context, ILogger<TransactionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>آغاز تراکنش جدید اگر قبلاً باز نشده باشد</summary>
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            _logger.LogDebug("تراکنش قبلاً باز است — از بازکردن مجدد خودداری شد");
            return;
        }
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>تثبیت تراکنش باز</summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <summary>به عقب برگرداندن تراکنش باز</summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
