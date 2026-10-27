namespace Sahand.ClinicPro.Desktop.ViewModels;

/// <summary>
/// نام کوتاه جنس دندان برای جعبههای ۳۴پیکسلی چارت بازطراحیشده.
/// </summary>
/// <remarks>
/// TypeName اصلی («آسیاب بزرگ اول»، «پیشین مرکزی»…) در جعبهٔ کوچک طرح
/// بریده میشد؛ این نسخهٔ کوتاه همان معنا را در یکخط نگه میدارد تا چارت WPF
/// دقیقاً مثل رندر HTML طرح دیده شود. کلاس ToothVM از پیش partial بود.
/// </remarks>
public partial class ToothVM
{
    /// <summary>برچسب کوتاه جنس دندان.</summary>
    public string TypeShort => (Number % 10) switch
    {
        1 or 2 => "پیشین",
        3 => "نیش",
        4 or 5 => "آسیاب",
        _ => "عقل"
    };
}
