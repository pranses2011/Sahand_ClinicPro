using System;

namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// الگوی Result — برگشت موفقیت یا خطا بدون استثنا
/// </summary>
public class Result
{
    /// <summary>آیا عملیات موفق بود</summary>
    public bool IsSuccess { get; }

    /// <summary>آیا عملیات شکست خورد</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>خطای رخ‌داده در صورت شکست</summary>
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>ساخت نتیجه موفق</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>ساخت نتیجه شکست</summary>
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// الگوی Result ژنریک — همراه با داده خروجی
/// </summary>
/// <typeparam name="T">نوع داده خروجی</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, Error error, T? value) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>مقدار خروجی در صورت موفقیت</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("دسترسی به مقدار نتیجه ناموفق مجاز نیست");

    /// <summary>ساخت نتیجه موفق با مقدار</summary>
    public static Result<T> Success(T value) => new(true, Error.None, value);

    /// <summary>ساخت نتیجه شکست</summary>
    public static new Result<T> Failure(Error error) => new(false, error, default);
}
