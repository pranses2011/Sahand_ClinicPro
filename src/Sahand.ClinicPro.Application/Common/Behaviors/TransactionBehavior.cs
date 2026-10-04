using MediatR;
using Microsoft.Extensions.Logging;
using Sahand.ClinicPro.Application.Interfaces;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار تراکنش — Handler را در تراکنش دیتابیس می‌پیچد
/// در صورت استثنا Rollback و در صورت موفقیت Commit می‌شود
/// فقط برای Commandها فعال است (Queryها تراکنش نمی‌خواهند)
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق مدیر تراکنش</summary>
    public TransactionBehavior(ITransactionManager transactionManager,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _transactionManager = transactionManager;
        _logger = logger;
    }

    /// <summary>پیچیدن اجرای Handler در تراکنش</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Queryها (پسوند Query) بدون تراکنش اجرا می‌شوند
        var isQuery = typeof(TRequest).Name.EndsWith("Query");
        if (isQuery)
            return await next();

        await _transactionManager.BeginAsync(cancellationToken);
        try
        {
            var response = await next();
            await _transactionManager.CommitAsync(cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await _transactionManager.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "تراکنش {RequestName} با خطا به عقب برگشت", typeof(TRequest).Name);
            throw;
        }
    }
}
