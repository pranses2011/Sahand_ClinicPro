using System;
using System.Linq;
using System.Text;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// نرمالسازی نام مرجع (سمت/دپارتمان) — چرخهٔ جاری (گزارش مالک:
/// «فیلد سمت رو که دستی تایپ میکنم در دیتابیس ذخیره نمیشه و همون مقدار
/// قبلی روش میمونه»).
///
/// ریشهٔ اول: فرم ویرایش، <c>PositionIndex</c> را هنگام بارگذاری پرسنل
/// مقداردهی میکرد ولی کامبوی ویرایشپذیر فقط <c>Text</c> را بایند کرده
/// بود؛ بنابراین هنگام ذخیره، اندیس کهنه بر متن تایپشده غلبه میکرد و همان
/// سمت قبلی ارسال میشد.
/// ریشهٔ دوم: ساخت مقدار تازه از طریق <c>POST lookups/positions</c> نیازمند
/// نقش Admin/Manager بود، در حالی که ویرایش پرسنل با دسترسی
/// <c>patients.create</c> هم مجاز است؛ در نتیجه کاربران عادی نمیتوانستند
/// سمت تازهای بسازند.
///
/// اکنون متن تایپشده منبع حقیقت است و نام نرمالشده همراه درخواست ذخیره
/// به سرور فرستاده میشود تا سرور خودش رکورد مرجع را بسازد.
/// </summary>
public static class LookupName
{
    /// <summary>نشانگر «بدون مقدار» که در فهرستها چاپ میشود</summary>
    public const string EmptyMarker = "—";

    /// <summary>
    /// نرمالسازی: تبدیل ارقام/حروف عربی پرتکرار به فارسی، حذف نیمفاصلهٔ
    /// کنترلی، یکدستکردن فاصلهها و کوتاهکردن دوسر رشته. با این کار
    /// «منشی»، «منشى» و « منشی  » یک مقدار محسوب میشوند.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (value is null) return null;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            // حذف نیمفاصلهٔ مجازی و کاراکترهای کنترل/جهتدهی نامرئی
            if (ch == '\u200C' || ch == '\u200D' || ch == '\u200E' ||
                ch == '\u200F' || ch == '\uFEFF' || char.IsControl(ch))
                continue;

            var c = ch switch
            {
                '\u0649' => '\u06CC', // ى → ی
                '\u064A' => '\u06CC', // ي → ی
                '\u0643' => '\u06A9', // ك → ک
                '\u0629' => '\u0647', // ة → ه
                '\u0623' => '\u0627', // أ → ا
                '\u0625' => '\u0627', // إ → ا
                '\u0622' => '\u0627', // آ → ا
                '\u0660' => '\u06F0', // ارقام عربی → فارسی
                '\u0661' => '\u06F1',
                '\u0662' => '\u06F2',
                '\u0663' => '\u06F3',
                '\u0664' => '\u06F4',
                '\u0665' => '\u06F5',
                '\u0666' => '\u06F6',
                '\u0667' => '\u06F7',
                '\u0668' => '\u06F8',
                '\u0669' => '\u06F9',
                _ => ch
            };
            sb.Append(c == '\u00A0' || c == '\u2007' || c == '\u202F' ? ' ' : c);
        }

        // فشردهسازی فاصلههای پیاپی و حذف فاصلههای ابتدا/انتها
        var parts = sb.ToString()
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : string.Join(" ", parts);
    }

    /// <summary>آیا متن، نشانگر «بدون مقدار» است؟</summary>
    public static bool IsEmptyMarker(string? normalized)
        => string.IsNullOrEmpty(normalized) || normalized == EmptyMarker;

    /// <summary>مقایسهٔ بیتفاوت به نویسههای عربی/فارسی و حالت حروف</summary>
    public static bool AreEqual(string? left, string? right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// یافتن شناسه از روی «متن تایپشده» — برخلاف نسخهٔ پیشین، اندیس انتخابی
    /// فقط زمانی استفاده میشود که متنی وجود نداشته باشد (انتخاب برنامهای).
    /// </summary>
    public static T? FindByName<T>(System.Collections.Generic.IEnumerable<T> items,
        Func<T, string?> nameSelector, string? typedText) where T : class
    {
        var name = Normalize(typedText);
        if (IsEmptyMarker(name)) return null;
        return items.FirstOrDefault(i => AreEqual(nameSelector(i), name));
    }
}
