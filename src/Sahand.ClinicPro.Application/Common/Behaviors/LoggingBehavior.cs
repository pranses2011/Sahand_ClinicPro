using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار لاگ‌گذاری — شروع و پایان هر درخواست با شناسه و مدت زمان را ثبت می‌کند
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>سازنده با تزریق لاگر</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>ثبت لاگ شروع و پایان درخواست</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("▶ شروع {RequestName}", name);
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("✔ پایان {RequestName} در {ElapsedMs} میلی‌ثانیه",
                name, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "✖ خطای {RequestName} پس از {ElapsedMs} میلی‌ثانیه",
                name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
