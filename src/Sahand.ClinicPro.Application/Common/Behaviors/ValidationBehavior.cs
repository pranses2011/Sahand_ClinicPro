using FluentValidation;
using MediatR;
using Sahand.ClinicPro.Application.Common.Exceptions;

namespace Sahand.ClinicPro.Application.Common.Behaviors;

/// <summary>
/// رفتار اعتبارسنجی خودکار — همه Validatorهای ثبت‌شده برای درخواست را اجرا می‌کند
/// اعتبارسنجی در Handler انجام نمی‌شود بلکه اینجا در خط لوله است
/// </summary>
/// <typeparam name="TRequest">نوع درخواست</typeparam>
/// <typeparam name="TResponse">نوع پاسخ</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>سازنده با تزریق همه اعتبارسنج‌های نوع درخواست</summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>اجرای همه اعتبارسنج‌ها قبل از Handler</summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new AppValidationException(
                failures.GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray()));

        return await next();
    }
}
