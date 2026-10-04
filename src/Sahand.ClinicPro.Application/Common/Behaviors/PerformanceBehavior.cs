using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار عملکرد — درخواست‌های کندتر از آستانه را هشدار می‌دهد
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>آستانه هشدار کندی به میلی‌ثانیه</summary>
    private const int WarningThresholdMs = 500;

    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق لاگر</summary>
    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>سنجش مدت درخواست و هشدار کندی</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "⚠ عملکرد پایین: {RequestName} در {ElapsedMs} میلی‌ثانیه اجرا شد (آستانه {Threshold})",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds, WarningThresholdMs);
        }

        return response;
    }
}
