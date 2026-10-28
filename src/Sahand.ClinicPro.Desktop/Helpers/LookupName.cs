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
    public static string? Normalize(string? value) => value?.Trim();

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
