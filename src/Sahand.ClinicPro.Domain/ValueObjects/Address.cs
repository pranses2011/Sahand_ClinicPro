using System.Collections.Generic;
using Sahand.ClinicPro.Domain.Common;

namespace Sahand.ClinicPro.Domain.ValueObjects;

/// <summary>
/// آدرس پستی ایران — داده حساس که با AES-256 رمزنگاری می‌شود
/// </summary>
public sealed class Address : ValueObject
{
    /// <summary>متن آدرس</summary>
    public string Street { get; }

    /// <summary>شهر</summary>
    public string? City { get; }

    /// <summary>استان</summary>
    public string? Province { get; }

    /// <summary>کد پستی ۱۰ رقمی</summary>
    public string? PostalCode { get; }

    private Address(string street, string? city, string? province, string? postalCode)
    {
        Street = street;
        City = city;
        Province = province;
        PostalCode = postalCode;
    }

    /// <summary>ساخت آدرس</summary>
    public static Address Of(string street, string? city = null, string? province = null, string? postalCode = null)
        => new(street, city, province, postalCode);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Province;
        yield return PostalCode;
    }

    public override string ToString()
        => $"{Province ?? string.Empty} {City ?? string.Empty} {Street}".Trim();
}
