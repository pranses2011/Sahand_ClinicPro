namespace Sahand.ClinicPro.Domain.Exceptions;

/// <summary>
/// استثنای پایه دامنه — همه استثناهای کسب‌وکار از این کلاس ارث می‌برند
/// </summary>
public class DomainException : Exception
{
    /// <summary>کد خطا برای پایش</summary>
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
