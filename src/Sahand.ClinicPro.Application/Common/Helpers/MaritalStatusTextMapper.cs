namespace Sahand.ClinicPro.Application.Common.Helpers;

/// <summary>
/// نگاشتگر دوطرفهٔ وضعیت تأهل — چرخه بیست‌ویکم (گزارش پانزدهم مالک)
/// دسکتاپ نام فارسی («مجرد/متأهل/بیوه/مطلقه») رد و بدل می‌کند اما دیتابیس
/// enum byte ذخیره می‌کند؛ این کلاس پل تبدیل امن دو سمت است.
/// </summary>
public static class MaritalStatusTextMapper
{
    /// <summary>نام فارسی → مقدار enum (مطابق Domain.Enums.MaritalStatus)</summary>
    private static readonly Dictionary<string, Domain.Enums.MaritalStatus> FromFa =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["مجرد"] = Domain.Enums.MaritalStatus.Single,
            ["متأهل"] = Domain.Enums.MaritalStatus.Married,
            ["متاهل"] = Domain.Enums.MaritalStatus.Married, // املا بدون همزه هم پذیرفته شود
            ["بیوه"] = Domain.Enums.MaritalStatus.Widowed,
            ["مطلقه"] = Domain.Enums.MaritalStatus.Divorced,
        };

    /// <summary>مقدار enum → نام فارسی</summary>
    private static readonly Dictionary<Domain.Enums.MaritalStatus, string> ToFa =
        new()
        {
            [Domain.Enums.MaritalStatus.Single] = "مجرد",
            [Domain.Enums.MaritalStatus.Married] = "متأهل",
            [Domain.Enums.MaritalStatus.Widowed] = "بیوه",
            [Domain.Enums.MaritalStatus.Divorced] = "مطلقه",
        };

    /// <summary>
    /// تبدیل نام فارسی به enum — مقدار نامعتبر یا تهی → null (بدون استثنا)
    /// </summary>
    public static Domain.Enums.MaritalStatus? FromFaText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return FromFa.TryGetValue(text.Trim(), out var value) ? value : null;
    }

    /// <summary>
    /// تبدیل enum به نام فارسی — مقدار تهی یا ناشناخته → null
    /// </summary>
    public static string? ToFaText(Domain.Enums.MaritalStatus? status)
    {
        if (status is null) return null;
        return ToFa.TryGetValue(status.Value, out var text) ? text : null;
    }
}
