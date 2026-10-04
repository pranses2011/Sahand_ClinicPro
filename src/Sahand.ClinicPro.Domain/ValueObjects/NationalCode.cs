using System;
using Sahand.ClinicPro.Domain.Common;
using Sahand.ClinicPro.Domain.Exceptions;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// کد ملی ۱۰ رقمی ایرانی — با اعتبارسنجی الگوریتم رقم کنترلی
/// </summary>
public sealed class NationalCode : ValueObject
{
    /// <summary>مقدار کد ملی</summary>
    public string Value { get; }

    private NationalCode(string value) => Value = value;

    /// <summary>ساخت کد ملی با اعتبارسنجی کامل</summary>
    public static NationalCode Of(string value)
    {
        if (!IsValid(value))
            throw new BusinessRuleException("کد ملی نامعتبر است");
        return new NationalCode(Normalize(value));
    }

    /// <summary>اعتبارسنجی الگوریتم رقم کنترلی کد ملی</summary>
    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = Normalize(code);
        if (code.Length != 10 || !long.TryParse(code, out _)) return false;
        if (new string(code[0], 10) == code) return false; // ارقام تکراری
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (code[i] - '0') * (10 - i);
        int remainder = sum % 11;
        int check = code[9] - '0';
        return remainder < 2 ? check == remainder : check == 11 - remainder;
    }

    private static string Normalize(string code) => code.Trim();

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
