namespace Sahand.ClinicPro.Application.Interfaces;

/// <summary>
/// مدیر تراکنش دیتابیس — انتزاع برای TransactionBehavior
/// </summary>
public interface ITransactionManager
{
    /// <summary>آغاز تراکنش جدید</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>تثبیت تراکنش</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>به عقب برگرداندن تراکنش</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
