namespace Sahand.ClinicPro.Domain.Common;

/// <summary>
/// مدل خطای استاندارد دامنه
/// </summary>
/// <param name="Code">کد خطا (مثال: PATIENT_DUPLICATE)</param>
/// <param name="Message">پیام فارسی قابل نمایش به کاربر</param>
public sealed record Error(string Code, string Message)
{
    /// <summary>خطای تهی — نبود خطا</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>ساخت خطای عدم یافتن</summary>
    public static Error NotFound(string entity, long id) =>
        new($"{entity.ToUpperInvariant()}_NOT_FOUND", $".موجودیت {entity} با شناسه {id} یافت نشد");
}
