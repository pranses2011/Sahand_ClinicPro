namespace Sahand.ClinicPro.Desktop.Services;

/// <summary>
/// پاک‌ساز ورودی متنی — چرخه هفدهم (گزارش یازدهم مالک).
/// نویسه‌های کنترلی دوجهت (Bidi) که ویندوز در حالت‌های ترکیبی کیبورد فارسی/لاتین
/// یا کپی از برنامه‌های راست‌به‌چپ می‌تواند داخل متن تزریق کند، نامرئی‌اند اما
/// مقدار واقعی رشته را عوض می‌کنند — نتیجه: «رمز درست» روی سرور نادرست دیده
/// می‌شود و کاربر فقط «نام کاربری یا رمز عبور نادرست است» می‌بیند.
/// این کلاس آن نویسه‌ها را پیش از ارسال به سرور حذف می‌کند.
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// نویسه‌های نامرئی جهت‌دهی متن که در هیچ رمز عبور معتبری نقش ندارند:
    /// ALM (٠٦1C)، LRM/RLM (200E/200F)، قفل‌های جهت (202A-202E)،
    /// جداسازهای دوجهته (2066-2069) و BOM (FEFF).
    /// </summary>
    private static readonly HashSet<char> DirectionMarks = new()
    {
        '\u061C',                                        // Arabic Letter Mark
        '\u200E', '\u200F',                              // LRM / RLM
        '\u202A', '\u202B', '\u202C', '\u202D', '\u202E', // LRE/RLE/PDF/LRO/RLO
        '\u2066', '\u2067', '\u2068', '\u2069',          // LRI/RLI/FSI/PDI
        '\uFEFF',                                        // BOM / Zero Width No-Break Space
    };

    /// <summary>
    /// حذف نویسه‌های نامرئی جهت از ورودی — مقدار تهی/نالی به رشته تهی تبدیل می‌شود.
    /// نویسه‌های مرئی (حروف، ارقام، فاصله، نمادها) دست‌نخورده باقی می‌مانند.
    /// </summary>
    public static string StripDirectionMarks(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var hadMarks = false;
        foreach (var c in input)
        {
            if (DirectionMarks.Contains(c))
            {
                hadMarks = true;
                break;
            }
        }

        // بهینه: وقتی نویسه‌ای برای حذف نیست، همان نمونه برگردد (بدون تخصیص جدید)
        if (!hadMarks)
            return input;

        var buffer = new char[input.Length];
        var index = 0;
        foreach (var c in input)
        {
            if (!DirectionMarks.Contains(c))
                buffer[index++] = c;
        }
        return new string(buffer, 0, index);
    }
}
