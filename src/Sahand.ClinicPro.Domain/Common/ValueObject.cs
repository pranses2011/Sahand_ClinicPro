using System;
using System.Collections.Generic;
using System.Linq;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// کلاس پایه اشیاء ارزشی — برابری بر اساس تمام اجزاء
/// </summary>
public abstract class ValueObject
{
    /// <summary>اجزاء تشکیل‌دهنده شیء ارزشی برای مقایسه</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x is null ? 0 : x.GetHashCode())
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}
