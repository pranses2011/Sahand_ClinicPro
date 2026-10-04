using System;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// مبلغ پولی به ریال — با عملیات جمع، تفریق و ضرب و مدیریت گرد کردن
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>مقدار به ریال</summary>
    public decimal Amount { get; }

    /// <summary>واحد پول (پیش‌فرض ریال ایران)</summary>
    public string Currency { get; } = "IRR";

    private Money(decimal amount, string currency = "IRR")
    {
        Amount = decimal.Round(amount, 0);
        Currency = currency;
    }

    /// <summary>ساخت مبلغ</summary>
    public static Money Of(decimal amount) => new(amount);

    /// <summary>صفر ریال</summary>
    public static Money Zero => new(0m);

    /// <summary>جمع دو مبلغ</summary>
    public Money Add(Money other) => new(Amount + other.Amount, Currency);

    /// <summary>تفریق دو مبلغ</summary>
    public Money Subtract(Money other) => new(Amount - other.Amount, Currency);

    /// <summary>ضرب در ضریب</summary>
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>درصدی از مبلغ</summary>
    public Money Percentage(decimal percent) => new(Amount * percent / 100m, Currency);

    /// <summary>آیا مبلغ منفی است</summary>
    public bool IsNegative => Amount < 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}
