using System.Globalization;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// کمکی قالب‌بندی مبالغ — چرخه بیستم (گزارش چهاردهم مالک):
/// «تمامی مبالغ در برنامه با جداکنندهٔ ۳ رقمی باشند؛ چه در جداول و نمایش
/// و چاپ و چه در کادرهای متنی که وارد می‌کنیم» + «واحد پولی بین ریال و
/// تومان قابل تعویض باشد که این تعویض فقط جنبهٔ نمایشی دارد و ارتباطی به
/// محاسبات ندارد».
/// همهٔ نمایش‌های مبلغ برنامه از همین کلاس می‌گذرند تا:
///   • جداکنندهٔ هزارگان (۱٬۲۳۴٬۵۶۷) همه‌جا یکدست باشد
///   • واحد پول فقط هنگام «نمایش» بر اساس تنظیم کاربر عوض شود —
///     مقادیر ذخیره‌شده و محاسبات همیشه ریالی می‌مانند
/// </summary>
public static class Money
{
    /// <summary>
    /// واحد پول نمایشی فعلی — "Rial" (پیش‌فرض) یا "Toman".
    /// ThemeService هنگام اعمال تنظیمات ظاهری این فیلد را مقداردهی می‌کند.
    /// فقط خواندنی از بیرون؛ تغییر از تنظیمات → ApplyAppearance.
    /// </summary>
    public static string CurrencyUnit { get; internal set; } = "Rial";

    /// <summary>آیا واحد نمایشی تومان است؟</summary>
    public static bool IsToman => string.Equals(CurrencyUnit, "Toman", StringComparison.OrdinalIgnoreCase);

    /// <summary>متن واحد پول — «ریال» یا «تومان»</summary>
    public static string UnitLabel => IsToman ? "تومان" : "ریال";

    /// <summary>
    /// تبدیل نمایشی مقدار — ریال ذخیره‌شده → مقدار نمایشی.
    /// در حالت تومان، مبلغ بر ۱۰ تقسیم می‌شود (صرفاً برای نمایش)؛
    /// محاسبات و ذخیره‌سازی هرگز از این خروجی استفاده نمی‌کنند.
    /// </summary>
    public static decimal ToDisplay(decimal rialAmount) => IsToman ? rialAmount / 10m : rialAmount;

    /// <summary>
    /// تبدیل معکوس مقدار ورودی کاربر → ریال ذخیره‌شده.
    /// کاربر در حالت تومان مقدار را به تومان وارد می‌کند؛ ×۱۰ برای ریال.
    /// </summary>
    public static decimal FromDisplayInput(decimal displayAmount) => IsToman ? displayAmount * 10m : displayAmount;

    /// <summary>
    /// قالب‌بندی مبلغ با جداکنندهٔ ۳ رقمی و بدون اعشار — «۱٬۲۳۴٬۵۶۷».
    /// واحد پول اعمال نمی‌شود (برای ستون‌های جدول و کادرهای عددی).
    /// چرخه بیست‌وسوم (گزارش شانزدهم مالک — «تمامی اعداد بدون استثنا فارسی»):
    /// ارقام به فارسی و جداکنندهٔ هزارگان به «٬» (جداکنندهٔ عربی-فارسی) تبدیل شد.
    /// </summary>
    public static string Format(decimal rialAmount)
    {
        // جداکنندهٔ هزارگانِ خودِ چارچوب برای متن فارسی — همان جداکنندهٔ «٬» است؛
        // برای اطمینان از یکدستی، کامای لاتین پس از قالب‌بندی به «٬» تبدیل می‌شود
        var latin = ToDisplay(Math.Round(rialAmount)).ToString("#,0", CultureInfo.InvariantCulture);
        return ToPersianDigits(latin.Replace(',', '٬'));
    }

    /// <summary>قالب‌بندی مقدار قابل تهی — تهی → خط تیره</summary>
    public static string FormatOrDash(decimal? rialAmount)
        => rialAmount is null ? "—" : Format(rialAmount.Value);

    /// <summary>
    /// قالب‌بندی مبلغ همراه واحد پول نمایشی — «۱٬۲۳۴٬۵۶۷ ریال» یا «۱۲۳٬۴۵۶ تومان».
    /// برای جمع‌های پایین فاکتور، فیش حقوقی و گزارش‌ها.
    /// </summary>
    public static string FormatWithUnit(decimal rialAmount) => $"{Format(rialAmount)} {UnitLabel}";

    /// <summary>
    /// پارس ورودی کاربر — جداکننده‌ها و ارقام فارسی و فاصله را می‌پذیرد:
    /// «۱٬۲۳۴٬۵۶۷» و «1,234,567» هر دو → 1234567. تهی/نامعتبر → false.
    /// جهتِ تبدیل: خروجی «مقدار نمایشی» است (در حالت تومان همان تومان)؛
    /// برای ذخیره، با FromDisplayInput به ریال برگردانده می‌شود.
    /// </summary>
    public static bool TryParse(string? text, out decimal displayValue)
    {
        displayValue = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // ارقام فارسی/عربی → لاتین + حذف جداکننده‌ها و فاصله
        var normalized = PersianDigitsToLatin(text.Trim())
            .Replace(",", string.Empty)
            .Replace("٬", string.Empty)
            .Replace("،", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("‌", string.Empty); // نیم‌فاصله

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out displayValue);
    }

    /// <summary>تبدیل ارقام فارسی و عربی به لاتین — برای پارس ورودی مبلغ و کد</summary>
    public static string PersianDigitsToLatin(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + ch - '۰'),
                >= '٠' and <= '٩' => (char)('0' + ch - '٠'),
                _ => ch
            });
        }
        return sb.ToString();
    }

    /// <summary>
    /// تبدیل ارقام لاتین به فارسی — چرخه بیست‌وسوم (گزارش شانزدهم مالک):
    /// «تمامی اعداد داخل برنامه بدون استثنا اعداد فارسی باشند».
    /// این متد روی هر متنی بی‌خطر است: نویسه‌های غیررقمی دست‌نخورده می‌مانند.
    /// </summary>
    public static string ToPersianDigits(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                >= '0' and <= '9' => (char)('۰' + ch - '0'),
                _ => ch
            });
        }
        return sb.ToString();
    }
}
