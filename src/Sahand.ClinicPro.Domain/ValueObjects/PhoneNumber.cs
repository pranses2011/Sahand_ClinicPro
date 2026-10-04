using System;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// شماره موبایل ایرانی ۱۱ رقمی (09XXXXXXXXX)
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    /// <summary>مقدار شماره موبایل</summary>
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    /// <summary>ساخت شماره موبایل با اعتبارسنجی</summary>
    public static PhoneNumber Of(string value)
    {
        if (!IsValid(value))
            throw new BusinessRuleException("شماره موبایل نامعتبر است — الگوی صحیح: 09XXXXXXXXX");
        return new PhoneNumber(value.Trim());
    }

    /// <summary>اعتبارسنجی الگوی موبایل ایران</summary>
    public static bool IsValid(string? number)
    {
        if (string.IsNullOrWhiteSpace(number)) return false;
        number = number.Trim();
        return number.Length == 11 && number.StartsWith("09") && long.TryParse(number, out _);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
