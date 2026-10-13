using System.IO;
using System.Reflection;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// 📌 اطلاعات اسمبلی و نسخه برنامه — چرخه چهاردهم (v2.0.1)
/// نسخه از Directory.Build.props (ویژگی MSBuild «Version») می‌آید و به‌صورت
/// خودکار در پانویس پنجره ورود و نوار وضعیت پنجره اصلی نمایش داده می‌شود —
/// پیش از این متن نسخه در XAML هاردکد بود و کهنه می‌شد (پنجره ورود تا چرخه ۱۴
/// هنوز «نسخه ۱.۲.۰» نشان می‌داد در حالی که برنامه ۲.۰.۰ بود).
/// ═══ چرخهٔ ۶۷ — نشان بیلد (BuildStamp) ═══
/// گزارش مالک (چندباره): «دکمه‌های میانبر/توست هنوز خراب‌اند» در حالی که
/// رفع‌ها از چرخه‌های ۴۵-۴۷ در مخزن بودند. ریشه‌یابی: بیلد محلی مالک با فایل‌های
/// کهنه/مخلوط ساخته می‌شد و کسی نمی‌توانست بفهمد «کدام بیلد» در حال اجراست —
/// نسخهٔ اسمبلی (۲.۱۱.۰) با فایل‌های XAML کهنه در یک DLL می‌نشست! اکنون علاوه بر
/// نسخه، «تاریخ و ساعت ساخت فایل EXE در ایستگاه» هم نمایش داده می‌شود: اگر پس
/// از نصب بستهٔ آپدیت، نشان بیلد همان تاریخ قبل ماند، یعنی فایل‌ها جایگزین
/// نشده‌اند یا Clean + Rebuild لازم است (تشخیص قطعی در چند ثانیه).
/// </summary>
public static class AppInfo
{
    /// <summary>شماره نسخه سه‌بخشی اسمبلی ورودی (مطابق ویژگی Version فایل ساخت).</summary>
    public static string Version =>
        (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
        .GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>متن فارسی نسخه با ارقام فارسی — برای نمایش مستقیم در XAML با x:Static.</summary>
    public static string VersionText => $"نسخه {ToPersianDigits(Version)}";

    /// <summary>
    /// مهر زمانی ساخت فایل اجرایی ایستگاه — تاریخ آخرین نوشتن EXE روی دیسک.
    /// پس از جایگزینی فایل‌های آپدیت و بیلد تازه، این مهر «همان روز نصب»
    /// می‌شود؛ ماندنش روی تاریخ قدیم = آپدیت اعمال نشده (تشخیص چرخهٔ ۶۷).
    /// </summary>
    public static string BuildStamp
    {
        get
        {
            try
            {
                var location = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;
                if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
                    return "نامشخص";
                var built = File.GetLastWriteTime(location);
                return ToPersianDigits(
                    Application.Common.Helpers.PersianDateHelper.ToShamsi(built)
                    + " " + built.ToString("HH:mm"));
            }
            catch
            {
                return "نامشخص";
            }
        }
    }

    /// <summary>متن کامل نسخه + بیلد — پانویس نوار وضعیت و تولتیپ بج داشبورد.</summary>
    public static string VersionFullText =>
        $"{VersionText} · بیلد {BuildStamp}";

    /// <summary>سال کپی‌رایت شمسی ثابت محصول.</summary>
    public static string CopyrightText => "© ۱۴۰۵ ClinicPro — درمانگاه سهند";

    /// <summary>تبدیل ارقام لاتین رشته به ارقام فارسی (۰ تا ۹).</summary>
    private static string ToPersianDigits(string value)
    {
        var digits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsAsciiDigit(chars[i]))
            {
                chars[i] = digits[chars[i] - '0'];
            }
        }

        return new string(chars);
    }
}
