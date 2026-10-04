namespace Sahand.ClinicPro.Application.Common.Exceptions;

/// <summary>
/// استثنای اعتبارسنجی — با دیکشنری فیلد به پیام فارسی
/// توسط ValidationBehavior پرتاب و توسط ExceptionMiddleware به 400 تبدیل می‌شود
/// </summary>
public class AppValidationException : Exception
{
    /// <summary>خطاهای اعتبارسنجی بر اساس نام فیلد</summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>ساخت استثنا از دیکشنری خطاها</summary>
    public AppValidationException(IDictionary<string, string[]> errors)
        : base("یک یا چند خطای اعتبارسنجی رخ داده است")
    {
        Errors = errors;
    }

    /// <summary>ساخت استثنا از یک فیلد و پیام</summary>
    public AppValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }
}
