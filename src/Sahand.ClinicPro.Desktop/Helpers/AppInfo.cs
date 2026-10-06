using System.Reflection;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// 📌 اطلاعات اسمبلی و نسخه برنامه — چرخه چهاردهم (v2.0.1)
/// نسخه از Directory.Build.props (ویژگی MSBuild «Version») می‌آید و به‌صورت
/// خودکار در پانویس پنجره ورود و نوار وضعیت پنجره اصلی نمایش داده می‌شود —
/// پیش از این متن نسخه در XAML هاردکد بود و کهنه می‌شد (پنجره ورود تا چرخه ۱۴
/// هنوز «نسخه ۱.۲.۰» نشان می‌داد در حالی که برنامه ۲.۰.۰ بود).
/// </summary>
public static class AppInfo
{
    /// <summary>شماره نسخه سه‌بخشی اسمبلی ورودی (مطابق ویژگی Version فایل ساخت).</summary>
    public static string Version =>
        (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
        .GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>متن فارسی نسخه با ارقام فارسی — برای نمایش مستقیم در XAML با x:Static.</summary>
    public static string VersionText => $"نسخه {ToPersianDigits(Version)}";

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
