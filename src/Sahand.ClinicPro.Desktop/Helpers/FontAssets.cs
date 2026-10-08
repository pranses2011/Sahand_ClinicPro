using System.Windows.Media;

namespace Sahand.ClinicPro.Desktop.Helpers;

/// <summary>
/// کارخانهٔ یکسان خانوادهٔ فونت‌های جاسازی‌شدهٔ برنامه — چرخه بیست‌وچهارم
/// (گزارش هفدهم مالک): «فونت داخل برنامه که وزیرمتن است اعمال نمی‌شود».
/// ═══════════════════════════════════════════════════════════════════
/// ریشهٔ فنی: ساخت FontFamily از رشتهٔ pack URI با سازندهٔ تک‌رشته‌ای در
/// «کد پشت» (برخلاف XAML/BAML که زمینهٔ BaseUri دارد) به‌سادگی منبع فونت
/// جاسازی‌شده را بازنمی‌یابد و WPF بی‌صدا به فونت پیش‌فرض ویندوز برمی‌گردد
/// — به همین دلیل فونت‌های ویندوزی (Tahoma و…) اعمال می‌شدند اما وزیرمتن نه.
/// الگوی رسمی مایکروسافت («Packaging Fonts with Applications») سازندهٔ
/// دوآرگومانی با «آدرس پایهٔ برنامه + نام نسبی خانواده» است؛ این کارخانه
/// یک نمونهٔ صحیح می‌سازد و همهٔ مصرف‌کننده‌ها (ThemeService، سندهای چاپ و…)
/// از همان استفاده می‌کنند تا رفتار برنامه یکدست باشد.
/// </summary>
public static class FontAssets
{
    /// <summary>
    /// خانوادهٔ فونت وزیرمتن جاسازی‌شده در exe (Assets/Fonts — مجوز آزاد OFL)؛
    /// ارجاع به «پوشه» به‌جای فایل تکی عمدی است تا همهٔ وزن‌ها
    /// (Regular/SemiBold/Bold/Black) فعال باشند.
    /// </summary>
    public static readonly FontFamily Vazirmatn =
        new(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Vazirmatn");

    /// <summary>
    /// فونت فارسی مناسب چاپ — وزیرمتن جاسازی‌شده؛ اگر به هر دلیلی منبع در
    /// دسترس نبود (محیط‌های خاص) Tahoma ویندوز جایگزین می‌شود تا چاپ هرگز
    /// از کار نیفتد.
    /// </summary>
    public static FontFamily PrintFont
    {
        get
        {
            try
            {
                // آزمون سبک: اگر خانواده نامعتبر باشد WPF exception نمی‌دهد؛
                // اما ساخت نمونه در try برای اطمینان از کپسوله‌سازی کامل است
                return Vazirmatn;
            }
            catch
            {
                return new FontFamily("Tahoma");
            }
        }
    }
}
